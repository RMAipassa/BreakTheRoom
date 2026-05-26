using UnityEngine;
using BreakTheRoom.Combat;

namespace BreakTheRoom.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class DesktopMeleeTool : MonoBehaviour
    {
        [SerializeField] private string toolName = "Tool";
        [SerializeField] private float swingDamage = 55f;
        [SerializeField] private float swingImpulse = 8f;
        [SerializeField] private float swingRadius = 0.2f;
        [SerializeField] private float swingReachOffset = 0.45f;
        [SerializeField] private Transform tipTransform;
        [SerializeField] private ToolHitFaceProfile hitFaceProfile;
        [SerializeField] private bool drawHitFaceGizmos = true;
        [SerializeField] private bool drawHitFaceZoneLabels = true;
        [SerializeField] private Transform gripTransform;
        [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 holdEulerOffset = Vector3.zero;
        [SerializeField] private bool flipViewYaw180 = true;
        [SerializeField] private bool reverseSwingArc = false;

        private Rigidbody _rb;
        private Collider[] _colliders;
        private Renderer[] _renderers;
        private Behaviour _xrGrab;
        private bool _hasRuntimeHoldOverride;
        private Vector3 _runtimeHoldPositionOffset;
        private Vector3 _runtimeHoldEulerOffset;
        private bool _runtimeFlipYaw180;

        public string ToolName => toolName;
        public float SwingDamage => swingDamage;
        public float SwingImpulse => swingImpulse;
        public float SwingRadius => swingRadius;
        public float SwingReachOffset => swingReachOffset;
        public Transform TipTransform => tipTransform;
        public ToolHitFaceProfile HitFaceProfile => hitFaceProfile;
        public Vector3 HoldPositionOffset => holdPositionOffset;
        public Vector3 HoldEulerOffset => holdEulerOffset;
        public bool FlipViewYaw180 => flipViewYaw180;
        public bool ReverseSwingArc => reverseSwingArc;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _xrGrab = ResolveGrabComponent();
            if (tipTransform == null)
            {
                var found = transform.Find("ToolTip");
                if (found != null)
                {
                    tipTransform = found;
                }
            }

            if (gripTransform == null)
            {
                var found = transform.Find("Grip");
                if (found != null)
                {
                    gripTransform = found;
                }
            }

            if (_xrGrab != null)
            {
                _xrGrab.enabled = true;
            }
        }

        public void Equip(Transform handMount)
        {
            if (handMount == null)
            {
                return;
            }

            transform.SetParent(handMount, false);
            AlignGripToMount();

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            SetCollidersEnabled(false);
            SetRenderersEnabled(true);

            if (_xrGrab != null)
            {
                _xrGrab.enabled = false;
            }
        }

        public void Holster(Transform holsterMount)
        {
            if (holsterMount == null)
            {
                return;
            }

            transform.SetParent(holsterMount, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            SetCollidersEnabled(false);
            SetRenderersEnabled(true);

            if (_xrGrab != null)
            {
                _xrGrab.enabled = false;
            }
        }

        public void Drop(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            transform.SetParent(null);
            ClearRuntimeHoldOverride();
            _rb.isKinematic = false;
            _rb.linearVelocity = linearVelocity;
            _rb.angularVelocity = angularVelocity;
            SetCollidersEnabled(true);
            SetRenderersEnabled(true);

            if (_xrGrab != null)
            {
                _xrGrab.enabled = true;
            }
        }

        private void SetCollidersEnabled(bool enabledState)
        {
            for (var i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].enabled = enabledState;
            }
        }

        private void SetRenderersEnabled(bool enabledState)
        {
            for (var i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].enabled = enabledState;
            }
        }

        public void NudgeHoldPosition(Vector3 delta)
        {
            holdPositionOffset += delta;
            AlignGripToMount();
        }

        public void NudgeHoldEuler(Vector3 deltaEuler)
        {
            holdEulerOffset += deltaEuler;
            AlignGripToMount();
        }

        public void ReapplyHoldPose()
        {
            AlignGripToMount();
        }

        public void SetRuntimeHoldOverride(Vector3 positionOffset, Vector3 eulerOffset, bool flipYaw180)
        {
            _hasRuntimeHoldOverride = true;
            _runtimeHoldPositionOffset = positionOffset;
            _runtimeHoldEulerOffset = eulerOffset;
            _runtimeFlipYaw180 = flipYaw180;
            AlignGripToMount();
        }

        public void ClearRuntimeHoldOverride()
        {
            _hasRuntimeHoldOverride = false;
        }

        private void AlignGripToMount()
        {
            if (gripTransform == null)
            {
                transform.localPosition = holdPositionOffset;
                transform.localRotation = Quaternion.Euler(holdEulerOffset);
                return;
            }

            var alignRotation = Quaternion.Inverse(gripTransform.localRotation);
            var alignPosition = -(alignRotation * gripTransform.localPosition);
            var holdPos = _hasRuntimeHoldOverride ? _runtimeHoldPositionOffset : holdPositionOffset;
            var holdEuler = _hasRuntimeHoldOverride ? _runtimeHoldEulerOffset : holdEulerOffset;
            var flip = _hasRuntimeHoldOverride ? _runtimeFlipYaw180 : flipViewYaw180;
            var viewFlip = flip ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

            transform.localPosition = alignPosition + holdPos;
            transform.localRotation = alignRotation * Quaternion.Euler(holdEuler) * viewFlip;
        }

        private Behaviour ResolveGrabComponent()
        {
            var t =
                System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit")
                ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");

            if (t == null)
            {
                return null;
            }

            return GetComponent(t) as Behaviour;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawHitFaceGizmos || hitFaceProfile == null || hitFaceProfile.Zones == null)
            {
                return;
            }

            for (var i = 0; i < hitFaceProfile.Zones.Length; i++)
            {
                var zone = hitFaceProfile.Zones[i];
                if (zone == null)
                {
                    continue;
                }

                var center = transform.TransformPoint(zone.localPosition);
                var rotation = transform.rotation * Quaternion.Euler(zone.localEuler);
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.65f);

                if (zone.shape == ToolHitFaceProfile.ZoneShape.Sphere)
                {
                    var radius = Mathf.Max(0.001f, zone.localScale.x * 0.5f);
                    Gizmos.DrawWireSphere(center, radius);
                }
                else
                {
                    var oldMatrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, zone.localScale);
                    Gizmos.matrix = oldMatrix;
                }

#if UNITY_EDITOR
                if (drawHitFaceZoneLabels)
                {
                    UnityEditor.Handles.color = new Color(0.95f, 0.15f, 0.15f, 1f);
                    UnityEditor.Handles.Label(
                        center + Vector3.up * 0.035f,
                        $"{zone.zoneId}\nminSpd:{zone.minSpeed:0.0} dot>={zone.minDot:0.00}");
                }
#endif
            }
        }
    }
}
