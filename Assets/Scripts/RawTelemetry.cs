using System;

namespace RobotFacility.Telemetry
{
    /// <summary>
    /// Wire-format record exactly as received. Numeric fields are nullable so that a
    /// *missing* value can be told apart from a legitimate 0 (JsonUtility cannot do this).
    /// </summary>
    [Serializable]
    public class RawTelemetry
    {
        public string unitId;
        public string name;
        public string timestamp;
        public float? x;
        public float? z;
        public float? heading;
        public float? speed;
    }
}
