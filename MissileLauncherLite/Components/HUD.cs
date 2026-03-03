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
        public class HUD
        {
            private UICoordinator _uiCoordinator;
            private FlightHUDSpriteBuilder _flightHUDSpriteBuilder;
            private TargetingHUDSpriteBuilder _targetingHUDSpriteBuilder;
            private IMyTextSurface _hudDisplay;
            private List<MySpriteExt> _allSprites = new List<MySpriteExt>();
            private IEnumerator<MySpriteExt> _searchingCoroutine;
            private float _resScale;
            private RectangleF _screenBounds;
            private float _opacity = 0.25f;
            private StringBuilder _sb = new StringBuilder();

            public HUD(UICoordinator uiCoordinator, float opacity = 0.25f)
            {
                _uiCoordinator = uiCoordinator;
                _opacity = opacity;
                Init();
            }

            private void Init()
            {
                _hudDisplay = AllBlocks.FirstOrDefault(block => block is IMyTextSurface && block.CustomName.ToUpper().Contains("HUD")) as IMyTextSurface;

                if (_hudDisplay == null)
                {
                    throw new Exception("No HUD display found!");
                }

                _hudDisplay.ContentType = ContentType.SCRIPT;
                _hudDisplay.Script = "";
                _hudDisplay.ScriptBackgroundColor = Color.Black;

                Vector2 surfaceSize = _hudDisplay.SurfaceSize;
                Vector2 textureSize = _hudDisplay.TextureSize;
                float res = Math.Min(surfaceSize.X, surfaceSize.Y);
                _resScale = res / 1024f;
                _screenBounds = new RectangleF(0, 0, surfaceSize.X, surfaceSize.Y);

                IMyTerminalBlock cameraReference = SystemCoordinator.ReferenceController;

                float screenWidthMeters = Config.Get("HUD Config", "ScreenWidthMeters").ToSingle(2.3f);
                Config.Set("HUD Config", "ScreenWidthMeters", screenWidthMeters.ToString());
                float screenHeightMeters = Config.Get("HUD Config", "ScreenHeightMeters").ToSingle(2.3f);
                Config.Set("HUD Config", "ScreenHeightMeters", screenHeightMeters.ToString());
                float screenDistanceMeters = Config.Get("HUD Config", "ScreenDistanceMeters").ToSingle(3.75f);
                Config.Set("HUD Config", "ScreenDistanceMeters", screenDistanceMeters.ToString());
                float screenHorizontalOffsetMeters = Config.Get("HUD Config", "ScreenHorizontalOffsetMeters").ToSingle(0f);
                Config.Set("HUD Config", "ScreenHorizontalOffsetMeters", screenHorizontalOffsetMeters.ToString());
                float screenVerticalOffsetMeters = Config.Get("HUD Config", "ScreenVerticalOffsetMeters").ToSingle(0.015f);
                Config.Set("HUD Config", "ScreenVerticalOffsetMeters", screenVerticalOffsetMeters.ToString());
                MePb.CustomData = Config.ToString();

                float n = screenDistanceMeters;
                float l = -screenWidthMeters / 2f + screenHorizontalOffsetMeters;
                float r = screenWidthMeters / 2f + screenHorizontalOffsetMeters;
                float b = -screenHeightMeters / 2f + screenVerticalOffsetMeters;
                float t = screenHeightMeters / 2f + screenVerticalOffsetMeters;
                _flightHUDSpriteBuilder = new FlightHUDSpriteBuilder(cameraReference, _hudDisplay, _screenBounds, l, r, b, t, n, 10f, _opacity);
                _targetingHUDSpriteBuilder = new TargetingHUDSpriteBuilder(cameraReference, _hudDisplay, _screenBounds, l, r, b, t, n, 7500f, _opacity);
            }

            public void Draw()
            {
                _allSprites.Clear();
                var entities = _uiCoordinator.AllEntities;
                long targetId = _uiCoordinator.TargetCoordinator.LockedTargetID;
                bool searching = _uiCoordinator.TargetCoordinator.Searching;
                var missileBays = _uiCoordinator.MissileBays;

                _targetingHUDSpriteBuilder.BuildSprites(entities, targetedID: targetId);
                _allSprites.AddRange(_targetingHUDSpriteBuilder.FinalSprites);

                _flightHUDSpriteBuilder.BuildSprites();
                _allSprites.AddRange(_flightHUDSpriteBuilder.FinalSprites);

                if (_searchingCoroutine != null)
                {
                    if (_searchingCoroutine.MoveNext())
                    {
                        _allSprites.Add(_searchingCoroutine.Current);
                    }
                    else
                    {
                        _searchingCoroutine = null;
                    }
                }
                else if (searching)
                {
                    _searchingCoroutine = SearchingCoroutine();
                    _searchingCoroutine.MoveNext();
                    _allSprites.Add(_searchingCoroutine.Current);
                }

                _sb.Clear();
                _sb.Append(MiscEnumHelper.GetFlightControlModeStr(_uiCoordinator.FlightControl.FlightControlMode));
                Vector2 flightControlTextPos = _screenBounds.Position + new Vector2(10f, 150f) * _resScale;
                Vector2 flightControlTextSize = SpriteHelper.MeasureStringInPixels(_hudDisplay, _sb, "Monospace", 1.2f * _resScale);
                var flightControlTextSprite = SpriteHelper.CreateText(flightControlTextPos, _sb, new Color(Color.White, _opacity), _hudDisplay, scale: 1.2f * _resScale, fontID: "Monospace");

                MySprite textBackground = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = flightControlTextPos + flightControlTextSize / 2f,
                    Size = flightControlTextSize + new Vector2(20f, 20f) * _resScale,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER
                };

                _allSprites.Add(new MySpriteExt(textBackground, 0.02f));
                _allSprites.Add(new MySpriteExt(flightControlTextSprite, 0.01f));

                var frame = _hudDisplay.DrawFrame();
                foreach (var sprite in _allSprites)
                {
                    sprite.Draw(frame);
                }
                frame.Dispose();
            }

            private IEnumerator<MySpriteExt> SearchingCoroutine()
            {
                int yieldCounter = 0;
                float maxDistance = (float)Math.Sqrt(64 * 64 + 64 * 64) / 2f * _resScale;

                MySprite temp = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Selector_0",
                    Position = _screenBounds.Center,
                    Color = new Color(Color.Orange, _opacity),
                    Size = new Vector2(128f, 128f) * _resScale,
                    Alignment = TextAlignment.CENTER
                };

                while (_uiCoordinator.TargetCoordinator.Searching)
                {
                    var entitySprites = _targetingHUDSpriteBuilder.EntitySprites;
                    float minDistance = maxDistance;
                    EntityInfoExt entity = default(EntityInfoExt);
                    foreach (var sprite in entitySprites.Values)
                    {
                        float distance = Vector2.Distance(sprite.Pos, _screenBounds.Center);
                        if (distance < minDistance && sprite.Entity.IsValid && sprite.Entity.Relation != EntityRelation.Me)
                        {
                            minDistance = distance;
                            entity = sprite.Entity;
                        }
                    }

                    if (entity.IsValid)
                    {
                        _uiCoordinator.TargetCoordinator.LockTarget(entity.EntityID);
                        temp.Color = new Color(Color.GreenYellow, _opacity);
                        yieldCounter++;
                        yield return new MySpriteExt(temp, 0.01f);
                    }
                    else if (yieldCounter % 24 < 12)
                    {
                        temp.Color = new Color(Color.Orange, _opacity);
                        yieldCounter++;
                        yield return new MySpriteExt(temp, 0.01f);
                    }
                    else
                    {
                        yieldCounter++;
                        yield return new MySpriteExt();
                    }
                }
            }
        }
    }
}
