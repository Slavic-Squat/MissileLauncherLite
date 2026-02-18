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
        public class TargetingDisplays
        {
            private UICoordinator _uiCoordinator;
            private TargetingSpriteBuilderSimple _spriteBuilderSimple;
            private TargetingSpriteBuilder _spriteBuilderAdvanced;
            private List<IMyTextSurface> _simpleDisplays = new List<IMyTextSurface>();
            private List<IMyTextSurface> _advancedDisplays = new List<IMyTextSurface>();
            private IReadOnlyDictionary<long, EntityInfoExt> _entities = new Dictionary<long, EntityInfoExt>();
            public TargetingDisplays(UICoordinator uiCoordinator)
            {
                _uiCoordinator = uiCoordinator;
                _spriteBuilderSimple = new TargetingSpriteBuilderSimple(1024f);
                _spriteBuilderAdvanced = new TargetingSpriteBuilder(1024f, 1.5f);
                _entities = _uiCoordinator.AllEntities;

                Init();
            }

            private void Init()
            {
                IEnumerable<IMyTerminalBlock> temp = AllGridBlocks.Where(b => b is IMyTextSurface && b.CustomName.ToUpper().Contains("TARGETING DISPLAY"));

                foreach (var displayBlock in temp)
                {
                    MyIni config = new MyIni();
                    if (!config.TryParse(displayBlock.CustomData))
                    {
                        config.Clear();
                    }
                    bool isAdvanced = config.Get("Config", "Advanced").ToBoolean(false);
                    config.Set("Config", "Advanced", isAdvanced);
                    displayBlock.CustomData = config.ToString();

                    AddDisplay(displayBlock as IMyTextSurface, isAdvanced);
                }
            }

            public void Draw()
            {
                long lockedTargetID = _uiCoordinator.TargetCoordinator.LockedTargetID;

                if (_simpleDisplays.Count != 0)
                {
                    _spriteBuilderSimple.BuildSprites(_entities, lockedTargetID);
                }
                if (_advancedDisplays.Count != 0)
                {
                    _spriteBuilderAdvanced.BuildSprites(_entities, lockedTargetID);
                }

                foreach (var display in _simpleDisplays)
                {
                    var frame = display.DrawFrame();
                    foreach (var sprite in _spriteBuilderSimple.FinalSprites)
                    {
                        sprite.Draw(frame);
                    }
                    frame.Dispose();
                }

                foreach (var display in _advancedDisplays)
                {
                    var frame = display.DrawFrame();
                    foreach (var sprite in _spriteBuilderAdvanced.FinalSprites)
                    {
                        sprite.Draw(frame);
                    }
                    frame.Dispose();
                }
            }

            public void AddDisplay(IMyTextSurface display, bool isAdvanced)
            {
                if (isAdvanced)
                {
                    if (_advancedDisplays.Contains(display)) return;
                    _advancedDisplays.Add(display);
                }
                else
                {
                    if (_simpleDisplays.Contains(display)) return;
                    _simpleDisplays.Add(display);
                }

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;
            }
        }
    }
}
