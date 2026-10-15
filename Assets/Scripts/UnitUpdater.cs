using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Corrected version of the assessment's UnitUpdater. See DEBUGGING_EXERCISE.md for the reasoning.
/// Keeps <see cref="unit"/> at <see cref="target"/>'s position and drops a trail marker every
/// <see cref="markerSpacing"/> metres, recycling the oldest markers once <see cref="maxMarkers"/> is reached.
/// </summary>
public class UnitUpdater : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform unit;            // assigned, not looked up with GameObject.Find
    [SerializeField] private GameObject markerPrefab;
    [SerializeField, Min(0.01f)] private float markerSpacing = 1f;
    [SerializeField, Min(1)] private int maxMarkers = 100;

    private ObjectPool<GameObject> pool;
    private readonly Queue<GameObject> activeMarkers = new Queue<GameObject>();
    private Transform markerRoot;
    private Vector3 lastMarkerPosition;
    private bool hasMarker;
    private float spacingSqr;

    private void Awake()
    {
        if (target == null || unit == null || markerPrefab == null)
        {
            Debug.LogError($"{nameof(UnitUpdater)} on '{name}': target, unit and markerPrefab must be assigned.", this);
            enabled = false;
            return;
        }

        spacingSqr = markerSpacing * markerSpacing;
        markerRoot = new GameObject($"{name}_Markers").transform;

        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(markerPrefab, markerRoot),
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: maxMarkers,
            maxSize: maxMarkers);
    }

    // LateUpdate so we read the target after it has finished moving this frame.
    private void LateUpdate()
    {
        Vector3 position = target.position;
        unit.position = position;

        if (!hasMarker || (position - lastMarkerPosition).sqrMagnitude >= spacingSqr)
            DropMarker(position);
    }

    private void DropMarker(Vector3 position)
    {
        if (activeMarkers.Count >= maxMarkers)
            pool.Release(activeMarkers.Dequeue()); // recycle the oldest instead of growing forever

        GameObject marker = pool.Get();
        marker.transform.SetPositionAndRotation(position, Quaternion.identity);
        activeMarkers.Enqueue(marker);

        lastMarkerPosition = position;
        hasMarker = true;
    }

    private void OnDestroy()
    {
        pool?.Dispose();
        if (markerRoot != null) Destroy(markerRoot.gameObject);
    }
}
