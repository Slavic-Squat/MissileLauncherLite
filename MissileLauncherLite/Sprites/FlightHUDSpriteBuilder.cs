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
        public class FlightHUDSpriteBuilder
        {
            private List<MySpriteExt> _sprites = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSprites = new List<MySpriteExt>();
            private List<MySpriteExt> _finalSprites = new List<MySpriteExt>();
            private Dictionary<int, Vector3D> _demPosGravLocal = new Dictionary<int, Vector3D>();
            private MatrixD _projectionMatrix = MatrixD.Identity;
            private RectangleF _screenBounds;
            private IMyTerminalBlock _cameraReference;
            private float _resScale = 1f;
            private float _l, _r, _b, _t, _n, _f;
            public IReadOnlyList<MySpriteExt> FinalSprites => _finalSprites;
            public FlightHUDSpriteBuilder(IMyTerminalBlock cameraReference, float res, float l, float r, float b, float t, float n, float f)
            {
                _resScale = res / 1024f;
                _cameraReference = cameraReference;
                _screenBounds = new RectangleF(0, 0, res, res);
                for (int i = 0; i < 19; i++)
                {
                    int angle = -90 + i * 10;
                    float radians = MathHelper.ToRadians(angle);
                    _demPosGravLocal[angle] = new Vector3(0, Math.Sin(radians), -Math.Cos(radians));
                }
                _l = l;
                _r = r;
                _b = b;
                _t = t;
                _n = n;
                _f = f;
                _projectionMatrix = MatrixD.CreatePerspectiveOffCenter(l, r, b, t, n, f);
                BuildStaticSprites();
            }

            private void BuildStaticSprites()
            {
                _staticSprites.Clear();

                MySprite boreSight = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "AH_BoreSight",
                    Position = _screenBounds.Center,
                    Size = new Vector2(64f, 64f) * _resScale,
                    Color = new Color(Color.White, 0.1f),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = -(float)Math.PI / 2f
                };

                _staticSprites.Add(new MySpriteExt(boreSight, 0.9f));
            }

            public void BuildSprites()
            {
                _sprites.Clear();
                _finalSprites.Clear();

                RectangleF speedIndBounds = new RectangleF(_screenBounds.X + 20f * _resScale, _screenBounds.Y + 80f * _resScale, 250f * _resScale, 80f * _resScale);
                RectangleF altIndBounds = new RectangleF(speedIndBounds.X, speedIndBounds.Bottom + 10f * _resScale, 250f * _resScale, 80f * _resScale);
                RectangleF rollIndBounds = new RectangleF(altIndBounds.X, altIndBounds.Bottom + 10f * _resScale, 250f * _resScale, 80f * _resScale);

                MatrixD cameraFrame = _cameraReference.WorldMatrix;
                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraFrame.Translation, cameraFrame.Translation + cameraFrame.Forward, cameraFrame.Up);

                Vector3D gravVector = SystemCoordinator.ReferenceGravity;
                if (gravVector.LengthSquared() > 0.01f)
                {
                    Vector3D gravVectorView = Vector3D.TransformNormal(gravVector, viewMatrix);
                    Vector3D upVector = -Vector3D.Normalize(gravVectorView);
                    Vector3D rightVector = Vector3D.Normalize(Vector3D.Cross(upVector, Vector3D.Backward));
                    Vector3D backwardVector = Vector3D.Normalize(Vector3.Cross(rightVector, upVector));

                    MatrixD gravAlignedView = MatrixD.Identity;
                    gravAlignedView.Right = rightVector;
                    gravAlignedView.Up = upVector;
                    gravAlignedView.Backward = backwardVector;

                    double rollRadians = -Math.Atan2(gravAlignedView.M12, gravAlignedView.M11);
                    double rollDeg = MathHelper.ToDegrees(rollRadians);

                    MySprite rollTextSprite = SpriteHelper.CreateText(rollIndBounds, $"ROLL: {rollDeg:F0}°", new Color(Color.White, 0.1f), maxScale: 1f, alignment: TextAlignment.LEFT, vertCentered: true, padding: 10f * _resScale);
                    _sprites.Add(new MySpriteExt(rollTextSprite, 0.05f));

                    double alt = SystemCoordinator.ReferenceSurfaceAlt;
                    if (alt < 0)
                    {
                        alt = SystemCoordinator.ReferenceSeaLevelAlt;
                    }

                    MySprite altTextSprite = SpriteHelper.CreateText(altIndBounds, $"ALT: {alt:F0}m", new Color(Color.White, 0.1f), maxScale: 1f, alignment: TextAlignment.LEFT, vertCentered: true, padding: 10f * _resScale);
                    _sprites.Add(new MySpriteExt(altTextSprite, 0.05f));

                    foreach (var kvp in _demPosGravLocal)
                    {
                        Vector3D posView = Vector3D.TransformNormal(kvp.Value, gravAlignedView);
                        Vector4D posClip = Vector4D.Transform(new Vector4D(posView, 1), _projectionMatrix);
                        Vector3 posNDC = new Vector3(posClip.X / posClip.W, posClip.Y / posClip.W, posClip.Z / posClip.W);

                        if (Math.Abs(posNDC.X) > 1.5f || Math.Abs(posNDC.Y) > 1.5f || posNDC.Z > 1f || posNDC.Z < 0 || posClip.W < 0)
                        {
                            continue;
                        }

                        Vector2 pixelPos = new Vector2((1 + posNDC.X) * _screenBounds.Width / 2f, (1 - posNDC.Y) * _screenBounds.Height / 2f);
                        string spriteName;
                        Vector2 spriteSize;

                        if (kvp.Key > 0)
                        {
                            spriteName = "AH_GravityHudPositiveDegrees";
                            spriteSize = new Vector2(350f, 32f) * _resScale;
                        }
                        else if (kvp.Key < 0)
                        {
                            spriteName = "AH_GravityHudNegativeDegrees";
                            spriteSize = new Vector2(350f, 32f) * _resScale;
                        }
                        else
                        {
                            spriteName = "SquareSimple";
                            spriteSize = new Vector2(500f, 8f) * _resScale;
                        }

                        MySprite demSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = spriteName,
                            Position = pixelPos,
                            Size = spriteSize,
                            Color = new Color(Color.White, 0.1f),
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = (float)rollRadians
                        };

                        _sprites.Add(new MySpriteExt(demSprite, 1f));

                        Vector2 textSize = new Vector2(96f, 48f);
                        Vector2 textLeftPos = pixelPos + new Vector2(-(spriteSize.X / 2f + textSize.X / 2f) * (float)Math.Cos(rollRadians), -(spriteSize.X / 2f + textSize.X / 2f) * (float)Math.Sin(rollRadians));
                        RectangleF textLeftBounds = new RectangleF(textLeftPos.X - textSize.X / 2f, textLeftPos.Y - textSize.Y / 2f, textSize.X, textSize.Y);
                        Vector2 textRightPos = pixelPos + new Vector2((spriteSize.X / 2f + textSize.X / 2f) * (float)Math.Cos(rollRadians), (spriteSize.X / 2f + textSize.X / 2f) * (float)Math.Sin(rollRadians));
                        RectangleF textRightBounds = new RectangleF(textRightPos.X - textSize.X / 2f, textRightPos.Y - textSize.Y / 2f, textSize.X, textSize.Y);

                        string degreeStr = kvp.Key.ToString();
                        MySprite textLeftSprite = SpriteHelper.CreateText(textLeftBounds, degreeStr, new Color(Color.White, 0.1f), alignment: TextAlignment.CENTER, vertCentered: true);
                        _sprites.Add(new MySpriteExt(textLeftSprite, 1f));
                        MySprite textRightSprite = SpriteHelper.CreateText(textRightBounds, degreeStr, new Color(Color.White, 0.1f), alignment: TextAlignment.CENTER, vertCentered: true);
                        _sprites.Add(new MySpriteExt(textRightSprite, 1f));
                    }
                }
                Vector3D velocity = SystemCoordinator.ReferenceVelocity;
                double speed = velocity.Length();
                MySprite speedTextSprite = SpriteHelper.CreateText(speedIndBounds, $"SPD: {speed:F0}m/s", new Color(Color.White, 0.1f), maxScale: 1f, alignment: TextAlignment.LEFT, vertCentered: true, padding: 10f * _resScale);
                _sprites.Add(new MySpriteExt(speedTextSprite, 0.05f));

                Vector3D velocityDir;
                if (speed <= 1)
                {
                    velocityDir = cameraFrame.Forward;
                }
                else
                {
                    velocityDir = velocity / speed;
                    
                }

                Vector3D velVectorPosView = Vector3D.TransformNormal(velocityDir, viewMatrix);
                Vector4D velVectorPosClip = Vector4D.Transform(new Vector4(velVectorPosView, 1), _projectionMatrix);
                Vector3 velVectorPosNDC = new Vector3(velVectorPosClip.X / velVectorPosClip.W, velVectorPosClip.Y / velVectorPosClip.W, velVectorPosClip.Z / velVectorPosClip.W);
                if (Math.Abs(velVectorPosNDC.X) < 1f && Math.Abs(velVectorPosNDC.Y) < 1f)
                {
                    Vector2 posPixel = new Vector2((1 + velVectorPosNDC.X) * _screenBounds.Width / 2f, (1 - velVectorPosNDC.Y) * _screenBounds.Height / 2f);
                    Color color = new Color(Color.GreenYellow, 0.1f);

                    if (velVectorPosClip.W < 0)
                    {
                        color = new Color(Color.OrangeRed, 0.1f);
                    }
                    MySprite velSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "AH_VelocityVector",
                        Position = posPixel,
                        Size = new Vector2(96f, 96f) * _resScale,
                        Color = color,
                        Alignment = TextAlignment.CENTER
                    };

                    _sprites.Add(new MySpriteExt(velSprite, 0.95f));
                }
                else
                {
                    float max = Math.Max(Math.Abs(velVectorPosNDC.X), Math.Abs(velVectorPosNDC.Y));
                    
                    Vector2 posPixel = new Vector2((1 + velVectorPosNDC.X / max) * (_screenBounds.Width - 64f * _resScale) / 2f, (1 - velVectorPosNDC.Y / max) * (_screenBounds.Height - 64f * _resScale) / 2f) + 32f * _resScale;
                    double rot = Math.Atan2(posPixel.X - _screenBounds.Center.X, -(posPixel.Y - _screenBounds.Center.Y));

                    MySprite dirSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Triangle",
                        Position = posPixel,
                        Size = new Vector2(64f, 64f) * _resScale,
                        Color = velVectorPosClip.W < 0 ? new Color(Color.OrangeRed, 0.1f) : new Color(Color.GreenYellow, 0.1f),
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = (float)rot
                    };

                    _sprites.Add(new MySpriteExt(dirSprite, 0.95f));

                    posPixel = new Vector2(_screenBounds.Width - posPixel.X, _screenBounds.Height - posPixel.Y);
                    rot = Math.Atan2(posPixel.X - _screenBounds.Center.X, -(posPixel.Y - _screenBounds.Center.Y));

                    MySprite dirSpriteNegative = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Triangle",
                        Position = posPixel,
                        Size = new Vector2(64f, 64f) * _resScale,
                        Color = velVectorPosClip.W < 0 ? new Color(Color.GreenYellow, 0.1f) : new Color(Color.OrangeRed, 0.1f),
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = (float)rot
                    };

                    _sprites.Add(new MySpriteExt(dirSpriteNegative, 0.95f));
                }

                _finalSprites.AddRange(_staticSprites);
                _finalSprites.AddRange(_sprites);
                _finalSprites.SortNoAlloc((a, b) => b.Depth.CompareTo(a.Depth));
            }
        }
    }
}
