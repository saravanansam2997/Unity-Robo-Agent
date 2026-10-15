using System.Collections.Generic;
using UnityEngine;

namespace RobotFacility.Zones
{
    /// <summary>
    /// Reusable oriented-rectangle restricted zone (XZ plane). Drop this on any GameObject to add a zone;
    /// it self-registers, so no robot or monitor code changes are needed.
    /// Size is in world metres; the transform's scale is ignored, position and Y rotation are honoured.
    /// </summary>
    public class RestrictedZone : MonoBehaviour
    {
        private static readonly List<RestrictedZone> AllZones = new List<RestrictedZone>();
        public static IReadOnlyList<RestrictedZone> All => AllZones;

        [SerializeField] private string zoneName = "Restricted Zone";
        [SerializeField] private Vector2 size = new Vector2(10f, 10f);
        [SerializeField, Tooltip("Optional visual (e.g. a flat cube); auto-scaled to the zone size.")]
        private Transform visual;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.9f);

        public string ZoneName => zoneName;

        private void OnEnable() => AllZones.Add(this);
        private void OnDisable() => AllZones.Remove(this);

        public bool Contains(Vector3 worldPosition)
        {
            Vector3 local = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.y * 0.5f;
        }

        private void OnValidate()
        {
            size = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
            if (visual != null)
            {
                Vector3 s = visual.localScale;
                visual.localScale = new Vector3(size.x, s.y, size.y);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.2f, size.y));
        }
    }
}
