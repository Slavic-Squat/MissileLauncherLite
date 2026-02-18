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
        public static class UIUtilities
        {
            public static void AppendTime(StringBuilder sb, double totalSeconds)
            {
                if (double.IsPositiveInfinity(totalSeconds))
                {
                    sb.Append("∞");
                    return;
                }

                TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
                if (timeSpan.Days > 0)
                {
                    sb.Append(timeSpan.Days).Append("d, ").Append(timeSpan.Hours).Append("h");
                }
                else if (timeSpan.Hours > 0)
                {
                    sb.Append(timeSpan.Hours).Append("h, ").Append(timeSpan.Minutes).Append("m");
                }
                else if (timeSpan.Minutes > 0)
                {
                    sb.Append(timeSpan.Minutes).Append("m, ").Append(timeSpan.Seconds).Append("s");
                }
                else if (timeSpan.Seconds > 0)
                {
                    sb.Append(timeSpan.Seconds).Append("s");
                }
                else
                {
                    sb.Append(timeSpan.Milliseconds).Append("ms");
                }
            }

            public static void AppendVolume(StringBuilder sb, double volumeM3)
            {
                if (Math.Abs(volumeM3) >= 1)
                {
                    AppendNumber(sb, volumeM3);
                    sb.Append(" m³");
                }
                else
                {
                    AppendNumber(sb, volumeM3 * 1000);
                    sb.Append(" L");
                }
            }

            public static void AppendPower(StringBuilder sb, double powerWatts)
            {
                if (Math.Abs(powerWatts) >= 1000000)
                {
                    AppendNumber(sb, powerWatts / 1000000);
                    sb.Append(" MW");
                }
                else if (Math.Abs(powerWatts) >= 1000)
                {
                    AppendNumber(sb, powerWatts / 1000);
                    sb.Append(" kW");
                }
                else
                {
                    AppendNumber(sb, powerWatts);
                    sb.Append(" W");
                }
            }

            public static void AppendDistance(StringBuilder sb, double distanceMeters)
            {
                if (Math.Abs(distanceMeters) >= 1000000)
                {
                    AppendNumber(sb, distanceMeters / 1000000);
                    sb.Append(" Mm");
                }
                else if (Math.Abs(distanceMeters) >= 1000)
                {
                    AppendNumber(sb, distanceMeters / 1000);
                    sb.Append(" km");
                }
                else
                {
                    AppendNumber(sb, distanceMeters);
                    sb.Append(" m");
                }
            }

            public static void AppendNumber(StringBuilder sb, double d)
            {
                if (Math.Abs(d) >= 1000000)
                {
                    sb.AppendFormat("{0:F1}", d / 1000000).Append("M");
                }
                else if (Math.Abs(d) >= 1000)
                {
                    sb.AppendFormat("{0:F1}", d / 1000).Append("k");
                }
                else
                {
                    sb.AppendFormat("{0:F1}", d);
                }
            }
        }
    }
}
