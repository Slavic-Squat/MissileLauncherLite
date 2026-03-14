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
        public class TargetingSpriteBuilder
        {
            public IReadOnlyList<MySpriteExt> FinalSprites => _finalSprites;
            public IReadOnlyDictionary<long, MyEntitySprite> EntitySprites => _entitySprites;

            private float _FOV = 30;
            private float _AR = 1;
            private float _n = 100;
            private float _f = 100000;
            private float _scopeScale = 0.5f;

            private Vector3D _localCameraPos = new Vector3D(31334, 30557, 63764);

            private List<MySpriteExt> _spritesPrePlane = new List<MySpriteExt>();
            private List<MySpriteExt> _planeSprites = new List<MySpriteExt>();
            private List<MySpriteExt> _spritesPostPlane = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSpritesPostPlane = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSpritesPrePlane = new List<MySpriteExt>();
            private List<MySpriteExt> _finalSprites = new List<MySpriteExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();

            private string _rangeStr = "6 km";
            private StringBuilder _sb = new StringBuilder();

            private MatrixD _projectionMatrix = MatrixD.Identity;
            private RectangleF _screenBounds;
            private float _resScale = 1f;
            private float _scale = 1f;
            private IMyTextSurface _surface;

            public TargetingSpriteBuilder(IMyTextSurface surface, RectangleF screenBounds, float scale)
            {
                _resScale = Math.Max(screenBounds.Width, screenBounds.Height) / 1024f;
                _scale = scale;
                _surface = surface;
                _screenBounds = screenBounds;
                _projectionMatrix = MatrixD.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n * _scopeScale, _f * _scopeScale);

                BuildStaticSprites();
                
            }

            private void BuildStaticSprites()
            {
                MatrixD cameraTargetWorld = SystemCoordinator.ReferenceWorldMatrix;
                Vector3D cameraPositionWorld = Vector3D.Transform(_localCameraPos, cameraTargetWorld);

                Vector3D targetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                double targetToCameraDist = targetToCamera.Length();
                Vector3D targetToCameraDir = targetToCamera / targetToCameraDist;

                targetToCameraDist *= _scopeScale;
                cameraPositionWorld = cameraTargetWorld.Translation + targetToCameraDir * targetToCameraDist;

                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);

                PlaneD gridPlaneWorld = new PlaneD(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                Vector3D selfPosLocal = new Vector3D(0, 200, 0);
                Vector3D selfPosWorld = Vector3D.Transform(selfPosLocal, cameraTargetWorld);
                Vector3D selfPosView = Vector3D.Transform(selfPosWorld, viewMatrix);
                Vector4D selfPosClip = Vector4D.Transform(new Vector4D(selfPosView, 1), _projectionMatrix);
                Vector3 selfPosNDC = new Vector3(selfPosClip.X / selfPosClip.W, selfPosClip.Y / selfPosClip.W, selfPosClip.Z / selfPosClip.W);
                Vector2 selfPosPixel = new Vector2((1 + selfPosNDC.X * _scale) * _screenBounds.Width / 2f, (1 - selfPosNDC.Y * _scale) * _screenBounds.Height / 2f);

                MySprite tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Self_0",
                    Position = selfPosPixel,
                    Size = new Vector2(128, 128) * _resScale * _scale,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt selfSpriteExt = new MySpriteExt(tempSprite, selfPosNDC.Z);

                Vector3D basePosWorld = selfPosWorld - (Vector3D.Dot(gridPlaneWorld.Normal, selfPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                Vector3D basePosView = Vector3D.Transform(basePosWorld, viewMatrix);
                Vector4D basePosClip = Vector4D.Transform(new Vector4D(basePosView, 1), _projectionMatrix);
                Vector3 basePosNDC = new Vector3(basePosClip.X / basePosClip.W, basePosClip.Y / basePosClip.W, basePosClip.Z / basePosClip.W);
                Vector2 basePosPixel = new Vector2((1 + basePosNDC.X * _scale) * _screenBounds.Width / 2f, (1 - basePosNDC.Y * _scale) * _screenBounds.Height / 2f);
                float baseDepthScale = (float)(targetToCameraDist / -basePosView.Z);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Base_0",
                    Position = basePosPixel,
                    Size = new Vector2(32, 32) * baseDepthScale * _resScale * _scale,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt baseSpriteExt = new MySpriteExt(tempSprite, basePosNDC.Z);

                Vector3D stemPosWorld = 0.5f * (selfPosWorld + basePosWorld);
                Vector3D stemPosView = Vector3D.Transform(stemPosWorld, viewMatrix);
                Vector4D stemPosClip = Vector4D.Transform(new Vector4D(stemPosView, 1), _projectionMatrix);
                Vector3 stemPosNDC = new Vector3(stemPosClip.X / stemPosClip.W, stemPosClip.Y / stemPosClip.W, stemPosClip.Z / stemPosClip.W);
                Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X * _scale) * _screenBounds.Width / 2f, (1 - stemPosNDC.Y * _scale) * _screenBounds.Height / 2f);

                Vector2 stemVector = new Vector2(selfPosPixel.X - basePosPixel.X, selfPosPixel.Y - basePosPixel.Y);
                float stemLength = stemVector.Length();
                double stemAngle = Math.Atan2(stemVector.Y, stemVector.X);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = stemPosPixel,
                    Size = new Vector2(stemLength, 1),
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = (float)stemAngle,
                };

                MySpriteExt stemSpriteExt = new MySpriteExt(tempSprite, stemPosNDC.Z);

                Vector3D cameraTargetView = Vector3D.Transform(cameraTargetWorld.Translation, viewMatrix);
                Vector4D cameraTargetClip = Vector4D.Transform(new Vector4D(cameraTargetView, 1), _projectionMatrix);
                Vector3 cameraTargetNDC = new Vector3(cameraTargetClip.X / cameraTargetClip.W, cameraTargetClip.Y / cameraTargetClip.W, cameraTargetClip.Z / cameraTargetClip.W);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_0",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size * _scale,
                    Color = Color.Gray,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gridSpriteExt = new MySpriteExt(tempSprite, cameraTargetNDC.Z);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_0",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size * _scale,
                    Color = new Color(64, 64, 64, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gradSpriteExt = new MySpriteExt(tempSprite, cameraTargetNDC.Z);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "StarryBackground",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size * _scale,
                    Color = Color.LightGray,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgSpriteExt = new MySpriteExt(tempSprite, 0.99999f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size * _scale,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgFillSpriteExt = new MySpriteExt(tempSprite, 1f);

                _staticSpritesPrePlane.Clear();
                _planeSprites.Clear();
                _staticSpritesPostPlane.Clear();
                _staticSpritesPrePlane.Add(bgSpriteExt);
                _staticSpritesPrePlane.Add(bgFillSpriteExt);
                _planeSprites.Add(gridSpriteExt);
                _planeSprites.Add(gradSpriteExt);                
                _staticSpritesPostPlane.Add(selfSpriteExt);
                _staticSpritesPostPlane.Add(baseSpriteExt);
                _staticSpritesPostPlane.Add(stemSpriteExt);

                _planeSprites.SortNoAlloc((a, b) => b.Depth.CompareTo(a.Depth));
            }

            public void BuildSprites(IReadOnlyDictionary<long, EntityInfoExt> entities, long targetedID = -1)
            {
                _finalSprites.Clear();
                _entitySprites.Clear();

                _spritesPrePlane.Clear();
                _spritesPostPlane.Clear();

                MatrixD cameraTargetWorld = SystemCoordinator.ReferenceWorldMatrix;
                Vector3D cameraPositionWorld = Vector3D.Transform(_localCameraPos, cameraTargetWorld);

                Vector3D targetToCamera = cameraPositionWorld - cameraTargetWorld.Translation;
                double targetToCameraDist = targetToCamera.Length();
                Vector3D targetToCameraDir = targetToCamera / targetToCameraDist;

                targetToCameraDist *= _scopeScale;
                cameraPositionWorld = cameraTargetWorld.Translation + targetToCameraDir * targetToCameraDist;
                

                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraPositionWorld, cameraTargetWorld.Translation, cameraTargetWorld.Up);

                PlaneD gridPlaneWorld = new PlaneD(cameraTargetWorld.Translation, cameraTargetWorld.Up);

                double farthestDistance = 0;
                foreach (var entity in entities.Values)
                {
                    double distance = Vector3D.Distance(cameraTargetWorld.Translation, entity.Position);
                    if (distance > farthestDistance) farthestDistance = distance;
                }

                AdjustScopeScale((float)farthestDistance);

                Vector2 rangeTextPos = _screenBounds.Position + new Vector2(10f, 10f) * _resScale;
                MySprite rangeTextSprite = SpriteHelper.CreateText(rangeTextPos, _sb.Clear().Append(_rangeStr), Color.White, _surface, text: _rangeStr, fontID: "Monospace", scale: 1.5f * _resScale);
                _spritesPostPlane.Add(new MySpriteExt(rangeTextSprite, 0.01f));

                foreach (var entity in entities.Values)
                {
                    double distance = Vector3D.Distance(cameraTargetWorld.Translation, entity.Position);

                    if (distance > 12000f * _scopeScale)
                    {
                        continue;
                    }

                    Vector3D entityPosWorld = entity.Position;
                    Vector3D entityPosView = Vector3D.Transform(entityPosWorld, viewMatrix);
                    Vector4D entityPosClip = Vector4D.Transform(new Vector4D(entityPosView, 1), _projectionMatrix);
                    Vector3 entityPosNDC = new Vector3(entityPosClip.X / entityPosClip.W, entityPosClip.Y / entityPosClip.W, entityPosClip.Z / entityPosClip.W);
                    Vector2 entityPosPixel = new Vector2((1 + entityPosNDC.X * _scale) * _screenBounds.Width / 2f, (1 - entityPosNDC.Y * _scale) * _screenBounds.Height / 2f);
                    float entityDepthScale = (float)(targetToCameraDist / -entityPosView.Z);

                    string spriteName = default(string);
                    Vector2 spriteSize = default(Vector2);
                    Color spriteColor = default(Color);

                    switch (entity.Relation)
                    {
                        case EntityRelation.Me:
                            spriteColor = UIConfig.MeColor;
                            break;
                        case EntityRelation.Neutral:
                            spriteColor = UIConfig.NeutralColor;
                            break;
                        case EntityRelation.Friendly:
                            spriteColor = UIConfig.FriendlyColor;
                            break;
                        case EntityRelation.Hostile:
                            spriteColor = UIConfig.HostileColor;
                            break;
                        default:
                            spriteColor = Color.White;
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
                        Size = spriteSize * entityDepthScale * _scale,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt mySpriteExtEntity = new MySpriteExt(tempSprite, entityPosNDC.Z);
                    MyEntitySprite entitySprite = new MyEntitySprite(entity, mySpriteExtEntity);

                    _entitySprites.Add(entity.EntityID, entitySprite);

                    MySpriteExt selectorSpriteExt = default(MySpriteExt);

                    if (entity.EntityID == targetedID)
                    {
                        tempSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "Selector_0",
                            Position = entityPosPixel,
                            Size = mySpriteExtEntity.Sprite.Size * 1.5f,
                            Color = Color.OrangeRed,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = 0f,
                        };

                        selectorSpriteExt = new MySpriteExt(tempSprite, entityPosNDC.Z - 0.001f);
                    }

                    Vector3D basePosWorld = entityPosWorld - (Vector3D.Dot(gridPlaneWorld.Normal, entityPosWorld) + gridPlaneWorld.D) * gridPlaneWorld.Normal;
                    Vector3D basePosView = Vector3D.Transform(basePosWorld, viewMatrix);
                    Vector4D basePosClip = Vector4D.Transform(new Vector4D(basePosView, 1), _projectionMatrix);
                    Vector3 basePosNDC = new Vector3(basePosClip.X / basePosClip.W, basePosClip.Y / basePosClip.W, basePosClip.Z / basePosClip.W);
                    Vector2 basePosPixel = new Vector2((1 + basePosNDC.X * _scale) * _screenBounds.Width / 2f, (1 - basePosNDC.Y * _scale) * _screenBounds.Height / 2f);
                    float baseDepthScale = (float)(targetToCameraDist / -basePosView.Z);

                    tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "Base_0",
                        Position = basePosPixel,
                        Size = spriteSize * baseDepthScale * _scale,
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt baseSpriteExt = new MySpriteExt(tempSprite, basePosNDC.Z);

                    Vector3D stemPosWorld = 0.5f * (entityPosWorld + basePosWorld);
                    Vector3D stemPosView = Vector3D.Transform(stemPosWorld, viewMatrix);
                    Vector4D stemPosClip = Vector4D.Transform(new Vector4D(stemPosView, 1), _projectionMatrix);
                    Vector3 stemPosNDC = new Vector3(stemPosClip.X / stemPosClip.W, stemPosClip.Y / stemPosClip.W, stemPosClip.Z / stemPosClip.W);
                    Vector2 stemPosPixel = new Vector2((1 + stemPosNDC.X * _scale) * _screenBounds.Width / 2f, (1 - stemPosNDC.Y * _scale) * _screenBounds.Height / 2f);

                    Vector2 stemVector = new Vector2(entityPosPixel.X - basePosPixel.X, entityPosPixel.Y - basePosPixel.Y);
                    float stemLength = stemVector.Length();
                    double stemAngle = Math.Atan2(stemVector.Y, stemVector.X);

                    tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = stemPosPixel,
                        Size = new Vector2(stemLength, 1),
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = (float)stemAngle,
                    };

                    MySpriteExt stemSpriteExt = new MySpriteExt(tempSprite, stemPosNDC.Z);

                    if ((Vector3D.Dot(cameraPositionWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) * (Vector3D.Dot(entityPosWorld, gridPlaneWorld.Normal) + gridPlaneWorld.D) > 0)
                    {
                        _spritesPostPlane.Add(mySpriteExtEntity);
                        _spritesPostPlane.Add(baseSpriteExt);
                        _spritesPostPlane.Add(stemSpriteExt);

                        if (selectorSpriteExt.IsValid)
                        {
                            _spritesPostPlane.Add(selectorSpriteExt);
                        }
                    }
                    else
                    {
                        _spritesPrePlane.Add(mySpriteExtEntity);
                        _spritesPrePlane.Add(baseSpriteExt);
                        _spritesPrePlane.Add(stemSpriteExt);

                        if (selectorSpriteExt.IsValid)
                        {
                            _spritesPrePlane.Add(selectorSpriteExt);
                        }
                    }
                }

                _spritesPrePlane.AddRange(_staticSpritesPrePlane);
                _spritesPrePlane.SortNoAlloc((a, b) => b.Depth.CompareTo(a.Depth));

                _spritesPostPlane.AddRange(_staticSpritesPostPlane);
                _spritesPostPlane.SortNoAlloc((a, b) => b.Depth.CompareTo(a.Depth));

                _finalSprites.AddRange(_spritesPrePlane);
                _finalSprites.AddRange(_planeSprites);
                _finalSprites.AddRange(_spritesPostPlane);
            }

            private void AdjustScopeScale(float requestedDistance)
            {
                float scale;
                if (requestedDistance > 12000)
                {
                    scale = 1.25f;
                    _rangeStr = "15 km";
                }
                else if (requestedDistance > 9000)
                {
                    scale = 1f;
                    _rangeStr = "12 km";
                }
                else if (requestedDistance > 6000)
                {
                    scale = 0.75f;
                    _rangeStr = "9 km";
                }
                else if (requestedDistance > 3000)
                {
                    scale = 0.5f;
                    _rangeStr = "6 km";
                }
                else
                {
                    scale = 0.25f;
                    _rangeStr = "3 km";
                }
                if (scale == _scopeScale) return;
                _scopeScale = scale;
                _projectionMatrix = MatrixD.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_FOV), _AR, _n * _scopeScale, _f * _scopeScale);
                BuildStaticSprites();
            }
        }
    }
}
