using System;
using System.Collections.Generic;
using RobotFacility.Core;
using UnityEngine;

namespace RobotFacility.Safety
{
    public readonly struct ProximityWarning
    {
        public readonly string UnitA, UnitB;
        public readonly float Distance;
        public ProximityWarning(string a, string b, float distance) { UnitA = a; UnitB = b; Distance = distance; }
    }

    /// <summary>
    /// Pairwise distance check on the XZ plane. O(n^2) at a throttled rate is fine for ~10-50 units;
    /// see README for the spatial-hash upgrade path for 100+. Uses hysteresis so a pair hovering at the
    /// threshold does not flicker. DATA LOST robots are ignored.
    /// </summary>
    public class ProximityMonitor : MonoBehaviour
    {
        [SerializeField] private FleetManager fleet;
        [SerializeField, Min(0.1f)] private float warningDistance = 3f;
        [SerializeField, Min(0f), Tooltip("Warning clears only above warningDistance + hysteresis.")]
        private float clearHysteresis = 0.5f;
        [SerializeField, Min(0.01f)] private float checkInterval = 0.1f;

        private sealed class ActivePair
        {
            public RobotController A, B;
            public float Distance;
            public int Pass;
        }

        private readonly Dictionary<(string, string), ActivePair> active = new Dictionary<(string, string), ActivePair>();
        private readonly List<(string, string)> staleKeys = new List<(string, string)>();
        private float nextCheck;
        private int pass;

        public event Action<string, string, float> WarningRaised;
        public event Action<string, string> WarningCleared;

        private void Update()
        {
            if (fleet == null || Time.time < nextCheck) return;
            nextCheck = Time.time + checkInterval;
            pass++;

            float warnSqr = warningDistance * warningDistance;
            float clear = warningDistance + clearHysteresis;
            float clearSqr = clear * clear;

            IReadOnlyList<RobotController> robots = fleet.Robots;
            for (int i = 0; i < robots.Count; i++)
            {
                RobotController a = robots[i];
                if (a.Status == RobotStatus.DataLost) continue;

                for (int j = i + 1; j < robots.Count; j++)
                {
                    RobotController b = robots[j];
                    if (b.Status == RobotStatus.DataLost) continue;

                    float sqr = (a.PositionXZ - b.PositionXZ).sqrMagnitude;
                    (string, string) key = MakeKey(a.UnitId, b.UnitId);

                    if (active.TryGetValue(key, out ActivePair pair))
                    {
                        if (sqr <= clearSqr) { pair.Distance = Mathf.Sqrt(sqr); pair.Pass = pass; }
                        // else: left unstamped -> removed below as cleared
                    }
                    else if (sqr < warnSqr)
                    {
                        float d = Mathf.Sqrt(sqr);
                        active.Add(key, new ActivePair { A = a, B = b, Distance = d, Pass = pass });
                        WarningRaised?.Invoke(key.Item1, key.Item2, d);
                    }
                }
            }

            // Anything not refreshed this pass is out of range, lost, or gone.
            staleKeys.Clear();
            foreach (var kv in active)
                if (kv.Value.Pass != pass) staleKeys.Add(kv.Key);
            for (int i = 0; i < staleKeys.Count; i++)
            {
                active.Remove(staleKeys[i]);
                WarningCleared?.Invoke(staleKeys[i].Item1, staleKeys[i].Item2);
            }
        }

        public void CopyActive(List<ProximityWarning> buffer)
        {
            buffer.Clear();
            foreach (ActivePair p in active.Values)
                buffer.Add(new ProximityWarning(p.A.UnitId, p.B.UnitId, p.Distance));
        }

        private static (string, string) MakeKey(string a, string b) =>
            string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
    }
}
