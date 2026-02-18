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
        public class Rotor
        {
            public bool Inverted { get; private set; }
            public float OffsetDeg { get; private set; }
            public float OffsetRad => MathHelper.ToRadians(OffsetDeg);
            public float MinAngleDeg { get; private set; }
            public float MinAngleRad => MathHelper.ToRadians(MinAngleDeg);
            public float MaxAngleDeg { get; private set; }
            public float MaxAngleRad => MathHelper.ToRadians(MaxAngleDeg);
            public float RangeRad => MaxAngleRad - MinAngleRad;
            public float RangeDeg => MathHelper.ToDegrees(RangeRad);
            public float AngleRad => MathHelper.WrapAngle((Inverted ? RotorBlock.Angle : -RotorBlock.Angle) + OffsetRad);
            public float AngleDeg => MathHelper.ToDegrees(AngleRad);
            public float VelocityRad
            {
                get
                {
                    return Inverted ? RotorBlock.TargetVelocityRad : -RotorBlock.TargetVelocityRad;
                }
                set
                {
                    RotorBlock.TargetVelocityRad = Inverted ? value : -value;
                }
            }
            public float VelocityDeg
            {
                get
                {
                    return MathHelper.ToDegrees(VelocityRad);
                }
                set
                {
                    VelocityRad = MathHelper.ToRadians(value);
                }
            }

            public bool IsMaxed => AngleRad >= MaxAngleRad - 0.05f;
            public bool IsMinned => AngleRad <= MinAngleRad + 0.05f;
            public bool IsSaturated
            {
                get
                {
                    if (VelocityRad > 0)
                    {
                        return IsMaxed;
                    }
                    else if (VelocityRad < 0)
                    {
                        return IsMinned;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public IMyMotorStator RotorBlock { get; private set; }

            private MyIni _config;
            public Rotor(string blockName)
            {
                blockName = blockName.ToUpper();
                RotorBlock = AllGridBlocks.Where(b => b is IMyMotorStator && b.CustomName.ToUpper().Contains(blockName)).FirstOrDefault() as IMyMotorStator;
                if (RotorBlock == null)
                {
                    throw new ArgumentException($"Rotor block '{blockName}' not found!");
                }
                Init();
            }

            public Rotor(IMyMotorStator rotorBlock)
            {
                if (rotorBlock == null)
                {
                    throw new ArgumentException($"Rotor block is null!");
                }
                RotorBlock = rotorBlock;
                Init();
            }

            private void Init()
            {
                _config = new MyIni();

                if (!_config.TryParse(RotorBlock.CustomData))
                {
                    _config.Clear();
                }

                Inverted = _config.Get("Config", "Inverted").ToBoolean(false);
                _config.Set("Config", "Inverted", Inverted);
                OffsetDeg = _config.Get("Config", "OffsetDeg").ToSingle(0);
                _config.Set("Config", "OffsetDeg", OffsetDeg);
                MinAngleDeg = _config.Get("Config", "MinAngleDeg").ToSingle(float.NegativeInfinity);
                _config.Set("Config", "MinAngleDeg", MinAngleDeg);
                MaxAngleDeg = _config.Get("Config", "MaxAngleDeg").ToSingle(float.PositiveInfinity);
                _config.Set("Config", "MaxAngleDeg", MaxAngleDeg);
                RotorBlock.CustomData = _config.ToString();
            }
        }
    }
}
