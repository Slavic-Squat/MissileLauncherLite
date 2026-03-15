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
            private List<ITurret> _turrets = new List<ITurret>();

            private IReadOnlyDictionary<long, EntityInfoExt> _targets = new Dictionary<long, EntityInfoExt>();

            private bool _targetNeutral = false;
            private bool _enabled = true;

            private List<string> _targetingGroups = new List<string>() { "Default", "Weapons", "PowerSystems", "Propulsion"};
            private List<string> _targetingGroupDisplayNames = new List<string>() { "DEFAULT", "WEAPONS", "POWER", "PROPULSION"};
            private int _targetingGroupIndex = 0;
            public TurretCoordinator(IReadOnlyDictionary<long, EntityInfoExt> targets)
            {
                _targets = targets;
                Init();
            }

            private void Init()
            {
                _turrets.Clear();
                foreach (var block in AllBlocks)
                {
                    if (block is IMyLargeTurretBase)
                    {
                        _turrets.Add(new BasicTurret(block as IMyLargeTurretBase));
                    }

                    if (block is IMyTurretControlBlock)
                    {
                        _turrets.Add(new CustomTurret(block as IMyTurretControlBlock));
                    }
                }

                foreach (var t in _turrets)
                {
                    t.SetTargetingGroup(_targetingGroups[_targetingGroupIndex]);
                    t.Enabled = _enabled;
                    t.TargetNeutral = _targetNeutral;
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
            }

            public void ToggleNeutral()
            {
                _targetNeutral = !_targetNeutral;
                foreach (var t in _turrets)
                {
                    t.TargetNeutral = _targetNeutral;
                }
            }

            public void CycleTargetingGroup()
            {
                _targetingGroupIndex = (_targetingGroupIndex + 1) % _targetingGroups.Count;
                foreach (var t in _turrets)
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
            }

            public void Focus()
            {
                foreach (var t in _turrets)
                {
                    t.Focus();
                }
            }

            public void AppendStatus(StringBuilder sb)
            {
                sb.AppendLine("[TURRETS]");
                sb.AppendLine("----------");
                sb.Append(" STATUS: ").AppendLine(_enabled ? "ENABLED" : "DISABLED");
                sb.Append("  FOCUS: ").AppendLine(_targetingGroupDisplayNames[_targetingGroupIndex]);
                sb.Append("  NTRLS: ").Append(_targetNeutral ? "YES" : "NO");
            }
        }
    }
}
