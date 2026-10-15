using System.Globalization;
using System.Text;
using RobotFacility.Core;
using RobotFacility.Interaction;
using TMPro;
using UnityEngine;

namespace RobotFacility.UI
{
    /// <summary>Live "UNIT DETAILS" panel for the selected robot, refreshed at a fixed rate.</summary>
    public class DetailsPanel : MonoBehaviour
    {
        [SerializeField] private SelectionController selection;
        [SerializeField] private TMP_Text text;
        [SerializeField, Min(1f)] private float refreshHz = 10f;

        private readonly StringBuilder sb = new StringBuilder(256);
        private float nextRefresh;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private void Update()
        {
            if (text == null || selection == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 1f / refreshHz;

            RobotController r = selection.Selected;
            if (r == null)
            {
                text.text = "UNIT DETAILS\nClick a robot to select it.";
                return;
            }

            float age = Time.time - r.LastReceiveTime;
            sb.Clear();
            sb.AppendLine("UNIT DETAILS");
            sb.Append("ID       : ").AppendLine(r.UnitId);
            sb.Append("Name     : ").AppendLine(r.DisplayName);
            sb.Append("Speed    : ").Append(r.ReportedSpeed.ToString("F1", Inv)).AppendLine(" m/s");
            sb.Append("Heading  : ").Append(r.ReportedHeading.ToString("F0", Inv)).AppendLine(" deg");
            sb.Append("Position : X ").Append(r.ReportedPositionXZ.x.ToString("F1", Inv))
              .Append(" / Z ").AppendLine(r.ReportedPositionXZ.y.ToString("F1", Inv));
            sb.Append("Data Age : ").Append(age.ToString("F1", Inv)).AppendLine(" sec");
            sb.Append("Status   : ").Append(r.Status == RobotStatus.Normal ? "NORMAL" : "DATA LOST");
            text.text = sb.ToString();
        }
    }
}
