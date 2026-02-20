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
            Empty, Building, Fueling, Ready, Active, Launching
        }
        public enum Direction
        {
            Left, Right, Up, Down, Forward, Backward
        }

        public static class MiscEnumHelper
        {
            public static string GetBayStatusStr(BayStatus status)
            {
                switch (status)
                {
                    case BayStatus.Empty: return "EMPTY";
                    case BayStatus.Building: return "BUILDING";
                    case BayStatus.Fueling: return "FUELING";
                    case BayStatus.Ready: return "READY";
                    case BayStatus.Active: return "ACTIVE";
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
                    case BayStatus.Active: return "ACT";
                    case BayStatus.Launching: return "LNCH";
                    default: return "N/A";
                }
            }
        }
    }
}
