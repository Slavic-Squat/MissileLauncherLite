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
        public class TargetingHUDSpriteBuilder
        {
            public IReadOnlyList<MySpriteExt> FinalSprites => _finalSprites;
            public IReadOnlyDictionary<long, MyEntitySprite> EntitySprites => _entitySprites;

            private List<MySpriteExt> _sprites = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSprites = new List<MySpriteExt>();
            private List<MySpriteExt> _finalSprites = new List<MySpriteExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();
            private IMyTerminalBlock _cameraReference;
            private IMyTextSurface _surface;
            private RectangleF _screenBounds;
            private float _resScale = 1f;
            private float _l, _r, _b, _t, _n, _f;
            private float _opacity = 0.25f;
            private MatrixD _projectionMatrix;
            private StringBuilder _sb = new StringBuilder();

            public TargetingHUDSpriteBuilder(IMyTerminalBlock cameraReference, IMyTextSurface surface, RectangleF screenBounds, float l, float r, float b, float t, float n, float f, float opacity = 0.25f)
            {
                _resScale = Math.Max(screenBounds.Width, screenBounds.Height) / 1024f;
                _screenBounds = screenBounds;
                _cameraReference = cameraReference;
                _surface = surface;
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
            }

            public void BuildSprites(IReadOnlyDictionary<long, EntityInfoExt> entities, long targetedID = -1, bool sort = true)
            {
                _sprites.Clear();
                _finalSprites.Clear();
                _entitySprites.Clear();

                MatrixD cameraFrame = _cameraReference.WorldMatrix;
                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraFrame.Translation, cameraFrame.Translation + cameraFrame.Forward, cameraFrame.Up);

                Vector3D selfVel = SystemCoordinator.ReferenceVelocity;
                foreach (var entity in entities.Values)
                {
                    Vector3D entityPosWorld = entity.Position;
                    Vector3D entityPosView = Vector3D.Transform(entityPosWorld, viewMatrix);
                    Vector4D entityPosClip = Vector4D.Transform(new Vector4D(entityPosView, 1), _projectionMatrix);
                    Vector3 entityPosNDC = new Vector3(entityPosClip.X / entityPosClip.W, entityPosClip.Y / entityPosClip.W, entityPosClip.Z / entityPosClip.W);
                    Vector2 entityPosPixel = new Vector2((1 + entityPosNDC.X) * _screenBounds.Width / 2f, (1 - entityPosNDC.Y) * _screenBounds.Height / 2f);
                    float entityDepthScale = (float)(0.5f * _f / -entityPosView.Z);
                    entityDepthScale = MathHelper.Clamp(entityDepthScale, 0.75f, 1.5f);


                    Vector3D entityRelVelView = Vector3D.TransformNormal(entity.Velocity - selfVel, viewMatrix);
                    double dist = entityPosView.Length();
                    Vector3D range = dist > 0 ? entityPosView / dist : Vector3D.Zero;
                    double closingSpeed = Vector3D.Dot(entityRelVelView, -range);
                    entityRelVelView.Z = 0;
                    Vector3D entityVelPointView = entityPosView + entityRelVelView;
                    Vector4D entityVelPointClip = Vector4D.Transform(new Vector4D(entityVelPointView, 1), _projectionMatrix);
                    Vector3 entityVelPointNDC = new Vector3(entityVelPointClip.X / entityVelPointClip.W, entityVelPointClip.Y / entityVelPointClip.W, entityVelPointClip.Z / entityVelPointClip.W);
                    Vector2 entityVelPointPixel = new Vector2((1 + entityVelPointNDC.X) * _screenBounds.Width / 2f, (1 - entityVelPointNDC.Y) * _screenBounds.Height / 2f);

                    Vector2 velPixel = entityVelPointPixel - entityPosPixel;
                    float velLengthPixel = velPixel.Length();
                    Vector2 velDirPixel = velLengthPixel > 0 ? velPixel / velLengthPixel : Vector2.Zero;
                    velLengthPixel = MathHelper.Clamp(velLengthPixel, 0, 50f * _resScale) * entityDepthScale;
                    velPixel = velDirPixel * velLengthPixel;
                    Vector2 velPosPixel = entityPosPixel + velPixel / 2f;
                    float velAngle = (float)Math.Atan2(-velPixel.Y, velPixel.X);

                    string spriteName = default(string);
                    Vector2 spriteSize = default(Vector2);
                    Color spriteColor = default(Color);

                    switch (entity.Relation)
                    {
                        case EntityRelation.Me:
                            spriteColor = new Color(UIConfig.MeColor, _opacity);
                            break;
                        case EntityRelation.Neutral:
                            spriteColor = new Color(UIConfig.NeutralColor, _opacity);
                            break;
                        case EntityRelation.Friendly:
                            spriteColor = new Color(UIConfig.FriendlyColor, _opacity);
                            break;
                        case EntityRelation.Hostile:
                            spriteColor = new Color(UIConfig.HostileColor, _opacity);
                            break;
                        default:
                            spriteColor = new Color(Color.White, _opacity);
                            break;
                    }

                    if (entity.Type == EntityType.Missile)
                    {
                        spriteName = "Missile_0";
                        spriteSize = new Vector2(16, 16) * _resScale;
                    }
                    else
                    {
                        spriteName = "Target_1";
                        spriteSize = new Vector2(32, 32) * _resScale;
                    }

                    MySprite tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = spriteName,
                        Position = entityPosPixel,
                        Size = spriteSize * entityDepthScale,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt mySpriteExtEntity = new MySpriteExt(tempSprite, entityPosNDC.Z);
                    MyEntitySprite entitySprite = new MyEntitySprite(entity, mySpriteExtEntity);

                    _sprites.Add(mySpriteExtEntity);
                    _entitySprites.Add(entity.EntityID, entitySprite);

                    tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = velPosPixel,
                        Size = new Vector2(velLengthPixel, 4f * _resScale),
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = -velAngle
                    };

                    MySpriteExt velSprite = new MySpriteExt(tempSprite, entityPosNDC.Z - 0.001f);
                    _sprites.Add(velSprite);

                    MySpriteExt selectorSpriteExt = default(MySpriteExt);

                    if (entity.EntityID == targetedID)
                    {
                        tempSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "Selector_0",
                            Position = entityPosPixel,
                            Size = spriteSize * entityDepthScale * 1.5f,
                            Color = new Color(Color.OrangeRed, _opacity),
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = 0f,
                        };

                        selectorSpriteExt = new MySpriteExt(tempSprite, entityPosNDC.Z - 0.001f);
                        _sprites.Add(selectorSpriteExt);

                        _sb.Clear();
                        _sb.Append("RNG: ");
                        UIUtilities.AppendDistance(_sb, dist);
                        _sb.AppendLine();
                        _sb.AppendFormat("SPD: {0:F1} m/s", closingSpeed);

                        Vector2 textPos = entityPosPixel + spriteSize * entityDepthScale * 0.75f + new Vector2(20f * _resScale, 0);
                        tempSprite = SpriteHelper.CreateText(textPos, _sb, new Color(Color.White, _opacity), _surface, scale: 1f * _resScale * entityDepthScale);
                        _sprites.Add(new MySpriteExt(tempSprite, entityPosNDC.Z - 0.001f));
                    }

                    _finalSprites.AddRange(_staticSprites);
                    _finalSprites.AddRange(_sprites);
                    if (sort) _finalSprites.SortNoAlloc((a, b) => b.Depth.CompareTo(a.Depth));
                }
            }
        }
    }
}
