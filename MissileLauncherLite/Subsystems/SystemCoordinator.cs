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
            public static Vector3D ReferenceGravity => ReferenceController.GetNaturalGravity();
            public static float ReferenceMass => ReferenceController.CalculateShipMass().TotalMass;
            public static double ReferenceSeaLevelAlt
            {
                get
                {
                    double alt = double.MaxValue;
                    ReferenceController.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out alt);
                    return alt;
                }
            }

            public static double ReferenceSurfaceAlt
            {
                get
                {
                    double alt = double.MaxValue;
                    ReferenceController.TryGetPlanetElevation(MyPlanetElevation.Surface, out alt);
                    return alt;
                }
            }
            public static long SelfID => ReferenceController.CubeGrid.EntityId;

            private double _time;
            
            public TargetCoordinator TargetCoordinator { get; private set; }
            public MissileCoordinator MissileCoordinator { get; private set; }
            public UICoordinator UICoordinator { get; private set; }
            public FlightControl FlightControl { get; private set; }

            public SystemCoordinator()
            {
                Init();
            }

            private void Init()
            {
                ReferenceController = AllGridBlocks.FirstOrDefault(b => b is IMyShipController && b.CustomName.ToUpper().Contains("MAIN CONTROLLER")) as IMyShipController;
                if (ReferenceController == null)
                {
                    throw new Exception("Main controller not found!");
                }

                TargetCoordinator = new TargetCoordinator();
                MissileCoordinator = new MissileCoordinator(TargetCoordinator.Targets);
                FlightControl = new FlightControl();
                UICoordinator = new UICoordinator(this);

                CommandHandler0.RegisterCommand("START_SEARCH", (args) => TargetCoordinator.StartSearch());
                CommandHandler0.RegisterCommand("STOP_SEARCH", (args) => TargetCoordinator.StopSearch());
                CommandHandler0.RegisterCommand("UNLOCK_TARGET", (args) => TargetCoordinator.UnlockTarget());
                CommandHandler0.RegisterCommand("CYCLE_FLIGHT_CTRL", (args) => FlightControl.CycleFlightControlMode());
                CommandHandler0.RegisterCommand("TOGGLE_BAY", (args) => { if (args.Length > 0) MissileCoordinator.ToggleBay(args[0]); });
                CommandHandler0.RegisterCommand("ACTIVATE_ALL", (args) => MissileCoordinator.ActivateAll());
                CommandHandler0.RegisterCommand("DEACTIVATE_ALL", (args) => MissileCoordinator.DeactivateAll());
                CommandHandler0.RegisterCommand("LAUNCH", (args) => { if (TargetCoordinator.HasLockedTarget) MissileCoordinator.LaunchMissile(TargetCoordinator.LockedTargetID); });
                CommandHandler0.RegisterCommand("LAUNCH_ALL", (args) => { if (TargetCoordinator.HasLockedTarget) MissileCoordinator.LaunchMissiles(TargetCoordinator.LockedTargetID); });
                CommandHandler0.RegisterCommand("ABORT", (args) => MissileCoordinator.AbortAll());
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
