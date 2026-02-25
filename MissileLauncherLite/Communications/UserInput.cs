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
        public class UserInput
        {
            private double _lastRunTime;
            private IMyShipController _inputBlock;

            public bool WPress { get; private set; } = false;
            private double _secondsWPressed;
            public bool WHeld { get; private set; } = false;
            public bool WRelease { get; private set; } = false;
            public bool WHeldAndReleased { get; private set; } = false;

            public bool APress { get; private set; } = false;
            private double _secondsAPressed;
            public bool AHeld { get; private set; } = false;
            public bool ARelease { get; private set; } = false;
            public bool AHeldAndReleased { get; private set; } = false;

            public bool SPress { get; private set; } = false;
            private double _secondsSPressed;
            public bool SHeld { get; private set; } = false;
            public bool SRelease { get; private set; } = false;
            public bool SHeldAndReleased { get; private set; } = false;

            public bool DPress { get; private set; } = false;
            private double _secondsDPressed;
            public bool DHeld { get; private set; } = false;
            public bool DRelease { get; private set; } = false;
            public bool DHeldAndReleased { get; private set; } = false;

            public bool CPress { get; private set; } = false;
            private double _secondsCPressed;
            public bool CHeld { get; private set; } = false;
            public bool CRelease { get; private set; } = false;
            public bool CHeldAndReleased { get; private set; } = false;

            public bool SpacePress { get; private set; } = false;
            private double _secondsSpacePressed;
            public bool SpaceHeld { get; private set; } = false;
            public bool SpaceRelease { get; private set; } = false;
            public bool SpaceHeldAndReleased { get; private set; } = false;

            public bool QPress { get; private set; } = false;
            private double _secondsQPressed;
            public bool QHeld { get; private set; } = false;
            public bool QRelease { get; private set; } = false;
            public bool QHeldAndReleased { get; private set; } = false;

            public bool EPress { get; private set; } = false;
            private double _secondsEPressed;
            public bool EHeld { get; private set; } = false;
            public bool ERelease { get; private set; } = false;
            public bool EHeldAndReleased { get; private set; } = false;

            public Vector2 MouseInput { get; private set; } = Vector2.Zero;

            public UserInput(IMyShipController inputBlock)
            {
                _inputBlock = inputBlock;
            }

            public void Run(double time)
            {
                if (_lastRunTime == 0)
                {
                    _lastRunTime = time;
                    return;
                }

                ListenForInput(time);
                _lastRunTime = time;
            }

            private void ListenForInput(double time)
            {
                double deltaSeconds = time - _lastRunTime;

                if (_inputBlock.MoveIndicator.Z < 0)
                {
                    WPress = true;
                    _secondsWPressed += deltaSeconds;

                    if (_secondsWPressed > 1)
                    {
                        WHeld = true;
                    }
                }
                else
                {
                    if (WHeld == true)
                    {
                        WHeldAndReleased = true;
                    }
                    else if (WPress == true)
                    {
                        WRelease = true;
                    }
                    else
                    {
                        WRelease = false;
                        WHeldAndReleased = false;
                    }
                    WPress = false;
                    WHeld = false;
                    _secondsWPressed = 0;
                }

                if (_inputBlock.MoveIndicator.Z > 0)
                {
                    SPress = true;
                    _secondsSPressed += deltaSeconds;

                    if (_secondsSPressed > 1)
                    {
                        SHeld = true;
                    }
                }
                else
                {
                    if (SHeld == true)
                    {
                        SHeldAndReleased = true;
                    }
                    else if (SPress == true)
                    {
                        SRelease = true;
                    }
                    else
                    {
                        SRelease = false;
                        SHeldAndReleased = false;
                    }
                    SPress = false;
                    SHeld = false;
                    _secondsSPressed = 0;
                }

                if (_inputBlock.MoveIndicator.X < 0)
                {
                    APress = true;
                    _secondsAPressed += deltaSeconds;

                    if (_secondsAPressed > 1)
                    {
                        AHeld = true;
                    }
                }
                else
                {
                    if (AHeld == true)
                    {
                        AHeldAndReleased = true;
                    }
                    else if (APress == true)
                    {
                        ARelease = true;
                    }
                    else
                    {
                        ARelease = false;
                        AHeldAndReleased = false;
                    }
                    APress = false;
                    AHeld = false;
                    _secondsAPressed = 0;
                }

                if (_inputBlock.MoveIndicator.X > 0)
                {
                    DPress = true;
                    _secondsDPressed += deltaSeconds;

                    if (_secondsDPressed > 1)
                    {
                        DHeld = true;
                    }
                }
                else
                {
                    if (DHeld == true)
                    {
                        DHeldAndReleased = true;
                    }
                    else if (DPress == true)
                    {
                        DRelease = true;
                    }
                    else
                    {
                        DRelease = false;
                        DHeldAndReleased = false;
                    }
                    DPress = false;
                    DHeld = false;
                    _secondsDPressed = 0;
                }

                if (_inputBlock.MoveIndicator.Y < 0)
                {
                    CPress = true;
                    _secondsCPressed += deltaSeconds;

                    if (_secondsCPressed > 0.5f)
                    {
                        CHeld = true;
                    }
                }
                else
                {
                    if (CHeld == true)
                    {
                        CHeldAndReleased = true;
                    }
                    else if (CPress == true)
                    {
                        CRelease = true;
                    }
                    else
                    {
                        CRelease = false;
                        CHeldAndReleased = false;
                    }
                    CPress = false;
                    CHeld = false;
                    _secondsCPressed = 0;
                }

                if (_inputBlock.MoveIndicator.Y > 0)
                {
                    SpacePress = true;
                    _secondsSpacePressed += deltaSeconds;

                    if (_secondsSpacePressed > 1)
                    {
                        SpaceHeld = true;
                    }
                }
                else
                {
                    if (SpaceHeld == true)
                    {
                        SpaceHeldAndReleased = true;
                    }
                    else if (SpacePress == true)
                    {
                        SpaceRelease = true;
                    }
                    else
                    {
                        SpaceRelease = false;
                        SpaceHeldAndReleased = false;
                    }
                    SpacePress = false;
                    SpaceHeld = false;
                    _secondsSpacePressed = 0;
                }

                if (_inputBlock.RollIndicator < 0)
                {
                    QPress = true;
                    _secondsQPressed += deltaSeconds;

                    if (_secondsQPressed > 1)
                    {
                        QHeld = true;
                    }
                }
                else
                {
                    if (QHeld == true)
                    {
                        QHeldAndReleased = true;
                    }
                    else if (QPress == true)
                    {
                        QRelease = true;
                    }
                    else
                    {
                        QRelease = false;
                        QHeldAndReleased = false;
                    }
                    QPress = false;
                    QHeld = false;
                    _secondsQPressed = 0;
                }

                if (_inputBlock.RollIndicator > 0)
                {
                    EPress = true;
                    _secondsEPressed += deltaSeconds;

                    if (_secondsEPressed > 1)
                    {
                        EHeld = true;
                    }
                }
                else
                {
                    if (EHeld == true)
                    {
                        EHeldAndReleased = true;
                    }
                    else if (EPress == true)
                    {
                        ERelease = true;
                    }
                    else
                    {
                        ERelease = false;
                        EHeldAndReleased = false;
                    }
                    EPress = false;
                    EHeld = false;
                    _secondsEPressed = 0;
                }

                MouseInput = _inputBlock.RotationIndicator;
            }
        }
    }
}
