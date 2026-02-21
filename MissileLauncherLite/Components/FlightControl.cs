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
        public class FlightControl
        {
            private List<ThrusterGroup> _thrusterGroups = new List<ThrusterGroup>();
            private float _shipMass;
            private Dictionary<Direction, float> _maxThrust = new Dictionary<Direction, float>();

            public FlightControlMode FlightControlMode { get; private set; } = FlightControlMode.Free;
            public FlightControl()
            {
                Init();
            }

            private void Init()
            {
                _thrusterGroups.Add(new ThrusterGroup(AllGridBlocks.Where(b => b is IMyThrust && b.CustomName.ToUpper().Contains("THRUSTER GROUP 0")).Select(b => new Thruster(b as IMyThrust)).ToArray()));
                _thrusterGroups.Add(new ThrusterGroup(AllGridBlocks.Where(b => b is IMyThrust && b.CustomName.ToUpper().Contains("THRUSTER GROUP 1")).Select(b => new Thruster(b as IMyThrust)).ToArray()));
                _thrusterGroups.Add(new ThrusterGroup(AllGridBlocks.Where(b => b is IMyThrust && b.CustomName.ToUpper().Contains("THRUSTER GROUP 2")).Select(b => new Thruster(b as IMyThrust)).ToArray()));
                _thrusterGroups.Add(new ThrusterGroup(AllGridBlocks.Where(b => b is IMyThrust && b.CustomName.ToUpper().Contains("THRUSTER GROUP 3")).Select(b => new Thruster(b as IMyThrust)).ToArray()));
                _thrusterGroups.Add(new ThrusterGroup(AllGridBlocks.Where(b => b is IMyThrust && b.CustomName.ToUpper().Contains("THRUSTER GROUP 4")).Select(b => new Thruster(b as IMyThrust)).ToArray()));
                _thrusterGroups.Add(new ThrusterGroup(AllGridBlocks.Where(b => b is IMyThrust && b.CustomName.ToUpper().Contains("THRUSTER GROUP 5")).Select(b => new Thruster(b as IMyThrust)).ToArray()));

                if (_thrusterGroups.Count(tg => tg.Thrusters.Count > 0) == 0)
                {
                    throw new Exception("No thrusters found!");
                }

                SetFlightControlMode(FlightControlMode.Free);

                _shipMass = Config.Get("Config", "Mass").ToSingle(1000000);
                Config.Set("Config", "Mass", _shipMass);

                MatrixD referenceOrientation = SystemCoordinator.ReferenceWorldMatrix.GetOrientation();

                _maxThrust[Direction.Backward] = 0;
                _maxThrust[Direction.Forward] = 0;
                _maxThrust[Direction.Right] = 0;
                _maxThrust[Direction.Left] = 0;
                _maxThrust[Direction.Up] = 0;
                _maxThrust[Direction.Down] = 0;

                foreach (var thrusterGroup in _thrusterGroups)
                {
                    Vector3 thrust = Vector3.TransformNormal(thrusterGroup.Vector, MatrixD.Transpose(referenceOrientation)) * thrusterGroup.MaxThrust;

                    if (thrust.X > 0)
                    {
                        _maxThrust[Direction.Right] += thrust.X;
                    }
                    else if (thrust.X < 0)
                    {
                        _maxThrust[Direction.Left] += -thrust.X;
                    }

                    if (thrust.Y > 0)
                    {
                        _maxThrust[Direction.Up] += thrust.Y;
                    }
                    else if (thrust.Y < 0)
                    {
                        _maxThrust[Direction.Down] += -thrust.Y;
                    }

                    if (thrust.Z > 0)
                    {
                        _maxThrust[Direction.Backward] += thrust.Z;
                    }
                    else if (thrust.Z < 0)
                    {
                        _maxThrust[Direction.Forward] += -thrust.Z;
                    }
                }
            }

            public void Control(UserInput userInput)
            {
                MatrixD referenceOrienation = SystemCoordinator.ReferenceWorldMatrix.GetOrientation();
                switch (FlightControlMode)
                {
                    case FlightControlMode.Free:
                        break;
                    case FlightControlMode.GravComp:
                        _shipMass = SystemCoordinator.ReferenceController.CalculateShipMass().TotalMass;
                        Vector3D accelVector = Vector3D.Zero;
                        if (userInput.WPress)
                        {
                            float maxThrust = _maxThrust[Direction.Forward];
                            accelVector += maxThrust / _shipMass * referenceOrienation.Forward;
                        }
                        else if (userInput.SPress)
                        {
                            float maxThrust = _maxThrust[Direction.Backward];
                            accelVector += maxThrust / _shipMass * referenceOrienation.Backward;
                        }

                        if (userInput.APress)
                        {
                            float maxThrust = _maxThrust[Direction.Left];
                            accelVector += maxThrust / _shipMass * referenceOrienation.Left;
                        }
                        else if (userInput.DPress)
                        {
                            float maxThrust = _maxThrust[Direction.Right];
                            accelVector += maxThrust / _shipMass * referenceOrienation.Right;
                        }

                        if (userInput.SpacePress)
                        {
                            float maxThrust = _maxThrust[Direction.Up];
                            accelVector += maxThrust / _shipMass * referenceOrienation.Up;
                        }
                        else if (userInput.CPress)
                        {
                            float maxThrust = _maxThrust[Direction.Down];
                            accelVector += maxThrust / _shipMass * referenceOrienation.Down;
                        }

                        Vector3D gravVector = SystemCoordinator.ReferenceGravity;
                        if (gravVector.LengthSquared() > 0)
                        {
                            double accelMag = accelVector.Length();
                            Vector3D accelUnit = accelMag != 0 ? accelVector / accelMag : Vector3D.Zero;
                            Vector3D gravCompensation = -gravVector - Vector3D.Dot(-gravVector, accelUnit) * accelUnit;
                            accelVector += gravCompensation;
                        }

                        Vector3D desiredThrustVector = accelVector * _shipMass;
                        foreach (var thrusterGroup in _thrusterGroups)
                        {
                            double value = Vector3D.Dot(desiredThrustVector, thrusterGroup.Vector);
                            if (value < 0) value = 0;
                            thrusterGroup.ThrustOverride = (float)value;
                        }

                        break;

                    default:
                        break;
                }
            }

            public void CycleFlightControlMode()
            {
                FlightControlMode next = MiscEnumHelper.NextFlightControlMode(FlightControlMode);

                SetFlightControlMode(next);
            }

            public void SetFlightControlMode(FlightControlMode mode)
            {
                FlightControlMode = mode;

                switch (mode)
                {
                    case FlightControlMode.Free:
                        foreach (var thrusterGroup in _thrusterGroups)
                        {
                            thrusterGroup.ThrustOverride = 0f;
                        }
                        break;
                    case FlightControlMode.GravComp:
                        break;
                }
            }

            public void AppendOverview(StringBuilder sb)
            {
                sb.AppendLine("[FLIGHT CTRL]");
                sb.Append("  CTRL MODE: ").Append(MiscEnumHelper.GetFlightControlModeStr(FlightControlMode));
            }
        }
    }
}
