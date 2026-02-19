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
            private RectangleF _screenBounds;
            private float _resScale = 1f;
            private float _l, _r, _b, _t, _n, _f;
            private MatrixD _projectionMatrix;

            public TargetingHUDSpriteBuilder(IMyTerminalBlock cameraReference, float res, float l, float r, float b, float t, float n, float f)
            {
                _resScale = res / 1024f;
                _screenBounds = new RectangleF(0, 0, res, res);
                _cameraReference = cameraReference;
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
            }

            public void BuildSprites(IReadOnlyDictionary<long, EntityInfoExt> entities, long targetedID = -1)
            {
                _sprites.Clear();
                _finalSprites.Clear();
                _entitySprites.Clear();


                MatrixD cameraFrame = _cameraReference.WorldMatrix;
                MatrixD viewMatrix = MatrixD.CreateLookAt(cameraFrame.Translation, cameraFrame.Translation + cameraFrame.Forward, cameraFrame.Up);

                foreach (var entity in entities.Values)
                {
                    Vector3D entityPosWorld = entity.Position;
                    Vector3D entityPosView = Vector3D.Transform(entityPosWorld, viewMatrix);
                    Vector4D entityPosClip = Vector4D.Transform(new Vector4D(entityPosView, 1), _projectionMatrix);
                    Vector3 entityPosNDC = new Vector3(entityPosClip.X / entityPosClip.W, entityPosClip.Y / entityPosClip.W, entityPosClip.Z / entityPosClip.W);
                    Vector2 entityPosPixel = new Vector2((1 + entityPosNDC.X) * _screenBounds.Width / 2f, (1 - entityPosNDC.Y) * _screenBounds.Height / 2f);
                    float entityDepthScale = (float)(0.5f * _f / -entityPosView.Z);
                    entityDepthScale = MathHelper.Clamp(entityDepthScale, 0.75f, 1.5f);

                    string spriteName = default(string);
                    Vector2 spriteSize = default(Vector2);
                    Color spriteColor = default(Color);

                    switch (entity.Relation)
                    {
                        case EntityRelation.Me:
                            spriteColor = new Color(UIConfig.MeColor, 0.1f);
                            break;
                        case EntityRelation.Neutral:
                            spriteColor = new Color(UIConfig.NeutralColor, 0.1f);
                            break;
                        case EntityRelation.Friendly:
                            spriteColor = new Color(UIConfig.FriendlyColor, 0.1f);
                            break;
                        case EntityRelation.Hostile:
                            spriteColor = new Color(UIConfig.HostileColor, 0.1f);
                            break;
                        default:
                            spriteColor = new Color(Color.White, 0.1f);
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

                    MySpriteExt selectorSpriteExt = default(MySpriteExt);

                    if (entity.EntityID == targetedID)
                    {
                        tempSprite = new MySprite()
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "Selector_0",
                            Position = entityPosPixel,
                            Size = mySpriteExtEntity.Sprite.Size * 1.5f,
                            Color = new Color(Color.OrangeRed, 0.1f),
                            Alignment = TextAlignment.CENTER,
                            RotationOrScale = 0f,
                        };

                        selectorSpriteExt = new MySpriteExt(tempSprite, entityPosNDC.Z - 0.001f);
                        _sprites.Add(selectorSpriteExt);
                    }

                    _finalSprites.AddRange(_staticSprites);
                    _finalSprites.AddRange(_sprites);
                    _finalSprites.SortNoAlloc((a, b) => b.Depth.CompareTo(a.Depth));
                }
            }
        }
    }
}
