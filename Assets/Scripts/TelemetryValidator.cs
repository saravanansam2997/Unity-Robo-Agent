using UnityEngine;

namespace RobotFacility.Telemetry
{
    /// <summary>
    /// Pure, static validation/sanitisation. No scene dependencies, so it is trivially unit-testable.
    /// </summary>
    public static class TelemetryValidator
    {
        public static bool TryValidate(
            RawTelemetry raw,
            Rect worldBounds,
            float maxSpeed,
            out TelemetryUpdate update,
            out string error)
        {
            update = default;

            if (raw == null) { error = "record is null"; return false; }

            string id = raw.unitId?.Trim();
            if (string.IsNullOrEmpty(id)) { error = "unitId is missing or empty"; return false; }

            if (!raw.x.HasValue || !raw.z.HasValue) { error = "position (x/z) is missing"; return false; }
            if (!IsFinite(raw.x.Value) || !IsFinite(raw.z.Value)) { error = "position is NaN/Infinity"; return false; }

            var pos = new Vector2(raw.x.Value, raw.z.Value);
            if (!worldBounds.Contains(pos))
            {
                error = $"position ({pos.x}, {pos.y}) is outside the world bounds {worldBounds}";
                return false;
            }

            if (!raw.speed.HasValue) { error = "speed is missing"; return false; }
            float speed = raw.speed.Value;
            if (!IsFinite(speed) || speed < 0f) { error = $"speed {speed} is invalid (must be >= 0)"; return false; }
            if (speed > maxSpeed) { error = $"speed {speed} exceeds the plausible maximum {maxSpeed}"; return false; }

            if (!raw.heading.HasValue) { error = "heading is missing"; return false; }
            float heading = raw.heading.Value;
            if (!IsFinite(heading) || heading < 0f || heading > 360f)
            {
                error = $"heading {heading} is invalid (expected 0..360)";
                return false;
            }

            string name = string.IsNullOrWhiteSpace(raw.name) ? id : raw.name.Trim();
            update = new TelemetryUpdate(id, name, pos, Mathf.Repeat(heading, 360f), speed);
            error = null;
            return true;
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
