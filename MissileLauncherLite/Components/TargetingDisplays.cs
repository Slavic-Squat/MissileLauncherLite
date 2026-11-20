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
                    float sizeX = config.Get("Config", "SizeX").ToSingle(-1f);
                    if (sizeX < 0)
                    {
                        sizeX = (displayBlock as IMyTextSurface).TextureSize.X;
                    }
                    config.Set("Config", "SizeX", sizeX);
                    float sizeY = config.Get("Config", "SizeY").ToSingle(-1f);
                    if (sizeY < 0)
                    {
                        sizeY = (displayBlock as IMyTextSurface).TextureSize.Y;
                    }
                    config.Set("Config", "SizeY", sizeY);
                    float posX = config.Get("Config", "PosX").ToSingle(0f);
                    config.Set("Config", "PosX", posX);
                    float posY = config.Get("Config", "PosY").ToSingle(0f);
                    config.Set("Config", "PosY", posY);
                    displayBlock.CustomData = config.ToString();

                    AddDisplay(displayBlock as IMyTextSurface, isAdvanced, scale, sizeX, sizeY, posX, posY);
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

            public void AddDisplay(IMyTextSurface display, bool isAdvanced, float scale, float sizeX, float sizeY, float posX, float posY)
            {
                if (isAdvanced)
                {
                    if (_advancedDisplays.ContainsKey(display)) return;
                    Vector2 screenSize = new Vector2(sizeX, sizeY);
                    Vector2 pos = new Vector2(posX, posY);
                    RectangleF bounds = new RectangleF(pos, screenSize);
                    _advancedDisplays.Add(display, new TargetingSpriteBuilder(display, bounds, scale));
                }
                else
                {
                    if (_simpleDisplays.ContainsKey(display)) return;
                    Vector2 screenSize = new Vector2(sizeX, sizeY);
                    Vector2 pos = new Vector2(posX, posY);
                    RectangleF bounds = new RectangleF(pos, screenSize);
                    _simpleDisplays.Add(display, new TargetingSpriteBuilderSimple(display, bounds, scale));
                }

                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.ScriptBackgroundColor = Color.Black;
            }
        }
    }
}
