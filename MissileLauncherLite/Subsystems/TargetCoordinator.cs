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
            private double _time;
            private Dictionary<long, EntityInfoExt> _targets = new Dictionary<long, EntityInfoExt>();
            private long _lockedTargetID = -1;
            private List<long> _targetsToRemove = new List<long>();
            private bool _searching = false;

            public IReadOnlyDictionary<long, EntityInfoExt> Targets => _targets;
            public IReadOnlyDictionary<string, TargetingLaser> TargetingLasers => _targetingLasers;

            public TargetCoordinator()
            {
                Init();
            }

            private void Init()
            {
                int numLasers = Config.Get("Targeting", "NumLasers").ToInt32(0);
                Config.Set("Targeting", "NumLasers", numLasers);
                for (int i = 0; i < numLasers; i++)
                {
                    string id = i.ToString().ToUpper();
                    TargetingLaser laser = new TargetingLaser(id, false, true);
                    _targetingLasers[id] = laser;
                }

                _spottingLaser = new TargetingLaser("SPOTTER", true, false);
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }
                double globalTime = SystemCoordinator.GlobalTime;

                if (_searching)
                {
                    _spottingLaser.FireLaser();
                    if (_spottingLaser.HasTarget)
                    {
                        _searching = false;
                        LockTarget(_spottingLaser.Target.EntityID);
                        AddTarget(_spottingLaser.Target);
                    }
                }

                EntityInfoExt lockedTarget;
                _targets.TryGetValue(_lockedTargetID, out lockedTarget);

                if (lockedTarget.IsValid)
                {
                    _spottingLaser.SetTarget(lockedTarget);
                }

                _spottingLaser.Run(globalTime);
                AddTarget(_spottingLaser.Target);

                foreach (var laser in _targetingLasers.Values)
                {
                    if (lockedTarget.IsValid)
                    {
                        laser.SetTarget(lockedTarget);
                    }
                    laser.Run(globalTime);
                    AddTarget(laser.Target);
                }

                _targetsToRemove.Clear();
                foreach (var targetKey in _targets.Keys)
                {
                    double timeSinceLastDetection = globalTime - _targets[targetKey].TimeRecorded;

                    if (timeSinceLastDetection > 5f)
                    {
                        _targetsToRemove.Add(targetKey);
                    }
                }

                foreach (var targetKey in _targetsToRemove)
                {
                    RemoveTarget(targetKey);
                }

                _time = time;
            }

            public void AddTarget(EntityInfoExt target)
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
