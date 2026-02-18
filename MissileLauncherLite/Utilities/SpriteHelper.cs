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
        public static class SpriteHelper
        {
            public static MySprite CreateText(Vector2 pos, string text, Color color, float scale = 1f, string fontId = "White", TextAlignment alignment = TextAlignment.LEFT, bool vertCentered = false)
            {
                if (vertCentered)
                {
                    float textHeight = MeasureStringInPixels(text, fontId, scale).Y;
                    pos.Y -= textHeight / 2;
                }
                return new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = pos,
                    Color = color,
                    RotationOrScale = scale,
                    Alignment = alignment,
                    FontId = fontId
                };
            }

            public static MySprite CreateText(RectangleF bounds, string text, Color color, float maxScale = 10f, string fontId = "White", TextAlignment alignment = TextAlignment.LEFT, bool vertCentered = false, float padding = 0f)
            {
                bounds.Size -= 2 * padding;
                bounds.Position += padding;
                Vector2 pos = bounds.Position;

                Vector2 textSize = MeasureStringInPixels(text, fontId, 1);
                float fillScale = Math.Min(bounds.Size.X / textSize.X, bounds.Size.Y / textSize.Y);
                fillScale = Math.Min(fillScale, maxScale);

                if (vertCentered)
                {
                    pos.Y = bounds.Center.Y - (textSize.Y * fillScale) / 2;
                }

                switch (alignment)
                {
                    case TextAlignment.LEFT:
                        break;
                    case TextAlignment.RIGHT:
                        pos.X = bounds.Right;
                        break;
                    case TextAlignment.CENTER:
                        pos.X = bounds.Center.X;
                        break;
                }

                return new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = pos,
                    Color = color,
                    RotationOrScale = fillScale,
                    Alignment = alignment,
                    FontId = fontId
                };
            }

            public static Vector2 MeasureStringInPixels(string text, string font = "White", float scale = 1f)
            {
                IMyTextSurface referenceSurface = MePb.GetSurface(0);
                var sb = new StringBuilder(text);
                return referenceSurface.MeasureStringInPixels(sb, font, scale);
            }

            public static void CreateBoxFilled(List<MySprite> sprites, RectangleF bounds, Color borderColor, Color fillColor, float borderThickness)
            {
                // Border
                sprites.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = bounds.Center,
                    Size = bounds.Size,
                    Color = borderColor,
                    Alignment = TextAlignment.CENTER,
                });
                // Fill
                sprites.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = bounds.Center,
                    Size = bounds.Size - 2 * borderThickness,
                    Color = fillColor,
                    Alignment = TextAlignment.CENTER,
                });
            }

            public static void CreateBoxHollow(List<MySprite> sprites, RectangleF bounds, Color borderColor, float borderThickness)
            {
                // Top
                sprites.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(bounds.Center.X, bounds.Y + borderThickness / 2f),
                    Size = new Vector2(bounds.Width, borderThickness),
                    Color = borderColor,
                    Alignment = TextAlignment.CENTER,
                });
                // Bottom
                sprites.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(bounds.Center.X, bounds.Bottom - borderThickness / 2f),
                    Size = new Vector2(bounds.Width, borderThickness),
                    Color = borderColor,
                    Alignment = TextAlignment.CENTER,
                });
                // Left
                sprites.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(bounds.X + borderThickness / 2f, bounds.Center.Y),
                    Size = new Vector2(borderThickness, bounds.Height),
                    Color = borderColor,
                    Alignment = TextAlignment.CENTER,
                });
                // Right
                sprites.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = new Vector2(bounds.Right - borderThickness / 2f, bounds.Center.Y),
                    Size = new Vector2(borderThickness, bounds.Height),
                    Color = borderColor,
                    Alignment = TextAlignment.CENTER,
                });
            }
        }
    }
}
