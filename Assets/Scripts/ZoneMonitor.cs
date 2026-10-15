using System;
using System.Collections.Generic;
using RobotFacility.Core;
using UnityEngine;

namespace RobotFacility.Zones
{
    public readonly struct ZoneOccupancy
    {
        public readonly RobotController Robot;
        public readonly RestrictedZone Zone;
        public ZoneOccupancy(RobotController robot, RestrictedZone zone) { Robot = robot; Zone = zone; }
    }

    /// <summary>
    /// Detects robots entering/leaving every registered RestrictedZone. Throttled to checkInterval.
    /// Robots in DATA LOST are skipped (their position is stale), so their zone state is simply frozen.
    /// </summary>
    public class ZoneMonitor : MonoBehaviour
    {
        [SerializeField] private FleetManager fleet;
        [SerializeField, Min(0.01f)] private float checkInterval = 0.1f;

        private readonly Dictionary<RobotController, HashSet<RestrictedZone>> inside =
            new Dictionary<RobotController, HashSet<RestrictedZone>>();
        private readonly List<RestrictedZone> scratch = new List<RestrictedZone>();
        private float nextCheck;

        public event Action<RobotController, RestrictedZone> RobotEntered;
        public event Action<RobotController, RestrictedZone> RobotExited;

        private void Update()
        {
            if (fleet == null || Time.time < nextCheck) return;
            nextCheck = Time.time + checkInterval;

            IReadOnlyList<RobotController> robots = fleet.Robots;
            for (int i = 0; i < robots.Count; i++)
            {
                RobotController robot = robots[i];
                if (robot.Status == RobotStatus.DataLost) continue;

                if (!inside.TryGetValue(robot, out HashSet<RestrictedZone> set))
                {
                    set = new HashSet<RestrictedZone>();
                    inside.Add(robot, set);
                }
                Evaluate(robot, set);
            }
        }

        private void Evaluate(RobotController robot, HashSet<RestrictedZone> set)
        {
            // Zones that were disabled/destroyed while occupied -> treat as exited.
            scratch.Clear();
            foreach (RestrictedZone z in set)
                if (z == null || !z.isActiveAndEnabled) scratch.Add(z);
            for (int i = 0; i < scratch.Count; i++)
            {
                set.Remove(scratch[i]);
                if (scratch[i] != null) RobotExited?.Invoke(robot, scratch[i]);
            }

            Vector3 pos = robot.transform.position;
            IReadOnlyList<RestrictedZone> zones = RestrictedZone.All;
            for (int i = 0; i < zones.Count; i++)
            {
                RestrictedZone zone = zones[i];
                if (zone.Contains(pos))
                {
                    if (set.Add(zone)) RobotEntered?.Invoke(robot, zone);
                }
                else if (set.Remove(zone))
                {
                    RobotExited?.Invoke(robot, zone);
                }
            }
        }

        /// <summary>Copies the current (robot, zone) pairs into the caller's buffer (no allocation).</summary>
        public void CopyActive(List<ZoneOccupancy> buffer)
        {
            buffer.Clear();
            foreach (var kv in inside)
                foreach (RestrictedZone zone in kv.Value)
                    if (zone != null) buffer.Add(new ZoneOccupancy(kv.Key, zone));
        }
    }
}
