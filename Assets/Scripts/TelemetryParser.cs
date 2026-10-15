using System;
using Newtonsoft.Json;

namespace RobotFacility.Telemetry
{
    /// <summary>Turns a JSON string into a RawTelemetry without ever throwing.</summary>
    public static class TelemetryParser
    {
        public static bool TryParse(string json, out RawTelemetry raw, out string error)
        {
            raw = null;

            if (string.IsNullOrWhiteSpace(json)) { error = "payload is empty"; return false; }

            try
            {
                raw = JsonConvert.DeserializeObject<RawTelemetry>(json);
            }
            catch (Exception e) when (e is JsonException || e is FormatException || e is OverflowException)
            {
                error = "malformed JSON: " + e.Message;
                return false;
            }

            if (raw == null) { error = "payload deserialised to null"; return false; }
            error = null;
            return true;
        }
    }
}
