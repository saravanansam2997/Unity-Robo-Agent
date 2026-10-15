using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RobotFacility.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RobotFacility.Telemetry
{
    /// <summary>
    /// Synthetic feed. Simulates robots driving between random waypoints (no authored routes) and
    /// publishes their state at irregular intervals. Can also inject bad records and silent periods
    /// to exercise the validation and DATA LOST paths.
    /// </summary>
    public class MockTelemetrySource : TelemetrySourceBase
    {
        [SerializeField] private FleetSettings settings;

        [Header("Simulation")]
        [SerializeField, Min(1)] private int robotCount = 12;
        [SerializeField] private Vector2 updateIntervalRange = new Vector2(0.5f, 2f);
        [SerializeField] private Vector2 speedRange = new Vector2(2f, 8f);
        [SerializeField, Min(1f)] private float turnRateDegPerSec = 70f;
        [SerializeField, Tooltip("Robots first report after a random delay in [0, this] so spawning is visible.")]
        private float spawnStaggerSeconds = 4f;
        [SerializeField, Min(0f)] private float edgeMargin = 5f;
        [SerializeField, Tooltip("0 = random seed each run.")] private int seed;

        [Header("Fault injection")]
        [SerializeField, Range(0f, 1f)] private float invalidRecordChance = 0.04f;
        [SerializeField, Range(0f, 1f)] private float dropoutChance = 0.02f;
        [SerializeField, Min(0f), Tooltip("Should exceed the DATA LOST timeout to be visible.")]
        private float dropoutDurationSeconds = 8f;

        private sealed class SimRobot
        {
            public string Id, Name;
            public Vector2 Pos, Target;
            public float Heading, Speed, NextEmit, SilentUntil;
        }

        private readonly List<SimRobot> robots = new List<SimRobot>();

        private void Start()
        {
            if (settings == null) { Debug.LogError("MockTelemetrySource: FleetSettings not assigned.", this); enabled = false; return; }
            if (seed != 0) Random.InitState(seed);

            for (int i = 0; i < robotCount; i++)
            {
                var r = new SimRobot
                {
                    Id = $"BOT_{i + 1:000}",
                    Name = i < 26 ? $"ROBOT_{(char)('A' + i)}" : $"ROBOT_{i + 1}",
                    Pos = RandomPoint(),
                    Heading = Random.Range(0f, 360f),
                    NextEmit = Time.time + Random.Range(0f, spawnStaggerSeconds)
                };
                PickNewTarget(r);
                robots.Add(r);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float now = Time.time;

            for (int i = 0; i < robots.Count; i++)
            {
                SimRobot r = robots[i];
                Simulate(r, dt);

                if (now < r.NextEmit) continue;
                r.NextEmit = now + Random.Range(updateIntervalRange.x, updateIntervalRange.y);

                if (now < r.SilentUntil) continue; // simulating a comms dropout

                if (Random.value < dropoutChance) { r.SilentUntil = now + dropoutDurationSeconds; continue; }

                Publish(Random.value < invalidRecordChance ? BuildInvalid(r) : BuildValid(r));
            }
        }

        private void Simulate(SimRobot r, float dt)
        {
            Vector2 toTarget = r.Target - r.Pos;
            if (toTarget.sqrMagnitude < 4f) { PickNewTarget(r); toTarget = r.Target - r.Pos; }

            float desired = Mathf.Atan2(toTarget.x, toTarget.y) * Mathf.Rad2Deg; // 0 = +Z, clockwise
            r.Heading = Mathf.Repeat(Mathf.MoveTowardsAngle(r.Heading, desired, turnRateDegPerSec * dt), 360f);

            float rad = r.Heading * Mathf.Deg2Rad;
            r.Pos += new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * (r.Speed * dt);
        }

        private void PickNewTarget(SimRobot r)
        {
            r.Target = RandomPoint();
            r.Speed = Random.Range(speedRange.x, speedRange.y);
        }

        private Vector2 RandomPoint()
        {
            Rect b = settings.worldBounds;
            return new Vector2(
                Random.Range(b.xMin + edgeMargin, b.xMax - edgeMargin),
                Random.Range(b.yMin + edgeMargin, b.yMax - edgeMargin));
        }

        private static string BuildValid(SimRobot r)
        {
            var raw = new RawTelemetry
            {
                unitId = r.Id,
                name = r.Name,
                timestamp = DateTime.UtcNow.ToString("s"),
                x = r.Pos.x,
                z = r.Pos.y,
                heading = r.Heading,
                speed = r.Speed
            };
            return JsonConvert.SerializeObject(raw);
        }

        private static string BuildInvalid(SimRobot r)
        {
            JObject o = JObject.Parse(BuildValid(r));
            switch (Random.Range(0, 6))
            {
                case 0: o["unitId"] = ""; break;
                case 1: o["speed"] = -3.5f; break;
                case 2: o["heading"] = 999f; break;
                case 3: o.Remove("x"); break;
                case 4: o["z"] = "not-a-number"; break;
                default: return "{ this is not json";
            }
            return o.ToString(Formatting.None);
        }
    }
}
