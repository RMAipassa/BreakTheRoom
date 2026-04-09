# Break The Room (Unity VR)

This is a production-ready **starter framework** for a VR rage-room game with Teardown-style structural destruction.

## Core idea
- Smash everything with physics-based tools.
- Break objects into chunks and debris.
- Collapse structures by damaging support links.
- Earn chaos score before timer runs out.

## Unity setup
- Unity: `2022.3 LTS` or newer.
- Packages:
  - `XR Interaction Toolkit`
  - `Input System`
  - `XR Plugin Management` (OpenXR)
- Scene requirements:
  - A `ChaosGameManager` in scene.
  - Player rig from XR Interaction Toolkit.
  - Physics objects using `Rigidbody` and colliders.

## Script overview
- `Assets/Scripts/Core/ChaosGameManager.cs`
  - Run timer, state machine, and chaos score.
- `Assets/Scripts/Destruction/BreakablePiece.cs`
  - Health-based break logic, fracture prefab spawning, one-shot destruction event.
- `Assets/Scripts/Destruction/ImpactDamageDealer.cs`
  - Converts collision energy into damage for breakables.
- `Assets/Scripts/Destruction/StructuralLink.cs`
  - Monitors joint stress and snaps supports under force/torque.
- `Assets/Scripts/Gameplay/ScoreOnBreak.cs`
  - Adds score when objects break.
- `Assets/Scripts/Player/VelocityToolDamage.cs`
  - Velocity-based damage for tools (bat, hammer, crowbar).
- `Assets/Scripts/Optimization/DespawnAfterTime.cs`
  - Cleanup for debris to keep VR framerate stable.

## Quick start
1. Create a room with props.
2. Add `BreakablePiece` + `Rigidbody` + collider to each destructible prop.
3. Set `Fracture Prefab` on `BreakablePiece` with pre-fractured chunks (each chunk has collider + rigidbody).
4. Add `ImpactDamageDealer` to props that should damage on collisions.
5. Add `ScoreOnBreak` to breakables and set points.
6. Add `VelocityToolDamage` to tool hitboxes on your VR tools.
7. For beams/walls, connect pieces with joints + `StructuralLink`.
8. Press Play and tune values (health, impact multipliers, stress thresholds).

## One-click world generation
- Open Unity, then run one of these menu actions:
  - `Tools/Break The Room/Create New Scene and Build Starter World`
  - `Tools/Break The Room/Build Starter World In Active Scene`
  - `Tools/Break The Room/Force Rebuild With XRI Rig` (fails fast if XRI rig prefab is missing)
- This generates:
  - Room shell, lighting, and fallback camera rig.
  - Destructible prop field with break + score wiring.
  - Support columns and a crossbeam with structural snap links.
  - Auto-generated fracture prefabs and materials under `Assets/Generated`.
- If needed, remove generated content with `Tools/Break The Room/Delete Generated World`.
- For denser destruction set pieces, run `Tools/Break The Room/Build Teardown-Style Arena` after generating the starter world.
- For XRI + arena in one step, run `Tools/Break The Room/Force Rebuild XRI + Teardown Arena`.
- In desktop XR simulation, an on-screen controls panel appears; toggle it with backquote (`).

## No-headset testing in Editor
- Add simulator helper: `Tools/Break The Room/Add XR Device Simulator`.
- If simulator warnings mention missing action assets, run `Tools/Break The Room/Fix XR Device Simulator Action Assets` after importing XRI samples.
- The generated camera also gets `DesktopPlaytestController` for quick keyboard/mouse testing.
- Starter world fallback rig now includes visible left/right hand meshes for desktop playtesting presence.
- Hand visuals now use simple articulated fingers with open/close pose in desktop testing (`Q`/`E`).
- Breakable chunks now use directional + radial impulse and jitter for less uniform break patterns.
- Destructibles now emit per-surface impact/break particles and procedural audio (wood/glass/concrete/metal).
- Desktop avatar now includes a visible held melee tool; RMB swings the right arm/tool arc for close hits.
- Controls in Play mode:
  - Move: `WASD`
  - Sprint: `Left Shift`
  - Look: mouse (middle-click to lock cursor again)
  - Debug strike: left mouse button (raycast hit damage)
  - Arm swing attack: right mouse button (desktop body right-arm punch/swing)
  - Equip nearby tool: `F`
  - Drop equipped tool: `G`
  - Holster toggle: `1` (primary), `2` (secondary)
  - Hand pose test: `Q` (left hand close), `E` (right hand close)
  - Unlock cursor: `Esc`

## Design notes for a true Teardown feel
- Use many small support pieces rather than one giant wall collider.
- Build structures from modular chunks connected by joints.
- Give supports lower break thresholds than heavy decorative pieces.
- Keep chunks light but use drag and angular drag so debris feels weighty in VR.
- Target 72/90+ FPS: despawn debris aggressively and avoid expensive mesh runtime slicing.

## Next recommended layer
- Procedural mission system (destroy target value within time).
- Economy loop (earn cash, buy better tools/explosives).
- Replay mode with ghost path and combo multipliers.
- Optional bullet-time on major collapses.
