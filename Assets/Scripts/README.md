# Robot Test Facility - Real-Time Telemetry Visualiser (Unity)

A small Unity app showing multiple simulated robots driven by a synthetic telemetry feed: dynamic spawning, smooth movement,
selection + live details, free/follow camera, restricted zones, proximity warnings, DATA LOST handling and invalid-data rejection.

> **Status note (please read):** the C# source was written without being compiled in the Unity Editor, and the
> scene/prefabs/UI are **not** included - they must be assembled once using the steps below (~15 minutes). The Windows build
> must be produced from your own Editor. Expect at most small compile-level slips; the architecture is complete.

## 1. Setup

**Unity version:** 2021.3 LTS or newer (uses `UnityEngine.Pool`). Built-in RP or URP both work.

1. Create a 3D project. Package Manager -> add **`com.unity.nuget.newtonsoft-json`**; make sure **TextMeshPro** is imported.
2. Copy scripts into `Assets/Scripts/`. Put `RobotFacility.Runtime.asmdef` in `Assets/Scripts/`, and
   `TelemetryValidatorTests.cs` + `RobotFacility.Tests.asmdef` in `Assets/Tests/EditMode/`. (If the Newtonsoft asmdef reference name
   fails, check the exact name in the package's own `.asmdef`; or delete both asmdefs to compile everything in the default assembly.)
   `UnitUpdater.cs` is the debugging-exercise answer and is independent of the app.
3. Input: scripts use the legacy Input Manager. If the project uses the new Input System, set
   *Project Settings > Player > Active Input Handling* to **Both**.
4. Create **Assets > Create > Robot Facility > Fleet Settings** (defaults are fine).
5. **Robot prefab:** a Cube/Capsule body (with collider) + a small child "front" marker so heading is visible + an inactive child
   ring/disc named *SelectionIndicator*. Add `RobotController`; assign body renderer and indicator. Put it on a layer such as `Robots`.
6. **Scene:**
   * Ground plane (about 100 x 100, centred on origin to match `worldBounds`).
   * Empty `Systems` object with: `MockTelemetrySource`, `FleetManager`, `ZoneMonitor`, `ProximityMonitor`, `SelectionController`
     (assign source / settings / prefab / robotsRoot / fleet references).
   * Two or more empty GameObjects with `RestrictedZone` (name, size, rotation; optional flat semi-transparent cube as `visual`).
   * Main Camera with `CameraRig` (assign the `SelectionController`).
   * Canvas with TMP texts for `DetailsPanel` (1) and `WarningPanel` (2), and optionally a button calling `CameraRig.ToggleFollow`.
7. Press Play. Run **Window > General > Test Runner > EditMode** for the validator tests.

## 2. Controls

| Input | Action |
|-------|--------|
| WASD / Q E | Move camera (Shift = fast) |
| Hold RMB + mouse | Look |
| Scroll | Dolly forward/back |
| LMB on robot | Select (ring shown, panel updates live) |
| F (or UI button) | Toggle follow mode for the selected robot |
| Any move key while following | Leave follow mode (free camera), then click another robot |
| Esc | Clear selection |

## 3. Architecture & data flow

```
TelemetrySourceBase (MockTelemetrySource | future WebSocket/UDP)
   | MessageReceived(json)
   v
FleetManager.HandleMessage
   -> TelemetryParser.TryParse       (Newtonsoft, nullable fields, never throws)
   -> TelemetryValidator.TryValidate (empty id, missing/NaN/out-of-bounds pos, negative/huge speed, bad heading)
   -> reject: Debug.LogWarning with reason + truncated payload ; accept: create-or-update RobotController (by unitId)
   v
FleetManager.Update -> RobotController.Tick(now, dt)   (single loop, no per-robot Update)
   -> data-loss timeout check, dead-reckoned target, SmoothDamp position, SmoothDampAngle yaw
   v
ZoneMonitor / ProximityMonitor (throttled; events + queryable state) -> WarningPanel
SelectionController -> DetailsPanel, CameraRig
```

| Class | Responsibility |
|-------|----------------|
| `RawTelemetry`, `TelemetryUpdate` | Wire DTO vs validated value type |
| `TelemetryParser`, `TelemetryValidator` | Static, pure, unit-tested input hardening |
| `FleetSettings` (ScriptableObject) | Tunables: timeout, smoothing, bounds, max speed |
| `TelemetrySourceBase` | Swappable input boundary |
| `MockTelemetrySource` | Waypoint-driven simulation (no authored paths), 0.5-2 s irregular updates, fault injection |
| `FleetManager` | Dictionary of robots, dynamic creation, ticking |
| `RobotController` | One robot's state, status, smoothing |
| `RestrictedZone` | Reusable zone component; self-registers, so a new zone = a new GameObject |
| `ZoneMonitor`, `ProximityMonitor` | Enter/exit and pair-distance events with hysteresis |
| `SelectionController`, `DetailsPanel`, `WarningPanel`, `CameraRig` | Interaction/UI |

### Assumptions / design decisions

* **Heading:** degrees, 0 = +Z, clockwise (= Unity Y rotation). Valid range 0-360 (360 becomes 0).
* **Data age** uses the *local receive time*, not the payload `timestamp`, so sender/app clock skew cannot corrupt it.
  The payload timestamp is accepted but not used.
* **Smoothing:** the visual target is the last reported position dead-reckoned along heading x speed (capped by
  `maxExtrapolationSeconds`), then critically damped. 0.5-2 s update gaps glide instead of stepping; discontinuities larger than
  `teleportDistance` snap on purpose.
* **DATA LOST:** timeout is `FleetSettings.dataLossTimeoutSeconds`. The robot stops extrapolating, turns grey and is excluded from
  proximity/zone evaluation. Valid telemetry restores NORMAL immediately.
* Robots are never removed once seen (they stay DATA LOST). A removal policy is a small addition in `FleetManager`.
* Missing speed or heading is rejected rather than defaulted, because guessing would show wrong data as live.

## 4. Performance considerations (10 -> 100+ units)

In place: one tick loop instead of N `Update`s; throttled monitors (10 Hz); no steady-state allocation (reused buffers, struct
events, `StringBuilder`, `MaterialPropertyBlock`); UI refreshed at 5-10 Hz; no `Find`/`GetComponent` per frame.

For 100+ units:
1. **Proximity:** O(n^2) is ~5,000 pairs at 100 units (still cheap at 10 Hz) but should move to a **uniform spatial hash**
   (cell size = warning distance, check neighbouring cells only) -> roughly O(n).
2. **Zones:** O(robots x zones); add per-zone AABB pre-check or reuse the grid.
3. **Rendering:** GPU instancing / SRP Batcher with shared materials, LODs, pooling for spawn/despawn.
4. **Movement:** Burst jobs + `TransformAccessArray`, or DOTS for thousands.
5. **Ingest:** network thread -> concurrent queue, drain a bounded number per frame; rate-limit rejection logs.
6. **Verify with the Profiler** (`FleetManager.Update`, monitors, GC.Alloc) - no measurements are included here; none were taken.

## 5. Known limitations

* Not compiled/tested in the Editor; scene, prefabs, UI and Windows build still need to be created (steps above).
* Only parser/validator logic has automated tests; proximity/zone logic is not covered yet (extracting the pair/hysteresis logic into a pure class is the next step).
* Legacy Input API; no warning flashing; no stale-robot removal; rejection logging is not rate-limited.
* Bonus features (trail, playback, robot pooling, networking, profiler note) are not implemented.

## 6. Debugging exercise

See `DEBUGGING_EXERCISE.md` and `UnitUpdater.cs`.

## 7. Likely live-review changes

* Independent DATA LOST timeout: already `FleetSettings.dataLossTimeoutSeconds`; a per-robot override = a serialized field on `RobotController` used in `Tick`.
* New restricted zone: add a GameObject with `RestrictedZone`, no code changes.
* Data flow explanation: section 3.
