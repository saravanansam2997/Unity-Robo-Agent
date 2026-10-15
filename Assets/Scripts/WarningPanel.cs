using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RobotFacility.Core;
using RobotFacility.Safety;
using RobotFacility.Zones;
using TMPro;
using UnityEngine;

namespace RobotFacility.UI
{
    /// <summary>
    /// Shows (a) the live list of active warnings (zone occupancy + proximity pairs with distance) and
    /// (b) a short rolling event log. Active warnings are polled at a low rate; the log is event-driven.
    /// </summary>
    public class WarningPanel : MonoBehaviour
    {
        [SerializeField] private ZoneMonitor zoneMonitor;
        [SerializeField] private ProximityMonitor proximityMonitor;
        [SerializeField] private TMP_Text activeText;
        [SerializeField] private TMP_Text logText;
        [SerializeField, Min(1f)] private float refreshHz = 5f;
        [SerializeField, Min(1)] private int maxLogLines = 8;

        private readonly List<ZoneOccupancy> zoneBuffer = new List<ZoneOccupancy>();
        private readonly List<ProximityWarning> proxBuffer = new List<ProximityWarning>();
        private readonly Queue<string> log = new Queue<string>();
        private readonly StringBuilder sb = new StringBuilder(512);
        private float nextRefresh;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private void OnEnable()
        {
            if (zoneMonitor != null) { zoneMonitor.RobotEntered += OnEnter; zoneMonitor.RobotExited += OnExit; }
            if (proximityMonitor != null) { proximityMonitor.WarningRaised += OnProxRaised; proximityMonitor.WarningCleared += OnProxCleared; }
        }

        private void OnDisable()
        {
            if (zoneMonitor != null) { zoneMonitor.RobotEntered -= OnEnter; zoneMonitor.RobotExited -= OnExit; }
            if (proximityMonitor != null) { proximityMonitor.WarningRaised -= OnProxRaised; proximityMonitor.WarningCleared -= OnProxCleared; }
        }

        private void Update()
        {
            if (activeText == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1f / refreshHz;

            sb.Clear();
            sb.AppendLine("ACTIVE WARNINGS");
            int count = 0;

            if (zoneMonitor != null)
            {
                zoneMonitor.CopyActive(zoneBuffer);
                for (int i = 0; i < zoneBuffer.Count; i++, count++)
                    sb.Append("ZONE  ").Append(zoneBuffer[i].Robot.UnitId).Append(" in ").AppendLine(zoneBuffer[i].Zone.ZoneName);
            }
            if (proximityMonitor != null)
            {
                proximityMonitor.CopyActive(proxBuffer);
                for (int i = 0; i < proxBuffer.Count; i++, count++)
                    sb.Append("PROX  ").Append(proxBuffer[i].UnitA).Append(" <-> ").Append(proxBuffer[i].UnitB)
                      .Append("  ").Append(proxBuffer[i].Distance.ToString("F1", Inv)).AppendLine(" m");
            }
            if (count == 0) sb.Append("none");
            activeText.text = sb.ToString();
        }

        private void OnEnter(RobotController r, RestrictedZone z) => Log($"{r.UnitId} ENTERED {z.ZoneName}");
        private void OnExit(RobotController r, RestrictedZone z) => Log($"{r.UnitId} left {z.ZoneName}");
        private void OnProxRaised(string a, string b, float d) => Log($"PROXIMITY {a} / {b} at {d.ToString("F1", Inv)} m");
        private void OnProxCleared(string a, string b) => Log($"proximity cleared {a} / {b}");

        private void Log(string message)
        {
            log.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
            while (log.Count > maxLogLines) log.Dequeue();
            if (logText != null) logText.text = "EVENT LOG\n" + string.Join("\n", log);
        }
    }
}
