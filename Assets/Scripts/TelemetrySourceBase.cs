using System;
using UnityEngine;

namespace RobotFacility.Telemetry
{
    /// <summary>
    /// Abstraction over "where telemetry comes from". The fleet only depends on this, so the mock can be
    /// swapped for a WebSocket/TCP/UDP source without touching any other code.
    /// Implementations MUST raise the event on the Unity main thread (queue from network threads).
    /// </summary>
    public abstract class TelemetrySourceBase : MonoBehaviour
    {
        /// <summary>Raised with one raw JSON record per message.</summary>
        public event Action<string> MessageReceived;

        protected void Publish(string json) => MessageReceived?.Invoke(json);
    }
}
