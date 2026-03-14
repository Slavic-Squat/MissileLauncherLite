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
        public class TurretCoordinator
        {
            private List<IMyLargeTurretBase> _turrets = new List<IMyLargeTurretBase>();
            private List<IMyTurretControlBlock> _customTurrets = new List<IMyTurretControlBlock>();

            private IReadOnlyDictionary<long, EntityInfoExt> _targets = new Dictionary<long, EntityInfoExt>();

            private bool _targetNeutrals = false;
            private bool _enabled = true;

            private List<string> _targetingGroups = new List<string>() { "Default" };
            private List<string> _targetingGroupDisplayNames = new List<string>();
            private int _targetingGroupIndex = 0;

            private StringBuilder _sb = new StringBuilder();
            public TurretCoordinator(IReadOnlyDictionary<long, EntityInfoExt> targets)
            {
                _targets = targets;
                Init();
            }

            private void Init()
            {
                _turrets = AllBlocks.Where(b => b is IMyLargeTurretBase).Cast<IMyLargeTurretBase>().ToList();
                _customTurrets = AllBlocks.Where(b => b is IMyTurretControlBlock).Cast<IMyTurretControlBlock>().ToList();

                if (_turrets.Count > 0)
                {
                    _turrets[0].GetTargetingGroups(_targetingGroups);
                }
                else if (_customTurrets.Count > 0)
                {
                    _customTurrets[0].GetTargetingGroups(_targetingGroups);
                }

                _targetingGroupDisplayNames = _targetingGroups.Select(s => s.ToUpper()).ToList();

                foreach (var t in _turrets)
                {
                    t.SetTargetingGroup(_targetingGroups[_targetingGroupIndex]);
                    t.Enabled = _enabled;
                    t.TargetNeutrals = _targetNeutrals;
                }

                foreach (var t in _customTurrets)
                {
                    t.SetTargetingGroup(_targetingGroups[_targetingGroupIndex]);
                    t.Enabled = _enabled;
                    t.TargetNeutrals = _targetNeutrals;
                }
            }

            public void GetTurretTargets(List<EntityInfoExt> turretTargets)
            {
                turretTargets.Clear();
                double globalTime = SystemCoordinator.GlobalTime;
                foreach (var t in _turrets)
                {
                    var entity = t.GetTargetedEntity();
                    if (!entity.IsEmpty())
                    {
                        EntityInfoExt target = new EntityInfoExt(entity, globalTime);
                        turretTargets.Add(target);
                    }
                }
                foreach (var t in _customTurrets)
                {
                    var entity = t.GetTargetedEntity();
                    if (!entity.IsEmpty())
                    {
                        EntityInfoExt target = new EntityInfoExt(entity, globalTime);
                        turretTargets.Add(target);
                    }
                }
            }

            public void ToggleNeutrals()
            {
                _targetNeutrals = !_targetNeutrals;
                foreach (var t in _turrets)
                {
                    t.TargetNeutrals = _targetNeutrals;
                }
                foreach (var t in _customTurrets)
                {
                    t.TargetNeutrals = _targetNeutrals;
                }
            }

            public void CycleTargetingGroup()
            {
                _targetingGroupIndex = (_targetingGroupIndex + 1) % _targetingGroups.Count;
                foreach (var t in _turrets)
                {
                    t.SetTargetingGroup(_targetingGroups[_targetingGroupIndex]);
                }
                foreach (var t in _customTurrets)
                {
                    t.SetTargetingGroup(_targetingGroups[_targetingGroupIndex]);
                }
            }

            public void ToggleEnabled()
            {
                _enabled = !_enabled;
                foreach (var t in _turrets)
                {
                    t.Enabled = _enabled;
                }
                foreach (var t in _customTurrets)
                {
                    t.Enabled = _enabled;
                }
            }

            public void AppendStatus(StringBuilder sb)
            {
                sb.AppendLine("[TURRETS]");
                sb.Append("  STATUS: ").AppendLine(_enabled ? "  ENABLED" : "  DISABLED");
                sb.Append("  TRGT GRP: ").AppendLine(_targetingGroupDisplayNames[_targetingGroupIndex]);
                sb.Append("  NTRLS: ").Append(_targetNeutrals ? "YES" : "NO");
            }
        }
    }
}
