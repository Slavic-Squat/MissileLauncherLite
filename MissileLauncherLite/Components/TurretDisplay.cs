using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
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
        public class TurretDisplay
        {
            private UICoordinator _uiCoordinator;
            private IMyTextSurface _display;
            private StringBuilder _sb = new StringBuilder();
            public TurretDisplay(UICoordinator uiCoordinator)
            {
                _uiCoordinator = uiCoordinator;
                Init();
            }

            private void Init()
            {
                _display = (SystemCoordinator.ReferenceController as IMyTextSurfaceProvider).GetSurface(1);
            }

            public void Draw()
            {
                _sb.Clear();
                _uiCoordinator.TurretCoordinator.AppendStatus(_sb);
                _display.WriteText(_sb);
            }
        }
    }
}
