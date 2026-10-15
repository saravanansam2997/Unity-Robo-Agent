using UnityEngine;

namespace RobotFacility.Core
{
    /// <summary>Shared tuning data. Create via Assets > Create > Robot Facility > Fleet Settings.</summary>
    [CreateAssetMenu(menuName = "Robot Facility/Fleet Settings", fileName = "FleetSettings")]
    public class FleetSettings : ScriptableObject
    {
        [Header("Data loss")]
        [Tooltip("Seconds without valid telemetry before a robot becomes DATA LOST.")]
        [Min(0.1f)] public float dataLossTimeoutSeconds = 5f;

        [Header("Movement smoothing")]
        [Tooltip("Approximate time (s) for position to catch up with the target.")]
        [Min(0.01f)] public float positionSmoothTime = 0.35f;
        [Min(0.01f)] public float headingSmoothTime = 0.25f;
        [Tooltip("How far ahead (s) a robot may dead-reckon along its heading while waiting for the next update.")]
        [Min(0f)] public float maxExtrapolationSeconds = 1.5f;
        [Tooltip("If the reported position is further than this from the visual one, snap instead of gliding.")]
        [Min(0f)] public float teleportDistance = 15f;

        [Header("Validation")]
        public Rect worldBounds = new Rect(-50f, -50f, 100f, 100f); // x,y == world X,Z
        [Min(0f)] public float maxPlausibleSpeed = 15f;
    }
}
