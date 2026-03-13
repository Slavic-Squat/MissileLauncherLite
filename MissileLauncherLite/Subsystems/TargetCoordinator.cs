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
            private List<IMyLargeTurretBase> _turretBlocks = new List<IMyLargeTurretBase>();
            private List<IMyTurretControlBlock> _customTurretBlocks = new List<IMyTurretControlBlock>();
            private double _lastRunTime;
            private Dictionary<long, EntityInfoExt> _targets = new Dictionary<long, EntityInfoExt>();
            private long _lockedTargetID = -1;
            private List<long> _targetsToRemove = new List<long>();

            public IReadOnlyDictionary<long, EntityInfoExt> Targets => _targets;
            public long LockedTargetID => _lockedTargetID;
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
                    string id = i.ToString("D2");
                    TargetingLaser laser = new TargetingLaser(id, true);
                    laser.OnTargetUpdated += AddTarget;
                    _targetingLasers[id] = laser;
                }

                _spottingLaser = new TargetingLaser("SPOTTER", false);
                _spottingLaser.OnTargetUpdated += AddTarget;
                _spottingLaser.OnTargetAquired += (target) =>
                {
                    AddTarget(target);
                    LockTarget(target.EntityID);
                };
                _spottingLaser.RequestUnlock += UnlockTarget;

                _turretBlocks = AllBlocks.Where(b => b is IMyLargeTurretBase).Cast<IMyLargeTurretBase>().ToList();
                _customTurretBlocks = AllBlocks.Where(b => b is IMyTurretControlBlock).Cast<IMyTurretControlBlock>().ToList();
            }

            public void Run(double time)
            {
                if (_lastRunTime == 0)
                {
                    _lastRunTime = time;
                    return;
                }
                double globalTime = SystemCoordinator.GlobalTime;

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

                foreach (var turretBlock in _turretBlocks)
                {
                    if (turretBlock.HasTarget)
                    {
                        MyDetectedEntityInfo detectedInfo = turretBlock.GetTargetedEntity();
                        EntityInfoExt info = new EntityInfoExt(detectedInfo, globalTime);
                        AddTarget(info);
                    }
                }
                foreach (var turretBlock in _customTurretBlocks)
                {
                    if (turretBlock.HasTarget)
                    {
                        MyDetectedEntityInfo detectedInfo = turretBlock.GetTargetedEntity();
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
        }
    }
}
