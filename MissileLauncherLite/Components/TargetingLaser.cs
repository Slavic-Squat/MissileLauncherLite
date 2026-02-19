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
        public class TargetingLaser
        {
            private Rotor _azimuthRotor;
            private Rotor _elevationRotor;
            private CameraArray _cameraArray;
            private IMyCameraBlock _referenceCamera;

            private float _maxRaycastDistance;
            private MatrixD _referenceMatrix;
            private int _matchingDetectionCounter;
            private int _countThreshold;
            private MyDetectedEntityInfo _previouslyDetectedEntity;
            private double _time;
            private float _sensitivity;
            private bool _isStatic;
            private bool _isAuxiliary;
            private bool _hasAzimuthCtrl;
            private bool _hasElevationCtrl;

            private PIDControl _azimuthPID;
            private PIDControl _elevationPID;

            public string ID { get; private set; }
            public bool TargetSet => Target.IsValid;
            public float MaxRaycastDistance
            {
                get
                {
                    return _maxRaycastDistance;
                }
                set
                {
                    _maxRaycastDistance = value;
                    _cameraArray.MaxRaycastDistance = value * 1.1f;
                }
            }
            public EntityInfoExt Target { get; private set; }
            public bool TargetAquired { get; private set; }
            public event Action<EntityInfoExt> OnTargetUpdated;

            public TargetingLaser(string id, bool isStatic, bool isAuxiliary)
            {
                ID = id.ToUpper();
                _isStatic = isStatic;
                _isAuxiliary = isAuxiliary;
                Init();
            }

            private void Init()
            {
                _maxRaycastDistance = Config.Get($"Laser {ID} Config", "MaxDistance").ToSingle(5000);
                Config.Set($"Laser {ID} Config", "MaxDistance", _maxRaycastDistance);
                _sensitivity = Config.Get($"Laser {ID} Config", "Sensitivity").ToSingle(0.05f);
                Config.Set($"Laser {ID} Config", "Sensitivity", _sensitivity);
                _countThreshold = Config.Get($"Laser {ID} Config", "CountThreshold").ToInt32(2);
                Config.Set($"Laser {ID} Config", "CountThreshold", _countThreshold);

                if (!_isStatic)
                {
                    _hasAzimuthCtrl = Config.Get($"Laser {ID} Config", "HasAzimuthControl").ToBoolean(true);
                    Config.Set($"Laser {ID} Config", "HasAzimuthControl", _hasAzimuthCtrl);
                    _hasElevationCtrl = Config.Get($"Laser {ID} Config", "HasElevationControl").ToBoolean(true);
                    Config.Set($"Laser {ID} Config", "HasElevationControl", _hasElevationCtrl);
                }
                else
                {
                    _hasAzimuthCtrl = false;
                    _hasElevationCtrl = false;
                }

                if (_hasAzimuthCtrl)
                {
                    _azimuthRotor = new Rotor($"LASER {ID} AZIMUTH ROTOR");
                    _azimuthPID = new PIDControl(25, 2, 0.1f);
                }
                else
                {
                    _azimuthRotor = null;
                    _azimuthPID = null;
                }
                if (_hasElevationCtrl)
                {
                    _elevationRotor = new Rotor($"LASER {ID} ELEVATION ROTOR");
                    _elevationPID = new PIDControl(25, 2, 0.1f);
                }
                else
                {
                    _elevationRotor = null;
                    _elevationPID = null;
                }

                _referenceCamera = AllGridBlocks.FirstOrDefault(b => b is IMyCameraBlock && b.CustomName.ToUpper().Contains($"LASER {ID} REFERENCE CAMERA")) as IMyCameraBlock;
                if (_referenceCamera == null)
                {
                    throw new Exception($"Reference Camera for Laser {ID} not found!");
                }
                _cameraArray = new CameraArray($"LASER {ID}", _maxRaycastDistance * 1.1f);
                _cameraArray.AddCamera(_referenceCamera);

                MePb.CustomData = Config.ToString();
            }

            public void Run(double time)
            {
                if (_time == 0)
                {
                    _time = time;
                    return;
                }

                _cameraArray.Update(time);

                _referenceMatrix = _referenceCamera.WorldMatrix;

                if (!TargetSet && !_isStatic)
                {
                    Vector3D vectorToAimAt = SystemCoordinator.ReferencePosition + SystemCoordinator.ReferenceWorldMatrix.Forward * 5000;
                    AimAt(vectorToAimAt, time);
                }
                else if (TargetSet)
                {
                    AutoTrack(time);
                }
                _time = time;
            }

            private void AutoTrack(double time)
            {
                double globalTime = SystemCoordinator.GlobalTime;

                double timeSinceLastDetection = globalTime - Target.TimeRecorded;
                Vector3D estimatedTargetPosition = Target.Position + Target.Velocity * timeSinceLastDetection;
                double estimatedTargetDistance = (estimatedTargetPosition - _referenceMatrix.Translation).Length();

                if (estimatedTargetDistance > _maxRaycastDistance || timeSinceLastDetection > 5)
                {
                    ForgetTarget();
                    return;
                }

                if (!_isStatic)
                {
                    AimAt(estimatedTargetPosition, time);
                }

                FireLaser(estimatedTargetPosition, 10f);
            }

            private void MoveLaser(float azimuthInput = 0, float elevationInput = 0)
            {
                _elevationRotor.VelocityRad = elevationInput * _sensitivity;
                _azimuthRotor.VelocityRad = azimuthInput * _sensitivity;
            }

            public void ForgetTarget()
            {
                Target = default(EntityInfoExt);
                _matchingDetectionCounter = 0;
                TargetAquired = false;
            }

            private void FireLaser(Vector3D raycastTarget, float overshoot)
            {
                double globalTime = SystemCoordinator.GlobalTime;
                if (!_cameraArray.CanScan(raycastTarget, overshoot))
                {
                    return;
                }
                var raycastResult = _cameraArray.Raycast(raycastTarget, overshoot);

                if (!raycastResult.IsEmpty() && (raycastResult.Type == MyDetectedEntityType.SmallGrid || raycastResult.Type == MyDetectedEntityType.LargeGrid))
                {
                    if (!TargetSet && !_isAuxiliary)
                    {
                        if (raycastResult.EntityId == _previouslyDetectedEntity.EntityId)
                        {
                            _matchingDetectionCounter++;
                        }
                        else
                        {
                            _matchingDetectionCounter = 0;
                        }

                        _previouslyDetectedEntity = raycastResult;

                        if (_matchingDetectionCounter >= _countThreshold)
                        {
                            Target = new EntityInfoExt(raycastResult, globalTime);
                            TargetAquired = true;
                            OnTargetUpdated?.Invoke(Target);
                        }
                    }
                    else if (raycastResult.EntityId == Target.EntityID)
                    {
                        Target = new EntityInfoExt(raycastResult, globalTime);
                        TargetAquired = true;
                        OnTargetUpdated?.Invoke(Target);
                    }
                }
            }

            public void FireLaser()
            {
                if (TargetSet) return;
                Vector3D raycastTarget = _referenceMatrix.Translation + _referenceMatrix.Forward * _maxRaycastDistance;
                FireLaser(raycastTarget, 0);
            }

            public void SetTarget(EntityInfoExt target)
            {
                if (target.EntityID != Target.EntityID)
                {
                    Target = target;
                    TargetAquired = false;
                }
                else
                {
                    Target = Target.Merge(target);
                }
            }

            private void AimAt(Vector3D aimTarget, double time)
            {
                double timeDeltaSeconds = time - _time;
                Vector3D aimTargetLocal = Vector3D.TransformNormal(aimTarget - _referenceMatrix.Translation, MatrixD.Transpose(_referenceMatrix));
                double aimTargetDistance = aimTargetLocal.Length();
                Vector3D aimTargetDirLocal = aimTargetDistance == 0 ? Vector3D.Zero : aimTargetLocal / aimTargetDistance;

                float azimuthInput = 0;
                float elevationInput = 0;
                if (_hasAzimuthCtrl)
                {
                    double azimuthError = Math.Atan2(-aimTargetDirLocal.X, -aimTargetDirLocal.Z);
                    azimuthInput = _azimuthPID.Run((float)azimuthError, (float)timeDeltaSeconds) / _sensitivity;
                }
                if (_hasElevationCtrl)
                {
                    double elevationError = Math.Asin(aimTargetDirLocal.Y);
                    elevationInput = _elevationPID.Run((float)elevationError, (float)timeDeltaSeconds) / _sensitivity;
                }

                MoveLaser(azimuthInput, elevationInput);
            }

            public void AppendOverview(StringBuilder sb)
            {
                sb.AppendLine($"[LASER {ID}]");
                sb.Append("  RNG: ").AppendFormat("{0:F0} m", _maxRaycastDistance).AppendLine();
                sb.Append("  STATUS: ");
                if (TargetAquired)
                {
                    sb.Append("LOCKED");
                }
                else if (TargetSet)
                {
                    sb.Append("AQUIRING");
                }
                else
                {
                    sb.Append("IDLE");
                }
            }
        }
    }
}
