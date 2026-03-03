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
        public class UICoordinator
        {
            private SystemCoordinator _systemCoordinator;
            private Dictionary<long, EntityInfoExt> _allEntities = new Dictionary<long, EntityInfoExt>();
            private TargetingDisplays _targetingDisplays;
            private HUD _hud;
            private BayDisplay _bayDisplay;

            public IReadOnlyDictionary<long, EntityInfoExt> Targets => _systemCoordinator.TargetCoordinator.Targets;
            public IReadOnlyDictionary<long, EntityInfoExt> MyMissiles => _systemCoordinator.MissileCoordinator.MyMissiles;
            public IReadOnlyDictionary<long, EntityInfoExt> AllEntities => _allEntities;
            public MissileCoordinator MissileCoordinator => _systemCoordinator.MissileCoordinator;
            public TargetCoordinator TargetCoordinator => _systemCoordinator.TargetCoordinator;
            public FlightControl FlightControl => _systemCoordinator.FlightControl;
            public IReadOnlyDictionary<string, MissileBay> MissileBays => _systemCoordinator.MissileCoordinator.MissileBays;
            public IReadOnlyList<string> OrderedBays => _systemCoordinator.MissileCoordinator.OrderedBays;
            public IReadOnlyDictionary<string, TargetingLaser> TargetingLasers => _systemCoordinator.TargetCoordinator.TargetingLasers;

            private int _runCounter = 0;

            public UICoordinator(SystemCoordinator systemCoordinator)
            {
                _systemCoordinator = systemCoordinator;
                _targetingDisplays = new TargetingDisplays(this);
                _hud = new HUD(this);
                _bayDisplay = new BayDisplay(this);
            }

            public void Run()
            {
                if (_runCounter >= int.MaxValue) _runCounter = 0;
                _runCounter++;

                _allEntities.Clear();
                foreach (var kvp in Targets)
                {
                    _allEntities[kvp.Key] = kvp.Value;
                }
                foreach (var kvp in MyMissiles)
                {
                    _allEntities[kvp.Key] = kvp.Value;
                }

                if (_runCounter % 5 == 0)
                {
                    _targetingDisplays.Draw();
                    _hud.Draw();
                    _bayDisplay.Draw();
                }
            }

            public void CyclePage()
            {
                _bayDisplay.CyclePage();
            }

            public void CycleDisplayMode()
            {
                _bayDisplay.CycleDisplayMode();
            }
        }
    }
}
