using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
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
        public class BayDisplay
        {
            private IMyTextSurface _display;
            private UICoordinator _uiCoordinator;
            private StringBuilder _sb = new StringBuilder();
            private int _pageIndex = 0;
            private int _pageCount;
            private string _pageStr;
            private int _displayMode = 0;
            private int _displayModes = 4;
            private string _displayModeStr;
            private int _rows = 10;
            private int _columns = 2;
            private int _baysPerPage;
            private Action<MissileBay, StringBuilder> _bayStrGetter;

            public BayDisplay(UICoordinator uiCoordinator)
            {
                _uiCoordinator = uiCoordinator;
                Init();
            }

            private void Init()
            {
                _display = (SystemCoordinator.ReferenceController as IMyTextSurfaceProvider).GetSurface(0);

                _baysPerPage = _rows * _columns;
                _pageCount = (int)Math.Ceiling((double)_uiCoordinator.MissileCoordinator.NumBays / _baysPerPage);

                _pageStr = "PAGE " + (_pageIndex + 1) + "/" + _pageCount;

                _bayStrGetter = (bay, sb) => bay.AppendStatusShort(sb);
                _displayModeStr = "STATUS";
            }

            public void Draw()
            {
                _sb.Clear();

                var bayIds = _uiCoordinator.MissileCoordinator.OrderedBays;
                var bays = _uiCoordinator.MissileCoordinator.MissileBays;

                int charCountPre = _sb.Length;
                _sb.Append("[BAY - ").Append(_displayModeStr).Append("]");
                int charCountPost = _sb.Length;

                int rem = 37 - (charCountPost - charCountPre) - _pageStr.Length;
                if (rem > 0)
                {
                    _sb.Append(' ', rem);
                }
                _sb.AppendLine(_pageStr);
                _sb.Append('-', 37);

                for (int i = 0; i < _rows; i++)
                {
                    _sb.Append("\n");
                    for (int j = 0; j < _columns; j++)
                    {
                        _sb.Append(" ");
                        int bayIndex = _pageIndex * _baysPerPage + j * _rows + i;
                        if (bayIndex >= bayIds.Count)
                        {
                            _sb.Append(' ', 16);
                        }
                        else
                        {
                            charCountPre = _sb.Length;
                            _bayStrGetter(bays[bayIds[bayIndex]], _sb);
                            charCountPost = _sb.Length;
                            rem = 16 - (charCountPost - charCountPre);
                            if (rem > 0)
                            {
                                _sb.Append(' ', rem);
                            }
                        }
                        if (j < _columns - 1)
                        {
                            _sb.Append(" |");
                        }
                    }
                }

                _display.WriteText(_sb);
            }

            public void CyclePage()
            {
                _pageIndex = ++_pageIndex % _pageCount;
                _pageStr = "PAGE " + (_pageIndex + 1) + "/" + _pageCount;
            }

            public void CycleDisplayMode()
            {
                _displayMode = ++_displayMode % _displayModes;

                switch (_displayMode)
                {
                    case 0: 
                        _bayStrGetter = (bay, sb) => bay.AppendStatusShort(sb);
                        _displayModeStr = "STATUS";
                        break;
                    case 1: 
                        _bayStrGetter = (bay, sb) => bay.AppendPayloadShort(sb);
                        _displayModeStr = "PAYLOAD";
                        break;
                    case 2: 
                        _bayStrGetter = (bay, sb) => bay.AppendTypeShort(sb);
                        _displayModeStr = "TYPE";
                        break;
                    case 3: 
                        _bayStrGetter = (bay, sb) => bay.AppendGuidanceShort(sb);
                        _displayModeStr = "GUIDANCE";
                        break;
                    default: 
                        _bayStrGetter = (bay, sb) => bay.AppendStatusShort(sb);
                        _displayModeStr = "STATUS";
                        break;
                }
            }
        }
    }
}
