using BreakTheRoom.Destruction;
using System.Collections.Generic;
using UnityEngine;

namespace BreakTheRoom.Player
{
    [RequireComponent(typeof(Camera))]
    public class DesktopPlaytestController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.8f;
        [SerializeField] private float lookSensitivity = 2.3f;

        [Header("Body")]
        [SerializeField] private Transform movementRoot;
        [SerializeField] private Transform rightArmPivot;
        [SerializeField] private Transform rightFistAnchor;
        [SerializeField] private Transform swingToolTip;
        [SerializeField] private Transform rightHandMount;
        [SerializeField] private Transform primaryHolster;
        [SerializeField] private Transform secondaryHolster;

        [Header("Debug Strike")]
        [SerializeField] private float hitDistance = 6f;
        [SerializeField] private float hitDamage = 35f;
        [SerializeField] private float hitImpulse = 4f;

        [Header("Right Swing")]
        [SerializeField] private float swingDuration = 0.24f;
        [SerializeField] private float swingAngle = 92f;
        [SerializeField] private float swingDamage = 50f;
        [SerializeField] private float swingImpulse = 7f;
        [SerializeField] private float swingRadius = 0.2f;
        [SerializeField] private float swingForwardReach = 0.35f;
        [SerializeField] private float interactionRadius = 2f;

        private Camera _cam;
        private float _pitch;
        private float _yaw;
        private float _swingStartTime = -10f;
        private bool _swingHitDone;
        private Quaternion _rightArmRest;
        private Vector3 _lastToolTipPosition;
        private bool _hasLastToolTip;
        private readonly HashSet<Collider> _hitThisSwing = new HashSet<Collider>();
        private DesktopMeleeTool _equippedTool;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            movementRoot = movementRoot != null ? movementRoot : ResolveMovementRoot();
            _yaw = movementRoot != null ? movementRoot.eulerAngles.y : transform.eulerAngles.y;

