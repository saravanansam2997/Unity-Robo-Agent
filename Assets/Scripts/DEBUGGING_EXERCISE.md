# Debugging Exercise - `UnitUpdater`

Original:

```csharp
void Update()
{
    GameObject unit = GameObject.Find("Robot");
    unit.transform.position = target.position;
    Instantiate(marker, unit.transform.position, Quaternion.identity);
}
```

## Problems found (most important for a continuously running app first)

| # | Problem | Why it matters | Fix |
|---|---------|----------------|-----|
| 1 | `Instantiate` **every frame**, never destroyed | At 60 fps that is 3,600 objects/minute. Memory, draw calls and hierarchy grow without bound; GC spikes; eventually the app slows or crashes. Dominant bug for a long-running app. | Spawn only when the unit has moved `markerSpacing` metres; cap the total (`maxMarkers`) and recycle the oldest via an `ObjectPool`. |
| 2 | `GameObject.Find("Robot")` **every frame** | Scans active objects by name each frame: O(scene size), slow. | Cache the reference (serialized field / one-time lookup). |
| 3 | No null checks | `Find` returns `null` if the object is missing, renamed or destroyed, so `unit.transform` throws every frame and floods the log. `target` / `marker` may also be unassigned. | Validate in `Awake`, log one clear error with context, disable the component. |
| 4 | Name-based lookup is fragile | Two objects called "Robot" -> arbitrary winner; a rename silently breaks it. | Inspector reference, or a registry/event. |
| 5 | Work done even when nothing changed | A marker is spawned even if `target` is stationary, stacking identical objects at one point. | Distance gate on squared distance (no sqrt). |
| 6 | Update order | Reading `target.position` in `Update` can be a frame stale if the target moves in another script's `Update`. | Do follow-the-target work in `LateUpdate`. |
| 7 | Cleanup / ownership | Spawned markers have no owner and are never cleaned up when the component goes away. | Parent them under a root created in `Awake`; dispose the pool and destroy the root in `OnDestroy`. |
| 8 | Minor: repeated `unit.transform` reads | Negligible, but redundant. | Cache `position` in a local. |

## Corrected version

See `UnitUpdater.cs`. Key choices:

* `ObjectPool<GameObject>` (`UnityEngine.Pool`, Unity 2021+) with `maxSize = maxMarkers`. The oldest marker is released *before* a new
  one is requested, so the pool never creates or destroys beyond its capacity.
* A `Queue<GameObject>` of active markers gives FIFO recycling.
* Fail-fast validation: a misconfigured component disables itself with one actionable message rather than throwing every frame.

## Trade-off notes

* If "Robot" truly must be found at runtime (e.g. it is spawned dynamically), find it **once** when created, or have the spawner hand
  over the reference via an event - never per frame.
* A `TrailRenderer`/`LineRenderer` is cheaper still if the markers are only a visual breadcrumb trail.
