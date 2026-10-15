using System;
using RobotFacility.Telemetry;
using UnityEngine;

namespace RobotFacility.Core
{
    public enum RobotStatus { Normal, DataLost }

    /// <summary>
    /// Visual + state of ONE robot. It has no Update(): FleetManager ticks every robot from a single loop,
    /// which keeps per-unit overhead tiny and makes ordering deterministic.
    ///
    /// Smoothing: the target is the last reported position dead-reckoned forward along the reported
    /// heading/speed (capped), then position and yaw are critically-damped toward it. Irregular update
    /// intervals therefore never cause a jump; large discontinuities snap (teleportDistance).
    /// </summary>
    public class RobotController : MonoBehaviour
    {
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.75f, 0.3f);
        [SerializeField] private Color dataLostColor = new Color(0.5f, 0.5f, 0.5f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private FleetSettings settings;
        private MaterialPropertyBlock block;
        private Vector3 positionVelocity;
        private float headingVelocity;
        private Vector2 reportedPosition;
        private float groundY;
        private bool hasTelemetry;

        // Last accepted telemetry (what the UI shows).
        public string UnitId { get; private set; }
        public string DisplayName { get; private set; }
        public float ReportedSpeed { get; private set; }
        public float ReportedHeading { get; private set; }
        public Vector2 ReportedPositionXZ => reportedPosition;
        public float LastReceiveTime { get; private set; }
        public RobotStatus Status { get; private set; } = RobotStatus.Normal;

        /// <summary>Visual position on the XZ plane (what proximity/zone checks use).</summary>
        public Vector2 PositionXZ => new Vector2(transform.position.x, transform.position.z);

        public event Action<RobotController, RobotStatus> StatusChanged;

        public void Initialise(string unitId, string displayName, FleetSettings fleetSettings)
        {
            UnitId = unitId;
            DisplayName = displayName;
            settings = fleetSettings;
            groundY = transform.position.y;
            block = new MaterialPropertyBlock();
            SetSelected(false);
            ApplyColor();
        }

        public void SetSelected(bool selected)
        {
            if (selectionIndicator != null) selectionIndicator.SetActive(selected);
        }

        public void ApplyTelemetry(in TelemetryUpdate u, float now)
        {
            DisplayName = u.Name;
            ReportedSpeed = u.Speed;
            ReportedHeading = u.Heading;
            reportedPosition = u.PositionXZ;
            LastReceiveTime = now;

            bool snap = !hasTelemetry ||
                        Vector2.Distance(PositionXZ, reportedPosition) > settings.teleportDistance;
            if (snap)
            {
                transform.SetPositionAndRotation(
                    new Vector3(reportedPosition.x, groundY, reportedPosition.y),
                    Quaternion.Euler(0f, u.Heading, 0f));
                positionVelocity = Vector3.zero;
                headingVelocity = 0f;
            }
            hasTelemetry = true;

            if (Status != RobotStatus.Normal) SetStatus(RobotStatus.Normal); // recovery
        }

        /// <summary>Called once per frame by FleetManager.</summary>
        public void Tick(float now, float dt)
        {
            if (!hasTelemetry) return;

            if (Status == RobotStatus.Normal && now - LastReceiveTime > settings.dataLossTimeoutSeconds)
                SetStatus(RobotStatus.DataLost);

            Vector3 target;
            if (Status == RobotStatus.Normal)
            {
                float age = Mathf.Min(now - LastReceiveTime, settings.maxExtrapolationSeconds);
                float rad = ReportedHeading * Mathf.Deg2Rad;
                Vector2 ahead = reportedPosition + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * (ReportedSpeed * age);
                target = new Vector3(ahead.x, groundY, ahead.y);
            }
            else
            {
                target = transform.position; // stale data: stop extrapolating and come to rest
            }

            Vector3 pos = Vector3.SmoothDamp(transform.position, target, ref positionVelocity,
                settings.positionSmoothTime, Mathf.Infinity, dt);

            float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, ReportedHeading, ref headingVelocity,
                settings.headingSmoothTime, Mathf.Infinity, dt);

            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        }

        private void SetStatus(RobotStatus status)
        {
            Status = status;
            ApplyColor();
            StatusChanged?.Invoke(this, status);
        }

        private void ApplyColor()
        {
            if (bodyRenderer == null) return;
            Color c = Status == RobotStatus.Normal ? normalColor : dataLostColor;
            bodyRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c); // URP/HDRP
            block.SetColor(ColorId, c);     // Built-in
            bodyRenderer.SetPropertyBlock(block);
        }
    }
}