            rightArmPivot = rightArmPivot != null ? rightArmPivot : FindByNameInParents("RightArmPivot");
            rightFistAnchor = rightFistAnchor != null ? rightFistAnchor : FindByNameInParents("RightFist");
            _rightArmRest = rightArmPivot != null ? rightArmPivot.localRotation : Quaternion.identity;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.T))
            {
                return;
            }

            HandleLook();
            HandleMove();
            HandleToolInputs();
            HandleSwing();
            HandleDebugHit();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Input.GetMouseButtonDown(2))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                return;
            }

            var yaw = Input.GetAxis("Mouse X") * lookSensitivity;
            var pitchDelta = Input.GetAxis("Mouse Y") * lookSensitivity;

            _yaw += yaw;
            _pitch = Mathf.Clamp(_pitch - pitchDelta, -85f, 85f);

            if (movementRoot != null)
            {
                movementRoot.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }

            transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            var speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed;
            var root = movementRoot != null ? movementRoot : transform;
            var worldMove = root.TransformDirection(moveInput.normalized) * (speed * Time.deltaTime);
            root.position += worldMove;
        }

        private void HandleSwing()
        {
            if (rightArmPivot == null)
            {
                return;
            }

            if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButtonDown(1))
            {
                _swingStartTime = Time.time;
                _swingHitDone = false;
                _hasLastToolTip = false;
                _hitThisSwing.Clear();
            }

            var t = (Time.time - _swingStartTime) / Mathf.Max(0.01f, swingDuration);
            if (t < 0f || t > 1f)
            {
                rightArmPivot.localRotation = Quaternion.Slerp(rightArmPivot.localRotation, _rightArmRest, Time.deltaTime * 12f);
                return;
            }

            var arc = Mathf.Sin(t * Mathf.PI);
            var swingRot = Quaternion.Euler(-arc * swingAngle, 16f * arc, 18f * arc);
            rightArmPivot.localRotation = _rightArmRest * swingRot;

            if (t >= 0.2f && t <= 0.85f)
            {
                ApplySwingTraceDamage();
            }

            if (!_swingHitDone && t >= 0.34f)
            {
                _swingHitDone = true;
                ApplySwingDamage();
            }
        }

        private void HandleToolInputs()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                TryEquipNearbyTool();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                DropEquippedTool();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ToggleHolster(primaryHolster);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ToggleHolster(secondaryHolster);
            }
        }

        private void HandleDebugHit()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            var ray = _cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            if (!Physics.Raycast(ray, out var hit, hitDistance))
            {
                return;
            }

            var breakable = hit.collider.GetComponentInParent<BreakablePiece>();
            if (breakable != null)
            {
                breakable.ApplyDamage(hitDamage, hit.point, ray.direction * hitImpulse);
            }

            var rb = hit.rigidbody;
            if (rb != null)
            {
                rb.AddForceAtPosition(ray.direction * hitImpulse, hit.point, ForceMode.Impulse);
            }
        }

        private void ApplySwingDamage()
        {
            var toolDamage = _equippedTool != null ? _equippedTool.SwingDamage : swingDamage;
            var toolImpulse = _equippedTool != null ? _equippedTool.SwingImpulse : swingImpulse;
            var toolRadius = _equippedTool != null ? _equippedTool.SwingRadius : swingRadius;
            var toolReach = _equippedTool != null ? _equippedTool.SwingReachOffset : swingForwardReach;

            var tip = _equippedTool != null && _equippedTool.TipTransform != null ? _equippedTool.TipTransform : swingToolTip;
            var origin = tip != null
                ? tip.position
                : rightFistAnchor != null
                ? rightFistAnchor.position + transform.forward * toolReach
                : transform.position + transform.forward * 0.8f;

            var hits = Physics.OverlapSphere(origin, toolRadius, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hits.Length; i++)
            {
                var hitCol = hits[i];
                if (hitCol == null)
                {
                    continue;
                }

                var point = hitCol.ClosestPoint(origin);
                var impulseDir = (point - origin).sqrMagnitude > 0.001f ? (point - origin).normalized : transform.forward;
                var breakable = hitCol.GetComponentInParent<BreakablePiece>();
                if (breakable != null)
                {
                    breakable.ApplyDamage(toolDamage, point, impulseDir * toolImpulse);
                }

                var rb = hitCol.attachedRigidbody;
                if (rb != null)
                {
                    rb.AddForceAtPosition(impulseDir * toolImpulse, point, ForceMode.Impulse);
                }
            }
        }

        private void ApplySwingTraceDamage()
        {
            var tip = swingToolTip != null
                ? swingToolTip.position
                : rightFistAnchor != null
                    ? rightFistAnchor.position + transform.forward * 0.15f
                    : transform.position + transform.forward * 0.9f;

            if (!_hasLastToolTip)
            {
                _lastToolTipPosition = tip;
                _hasLastToolTip = true;
                return;
            }

            var delta = tip - _lastToolTipPosition;
            var distance = delta.magnitude;
            if (distance < 0.01f)
            {
                return;
            }

            var direction = delta / distance;
            var hits = Physics.SphereCastAll(_lastToolTipPosition, swingRadius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hits.Length; i++)
            {
                var hitCol = hits[i].collider;
                if (hitCol == null || _hitThisSwing.Contains(hitCol))
                {
                    continue;
                }

                _hitThisSwing.Add(hitCol);

                var point = hits[i].point;
                var impulseDir = direction;

                var breakable = hitCol.GetComponentInParent<BreakablePiece>();
                if (breakable != null)
                {
                    var dmg = (_equippedTool != null ? _equippedTool.SwingDamage : swingDamage) * 0.75f;
                    var imp = (_equippedTool != null ? _equippedTool.SwingImpulse : swingImpulse) * 0.8f;
                    breakable.ApplyDamage(dmg, point, impulseDir * imp);
                }

                var rb = hitCol.attachedRigidbody;
                if (rb != null)
                {
                    var imp = (_equippedTool != null ? _equippedTool.SwingImpulse : swingImpulse) * 0.8f;
                    rb.AddForceAtPosition(impulseDir * imp, point, ForceMode.Impulse);
                }
            }

            _lastToolTipPosition = tip;
        }

        private void TryEquipNearbyTool()
        {
            var hits = Physics.OverlapSphere(transform.position, interactionRadius, ~0, QueryTriggerInteraction.Collide);
            DesktopMeleeTool best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < hits.Length; i++)
            {
                var tool = hits[i].GetComponentInParent<DesktopMeleeTool>();
                if (tool == null)
                {
                    continue;
                }

                var dist = Vector3.Distance(transform.position, tool.transform.position);
                if (dist < bestDistance)
                {
                    best = tool;
                    bestDistance = dist;
                }
            }

            if (best == null)
            {
                return;
            }

            if (_equippedTool != null)
            {
                HolsterOrDropEquippedTool();
            }

            EquipTool(best);
        }

        private void EquipTool(DesktopMeleeTool tool)
        {
            if (tool == null)
            {
                return;
            }

            var mount = rightHandMount != null ? rightHandMount : rightFistAnchor;
            if (mount == null)
            {
                return;
            }

            _equippedTool = tool;
            _equippedTool.Equip(mount);
            swingToolTip = _equippedTool.TipTransform != null ? _equippedTool.TipTransform : swingToolTip;
        }

        private void DropEquippedTool()
        {
            if (_equippedTool == null)
            {
                return;
            }

            _equippedTool.Drop(transform.forward * 2.4f, Vector3.up * 1.2f);
            _equippedTool = null;
            swingToolTip = rightFistAnchor;
        }

        private void HolsterOrDropEquippedTool()
        {
            if (_equippedTool == null)
            {
                return;
            }

            if (primaryHolster != null && primaryHolster.childCount == 0)
            {
                _equippedTool.Holster(primaryHolster);
                _equippedTool = null;
                swingToolTip = rightFistAnchor;
                return;
            }

            if (secondaryHolster != null && secondaryHolster.childCount == 0)
            {
                _equippedTool.Holster(secondaryHolster);
                _equippedTool = null;
                swingToolTip = rightFistAnchor;
                return;
            }

            DropEquippedTool();
        }

        private void ToggleHolster(Transform holster)
        {
            if (holster == null)
            {
                return;
            }

            if (_equippedTool != null)
            {
                _equippedTool.Holster(holster);
                _equippedTool = null;
                swingToolTip = rightFistAnchor;
                return;
            }

            if (holster.childCount == 0)
            {
                return;
            }

            var tool = holster.GetComponentInChildren<DesktopMeleeTool>();
            if (tool == null)
            {
                return;
            }

            EquipTool(tool);
        }

        private Transform ResolveMovementRoot()
        {
            var t = transform;
            while (t.parent != null)
            {
                if (t.name.Contains("XROrigin") || t.name.Contains("XR Origin") || t.name.Contains("XROrigin_Fallback"))
                {
                    return t;
                }

                t = t.parent;
            }

            return t;
        }

        private Transform FindByNameInParents(string targetName)
        {
            var t = transform;
            while (t != null)
            {
                var found = FindRecursive(t, targetName);
                if (found != null)
                {
                    return found;
                }

                t = t.parent;
            }

            return null;
        }

        private static Transform FindRecursive(Transform root, string targetName)
        {
            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindRecursive(root.GetChild(i), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
