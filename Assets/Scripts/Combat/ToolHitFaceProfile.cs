using System;
using BreakTheRoom.Destruction;
using UnityEngine;

namespace BreakTheRoom.Combat
{
    [CreateAssetMenu(menuName = "BreakTheRoom/Combat/Tool Hit Face Profile", fileName = "HitFaceProfile")]
    public class ToolHitFaceProfile : ScriptableObject
    {
        public enum ZoneShape
        {
            Box,
            Sphere
        }

        public enum LocalAxis
        {
            Forward,
            Right,
            Up
        }

        [Serializable]
        public class HitFaceZone
        {
            public enum MotionMode
            {
                Any,
                Swing,
                Thrust
            }

            public string zoneId = "zone";
            public ZoneShape shape = ZoneShape.Box;
            public Vector3 localPosition = Vector3.zero;
            public Vector3 localEuler = Vector3.zero;
            public Vector3 localScale = new Vector3(0.1f, 0.1f, 0.1f);
            public float minSpeed = 1f;
            public MotionMode motionMode = MotionMode.Any;
            public LocalAxis axis = LocalAxis.Forward;
            public float minDot = -1f;
            public float damageMultiplier = 1f;
            public float impulseMultiplier = 1f;
            public bool allowGlancing = true;
            public float glancingMultiplier = 0.2f;
        }

        [Serializable]
        public class SurfaceModifier
        {
            public DestructionFeedback.SurfaceType surfaceType = DestructionFeedback.SurfaceType.Generic;
            public float damageMultiplier = 1f;
            public float impulseMultiplier = 1f;
        }

        [SerializeField] private string toolId = "tool";
        [SerializeField] private HitFaceZone[] zones = Array.Empty<HitFaceZone>();
        [SerializeField] private SurfaceModifier[] surfaceModifiers = Array.Empty<SurfaceModifier>();

        public string ToolId => toolId;
        public HitFaceZone[] Zones => zones;
        public SurfaceModifier[] SurfaceModifiers => surfaceModifiers;
    }
}
