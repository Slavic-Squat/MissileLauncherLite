using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
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
        public class SystemCoordinator
        {
            public static double GlobalTime { get; private set; }
            public static IMyShipController ReferenceController { get; private set; }
            public static MatrixD ReferenceWorldMatrix => ReferenceController.WorldMatrix;
            public static Vector3D ReferencePosition => ReferenceController.GetPosition();
            public static Vector3D ReferenceVelocity => ReferenceController.GetShipVelocities().LinearVelocity;
            public static long SelfID => ReferenceController.CubeGrid.EntityId;

            private double _time;
            
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UICoordinator UICoordinator { get; private set; }

            public SystemCoordinator()
            {
                GetBlocks();
                Init();
            }

            private void GetBlocks()
            {
                ReferenceController = AllGridBlocks.FirstOrDefault(b => b is IMyShipController && b.CustomName.ToUpper().Contains("MAIN CONTROLLER")) as IMyShipController;
                if (ReferenceController == null)
                {
                    throw new Exception($"main controller not found!");
                }
            }

            private void Init()
            {
                TargetCoordinator = new TargetCoordinator();
                MissileCoordinator = new MissileCoordinator(TargetCoordinator.Targets);
                UICoordinator = new UICoordinator(this);
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                GlobalTime = time;

                TargetCoordinator.Run(time);
                MissileCoordinator.Run(time);
                UICoordinator.Run();

                _time = time;
            }
        }
    }
}
