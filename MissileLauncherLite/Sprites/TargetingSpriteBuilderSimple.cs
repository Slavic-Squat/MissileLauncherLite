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
        public class TargetingSpriteBuilderSimple
        {
            public IReadOnlyList<MySpriteExt> FinalSprites => _finalSprites;
            public IReadOnlyDictionary<long, MyEntitySprite> EntitySprites => _entitySprites;

            private float _range = 6000f;
            private string _rangeStr = "6 km";
            private RectangleF _bounds;
            private float _scale = 1f;
            private float _resScaleX = 1f;
            private float _resScaleY = 1f;

            private StringBuilder _sb = new StringBuilder();
            private IMyTextSurface _surface;

            private List<MySpriteExt> _sprites = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSprites = new List<MySpriteExt>();
            private List<MySpriteExt> _finalSprites = new List<MySpriteExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();

            public TargetingSpriteBuilderSimple(IMyTextSurface surface, RectangleF bounds, float scale = 1f)
            {
                _surface = surface;
                _resScaleX = bounds.Width / 1024f;
                _resScaleY = bounds.Height / 1024f;
                _bounds = bounds;
                _scale = scale;
                BuildStaticSprites();
            }

            private void BuildStaticSprites()
            {
                MySprite tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Self_1",
                    Position = _bounds.Center,
                    Size = new Vector2(128, 128) * new Vector2(_resScaleX, _resScaleY) * _scale,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt selfSpriteExt = new MySpriteExt(tempSprite, 0.03f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_1",
                    Position = _bounds.Center,
                    Size = _bounds.Size * _scale,
                    Color = Color.Gray,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gridSpriteExt = new MySpriteExt(tempSprite, 0.02f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_1",
                    Position = _bounds.Center,
                    Size = _bounds.Size * _scale,
                    Color = new Color(64, 64, 64, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gradSpriteExt = new MySpriteExt(tempSprite, 0.01f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "StarryBackground",
                    Position = _bounds.Center,
                    Size = (_bounds.Width > 1024 || _bounds.Height > 1024) ? new Vector2(1024, 1024) * new Vector2(_resScaleX, _resScaleY) : new Vector2(1024, 1024),
                    Color = Color.LightGray,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgSpriteExt = new MySpriteExt(tempSprite, -100000f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _bounds.Center,
                    Size = _bounds.Size,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgFillSpriteExt = new MySpriteExt(tempSprite, -100001f);

                MySprite clip = new MySprite()
                {
                    Type = SpriteType.CLIP_RECT,
                    Position = _bounds.Position,
                    Size = _bounds.Size
                };

                _staticSprites.Clear();
                _staticSprites.Add(bgSpriteExt);
                _staticSprites.Add(bgFillSpriteExt);
                _staticSprites.Add(gridSpriteExt);
                _staticSprites.Add(gradSpriteExt);
                _staticSprites.Add(selfSpriteExt);
                _staticSprites.Add(new MySpriteExt(clip, -100002f));
                _staticSprites.Add(new MySpriteExt(MySprite.CreateClearClipRect(), 100000f));
            }

            public void BuildSprites(IReadOnlyDictionary<long, EntityInfoExt> entities, long targetedID = -1)
            {
                _finalSprites.Clear();
                _entitySprites.Clear();
                _sprites.Clear();

                MatrixD referenceWorldMatrix = SystemCoordinator.ReferenceWorldMatrix;

                double farthestDistance = 0;
                foreach (var entity in entities.Values)
                {
                    double distance = Vector3D.Distance(referenceWorldMatrix.Translation, entity.Position);
                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                    }
                }

                AdjustRange((float)farthestDistance);

                Vector2 rangeTextPos = _bounds.Center + new Vector2(-360f, 360f) * new Vector2(_resScaleX, _resScaleY) * _scale;
                MySprite rangeTextSprite = SpriteHelper.CreateText(rangeTextPos, _sb.Clear().Append(_rangeStr), Color.White, _surface, text: _rangeStr, fontID: "Monospace", scale: 2.0f * Math.Min(_resScaleX, _resScaleY) * _scale, alignment: TextAlignment.RIGHT);
                _sprites.Add(new MySpriteExt(rangeTextSprite, 0.01f));

                float pixelsPerMeterX = _bounds.Width * _scale / (2f * _range);
                float pixelsPerMeterY = _bounds.Height * _scale / (2f * _range);

                foreach (var entity in entities.Values)
                {
                    double distance = Vector3D.Distance(referenceWorldMatrix.Translation, entity.Position);

                    if (distance > _range)
                    {
                        continue;
                    }

                    Vector3D entityPosWorld = entity.Position;
                    Vector3D entityPosLocal = Vector3D.TransformNormal(entityPosWorld - referenceWorldMatrix.Translation, MatrixD.Transpose(referenceWorldMatrix.GetOrientation()));
                    Vector2 entityPosPixel = _bounds.Center + new Vector2((float)entityPosLocal.X * pixelsPerMeterX, (float)entityPosLocal.Z * pixelsPerMeterY);

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
                        spriteSize = new Vector2(16, 16) * new Vector2(_resScaleX, _resScaleY);
                    }
                    else
                    {
                        spriteName = "Target_1";
                        spriteSize = new Vector2(32, 32) * new Vector2(_resScaleX, _resScaleY);
                    }

                    double entityAge = SystemCoordinator.GlobalTime - entity.TimeRecorded;
                    float entityAgeFactor = MiscUtilities.Clamp(1 - (float)entityAge / 5f, 0, 1);
                    spriteColor.A = (byte)MiscUtilities.Lerp(0f, 255f, entityAgeFactor);

                    MySprite tempSprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = spriteName,
                        Position = entityPosPixel,
                        Size = spriteSize * _scale,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt MySpriteExtEntity = new MySpriteExt(tempSprite, (float)entityPosLocal.Y);
                    MyEntitySprite entitySprite = new MyEntitySprite(entity, MySpriteExtEntity);

                    _sprites.Add(MySpriteExtEntity);
                    _entitySprites.Add(entity.EntityID, entitySprite);

                    if (entity.EntityID == targetedID)
                    {
                        tempSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "Selector_0",
                            Position = entityPosPixel,
                            Size = MySpriteExtEntity.Sprite.Size * 1.5f * _scale,
                            Color = Color.OrangeRed,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = 0f,
                        };

                        MySpriteExt selectorSpriteExt = new MySpriteExt(tempSprite, (float)entityPosLocal.Y + 0.001f);
                        _sprites.Add(selectorSpriteExt);
                    }

                    if (entity.Type == EntityType.Missile && entities.ContainsKey(entity.Info.MissileInfo.TargetID))
                    {
                        Vector3D targetPosWorld = entities[entity.Info.MissileInfo.TargetID].Position;
                        Vector3D targetPosLocal = Vector3D.TransformNormal(targetPosWorld - referenceWorldMatrix.Translation, MatrixD.Transpose(referenceWorldMatrix.GetOrientation()));
                        Vector2 targetPosPixel = _bounds.Center + new Vector2((float)targetPosLocal.X * pixelsPerMeterX, (float)targetPosLocal.Z * pixelsPerMeterY);

                        Vector3D aimPosLocal = 0.5f * (entityPosLocal + targetPosLocal);
                        Vector2 aimPosPixel = 0.5f * (entityPosPixel + targetPosPixel);
                        Vector2 aimVector = targetPosPixel - entityPosPixel;
                        float aimLength = aimVector.Length();
                        double aimAngle = Math.Atan2(aimVector.Y, aimVector.X);

                        Color aimColor = Color.Gray;

                        switch (entity.Info.MissileInfo.TrackStatus)
                        {
                            case TrackStatus.Unknown:
                                aimColor = Color.Gray;
                                break;
                            case TrackStatus.Idle:
                                aimColor = Color.White;
                                break;
                            case TrackStatus.Aquiring:
                                aimColor = Color.Yellow;
                                break;
                            case TrackStatus.Tracking:
                                aimColor = Color.Green;
                                break;
                            case TrackStatus.Lost:
                                aimColor = Color.OrangeRed;
                                break;
                        }

                        tempSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareSimple",
                            Position = aimPosPixel,
                            Size = new Vector2(aimLength, 1),
                            Color = aimColor,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = (float)aimAngle,
                        };

                        MySpriteExt aimSpriteExt = new MySpriteExt(tempSprite, (float)aimPosLocal.Y);
                        _sprites.Add(aimSpriteExt);
                    }
                }

                _finalSprites.AddRange(_staticSprites);
                _finalSprites.AddRange(_sprites);
                _finalSprites.SortNoAlloc((a, b) => a.Depth.CompareTo(b.Depth));
            }

            private void AdjustRange(float requestedRange)
            {
                if (requestedRange > 12000)
                {
                    _range = 15000f;
                    _rangeStr = "15 km";
                }
                else if (requestedRange > 9000)
                {
                    _range = 12000f;
                    _rangeStr = "12 km";
                }
                else if (requestedRange > 6000)
                {
                    _range = 9000f;
                    _rangeStr = "9 km";
                }
                else if (requestedRange > 3000)
                {
                    _range = 6000f;
                    _rangeStr = "6 km";
                }
                else
                {
                    _range = 3000f;
                    _rangeStr = "3 km";
                }
            }
        }
    }
}
