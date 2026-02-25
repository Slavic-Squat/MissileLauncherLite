using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class TargetCoordinator
        {
            private Dictionary<string, TargetingLaser> _targetingLasers = new Dictionary<string, TargetingLaser>();
            private TargetingLaser _spottingLaser;
            private List<IMyLargeTurretBase> _targetingBlocks = new List<IMyLargeTurretBase>();
            private double _lastRunTime;
            private Dictionary<long, EntityInfoExt> _targets = new Dictionary<long, EntityInfoExt>();
            private long _lockedTargetID = -1;
            private List<long> _targetsToRemove = new List<long>();
            private bool _searching = false;

            public IReadOnlyDictionary<long, EntityInfoExt> Targets => _targets;
            public long LockedTargetID => _lockedTargetID;
            public bool Searching => _searching;
            public bool HasLockedTarget => _lockedTargetID != -1;
            public IReadOnlyDictionary<string, TargetingLaser> TargetingLasers => _targetingLasers;

            public TargetCoordinator()
            {
                Init();
            }

            private void Init()
            {
                int numLasers = Config.Get("Targeting", "NumLasers").ToInt32(0);
                Config.Set("Targeting", "NumLasers", numLasers);
                MePb.CustomData = Config.ToString();

                for (int i = 0; i < numLasers; i++)
                {
                    string id = i.ToString().ToUpper();
                    TargetingLaser laser = new TargetingLaser(id, false, true);
                    laser.OnTargetUpdated += AddTarget;
                    _targetingLasers[id] = laser;
                }

                _spottingLaser = new TargetingLaser("SPOTTER", true, false);
                _spottingLaser.OnTargetUpdated += AddTarget;

                _targetingBlocks = AllBlocks.Where(b => b is IMyLargeTurretBase).Cast<IMyLargeTurretBase>().ToList();
            }

            public void Run(double time)
            {
                if (_lastRunTime == 0)
                {
                    _lastRunTime = time;
                    return;
                }
                double globalTime = SystemCoordinator.GlobalTime;

                if (_searching)
                {
                    _spottingLaser.FireLaser();
                    if (_spottingLaser.TargetAquired)
                    {
                        _searching = false;
                        LockTarget(_spottingLaser.Target.EntityID);
                    }
                }

                EntityInfoExt lockedTarget;
                _targets.TryGetValue(_lockedTargetID, out lockedTarget);

                if (lockedTarget.IsValid)
                {
                    _spottingLaser.SetTarget(lockedTarget);
                }

                _spottingLaser.Run(time);

                foreach (var laser in _targetingLasers.Values)
                {
                    if (lockedTarget.IsValid)
                    {
                        laser.SetTarget(lockedTarget);
                    }
                    laser.Run(time);
                }

                foreach (var targetingBlock in _targetingBlocks)
                {
                    if (targetingBlock.HasTarget)
                    {
                        MyDetectedEntityInfo detectedInfo = targetingBlock.GetTargetedEntity();
                        EntityInfoExt info = new EntityInfoExt(detectedInfo, globalTime);
                        AddTarget(info);
                    }
                }

                _targetsToRemove.Clear();
                foreach (var target in _targets.Values)
                {
                    double timeSinceLastDetection = globalTime - target.TimeRecorded;

                    if (timeSinceLastDetection > 5f)
                    {
                        _targetsToRemove.Add(target.EntityID);
                    }
                }

                foreach (var targetKey in _targetsToRemove)
                {
                    RemoveTarget(targetKey);
                }

                _lastRunTime = time;
            }

            private void AddTarget(EntityInfoExt target)
            {
                if (!target.IsValid)
                {
                    return;
                }
                var entityID = target.EntityID;
                var relationID = entityID;

                if (entityID == SystemCoordinator.SelfID)
                {
                    return;
                }

                if (!_targets.ContainsKey(entityID))
                {
                    _targets.Add(entityID, target);
                }
                else
                {
                    var original = _targets[entityID];
                    _targets[entityID] = original.Merge(target);
                }
            }

            private void RemoveTarget(long entityID)
            {
                _targets.Remove(entityID);
                if (_lockedTargetID == entityID)
                {
                    UnlockTarget();
                }
            }

            public void UnlockTarget()
            {
                _lockedTargetID = -1;
                _spottingLaser.ForgetTarget();
                foreach (var laser in _targetingLasers.Values)
                {
                    laser.ForgetTarget();
                }
            }

            public void LockTarget(long entityID)
            {
                if (_lockedTargetID != -1) return;
                _lockedTargetID = entityID;
            }

            public void StartSearch()
            {
                UnlockTarget();
                _searching = true;
            }

            public void StopSearch()
            {
                _searching = false;
            }
        }
    }
}
