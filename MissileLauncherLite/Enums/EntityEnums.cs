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
        public enum EntityType : byte
        {
            Target, Missile
        }

        public enum EntityRelation : byte
        {
            Neutral, Hostile, Friendly, Me
        }
        public static class EntityEnumHelper
        {
            public static string GetEntityTypeStr(EntityType type)
            {
                switch (type)
                {
                    case EntityType.Target: return "TRGT";
                    case EntityType.Missile: return "MISL";
                    default: return "N/A";
                }
            }

            public static string GetEntityRelationStr(EntityRelation relation)
            {
                switch (relation)
                {
                    case EntityRelation.Neutral: return "NTRL";
                    case EntityRelation.Hostile: return "HSTL";
                    case EntityRelation.Friendly: return "FRND";
                    case EntityRelation.Me: return "ME";
                    default: return "N/A";
                }
            }
        }
    }
}
