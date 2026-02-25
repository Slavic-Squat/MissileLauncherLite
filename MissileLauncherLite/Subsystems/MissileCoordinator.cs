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
        public class MissileCoordinator
        {
            private Dictionary<string, MissileBay> _missileBays = new Dictionary<string, MissileBay>();
            private HashSet<MissileBay> _selectedBays = new HashSet<MissileBay>();
            private Dictionary<long, long> _addressTargetIDMap = new Dictionary<long, long>();
            private IReadOnlyDictionary<long, EntityInfoExt> _targetInfo = new Dictionary<long, EntityInfoExt>();
            private Dictionary<long, EntityInfoExt> _myMissiles = new Dictionary<long, EntityInfoExt>();
            private IEnumerator<int> _launchCoroutine;
            private double _lastClockSync;
            private double _lastLaunch;
            private double _time;
            private List<long> _addressesToRemove = new List<long>();
            private List<long> _idsToRemove = new List<long>();
            private byte[] _targetBuffer = new byte[256];

            public IReadOnlyDictionary<long, EntityInfoExt> MyMissiles => _myMissiles;
            public IReadOnlyDictionary<string, MissileBay> MissileBays => _missileBays;
            public int NumBays { get; private set; }
            public int NumSelectedBays => _selectedBays.Count;
            public int NumReadyBays => _missileBays.Count(bay => bay.Value.IsSelectable);
            public bool IsLaunching => _launchCoroutine != null;
            public int NumMissiles => _addressTargetIDMap.Count;

            public MissileCoordinator(IReadOnlyDictionary<long, EntityInfoExt> targetInfo)
            {
                _targetInfo = targetInfo;
                Init();
            }

            private void Init()
            {
                NumBays = Config.Get("Missiles", "NumBays").ToInt32(1);
                Config.Set("Missiles", "NumBays", NumBays);
                MePb.CustomData = Config.ToString();

                for (int i = 0; i < NumBays; i++)
                {
                    string id = i.ToString().ToUpper();
                    MissileBay bay = new MissileBay(id);
                    bay.MissileForgot += () => DeselectBay(bay);
                    bay.MissileLaunched += (long missileAddress, long targetID) => RegisterMissileTarget(missileAddress, targetID);
                    _missileBays[id] = bay;
                }

                CommunicationHandlerInst.RegisterTag("MY_MISSILES", true);
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                foreach (var bay in _missileBays.Values)
                {
                    bay.Run(time);
                    if (!bay.IsSelectable && _selectedBays.Contains(bay))
                    {
                        DeselectBay(bay);
                    }
                }

                Receive();

                _addressesToRemove.Clear();
                foreach (var missileAddress in _addressTargetIDMap.Keys)
                {
                    if (!CommunicationHandlerInst.CanReach(missileAddress))
                    {
                        _addressesToRemove.Add(missileAddress);
                    }
                }

                foreach (var address in _addressesToRemove)
                {
                    UnregisterMissile(address);
                }

                _idsToRemove.Clear();
                foreach (var missile in _myMissiles.Values)
                {
                    var missileID = missile.EntityID;
                    if ((time - missile.TimeRecorded) > 5f)
                    {
                        _idsToRemove.Add(missileID);
                    }
                }

                foreach (var id in _idsToRemove)
                {
                    RemoveMissile(id);
                }

                Transmit();

                if ((time - _lastClockSync) > 10f)
                {
                    SyncClocks();
                }

                if (_launchCoroutine != null && !_launchCoroutine.MoveNext())
                {
                    _launchCoroutine = null;
                }
                _time = time;
            }

            private void AddMissile(EntityInfo missile)
            {
                if (missile.Type != EntityType.Missile || !missile.MissileInfo.IsValid) return;

                long key = missile.EntityID;
                EntityRelation relation = EntityRelation.Me;
                EntityInfoExt entityInfoExt = new EntityInfoExt(missile, relation);
                if (!_myMissiles.ContainsKey(key))
                {
                    _myMissiles.Add(key, entityInfoExt);
                }
                else
                {
                    var original = _myMissiles[key];
                    _myMissiles[key] = original.Merge(entityInfoExt);
                }
            }

            private void RemoveMissile(long entityID)
            {
                _myMissiles.Remove(entityID);
            }

            private void RegisterMissileTarget(long address, long targetID)
            {
                _addressTargetIDMap[address] = targetID;
            }

            private void UnregisterMissile(long address)
            {
                _addressTargetIDMap.Remove(address);
            }

            public void SelectBays(params string[] bayIDs)
            {
                foreach (var bayID in bayIDs)
                {
                    MissileBay bay;
                    if (!_missileBays.TryGetValue(bayID, out bay)) return;
                    SelectBay(bay);
                }
            }

            private void SelectBay(MissileBay bay)
            {
                if (!bay.IsSelectable) return;
                _selectedBays.Add(bay);
                bay.IsSelected = true;
            }

            public void DeselectBays(params string[] bayIDs)
            {
                foreach (var bayID in bayIDs)
                {
                    MissileBay bay;
                    if (!_missileBays.TryGetValue(bayID, out bay)) return;
                    DeselectBay(bay);
                }
            }

            private void DeselectBay(MissileBay bay)
            {
                _selectedBays.Remove(bay);
                bay.IsSelected = false;
            }

            public void ToggleBays(params string[] bayIDs)
            {
                foreach (var bayID in bayIDs)
                {
                    MissileBay bay;
                    if (!_missileBays.TryGetValue(bayID, out bay)) return;
                    ToggleBay(bay);
                }
            }

            private void ToggleBay(MissileBay bay)
            {
                if (_selectedBays.Contains(bay))
                {
                    DeselectBay(bay);
                }
                else
                {
                    SelectBay(bay);
                }
            }

            public void DeselectAll()
            {
                foreach (var bay in _selectedBays.ToList())
                {
                    DeselectBay(bay);
                }
            }

            public void SelectAll()
            {
                foreach (var bay in _missileBays.Values)
                {
                    SelectBay(bay);
                }
            }

            public void LaunchMissile(long targetID)
            {
                if (IsLaunching || _selectedBays.Count == 0) return;
                var bay = _selectedBays.First();
                LaunchMissile(bay, targetID);
            }

            private void LaunchMissile(MissileBay bay, long targetID)
            {
                if ((_time - _lastLaunch) < 1f || !_selectedBays.Contains(bay)) return;

                bay.Launch(targetID);
                _lastLaunch = _time;
            }

            public void LaunchMissiles(long targetID)
            {
                if (IsLaunching) return;
                _launchCoroutine = HandleLaunch(targetID);
            }

            private IEnumerator<int> HandleLaunch(long targetID)
            {
                int loopCounter = 0;
                foreach (var bay in _selectedBays.ToList())
                {
                    LaunchMissile(bay, targetID);
                    while ((_time - _lastLaunch) < 1f)
                    {
                        yield return loopCounter++;
                    }
                }
                yield return loopCounter;
            }

            private void SyncClocks()
            {
                _lastClockSync = _time;
                double globalTime = SystemCoordinator.GlobalTime;

                foreach (long address in _addressTargetIDMap.Keys)
                {
                    string command = "SYNC_CLOCK " + globalTime;
                    CommunicationHandlerInst.SendUnicast(command, address, "COMMANDS", true);
                }
            }

            private void AbortMissile(long address)
            {
                if (CommunicationHandlerInst.CanReach(address))
                {
                    string command = "ABORT";
                    CommunicationHandlerInst.SendUnicast(command, address, "COMMANDS", true);
                }
            }

            public void AbortAll()
            {
                foreach (long address in _addressTargetIDMap.Keys)
                {
                    AbortMissile(address);
                }
            }

            public void AppendOverview(StringBuilder sb)
            {
                sb.AppendLine("[MISL COORDINATOR]");
                sb.Append("  SLCTD BAYS: ").Append(NumSelectedBays).Append("/").Append(NumBays).AppendLine();
                sb.Append("  RDY BAYS:  ").Append(NumReadyBays).Append("/").Append(NumBays).AppendLine();
                sb.Append("  TRCKD MISLS: ").Append(NumMissiles).AppendLine();
            }

            private void Transmit()
            {
                foreach (var kvp in _addressTargetIDMap)
                {
                    long address = kvp.Key;
                    long targetID = kvp.Value;

                    if (_targetInfo.ContainsKey(targetID))
                    {
                        int index = 0;
                        int sizeIndex = index++;
                        int bytesWritten = _targetInfo[targetID].Info.Serialize(_targetBuffer, index);
                        index += bytesWritten;
                        _targetBuffer[sizeIndex] = (byte)bytesWritten;
                        if (index > 1)
                        {
                            ImmutableArray<byte> bytes = ImmutableArray.Create(_targetBuffer, 0, index);
                            CommunicationHandlerInst.SendUnicast(bytes, address, "TARGET", true);
                        }
                    }
                }
            }

            private void Receive()
            {
                while (CommunicationHandlerInst.HasMessage("MY_MISSILES", true))
                {
                    MyIGCMessage message;
                    if (CommunicationHandlerInst.TryRetrieveMessage("MY_MISSILES", true, out message))
                    {
                        if (!_addressTargetIDMap.ContainsKey(message.Source))
                        {
                            continue;
                        }
                        ImmutableArray<byte> bytes = message.As<ImmutableArray<byte>>();
                        int index = 0;
                        byte size = bytes[index++];
                        int bytesRead;
                        EntityInfo missile = EntityInfo.Deserialize(bytes, index, out bytesRead);
                        if (!missile.IsValid || size != bytesRead)
                        {
                            continue;
                        }
                        AddMissile(missile);
                    }
                }
            }
        }
    }
}
