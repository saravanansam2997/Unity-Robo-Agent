using System;
using RobotFacility.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RobotFacility.Interaction
{
    /// <summary>Left-click picks a robot (collider raycast). Escape clears the selection.</summary>
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private LayerMask robotLayers = ~0;
        [SerializeField, Min(1f)] private float maxPickDistance = 500f;

        public RobotController Selected { get; private set; }
        public event Action<RobotController> SelectionChanged;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Select(null);

            if (!Input.GetMouseButtonDown(0) || viewCamera == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxPickDistance, robotLayers))
            {
                RobotController robot = hit.collider.GetComponentInParent<RobotController>();
                if (robot != null) Select(robot);
            }
            // Clicking empty space keeps the selection so camera interaction doesn't lose it.
        }

        public void Select(RobotController robot)
        {
            if (robot == Selected) return;
            if (Selected != null) Selected.SetSelected(false);
            Selected = robot;
            if (Selected != null) Selected.SetSelected(true);
            SelectionChanged?.Invoke(Selected);
        }
    }
}
