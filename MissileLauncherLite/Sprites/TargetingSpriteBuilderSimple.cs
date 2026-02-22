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
            private RectangleF _screenBounds;
            private float _resScale = 1f;

            private StringBuilder _sb = new StringBuilder();
            private IMyTextSurface _surface;

            private List<MySpriteExt> _sprites = new List<MySpriteExt>();
            private List<MySpriteExt> _staticSprites = new List<MySpriteExt>();
            private List<MySpriteExt> _finalSprites = new List<MySpriteExt>();
            private Dictionary<long, MyEntitySprite> _entitySprites = new Dictionary<long, MyEntitySprite>();

            public TargetingSpriteBuilderSimple(IMyTextSurface surface, RectangleF screenBounds)
            {
                _surface = surface;
                _resScale = Math.Max(screenBounds.Width, screenBounds.Height) / 1024f;
                _screenBounds = screenBounds;
                BuildStaticSprites();
            }

            private void BuildStaticSprites()
            {
                MySprite tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Self_1",
                    Position = _screenBounds.Center,
                    Size = new Vector2(128, 128) * _resScale,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt selfSpriteExt = new MySpriteExt(tempSprite, 0.03f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grid_1",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = Color.Gray,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gridSpriteExt = new MySpriteExt(tempSprite, 0.02f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Radial_Grad_1",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = new Color(64, 64, 64, 255),
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt gradSpriteExt = new MySpriteExt(tempSprite, 0.01f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "StarryBackground",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = Color.LightGray,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgSpriteExt = new MySpriteExt(tempSprite, -100000f);

                tempSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = _screenBounds.Center,
                    Size = _screenBounds.Size,
                    Color = Color.Black,
                    Alignment = TextAlignment.CENTER,
                    RotationOrScale = 0f
                };

                MySpriteExt bgFillSpriteExt = new MySpriteExt(tempSprite, -100001f);

                _staticSprites.Clear();
                _staticSprites.Add(bgSpriteExt);
                _staticSprites.Add(bgFillSpriteExt);
                _staticSprites.Add(gridSpriteExt);
                _staticSprites.Add(gradSpriteExt);
                _staticSprites.Add(selfSpriteExt);
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

                _range = farthestDistance > 3000 ? 6000f : 3000f;
                _rangeStr = _range == 6000f ? "6 km" : "3 km";

                Vector2 rangeTextPos = _screenBounds.Position + new Vector2(10f, 10f) * _resScale;
                MySprite rangeTextSprite = SpriteHelper.CreateText(rangeTextPos, _sb.Clear().Append(_rangeStr), Color.White, _surface, text: _rangeStr, fontID: "Monospace", scale: 1.5f * _resScale);
                _sprites.Add(new MySpriteExt(rangeTextSprite, 0.01f));

                float pixelsPerMeter = _screenBounds.Width / (2f * _range);

                foreach (var entity in entities.Values)
                {
                    double distance = Vector3D.Distance(referenceWorldMatrix.Translation, entity.Position);

                    if (distance > _range)
                    {
                        continue;
                    }

                    Vector3D entityPosWorld = entity.Position;
                    Vector3D entityPosLocal = Vector3D.TransformNormal(entityPosWorld - referenceWorldMatrix.Translation, MatrixD.Transpose(referenceWorldMatrix.GetOrientation()));
                    Vector2 entityPosPixel = _screenBounds.Center + new Vector2((float)entityPosLocal.X * pixelsPerMeter, (float)entityPosLocal.Z * pixelsPerMeter);

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
                        Size = spriteSize,
                        Color = spriteColor,
                        Alignment = TextAlignment.CENTER,
                        RotationOrScale = 0f,
                    };

                    MySpriteExt MySpriteExtEntity = new MySpriteExt(tempSprite, (float)entityPosLocal.Y);
                    MyEntitySprite entitySprite = new MyEntitySprite(entity, MySpriteExtEntity);

                    _entitySprites.Add(entity.EntityID, entitySprite);

                    MySpriteExt selectorSpriteExt = default(MySpriteExt);

                    if (entity.EntityID == targetedID)
                    {
                        tempSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "Selector_0",
                            Position = entityPosPixel,
                            Size = MySpriteExtEntity.Sprite.Size * 1.5f,
                            Color = Color.OrangeRed,
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = 0f,
                        };

                        selectorSpriteExt = new MySpriteExt(tempSprite, (float)entityPosLocal.Y + 0.001f);
                    }

                    _sprites.Add(MySpriteExtEntity);

                    if (selectorSpriteExt.IsValid)
                    {
                        _sprites.Add(selectorSpriteExt);
                    }
                }

                _finalSprites.AddRange(_staticSprites);
                _finalSprites.AddRange(_sprites);
                _finalSprites.SortNoAlloc((a, b) => a.Depth.CompareTo(b.Depth));
            }
        }
    }
}
