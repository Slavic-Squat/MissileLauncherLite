using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
        public struct EntityInfoExt
        {
            public EntityInfo Info { get; private set; }
            public long EntityID => Info.EntityID;
            public Vector3D Position => Info.Position;
            public Vector3D Velocity => Info.Velocity;
            public double TimeRecorded => Info.TimeRecorded;
            public EntityType Type => Info.Type;
            public EntityRelation Relation { get; private set; }
            public bool SourceIsLocal { get; private set; }
            public bool IsValid { get; private set; }

            public EntityInfoExt(EntityInfo info, EntityRelation relation, bool sourceIsLocal)
            {
                Info = info;
                Relation = relation;
                SourceIsLocal = sourceIsLocal;
                IsValid = true;
            }

            public EntityInfoExt(MyDetectedEntityInfo entityInfo, double timeRecorded)
            {
                Info = new EntityInfo(entityInfo, timeRecorded);

                MyRelationsBetweenPlayerAndBlock raycastRelation = entityInfo.Relationship;

                switch (raycastRelation)
                {
                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                        Relation = EntityRelation.Hostile;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Neutral:
                        Relation = EntityRelation.Neutral;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Friends:
                        Relation = EntityRelation.Friendly;
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        Relation = EntityRelation.Me;
                        break;
                    default:
                        Relation = EntityRelation.Neutral;
                        break;
                }
                SourceIsLocal = true;
                IsValid = true;
            }

            public EntityInfoExt Merge(EntityInfoExt other)
            {
                if (EntityID != other.EntityID)
                {
                    return this;
                }
                Info = Info.Merge(other.Info);
                if (!SourceIsLocal && other.SourceIsLocal)
                {
                    Relation = other.Relation;
                    SourceIsLocal = true;
                }
                else if (SourceIsLocal == other.SourceIsLocal && Info.TimeRecorded < other.Info.TimeRecorded)
                {
                    Relation = other.Relation;
                }
                return this;
            }

            public EntityInfoExt MergeKinematics(EntityInfoExt other)
            {
                Info = Info.MergeKinematics(other.Info);
                return this;
            }

            public void AppendInfo(StringBuilder sb)
            {
                sb.AppendLine("[ENTITY INFO]");
                sb.AppendLine("--------------");
                sb.Append("  TYPE: ").AppendLine(EntityEnumHelper.GetEntityTypeStr(Type));
                sb.Append("  REL: ").AppendLine(EntityEnumHelper.GetEntityRelationStr(Relation));
                sb.Append("  SRC: ").AppendLine(SourceIsLocal ? "LOCAL" : "REMOTE");
                double distance = Vector3D.Distance(SystemCoordinator.ReferencePosition, Position);
                sb.Append("  RNG: ");
                UIUtilities.AppendDistance(sb, distance);
                sb.AppendLine();
                double speed = Info.Velocity.Length();
                sb.Append("  SPD: ");
                UIUtilities.AppendDistance(sb, speed);
                sb.AppendLine("/s");
                double age = SystemCoordinator.GlobalTime - Info.TimeRecorded;
                sb.Append("  AGE: ");
                UIUtilities.AppendTime(sb, age);

                if (Info.Type == EntityType.Missile && Info.MissileInfo.IsValid)
                {
                    var missileInfo = Info.MissileInfo;
                    sb.AppendLine();
                    sb.Append("  PAYLOAD: ").AppendLine(MissileEnumHelper.GetMissilePayloadStr(missileInfo.Payload));
                    sb.Append("  STAGE: ").Append(MissileEnumHelper.GetMissileStageStr(missileInfo.Stage));
                }
            }
        }
    }
}
