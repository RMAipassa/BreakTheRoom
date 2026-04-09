using UnityEngine;

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
        [SerializeField] private Transform gripTransform;
        [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 holdEulerOffset = Vector3.zero;
        [SerializeField] private bool flipViewYaw180 = true;
        [SerializeField] private bool reverseSwingArc = false;

        private Rigidbody _rb;
        private Collider[] _colliders;
        private Renderer[] _renderers;
        private Behaviour _xrGrab;

        public string ToolName => toolName;
        public float SwingDamage => swingDamage;
        public float SwingImpulse => swingImpulse;
        public float SwingRadius => swingRadius;
        public float SwingReachOffset => swingReachOffset;
        public Transform TipTransform => tipTransform;
        public Vector3 HoldPositionOffset => holdPositionOffset;
        public Vector3 HoldEulerOffset => holdEulerOffset;
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
            var viewFlip = flipViewYaw180 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

            transform.localPosition = alignPosition + holdPositionOffset;
            transform.localRotation = alignRotation * Quaternion.Euler(holdEulerOffset) * viewFlip;
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
    }
}
