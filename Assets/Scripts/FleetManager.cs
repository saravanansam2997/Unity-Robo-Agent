using System;
using System.Collections.Generic;
using RobotFacility.Telemetry;
using UnityEngine;

namespace RobotFacility.Core
{
    /// <summary>
    /// Owns the set of robots. Pipeline: raw JSON -> parse -> validate -> create-or-update robot.
    /// Also ticks all robots from one loop.
    /// </summary>
    public class FleetManager : MonoBehaviour
    {
        [SerializeField] private TelemetrySourceBase source;
        [SerializeField] private FleetSettings settings;
        [SerializeField] private RobotController robotPrefab;
        [SerializeField] private Transform robotsRoot;
        [SerializeField, Min(1)] private int maxLoggedPayloadChars = 160;

        private readonly Dictionary<string, RobotController> byId = new Dictionary<string, RobotController>();
        private readonly List<RobotController> robots = new List<RobotController>();

        public IReadOnlyList<RobotController> Robots => robots;
        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }

        public event Action<RobotController> RobotAdded;

        private void OnEnable()
        {
            if (source == null || settings == null || robotPrefab == null)
            {
                Debug.LogError("FleetManager: source, settings and robotPrefab must be assigned.", this);
                enabled = false;
                return;
            }
            source.MessageReceived += HandleMessage;
        }

        private void OnDisable()
        {
            if (source != null) source.MessageReceived -= HandleMessage;
        }

        private void Update()
        {
            float now = Time.time, dt = Time.deltaTime;
            for (int i = 0; i < robots.Count; i++) robots[i].Tick(now, dt);
        }

        public bool TryGetRobot(string unitId, out RobotController robot) => byId.TryGetValue(unitId, out robot);

        private void HandleMessage(string json)
        {
            if (!TelemetryParser.TryParse(json, out RawTelemetry raw, out string error) ||
                !TelemetryValidator.TryValidate(raw, settings.worldBounds, settings.maxPlausibleSpeed,
                    out TelemetryUpdate update, out error))
            {
                RejectedCount++;
                Debug.LogWarning($"[Telemetry] Rejected record: {error} | payload: {Truncate(json)}", this);
                return;
            }

            AcceptedCount++;

            if (!byId.TryGetValue(update.UnitId, out RobotController robot))
                robot = CreateRobot(update);

            robot.ApplyTelemetry(update, Time.time);
        }

        private RobotController CreateRobot(in TelemetryUpdate update)
        {
            RobotController robot = Instantiate(robotPrefab, robotsRoot);
            robot.name = update.UnitId;
            robot.Initialise(update.UnitId, update.Name, settings);
            byId.Add(update.UnitId, robot);
            robots.Add(robot);
            RobotAdded?.Invoke(robot);
            return robot;
        }

        private string Truncate(string s)
        {
            if (s == null) return "<null>";
            return s.Length <= maxLoggedPayloadChars ? s : s.Substring(0, maxLoggedPayloadChars) + "...";
        }
    }
}
