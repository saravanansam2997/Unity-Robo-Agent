using UnityEngine;

namespace RobotFacility.Telemetry
{
    /// <summary>A validated, sanitised telemetry record. Safe to use without further checks.</summary>
    public readonly struct TelemetryUpdate
    {
        public readonly string UnitId;
        public readonly string Name;
        /// <summary>World-space position on the XZ plane (x = world X, y = world Z).</summary>
        public readonly Vector2 PositionXZ;
        /// <summary>Degrees in [0,360). 0 = +Z (north), clockwise, matching Unity's Y rotation.</summary>
        public readonly float Heading;
        public readonly float Speed;

        public TelemetryUpdate(string unitId, string name, Vector2 positionXZ, float heading, float speed)
        {
            UnitId = unitId;
            Name = name;
            PositionXZ = positionXZ;
            Heading = heading;
            Speed = speed;
        }
    }
}
