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
            private IMyShipConnector _attachment;
            private IMyShipMergeBlock _mergeBlock;
            private IMyProjector _projector;
            private PrintMode _printMode;
            private double _timeSinceLastHandshake;
            private double _lastRunTime;
            private double _lastUpdateTime;
            private double _lastLaunchTime;
            private bool _isSelected;
            private MissilePayload _missilePayload = MissilePayload.Unknown;
            private MissileStage _missileStage = MissileStage.Unknown;
            private long _missileAddress = -1;
            private StringBuilder _cmdSb = new StringBuilder();

            public string ID { get; private set; }
            public BayStatus Status { get; private set; } = BayStatus.Empty;
            public bool IsSelected
            {
                get
                {
                    return _isSelected;
                }
                private set
                {
                    _isSelected = value;
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
                _attachment = AllBlocks.FirstOrDefault(b => b is IMyShipConnector && b.CustomName.ToUpper().Contains($"MISSILE BAY {ID} CONNECTOR")) as IMyShipConnector;
                if (_attachment == null)
                {
                    throw new Exception($"No connector found for Missile Bay {ID}!");
                }

                _printMode = MiscEnumHelper.GetPrintMode(Config.Get($"Missile Bay {ID} Config", "PrintMode").ToString("DISABLED"));
                Config.Set($"Missile Bay {ID} Config", "PrintMode", MiscEnumHelper.GetPrintModeStr(_printMode));

                _mergeBlock = AllBlocks.FirstOrDefault(b => b is IMyShipMergeBlock && b.CustomName.ToUpper().Contains($"MISSILE BAY {ID} MERGE")) as IMyShipMergeBlock;
                if (_mergeBlock == null)
                {
                    throw new Exception($"No merge block found for Missile Bay {ID} in print mode!");
                }

                if (_printMode != PrintMode.Disabled)
                {
                    _projector = AllBlocks.FirstOrDefault(b => b is IMyProjector && b.CustomName.ToUpper().Contains($"MISSILE BAY {ID} PROJECTOR")) as IMyProjector;
                    if (_projector == null)
                    {
                        throw new Exception($"No projector found for Missile Bay {ID} in print mode!");
                    }

                    if (_printMode == PrintMode.Manual)
                    {
                        _projector.Enabled = false;
                    }
                }

                CommandHandlerInst.RegisterCommand("HANDSHAKE_BAY_" + ID, (args) => { if (args.Length > 1) { ReceiveHandshake(args[0], args[1]); } });
                CommandHandlerInst.RegisterCommand("UPDATE_BAY_" + ID, (args) => { if (args.Length > 0) { ReceiveUpdate(args[0]); } });
            }

            private void InitHandshake()
            {
                _timeSinceLastHandshake = SystemTime;
                List<IMyProgrammableBlock> temp = new List<IMyProgrammableBlock>();
                GTS.GetBlocksOfType(temp, pb => pb.CustomName.ToUpper().Contains("MISSILE COMPUTER") && pb.CustomName.ToUpper().Contains($"[BAY {ID}]"));
                if (temp.Count == 0)
                {
                    return;
                }
                _missileComputer = temp[0];
                _missileComputer.Enabled = true;

                _cmdSb.Clear();
                _cmdSb.Append("INIT").Append(" | ");
                _cmdSb.Append("HANDSHAKE ").Append(ID).Append(" ").Append(IGCS.Me).Append(" ").Append(SystemCoordinator.SelfID);
                if (!_missileComputer.TryRun(_cmdSb.ToString()))
                {
                    return;
                }
            }

            private void ReceiveHandshake(string missileAddressStr, string payloadStr)
            {
                if (_attachment.Status == MyShipConnectorStatus.Unconnected)
                {
                    return;
                }
                long missileAddress;
                if (!long.TryParse(missileAddressStr, out missileAddress)) return;
                _missileAddress = missileAddress;
                _missilePayload = MissileEnumHelper.GetMissilePayload(payloadStr);

                Status = BayStatus.Building;
            }

            private void ForgetMissile()
            {
                _missileAddress = -1;
                _missilePayload = MissilePayload.Unknown;
                Status = BayStatus.Empty;
                _missileComputer = null;
            }

            private void RequestUpdate()
            {
                _lastUpdateTime = SystemTime;
                if (_missileComputer == null || _attachment.Status == MyShipConnectorStatus.Unconnected)
                {
                    return;
                }
                _cmdSb.Clear();
                _cmdSb.Append("UPDATE_BAY");
                _missileComputer.TryRun(_cmdSb.ToString());
            }

            public void ReceiveUpdate(string stageStr)
            {
                if (_attachment.Status == MyShipConnectorStatus.Unconnected)
                {
                    return;
                }
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

                if (Status > BayStatus.Projecting && _attachment.Status == MyShipConnectorStatus.Unconnected)
                {
                    ForgetMissile();
                }
                
                switch (Status)
                {
                    case BayStatus.Empty:
                        if ((time - _lastLaunchTime) < 1f)
                        {
                            return;
                        }
                        if (_printMode == PrintMode.Auto)
                        {
                            _projector.Enabled = true;
                            Status = BayStatus.Projecting;
                        }
                        else if (_attachment.Status != MyShipConnectorStatus.Unconnected)
                        {
                            Status = BayStatus.Handshake;
                        }
                        break;
                    case BayStatus.Projecting:
                        if (_projector.RemainingBlocks <= 0 && _attachment.Status != MyShipConnectorStatus.Unconnected)
                        {
                            _projector.Enabled = false;
                            Status = BayStatus.Handshake;
                        }
                        break;
                    case BayStatus.Handshake:
                        if ((time - _timeSinceLastHandshake) > 5f)
                        {
                            InitHandshake();
                        }
                        break;
                    case BayStatus.Building:
                        break;
                    case BayStatus.Fueling:
                        if (_attachment.Status == MyShipConnectorStatus.Connectable)
                        {
                            _attachment.Connect();
                        }
                        break;
                    case BayStatus.Ready:
                        break;
                    case BayStatus.Launching:
                        break;
                    default:
                        break;
                }

                if (Status > BayStatus.Handshake)
                {
                    if (time - _lastUpdateTime > 1f)
                    {
                        RequestUpdate();
                    }
                }
                _lastRunTime = time;
            }

            public void Launch(long targetID)
            {
                if (_isSelected && Status == BayStatus.Ready)
                {
                    double globalTime = SystemCoordinator.GlobalTime;
                    if (!_missileComputer?.TryRun("LAUNCH " + globalTime) ?? true) return;
                    _lastLaunchTime = SystemTime;
                    MissileLaunched?.Invoke(_missileAddress, targetID);
                }
            }

            public void Select()
            {
                _isSelected = true;
            }

            public void Deselect()
            {
                _isSelected = false;
            }

            public void Toggle()
            {
                if (_isSelected)
                {
                    Deselect();
                }
                else
                {
                    Select();
                }
            }

            public void StartPrinting()
            {
                if (_printMode == PrintMode.Manual && Status == BayStatus.Empty)
                {
                    _projector.Enabled = true;
                    Status = BayStatus.Projecting;
                }
            }

            public void StopPrinting()
            {
                if (_printMode == PrintMode.Manual && Status == BayStatus.Projecting)
                {
                    _projector.Enabled = false;
                    Status = BayStatus.Empty;
                }
            }

            public void TogglePrinting()
            {
                if (_printMode == PrintMode.Manual)
                {
                    if (Status == BayStatus.Empty)
                    {
                        StartPrinting();
                    }
                    else if (Status == BayStatus.Projecting)
                    {
                        StopPrinting();
                    }
                }
            }

            public void AppendOverview(StringBuilder sb)
            {
                sb.Append("[BAY ").Append(ID).AppendLine("]");
                sb.Append("  STATUS: ").AppendLine(MiscEnumHelper.GetBayStatusStr(Status));
                sb.Append("  MISL PAYLOAD: ").Append(MissileEnumHelper.GetMissilePayloadStr(_missilePayload));
            }

            public void AppendStatusShort(StringBuilder sb)
            {
                if (_isSelected)
                {
                    sb.Append("-");
                }
                sb.Append("[").Append(ID).Append("]: ").Append(MiscEnumHelper.GetBayStatusStrShort(Status));
            }

            public void AppendPayloadShort(StringBuilder sb)
            {
                if (_isSelected)
                {
                    sb.Append("-");

                }
                sb.Append("[").Append(ID).Append("]: ").Append(MissileEnumHelper.GetMissilePayloadStr(_missilePayload));
            }
        }
    }
}
