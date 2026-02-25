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
        public enum BayStatus : byte
        {
            Empty, Building, Fueling, Ready, Launching
        }
        public enum Direction
        {
            Left, Right, Up, Down, Forward, Backward
        }

        public enum FlightControlMode
        {
            Free, GravComp
        }

        public static class MiscEnumHelper
        {
            private static readonly FlightControlMode[] _flightControlModeCycles = new FlightControlMode[] { FlightControlMode.Free, FlightControlMode.GravComp };

            public static FlightControlMode NextFlightControlMode(FlightControlMode mode)
            {
                int index = Array.IndexOf(_flightControlModeCycles, mode);
                if (index < 0) return _flightControlModeCycles[0];
                index = (index + 1) % _flightControlModeCycles.Length;
                return _flightControlModeCycles[index];
            }

            public static string GetBayStatusStr(BayStatus status)
            {
                switch (status)
                {
                    case BayStatus.Empty: return "EMPTY";
                    case BayStatus.Building: return "BUILDING";
                    case BayStatus.Fueling: return "FUELING";
                    case BayStatus.Ready: return "READY";
                    case BayStatus.Launching: return "LAUNCHING";
                    default: return "N/A";
                }
            }

            public static string GetBayStatusStrShort(BayStatus status)
            {
                switch (status)
                {
                    case BayStatus.Empty: return "EMPTY";
                    case BayStatus.Building: return "BLD";
                    case BayStatus.Fueling: return "FUEL";
                    case BayStatus.Ready: return "RDY";
                    case BayStatus.Launching: return "LNCH";
                    default: return "N/A";
                }
            }

            public static Direction GetDirection(string dirStr)
            {
                switch (dirStr.ToUpper())
                {
                    case "LEFT":
                        return Direction.Left;
                    case "RIGHT":
                        return Direction.Right;
                    case "UP":
                        return Direction.Up;
                    case "DOWN":
                        return Direction.Down;
                    case "FORWARD":
                        return Direction.Forward;
                    case "BACKWARD":
                        return Direction.Backward;
                    default:
                        return Direction.Forward;
                }
            }

            public static string GetDirectionStr(Direction dir)
            {
                switch (dir)
                {
                    case Direction.Left:
                        return "LEFT";
                    case Direction.Right:
                        return "RIGHT";
                    case Direction.Up:
                        return "UP";
                    case Direction.Down:
                        return "DOWN";
                    case Direction.Forward:
                        return "FORWARD";
                    case Direction.Backward:
                        return "BACKWARD";
                    default:
                        return "FORWARD";
                }
            }

            public static FlightControlMode GetFlightControlMode(string modeStr)
            {
                switch (modeStr.ToUpper())
                {
                    case "FREE":
                        return FlightControlMode.Free;
                    case "GRAV_COMP":
                        return FlightControlMode.GravComp;
                    default:
                        return FlightControlMode.Free;
                }
            }

            public static string GetFlightControlModeStr(FlightControlMode mode)
            {
                switch (mode)
                {
                    case FlightControlMode.Free:
                        return "FREE";
                    case FlightControlMode.GravComp:
                        return "GRAV_COMP";
                    default:
                        return "FREE";
                }
            }
        }
    }
}
