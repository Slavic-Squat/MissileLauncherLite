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
            private Dictionary<int, Vector3D> _pitchPos = new Dictionary<int, Vector3D>();
            private Dictionary<int, Vector3D> _rollPos = new Dictionary<int, Vector3D>();
            private MatrixD _projectionMatrix = MatrixD.Identity;
            private RectangleF _screenBounds;
            private IMyTerminalBlock _cameraReference;
            private float _resScale = 1f;
            private float _l, _r, _b, _t, _n, _f;
            private float _opacity = 0.25f;
            private StringBuilder _sb = new StringBuilder();
            public IReadOnlyList<MySpriteExt> FinalSprites => _finalSprites;
            public FlightHUDSpriteBuilder(IMyTerminalBlock cameraReference, float res, float l, float r, float b, float t, float n, float f, float opacity = 0.25f)
            {
                _resScale = res / 1024f;
                _cameraReference = cameraReference;
                _screenBounds = new RectangleF(0, 0, res, res);

                for (int i = 0; i < 19; i++)
                {
                    int angle = -90 + i * 10;
                    float radians = MathHelper.ToRadians(angle);
                    _pitchPos[angle] = new Vector3(0, Math.Sin(radians), -Math.Cos(radians)) * 1.1f * n;
                }

                for (int i = 0; i < 37; i++)
                {
                    int angle = -180 + i * 10;
                    float radians = MathHelper.ToRadians(angle);
                    _rollPos[angle] = new Vector3(Math.Sin(radians), Math.Cos(radians), 0) * 1.1f * n;
                }

                _l = l;
                _r = r;
                _b = b;
                _t = t;
                _n = n;
                _f = f;
                _opacity = opacity;
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
                    Color = new Color(Color.White, _opacity),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = -(float)Math.PI / 2f
                };

                _staticSprites.Add(new MySpriteExt(boreSight, 0.9f));
            }

            public void BuildSprites()
            {
                _sprites.Clear();
                _finalSprites.Clear();

                MatrixD cameraFrame = _cameraReference.WorldMatrix;
                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraFrame.Translation, cameraFrame.Translation + cameraFrame.Forward, cameraFrame.Up);

                Vector3D velocity = SystemCoordinator.ReferenceVelocity;
                double speed = velocity.Length();
                Vector2 speedTextPos = new Vector2(10f, 50f) * _resScale;
                _sb.Clear();
                _sb.AppendFormat("SPD: {0:F0}m/s", speed);
                MySprite speedTextSprite = SpriteHelper.CreateText(speedTextPos, _sb, new Color(Color.White, _opacity), maxHeight: 40f * _resScale, fontID: "Monospace");
                _sprites.Add(new MySpriteExt(speedTextSprite, 0.05f));

                Vector3D velocityDir;
                if (speed <= 1)
                {
                    velocityDir = cameraFrame.Forward * 10;
                }
                else
                {
                    velocityDir = (velocity / speed) * 10;

                }

                Vector3D velVectorPosView = Vector3D.TransformNormal(velocityDir, viewMatrix);
                Vector4D velVectorPosClip = Vector4D.Transform(new Vector4(velVectorPosView, 1), _projectionMatrix);
                Vector3 velVectorPosNDC = new Vector3(velVectorPosClip.X / velVectorPosClip.W, velVectorPosClip.Y / velVectorPosClip.W, velVectorPosClip.Z / velVectorPosClip.W);
                if (Math.Abs(velVectorPosNDC.X) < 1f && Math.Abs(velVectorPosNDC.Y) < 1f)
                {
                    Vector2 posPixel = new Vector2((1 + velVectorPosNDC.X) * _screenBounds.Width / 2f, (1 - velVectorPosNDC.Y) * _screenBounds.Height / 2f);
                    Color color = new Color(Color.GreenYellow, _opacity);

                    if (velVectorPosClip.W < 0)
                    {
                        color = new Color(Color.OrangeRed, _opacity);
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
                        Color = velVectorPosClip.W < 0 ? new Color(Color.OrangeRed, _opacity) : new Color(Color.GreenYellow, _opacity),
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
                        Color = velVectorPosClip.W < 0 ? new Color(Color.GreenYellow, _opacity) : new Color(Color.OrangeRed, _opacity),
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = (float)rot
                    };

                    _sprites.Add(new MySpriteExt(dirSpriteNegative, 0.95f));
                }

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

                    gravAlignedView = MatrixD.Transpose(gravAlignedView);
                    double rollRadians = Math.Atan2(-gravAlignedView.M21, gravAlignedView.M11);
                    double rollDeg = MathHelper.ToDegrees(rollRadians);

                    double pitchRadians = Math.Asin(-gravAlignedView.M32);
                    double pitchDeg = MathHelper.ToDegrees(pitchRadians);

                    Vector2 rollTextPos = new Vector2(10f, 100f) * _resScale;
                    _sb.Clear();
                    _sb.AppendFormat("ROLL: {0:F0}°", rollDeg);
                    MySprite rollTextSprite = SpriteHelper.CreateText(rollTextPos, _sb, new Color(Color.White, _opacity), maxHeight: 40f * _resScale, fontID: "Monospace");
                    _sprites.Add(new MySpriteExt(rollTextSprite, 0.05f));

                    double alt = SystemCoordinator.ReferenceSurfaceAlt;
                    if (alt < 0)
                    {
                        alt = SystemCoordinator.ReferenceSeaLevelAlt;
                    }

                    Vector2 altTextPos = new Vector2(1024f - 10f, 50f) * _resScale;
                    _sb.Clear();
                    _sb.AppendFormat("ALT: {0:F0}m", alt);
                    MySprite altTextSprite = SpriteHelper.CreateText(altTextPos, _sb, new Color(Color.White, _opacity), maxHeight: 40f * _resScale, fontID: "Monospace", alignment: TextAlignment.RIGHT);
                    _sprites.Add(new MySpriteExt(altTextSprite, 0.05f));

                    foreach (var kvp in _pitchPos)
                    {
                        MatrixD pitch = MatrixD.CreateRotationX(-pitchRadians);
                        MatrixD roll = MatrixD.CreateRotationZ(-rollRadians);

                        Vector3D posView = Vector3D.TransformNormal(kvp.Value, pitch * roll);
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
                            Color = new Color(Color.White, _opacity),
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = (float)rollRadians
                        };

                        _sprites.Add(new MySpriteExt(demSprite, 1f));

                        Vector2 textLeftPos = pixelPos + new Vector2(-(spriteSize.X / 2f + 60f * _resScale) * (float)Math.Cos(rollRadians), -(spriteSize.X / 2f + 60f * _resScale) * (float)Math.Sin(rollRadians));
                        Vector2 textRightPos = pixelPos + new Vector2((spriteSize.X / 2f + 60f * _resScale) * (float)Math.Cos(rollRadians), (spriteSize.X / 2f + 60f * _resScale) * (float)Math.Sin(rollRadians));

                        _sb.Clear();
                        _sb.Append(kvp.Key);
                        MySprite textLeftSprite = SpriteHelper.CreateText(textLeftPos, _sb, new Color(Color.White, _opacity), maxHeight: 40f, maxWidth: 40f * _resScale, alignment: TextAlignment.CENTER, vertCentered: true, fontID: "Monospace");
                        _sprites.Add(new MySpriteExt(textLeftSprite, 1f));
                        MySprite textRightSprite = SpriteHelper.CreateText(textRightPos, _sb, new Color(Color.White, _opacity), maxHeight: 40f, maxWidth: 40f * _resScale, alignment: TextAlignment.CENTER, vertCentered: true, fontID: "Monospace");
                        _sprites.Add(new MySpriteExt(textRightSprite, 1f));
                    }
                }

                _finalSprites.AddRange(_staticSprites);
                _finalSprites.AddRange(_sprites);
                _finalSprites.SortNoAlloc((a, b) => b.Depth.CompareTo(a.Depth));
            }
        }
    }
}
