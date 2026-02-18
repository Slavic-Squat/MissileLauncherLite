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
        public struct MySpriteExt
        {
            public Vector2 Pos { get; private set; }
            public MySprite Sprite { get; private set; }
            public float Depth { get; private set; }
            public bool IsValid { get; private set; }
            public MySpriteExt(MySprite sprite, float depth)
            {
                Sprite = sprite;
                Depth = depth;
                Pos = sprite.Position.Value;
                IsValid = true;
            }

            public void Draw(MySpriteDrawFrame frame)
            {
                frame.Add(Sprite);
            }
        }
    }
}
