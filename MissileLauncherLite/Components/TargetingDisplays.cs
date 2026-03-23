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
            private Dictionary<IMyTextSurface, TargetingSpriteBuilder> _advancedDisplays = new Dictionary<IMyTextSurface, TargetingSpriteBuilder>();
            private Dictionary<IMyTextSurface, TargetingSpriteBuilderSimple> _simpleDisplays = new Dictionary<IMyTextSurface, TargetingSpriteBuilderSimple>();
            private IReadOnlyDictionary<long, EntityInfoExt> _entities = new Dictionary<long, EntityInfoExt>();
            public TargetingDisplays(UICoordinator uiCoordinator)
            {
                _uiCoordinator = uiCoordinator;
                _entities = _uiCoordinator.AllEntities;

                Init();
            }

            private void Init()
            {
                IEnumerable<IMyTerminalBlock> temp = AllBlocks.Where(b => b is IMyTextSurface && b.CustomName.ToUpper().Contains("TARGETING DISPLAY"));

                foreach (var displayBlock in temp)
                {
                    MyIni config = new MyIni();
                    if (!config.TryParse(displayBlock.CustomData))
                    {
                        config.Clear();
                    }
                    bool isAdvanced = config.Get("Config", "Advanced").ToBoolean(false);
                    config.Set("Config", "Advanced", isAdvanced);
                    float scale = config.Get("Config", "Scale").ToSingle(1f);
                    config.Set("Config", "Scale", scale);
                    displayBlock.CustomData = config.ToString();

                    AddDisplay(displayBlock as IMyTextSurface, isAdvanced);
                }
            }

            public void Draw()
            {
                long lockedTargetID = _uiCoordinator.TargetCoordinator.LockedTargetID;

                foreach (var kvp in _simpleDisplays)
                {
                    var display = kvp.Key;
                    var spriteBuilder = kvp.Value;
                    spriteBuilder.BuildSprites(_entities, lockedTargetID);
                    var frame = display.DrawFrame();
                    foreach (var sprite in spriteBuilder.FinalSprites)
                    {
                        sprite.Draw(frame);
                    }
                    frame.Dispose();
                }

                foreach (var kvp in _advancedDisplays)
                {
                    var display = kvp.Key;
                    var spriteBuilder = kvp.Value;
                    spriteBuilder.BuildSprites(_entities, lockedTargetID);
                    var frame = display.DrawFrame();
                    foreach (var sprite in spriteBuilder.FinalSprites)
                    {
                        sprite.Draw(frame);
                    }
                    frame.Dispose();
                }
            }

            public void AddDisplay(IMyTextSurface display, bool isAdvanced, float scale = 1f)
            {
                if (isAdvanced)
                {
                    if (_advancedDisplays.ContainsKey(display)) return;
                    Vector2 screenSize = display.SurfaceSize;
                    Vector2 screenPos = (display.TextureSize - screenSize) / 2;
                    RectangleF screenBounds = new RectangleF(screenPos, screenSize);
                    _advancedDisplays.Add(display, new TargetingSpriteBuilder(display, screenBounds, scale));
                }
                else
                {
                    if (_simpleDisplays.ContainsKey(display)) return;
                    Vector2 screenSize = display.SurfaceSize;
                    Vector2 screenPos = (display.TextureSize - screenSize) / 2;
                    RectangleF screenBounds = new RectangleF(screenPos, screenSize);
                    _simpleDisplays.Add(display, new TargetingSpriteBuilderSimple(display, screenBounds, scale));
                }

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;
            }
        }
    }
}
