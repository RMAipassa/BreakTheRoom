# Break The Room - Tool Hit Face Technical Plan

## Purpose
Translate `Docs/Tool_Hit_Face_Design.md` into concrete implementation steps using the current codebase.

Primary target:
- Make hits count only when the correct tool face is used with valid direction/speed.
- Keep both desktop fallback and real VR swing paths working.

---

## Current Integration Points

### Existing runtime scripts
- `Assets/Scripts/Player/DesktopMeleeTool.cs`
  - Holds per-tool damage/impulse/radius and grip alignment.
  - Best place to store `ToolHitFaceProfile` reference.

- `Assets/Scripts/Player/XrDesktopToolInteractor.cs`
  - Handles VR equip/drop and motion-based swing detection.
  - Best place to evaluate hit-face validity for VR swings.

- `Assets/Scripts/Player/DesktopPlaytestController.cs`
  - Handles desktop RMB swing and trace damage.
  - Best place to evaluate hit-face validity for desktop swings.

- `Assets/Scripts/Destruction/BreakablePiece.cs`
  - Applies damage + propagation.
  - Keep as damage receiver; no tool-specific face logic here.

---

## New Types To Add

## 1) `ToolHitFaceProfile` (ScriptableObject)
Path suggestion:
- `Assets/Scripts/Combat/ToolHitFaceProfile.cs`

Fields:
- `toolId` (string)
- `zones` (list of hit zones)

Zone fields:
- `zoneId`
- `shapeType` (`Box`, `Sphere`, `Capsule`)
- `localPosition`
- `localRotation`
- `localScale`
- `minSpeed`
- `axis` (`Forward`, `Right`, `Up`) for directional check
- `minDot` (direction gate)
- `damageMultiplier`
- `impulseMultiplier`
- `allowGlancing`
- `glancingMultiplier`

## 2) `HitFaceZoneRuntime` helper (optional)
Path suggestion:
- `Assets/Scripts/Combat/HitFaceZoneRuntime.cs`

Purpose:
- Convert local zone transforms into world-space checks.

---

## Desktop + VR Hit Validation Flow

For each detected contact (from sphere cast / overlap):

1. Compute impact point + tool velocity direction.
2. Find which zone (if any) contains impact point.
3. Check speed threshold against zone `minSpeed`.
4. Check direction:
   - Convert selected local axis to world direction.
   - `dot = Vector3.Dot(worldAxis, impactVelocity.normalized)`
   - Pass if `dot >= minDot`.
5. If valid:
   - `finalDamage = baseDamage * zone.damageMultiplier`
   - `finalImpulse = baseImpulse * zone.impulseMultiplier`
6. If invalid and `allowGlancing`:
   - apply reduced glancing damage.
7. Send to `BreakablePiece.ApplyDamage(...)`.

---

## Script-by-Script Changes

## `DesktopMeleeTool.cs`
Add:
- `ToolHitFaceProfile hitFaceProfile`
- getter `HitFaceProfile`

Optional debug:
- bool `drawHitFaceGizmos`

## `XrDesktopToolInteractor.cs`
Replace direct unconditional damage with:
- call `TryResolveHitWithFace(tool, point, velocityDir, speed, out damage, out impulse)`
- apply only when valid/glancing

Real VR swing path to update:
- `HandleRealVrSwingHits(...)`

Desktop fallback swing path to update:
- `TraceSwingDamage()` + `BurstSwingHit()`

## `DesktopPlaytestController.cs`
When equipped tool exists:
- use same face-validation helper before applying damage

Note:
- keep non-tool debug LMB ray hit as unrestricted for quick testing.

---

## Authoring Pipeline

1. Create profile assets:
- `Assets/Generated/ToolProfiles/HitFace_Bat.asset`
- `Assets/Generated/ToolProfiles/HitFace_Hammer.asset`
- `Assets/Generated/ToolProfiles/HitFace_Crowbar.asset`

2. Assign profile to generated tool instances in:
- `Assets/Scripts/Editor/ChaosWorldBuilder.cs`

3. Add tool-specific zones:
- Bat: distal barrel zone (large capsule)
- Hammer: front/rear face zones (small boxes)
- Crowbar: hook tip + hook inner curve zones

---

## Debugging / QA Tools

Add debug options:
- draw zone wireframes in scene/game gizmos
- log accepted vs rejected hits with reason:
  - `Rejected: outside zone`
  - `Rejected: speed below min`
  - `Rejected: direction dot < minDot`
- overlay current active zone id on hit

Recommended quick test map cases:
- Table top + legs chain reaction
- Shelf frame collapse chain
- Glass panel edge-only break with slash tool

---

## Suggested Initial Tuning

Crowbar:
- Hook tip `minSpeed=1.3`, `minDot=0.25`, `damageMult=1.0`
- Shaft `minSpeed=1.2`, `minDot=-1`, `damageMult=0.2`

Hammer:
- Face zones `minSpeed=1.1`, `minDot=0.35`, `damageMult=1.2`, `impulseMult=1.4`
- Side zone `damageMult=0.55`

Bat:
- Barrel zone `minSpeed=1.0`, `minDot=0.1`, `damageMult=1.0`
- Handle zone `damageMult=0.15`

---

## Rollout Plan

Phase 1 (current tools only):
1. Add profile types + assignment in builder.
2. Integrate validation in XR and desktop swing paths.
3. Add gizmo/debug logging.

Phase 2 (advanced tools):
1. Add sword + axe profiles with edge checks.
2. Add pierce/thrust zone support.
3. Add material interaction modifiers (wood/glass/metal bonuses).

Phase 3 (polish):
1. Animation/audio differentiation by hit-face zone.
2. Combo scoring for accurate face hits.
