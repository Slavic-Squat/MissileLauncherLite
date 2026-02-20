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
            public static MySprite CreateText(Vector2 pos, StringBuilder sb, Color color, float scale = -1, float maxWidth = float.PositiveInfinity, float maxHeight = float.PositiveInfinity, string fontID = "White", TextAlignment alignment = TextAlignment.LEFT, bool vertCentered = false)
            {
                Vector2 textSize;
                float fillScale;
                if (scale > 0)
                {
                    textSize = MeasureStringInPixels(sb, fontID, scale);
                    if (textSize.X <= maxWidth && textSize.Y <= maxHeight)
                    {
                        fillScale = scale;
                    }
                    else
                    {
                        fillScale = Math.Min(maxWidth / textSize.X, maxHeight / textSize.Y);
                    }
                }
                else
                {
                    textSize = MeasureStringInPixels(sb, fontID, 1);
                    fillScale = Math.Min(maxWidth / textSize.X, maxHeight / textSize.Y);
                }

                if (vertCentered)
                {
                    pos.Y -= (textSize.Y * fillScale) / 2f;
                }

                return new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = sb.ToString(),
                    Position = pos,
                    Color = color,
                    RotationOrScale = fillScale,
                    Alignment = alignment,
                    FontId = fontID
                };
            }

            public static Vector2 MeasureStringInPixels(StringBuilder sb, string font = "White", float scale = 1f)
            {
                IMyTextSurface referenceSurface = MePb.GetSurface(0);
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
