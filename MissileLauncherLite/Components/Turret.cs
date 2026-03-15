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
        public interface ITurret
        {
            bool TargetNeutral { get; set; }
            bool TargetHostile { get; set; }
            bool Enabled { get; set; }
            bool LockedTargetingGroup { get; }
            void SetTargetingGroup(string groupName);
            MyDetectedEntityInfo GetTargetedEntity();
            void Focus();
        }

        public class BasicTurret : ITurret
        {
            public IMyLargeTurretBase TurretBlock { get; private set; }
            public bool TargetNeutral
            {
                get { return TurretBlock.TargetNeutrals; }
                set { TurretBlock.TargetNeutrals = value; }
            }

            public bool TargetHostile
            {
                get { return TurretBlock.TargetEnemies; }
                set { TurretBlock.TargetEnemies = value; }
            }

            public bool Enabled
            {
                get { return TurretBlock.Enabled; }
                set { TurretBlock.Enabled = value; }
            }

            public bool LockedTargetingGroup { get; private set; }

            private MyIni _config = new MyIni();

            public BasicTurret(IMyLargeTurretBase turretBlock)
            {
                TurretBlock = turretBlock;

                _config.TryParse(turretBlock.CustomData);
                LockedTargetingGroup = _config.Get("Config", "LockedTargetingGroup").ToBoolean(false);
                _config.Set("Config", "LockedTargetingGroup", LockedTargetingGroup);
                turretBlock.CustomData = _config.ToString();
            }

            public void SetTargetingGroup(string groupName)
            {
                if (LockedTargetingGroup)
                {
                    return;
                }
                TurretBlock.SetTargetingGroup(groupName);
            }

            public MyDetectedEntityInfo GetTargetedEntity()
            {
                return TurretBlock.GetTargetedEntity();
            }

            public void Focus()
            {
                TurretBlock.ApplyAction("FocusLockedTarget");
            }
        }

        public class CustomTurret : ITurret
        {
            public IMyTurretControlBlock TurretControlBlock { get; private set; }

            public bool TargetNeutral
            {
                get { return TurretControlBlock.TargetNeutrals; }
                set { TurretControlBlock.TargetNeutrals = value; }
            }

            public bool TargetHostile
            {
                get { return TurretControlBlock.GetValueBool("TargetEnemies"); }
                set { TurretControlBlock.SetValueBool("TargetEnemies", value); }
            }

            public bool Enabled
            {
                get { return TurretControlBlock.Enabled; }
                set { TurretControlBlock.Enabled = value; }
            }

            public bool LockedTargetingGroup { get; private set; }

            private MyIni _config = new MyIni();

            public CustomTurret(IMyTurretControlBlock turretControlBlock)
            {
                TurretControlBlock = turretControlBlock;

                _config.TryParse(turretControlBlock.CustomData);
                LockedTargetingGroup = _config.Get("Config", "LockedTargetingGroup").ToBoolean(false);
                _config.Set("Config", "LockedTargetingGroup", LockedTargetingGroup);
                turretControlBlock.CustomData = _config.ToString();
            }

            public void SetTargetingGroup(string groupName)
            {
                if (LockedTargetingGroup)
                {
                    return;
                }
                TurretControlBlock.SetTargetingGroup(groupName);
            }

            public MyDetectedEntityInfo GetTargetedEntity()
            {
                return TurretControlBlock.GetTargetedEntity();
            }

            public void Focus()
            {
                TurretControlBlock.ApplyAction("FocusLockedTarget");
            }
        }
    }
}
