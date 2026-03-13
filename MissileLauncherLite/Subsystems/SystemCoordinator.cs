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

            private double _lastRunTime;
            private UserInput _userInput;
            
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
                ReferenceController = AllBlocks.FirstOrDefault(b => b is IMyShipController && b.CustomName.ToUpper().Contains("MAIN CONTROLLER")) as IMyShipController;
                if (ReferenceController == null)
                {
                    throw new Exception("Main controller not found!");
                }

                _userInput = new UserInput(ReferenceController);
                TargetCoordinator = new TargetCoordinator();
                MissileCoordinator = new MissileCoordinator(TargetCoordinator.Targets);
                FlightControl = new FlightControl();
                UICoordinator = new UICoordinator(this);

                CommunicationHandlerInst.RegisterTag("COMMANDS", true);

                CommandHandlerInst.RegisterCommand("UNLOCK_TARGET", (args) => TargetCoordinator.UnlockTarget());
                CommandHandlerInst.RegisterCommand("CYCLE_FLIGHT_CTRL", (args) => FlightControl.CycleFlightControlMode());
                CommandHandlerInst.RegisterCommand("TOGGLE_BAYS", (args) => MissileCoordinator.ToggleBays(args));
                CommandHandlerInst.RegisterCommand("SELECT_ALL", (args) => MissileCoordinator.SelectAll());
                CommandHandlerInst.RegisterCommand("DESELECT_ALL", (args) => MissileCoordinator.DeselectAll());
                CommandHandlerInst.RegisterCommand("LAUNCH", (args) => { if (TargetCoordinator.HasLockedTarget) MissileCoordinator.LaunchMissile(TargetCoordinator.LockedTargetID); });
                CommandHandlerInst.RegisterCommand("LAUNCH_ALL", (args) => { if (TargetCoordinator.HasLockedTarget) MissileCoordinator.LaunchMissiles(TargetCoordinator.LockedTargetID); });
                CommandHandlerInst.RegisterCommand("ABORT", (args) => MissileCoordinator.AbortAll());
                CommandHandlerInst.RegisterCommand("CYLCE_PAGE", (args) => UICoordinator.CyclePage());
                CommandHandlerInst.RegisterCommand("CYCLE_DISPLAY_MODE", (args) => UICoordinator.CycleDisplayMode());
                CommandHandlerInst.RegisterCommand("START_HUD_SEARCH", (args) => UICoordinator.StartHUDSearch());
                CommandHandlerInst.RegisterCommand("STOP_HUD_SEARCH", (args) => UICoordinator.StopHUDSearch());
            }

            public void Run(double time)
            {
                if (_lastRunTime == 0)
                {
                    _lastRunTime = time;
                    return;
                }

                GlobalTime = time;

                Receive();

                _userInput.Run(time);
                FlightControl.Control(_userInput);
                TargetCoordinator.Run(time);
                MissileCoordinator.Run(time);
                UICoordinator.Run();

                _lastRunTime = time;
            }

            private void Receive()
            {
                while (CommunicationHandlerInst.HasMessage("COMMANDS", true))
                {
                    MyIGCMessage msg;
                    if (CommunicationHandlerInst.TryRetrieveMessage("COMMANDS", true, out msg))
                    {
                        string command = msg.As<string>();
                        CommandHandlerInst.RunCommands(command);
                    }
                }
            }
        }
    }
}
