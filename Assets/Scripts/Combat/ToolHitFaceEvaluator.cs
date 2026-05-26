using UnityEngine;
using BreakTheRoom.Destruction;

namespace BreakTheRoom.Combat
{
    public static class ToolHitFaceEvaluator
    {
        public enum RejectReason
        {
            None,
            NoProfileZones,
            OutsideZones,
            BelowMinSpeed,
            WrongDirection
        }

        public static bool TryEvaluate(
            ToolHitFaceProfile profile,
            Transform toolTransform,
            Vector3 hitPoint,
            Vector3 velocityDirection,
            float speed,
            out float damageMultiplier,
            out float impulseMultiplier,
            out string zoneId,
            DestructionFeedback.SurfaceType surfaceType = DestructionFeedback.SurfaceType.Generic)
        {
            return TryEvaluate(profile, toolTransform, hitPoint, velocityDirection, speed, out damageMultiplier, out impulseMultiplier, out zoneId, out _, surfaceType);
        }

        public static bool TryEvaluate(
            ToolHitFaceProfile profile,
            Transform toolTransform,
            Vector3 hitPoint,
            Vector3 velocityDirection,
            float speed,
            out float damageMultiplier,
            out float impulseMultiplier,
            out string zoneId,
            out RejectReason rejectReason,
            DestructionFeedback.SurfaceType surfaceType = DestructionFeedback.SurfaceType.Generic)
        {
            damageMultiplier = 1f;
            impulseMultiplier = 1f;
            zoneId = "default";
            rejectReason = RejectReason.None;

            if (profile == null || toolTransform == null || profile.Zones == null || profile.Zones.Length == 0)
            {
                rejectReason = RejectReason.NoProfileZones;
                return true;
            }

            ToolHitFaceProfile.HitFaceZone bestZone = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < profile.Zones.Length; i++)
            {
                var zone = profile.Zones[i];
                if (zone == null)
                {
                    continue;
                }

                if (!ContainsPoint(zone, toolTransform, hitPoint, out var distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestZone = zone;
                }
            }

            if (bestZone == null)
            {
                rejectReason = RejectReason.OutsideZones;
                return false;
            }

            zoneId = bestZone.zoneId;

            if (speed < bestZone.minSpeed)
            {
                rejectReason = RejectReason.BelowMinSpeed;
                return bestZone.allowGlancing && ApplyGlancing(bestZone, out damageMultiplier, out impulseMultiplier);
            }

            if (!MotionModeAccepted(bestZone.motionMode, toolTransform, velocityDirection))
            {
                rejectReason = RejectReason.WrongDirection;
                return bestZone.allowGlancing && ApplyGlancing(bestZone, out damageMultiplier, out impulseMultiplier);
            }

            var axis = ResolveAxis(bestZone.axis, toolTransform, bestZone.localEuler);
            var dir = velocityDirection.sqrMagnitude > 0.0001f ? velocityDirection.normalized : toolTransform.forward;
            var dot = Vector3.Dot(axis, dir);
            if (dot < bestZone.minDot)
            {
                rejectReason = RejectReason.WrongDirection;
                return bestZone.allowGlancing && ApplyGlancing(bestZone, out damageMultiplier, out impulseMultiplier);
            }

            damageMultiplier = bestZone.damageMultiplier;
            impulseMultiplier = bestZone.impulseMultiplier;
            ApplySurfaceModifier(profile, surfaceType, ref damageMultiplier, ref impulseMultiplier);
            return true;
        }

        private static bool MotionModeAccepted(ToolHitFaceProfile.HitFaceZone.MotionMode mode, Transform toolTransform, Vector3 velocityDirection)
        {
            if (mode == ToolHitFaceProfile.HitFaceZone.MotionMode.Any)
            {
                return true;
            }

            var dir = velocityDirection.sqrMagnitude > 0.0001f ? velocityDirection.normalized : toolTransform.forward;
            var thrustDot = Mathf.Abs(Vector3.Dot(toolTransform.forward, dir));
            if (mode == ToolHitFaceProfile.HitFaceZone.MotionMode.Thrust)
            {
                return thrustDot >= 0.72f;
            }

            return thrustDot <= 0.72f;
        }

        private static void ApplySurfaceModifier(ToolHitFaceProfile profile, DestructionFeedback.SurfaceType surfaceType, ref float damageMult, ref float impulseMult)
        {
            var modifiers = profile.SurfaceModifiers;
            if (modifiers == null)
            {
                return;
            }

            for (var i = 0; i < modifiers.Length; i++)
            {
                var mod = modifiers[i];
                if (mod == null || mod.surfaceType != surfaceType)
                {
                    continue;
                }

                damageMult *= mod.damageMultiplier;
                impulseMult *= mod.impulseMultiplier;
                return;
            }
        }

        private static bool ApplyGlancing(ToolHitFaceProfile.HitFaceZone zone, out float damageMult, out float impulseMult)
        {
            damageMult = Mathf.Max(0f, zone.glancingMultiplier);
            impulseMult = Mathf.Max(0f, zone.glancingMultiplier);
            return damageMult > 0f || impulseMult > 0f;
        }

        private static Vector3 ResolveAxis(ToolHitFaceProfile.LocalAxis axis, Transform toolTransform, Vector3 zoneEuler)
        {
            var zoneRotation = toolTransform.rotation * Quaternion.Euler(zoneEuler);
            switch (axis)
            {
                case ToolHitFaceProfile.LocalAxis.Right:
                    return zoneRotation * Vector3.right;
                case ToolHitFaceProfile.LocalAxis.Up:
                    return zoneRotation * Vector3.up;
                default:
                    return zoneRotation * Vector3.forward;
            }
        }

        private static bool ContainsPoint(ToolHitFaceProfile.HitFaceZone zone, Transform toolTransform, Vector3 worldPoint, out float distance)
        {
            var zoneCenter = toolTransform.TransformPoint(zone.localPosition);
            var zoneRotation = toolTransform.rotation * Quaternion.Euler(zone.localEuler);
            var local = Quaternion.Inverse(zoneRotation) * (worldPoint - zoneCenter);

            if (zone.shape == ToolHitFaceProfile.ZoneShape.Sphere)
            {
                var radius = Mathf.Max(0.001f, zone.localScale.x * 0.5f);
                distance = local.magnitude;
                return distance <= radius;
            }

            var half = zone.localScale * 0.5f;
            var dx = Mathf.Abs(local.x) - half.x;
            var dy = Mathf.Abs(local.y) - half.y;
            var dz = Mathf.Abs(local.z) - half.z;
            distance = Mathf.Max(dx, Mathf.Max(dy, dz));
            return dx <= 0f && dy <= 0f && dz <= 0f;
        }
    }
}
