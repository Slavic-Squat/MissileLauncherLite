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
        public static class UIConfig
        {
            public static Color SelectorColor = new Color(252, 186, 3, 255);
            public static Color FriendlyColor = new Color(3, 252, 128, 255);
            public static Color NeutralColor = new Color(252, 144, 3, 255);
            public static Color HostileColor = new Color(252, 3, 65, 255);
            public static Color MeColor = new Color(3, 140, 252, 255);
        }
    }
}
