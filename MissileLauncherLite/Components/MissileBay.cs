using Sandbox;
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
        public class MissileBay
        {
            private IMyProgrammableBlock _missileComputer;
            private IMyMechanicalConnectionBlock _attachment;
            private double _timeSinceLastHandshake;
            private double _timeLastUpdate;
            private double _lastRunTime;
            private bool _isSelected;

            private MissileType _missileType = MissileType.Unknown;
            private MissileGuidanceType _missileGuidanceType = MissileGuidanceType.Unknown;
            private MissilePayload _missilePayload = MissilePayload.Unknown;
            private MissileStage _missileStage = MissileStage.Unknown;
            private long _missileAddress = -1;
            private StringBuilder _cmdSb = new StringBuilder();

            public string ID { get; private set; }
            public BayStatus Status { get; private set; } = BayStatus.Empty;
            public bool IsSelectable => Status == BayStatus.Ready;
            public bool IsSelected
            {
                get
                {
                    return _isSelected && IsSelectable;
                }
                private set
                {
                    _isSelected = IsSelectable && value;
                }
            }

            public event Action<long, long> MissileLaunched;

            public MissileBay(string id)
            {
                ID = id.ToUpper();

                Init();
            }

            private void Init()
            {
                _attachment = AllBlocks.FirstOrDefault(b => b is IMyMechanicalConnectionBlock && b.CustomName.ToUpper().Contains($"MISSILE BAY {ID} ATTACHMENT")) as IMyMechanicalConnectionBlock;
                if (_attachment == null)
                {
                    throw new Exception($"No attachment found for Missile Bay {ID}!");
                }

                CommandHandlerInst.RegisterCommand("HANDSHAKE_BAY_" + ID, (args) => { if (args.Length > 3) { ReceiveHandshake(args[0], args[1], args[2], args[3]); } });
                CommandHandlerInst.RegisterCommand("UPDATE_BAY_" + ID, (args) => { if (args.Length > 0) { ReceiveUpdate(args[0]); } });
            }

            private void InitHandshake()
            {
                _timeSinceLastHandshake = SystemTime;

                if (_attachment.TopGrid == null)
                {
                    Status = BayStatus.Empty;
                    return;
                }
                List<IMyProgrammableBlock> temp = new List<IMyProgrammableBlock>();
                GTS.GetBlocksOfType(temp, pb => pb.CubeGrid.EntityId == _attachment.TopGrid.EntityId && pb.CustomName.ToUpper().Contains("MISSILE COMPUTER"));
                if (temp.Count == 0)
                {
                    Status = BayStatus.Empty;
                    return;
                }
                _missileComputer = temp[0];
                _missileComputer.Enabled = true;

                _cmdSb.Clear();
                _cmdSb.Append("INIT").Append(" | ");
                _cmdSb.Append("HANDSHAKE ").Append(ID).Append(" ").Append(IGCS.Me).Append(" ").Append(SystemCoordinator.SelfID);
                if (!_missileComputer.TryRun(_cmdSb.ToString()))
                {
                    Status = BayStatus.Empty;
                    return;
                }
            }

            private void ReceiveHandshake(string missileAddressStr, string typeStr, string guidanceStr, string payloadStr)
            {
                long missileAddress;
                if (!long.TryParse(missileAddressStr, out missileAddress)) return;
                _missileAddress = missileAddress;
                _missileType = MissileEnumHelper.GetMissileType(typeStr);
                _missileGuidanceType = MissileEnumHelper.GetMissileGuidanceType(guidanceStr);
                _missilePayload = MissileEnumHelper.GetMissilePayload(payloadStr);

                Status = BayStatus.Building;
            }

            private void ForgetMissile()
            {
                _missileAddress = -1;
                _missileType = MissileType.Unknown;
                _missileGuidanceType = MissileGuidanceType.Unknown;
                _missilePayload = MissilePayload.Unknown;
                Status = BayStatus.Empty;
                _missileComputer = null;
                Deselect();
            }

            private void RequestUpdate()
            {
                _timeLastUpdate = SystemTime;
                if (_missileComputer == null)
                {
                    Status = BayStatus.Empty;
                    return;
                }
                _cmdSb.Clear();
                _cmdSb.Append("UPDATE_BAY");
                _missileComputer.TryRun(_cmdSb.ToString());
            }

            public void ReceiveUpdate(string stageStr)
            {
                _missileStage = MissileEnumHelper.GetMissileStage(stageStr);

                switch (_missileStage)
                {
                    case MissileStage.Building:
                        Status = BayStatus.Building;
                        break;
                    case MissileStage.Fueling:
                        Status = BayStatus.Fueling;
                        break;
                    case MissileStage.Idle:
                        Status = BayStatus.Ready;
                        break;
                    case MissileStage.Launching:
                        Status = BayStatus.Launching;
                        break;
                }
            }

            public void Run(double time)
            {
                if (_lastRunTime == 0)
                {
                    _lastRunTime = time;
                    return;
                }

                if (Status == BayStatus.Empty && (time - _timeSinceLastHandshake) > 5f && _attachment.TopGrid != null)
                {
                    InitHandshake();
                }
                else if (Status != BayStatus.Empty && (time - _timeLastUpdate) > 1f)
                {
                    RequestUpdate();
                }
                else if (Status != BayStatus.Empty && _attachment.TopGrid == null)
                {
                    ForgetMissile();
                }
                _lastRunTime = time;
            }

            public void Launch(long targetID)
            {
                if (IsSelected)
                {
                    double globalTime = SystemCoordinator.GlobalTime;
                    if (!_missileComputer?.TryRun("LAUNCH " + globalTime) ?? true) return;
                    Status = BayStatus.Launching;
                    _attachment.Detach();
                    Deselect();
                    MissileLaunched?.Invoke(_missileAddress, targetID);
                }
            }

            public void Select()
            {
                IsSelected = true;
            }

            public void Deselect()
            {
                IsSelected = false;
            }

            public void Toggle()
            {
                if (IsSelected)
                {
                    Deselect();
                }
                else
                {
                    Select();
                }
            }

            public void AppendOverview(StringBuilder sb)
            {
                sb.Append("[BAY ").Append(ID).AppendLine("]");
                sb.Append("  STATUS: ").AppendLine(MiscEnumHelper.GetBayStatusStr(Status));
                sb.Append("  MISL TYPE: ").AppendLine(MissileEnumHelper.GetMissileTypeStr(_missileType));
                sb.Append("  MISL GUIDANCE: ").AppendLine(MissileEnumHelper.GetMissileGuidanceStr(_missileGuidanceType));
                sb.Append("  MISL PAYLOAD: ").Append(MissileEnumHelper.GetMissilePayloadStr(_missilePayload));
            }

            public void AppendStatusShort(StringBuilder sb)
            {
                if (IsSelected)
                {
                    sb.Append("-");
                }
                sb.Append("[").Append(ID).Append("]: ").Append(MiscEnumHelper.GetBayStatusStrShort(Status));
            }

            public void AppendPayloadShort(StringBuilder sb)
            {
                if (IsSelected)
                {
                    sb.Append("-");

                }
                sb.Append("[").Append(ID).Append("]: ").Append(MissileEnumHelper.GetMissilePayloadStr(_missilePayload));
            }

            public void AppendTypeShort(StringBuilder sb)
            {
                if (IsSelected)
                {
                    sb.Append("-");
                }
                sb.Append("[").Append(ID).Append("]: ").Append(MissileEnumHelper.GetMissileTypeStr(_missileType));
            }

            public void AppendGuidanceShort(StringBuilder sb)
            {
                if (IsSelected)
                {
                    sb.Append("-");
                }
                sb.Append("[").Append(ID).Append("]: ").Append(MissileEnumHelper.GetMissileGuidanceStr(_missileGuidanceType));
            }
        }
    }
}
