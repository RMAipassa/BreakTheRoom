using BreakTheRoom.Destruction;
using BreakTheRoom.Combat;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace BreakTheRoom.Player
{
    public class XrDesktopToolInteractor : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform rightHand;
        [SerializeField] private bool enabledInRealVr = true;
        [SerializeField] private float interactionRadius = 2.25f;
        [SerializeField] private float vrInteractionRadius = 0.45f;
        [SerializeField] private float dropForwardForce = 2.5f;
        [SerializeField] private bool firstPersonToolView = true;
        [SerializeField] private Vector3 toolLocalPosition = new Vector3(0.24f, -0.19f, 0.46f);
        [SerializeField] private Vector3 toolLocalEuler = new Vector3(8f, 22f, 72f);
        [SerializeField] private Vector3 vrMountPositionOffset = new Vector3(0.03f, -0.05f, 0.10f);
        [SerializeField] private Vector3 vrMountEulerOffset = new Vector3(34.08f, -72.69f, 36.69f);
        [SerializeField] private bool vrControllerCalibrationEnabled = true;
        [SerializeField] private float vrCalibrateHoldSeconds = 1f;
        [SerializeField] private float vrPosAdjustSpeed = 0.35f;
        [SerializeField] private float vrRotAdjustSpeed = 95f;
        [SerializeField] private bool showDebugOverlay = true;
        [SerializeField] private bool drawDebugGizmo = true;
        [SerializeField] private bool logVrInputState = true;
        [SerializeField] private bool logHitFaceRejects = false;
        [SerializeField] private float swingDuration = 0.2f;
        [SerializeField] private float swingAngle = 95f;
        [SerializeField] private bool useUnifiedSwingProfile = true;
        [SerializeField] private float unifiedSwingDamage = 60f;
        [SerializeField] private float unifiedSwingImpulse = 8f;
        [SerializeField] private float unifiedSwingRadius = 0.18f;
        [Header("Real VR Swing")]
        [SerializeField] private float vrMinSwingSpeed = 1.2f;
        [SerializeField] private float vrDamagePerSpeed = 26f;
        [SerializeField] private float vrImpulsePerSpeed = 3.8f;
        [SerializeField] private float vrHitCooldown = 0.08f;
        [SerializeField] private float tunePositionStep = 0.01f;
        [SerializeField] private float tuneRotationStep = 5f;

        private DesktopMeleeTool _equipped;
        private DesktopMeleeTool _nearest;
        private float _nearestDistance = float.MaxValue;
        private Transform _toolMount;
        private Quaternion _toolRestLocalRotation;
        private float _swingStartTime = -10f;
        private bool _swingHitDone;
        private Vector3 _lastTip;
        private bool _hasLastTip;
        private readonly HashSet<Collider> _hitThisSwing = new HashSet<Collider>();
        private bool _rightTriggerWasPressed;
        private bool _lastTriggerButton;
        private bool _lastGripButton;
        private bool _lastPrimaryButton;
        private bool _lastSecondaryButton;
        private bool _lastMenuButton;
        private bool _leftTriggerWasPressed;
        private bool _vrCalibrating;
        private float _bothGripHeldTime;
        private bool _savePressed;
        private TextMesh _vrCalibText;
        private GameObject _locomotionRoot;
        private bool _locomotionWasActive;
        private readonly Dictionary<Collider, float> _vrNextHitTime = new Dictionary<Collider, float>();

        private void LateUpdate()
        {
            if (_equipped == null || _toolMount == null)
            {
                return;
            }

            if (_equipped.transform.parent != _toolMount)
            {
                _equipped.transform.SetParent(_toolMount, false);
            }

            var t = (Time.time - _swingStartTime) / Mathf.Max(0.01f, swingDuration);
            var isSwinging = t >= 0f && t <= 1f;
            if (!isSwinging)
            {
                _equipped.ReapplyHoldPose();
            }
        }

        private void Update()
        {
            var isRealVr = XRSettings.isDeviceActive;
            if (!enabledInRealVr && isRealVr) { return; }

            var mainCam = Camera.main;
            if (mainCam != null && head != mainCam.transform)
            {
                head = mainCam.transform;
                _toolMount = null;
            }

            if (head == null)
            {
                head = mainCam != null ? mainCam.transform : null;
            }

            if (rightHand == null)
            {
                rightHand = FindBestController("right");
            }

            RefreshNearestTool();

            if (!isRealVr && Input.GetKeyDown(KeyCode.F))
            {
                TryEquipNearest();
            }

            if (!isRealVr && Input.GetKeyDown(KeyCode.G))
            {
                DropEquipped();
            }

            HandleVrTriggerPickup(isRealVr);
            LogVrInputState(isRealVr);
            HandleRealVrSwingHits(isRealVr);
            HandleVrControllerCalibration(isRealVr);

            if (_equipped != null && _toolMount != null && _equipped.transform.parent != _toolMount)
            {
                _equipped.Equip(_toolMount);
            }

            HandleSwing();
            HandleHoldTuning();
            HandleVrMountTuning(isRealVr);
        }

        private void TryEquipNearest()
        {
            if (_nearest == null)
            {
                return;
            }

            if (_equipped != null)
            {
                DropEquipped();
            }

            var mount = EnsureToolMount();
            _nearest.Equip(mount);
            _equipped = _nearest;

            if (XRSettings.isDeviceActive)
            {
                // Keep the proven desktop orientation in VR, only remove extra positional offset.
                _equipped.SetRuntimeHoldOverride(Vector3.zero, _equipped.HoldEulerOffset, _equipped.FlipViewYaw180);
                _equipped.ReapplyHoldPose();
            }
            else
            {
                _equipped.ClearRuntimeHoldOverride();
            }

            _toolRestLocalRotation = _equipped.transform.localRotation;
        }

        private void DropEquipped()
        {
            if (_equipped == null)
            {
                return;
            }

            var basis = head != null ? head : transform;
            _equipped.Drop(basis.forward * dropForwardForce, Vector3.up * 1.2f);
            _equipped = null;
            _hasLastTip = false;
        }

        private Transform EnsureToolMount()
        {
            if (_toolMount != null)
            {
                return _toolMount;
            }

            var parent = XRSettings.isDeviceActive
                ? ResolveVrGripAnchor()
                : firstPersonToolView && head != null
                ? head
                : rightHand != null ? rightHand : transform;
            var existing = parent.Find("DesktopToolMount");
            if (existing != null)
            {
                _toolMount = existing;
            }
            else
            {
                var go = new GameObject("DesktopToolMount");
                _toolMount = go.transform;
                _toolMount.SetParent(parent, false);
            }

            if (XRSettings.isDeviceActive)
            {
                _toolMount.localPosition = vrMountPositionOffset;
                _toolMount.localRotation = Quaternion.Euler(vrMountEulerOffset);
            }
            else
            {
                _toolMount.localPosition = toolLocalPosition;
                _toolMount.localRotation = Quaternion.Euler(toolLocalEuler);
            }

            return _toolMount;
        }

        private Transform ResolveVrGripAnchor()
        {
            if (rightHand == null)
            {
                return transform;
            }

            var attach = FindBestAttachAnchor(rightHand);
            return attach != null ? attach : rightHand;
        }

        private static Transform FindBestAttachAnchor(Transform handRoot)
        {
            var all = handRoot.GetComponentsInChildren<Transform>(true);
            Transform best = null;
            var bestScore = int.MinValue;

            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                var n = t.name.ToLowerInvariant();
                var score = 0;
                if (n.Contains("interactionattach")) score += 80;
                if (n.Contains("attach")) score += 40;
                if (n.Contains("right")) score += 10;
                if (n.Contains("stabilized")) score -= 20;
                if (n.Contains("teleport")) score -= 20;
                if (score <= 0) continue;

                if (score > bestScore)
                {
                    best = t;
                    bestScore = score;
                }
            }

            return best;
        }

        private Transform FindBestController(string side)
        {
            var all = GetComponentsInChildren<Transform>(true);
            Transform best = null;
            var bestScore = int.MinValue;

            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                var name = t.name.ToLowerInvariant();
                if (!name.Contains(side))
                {
                    continue;
                }

                var score = 0;
                if (name == side + " controller") score += 100;
                if (name.Contains(side + " controller")) score += 50;
                if (name.Contains("hand")) score += 20;
                if (name.Contains("teleport")) score -= 30;
                if (name.Contains("stabilized")) score -= 30;

                if (score > bestScore)
                {
                    best = t;
                    bestScore = score;
                }
            }

            return best;
        }

        private void HandleSwing()
        {
            if (_equipped == null || _toolMount == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.R))
            {
                _swingStartTime = Time.time;
                _swingHitDone = false;
                _hasLastTip = false;
                _hitThisSwing.Clear();
            }

            var t = (Time.time - _swingStartTime) / Mathf.Max(0.01f, swingDuration);
            if (t < 0f || t > 1f)
            {
                _equipped.transform.localRotation = Quaternion.Slerp(_equipped.transform.localRotation, _toolRestLocalRotation, Time.deltaTime * 14f);
                return;
            }

            var arc = Mathf.Sin(t * Mathf.PI);
            var arcSign = _equipped.ReverseSwingArc ? -1f : 1f;
            var swingRot = Quaternion.Euler(arc * swingAngle, 0f, -arc * 24f * arcSign);
            _equipped.transform.localRotation = _toolRestLocalRotation * swingRot;

            TraceSwingDamage();

            if (!_swingHitDone && t >= 0.35f)
            {
                _swingHitDone = true;
                BurstSwingHit();
            }
        }

        private void TraceSwingDamage()
        {
            var tip = _equipped.TipTransform != null ? _equipped.TipTransform.position : _equipped.transform.position;
            if (!_hasLastTip)
            {
                _lastTip = tip;
                _hasLastTip = true;
                return;
            }

            var delta = tip - _lastTip;
            var distance = delta.magnitude;
            if (distance < 0.005f)
            {
                return;
            }

            var direction = delta / distance;
            var speed = distance / Mathf.Max(Time.deltaTime, 0.0001f);
            var hits = Physics.SphereCastAll(_lastTip, GetSwingRadius(), direction, distance, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hits.Length; i++)
            {
                var hitCol = hits[i].collider;
                if (hitCol == null || _hitThisSwing.Contains(hitCol))
                {
                    continue;
                }

                _hitThisSwing.Add(hitCol);
                ApplyDamageToHit(hitCol, hits[i].point, direction, 0.75f, speed);
            }

            _lastTip = tip;
        }

        private void BurstSwingHit()
        {
            var origin = _equipped.TipTransform != null ? _equipped.TipTransform.position : _equipped.transform.position;
            var hits = Physics.OverlapSphere(origin, GetSwingRadius() * 1.15f, ~0, QueryTriggerInteraction.Ignore);
            var dir = head != null ? head.forward : transform.forward;

            for (var i = 0; i < hits.Length; i++)
            {
                ApplyDamageToHit(hits[i], hits[i].ClosestPoint(origin), dir, 1f, vrMinSwingSpeed + 0.5f);
            }
        }

        private void ApplyDamageToHit(Collider hitCol, Vector3 point, Vector3 dir, float scale, float speed)
        {
            var damageMult = 1f;
            var impulseMult = 1f;
            var surfaceType = ResolveSurfaceType(hitCol);

            if (!ToolHitFaceEvaluator.TryEvaluate(
                _equipped.HitFaceProfile,
                _equipped.transform,
                point,
                dir,
                speed,
                out damageMult,
                out impulseMult,
                out var zone,
                out var reject,
                surfaceType))
            {
                if (logHitFaceRejects)
                {
                    Debug.Log($"Hit face rejected [{_equipped.ToolName}] reason={reject} zone={zone} speed={speed:0.00}");
                }
                return;
            }

            var breakable = hitCol.GetComponentInParent<BreakablePiece>();
            if (breakable != null)
            {
                breakable.ApplyDamage(GetSwingDamage() * scale * damageMult, point, dir * (GetSwingImpulse() * scale * impulseMult));
            }

            var rb = hitCol.attachedRigidbody;
            if (rb != null)
            {
                rb.AddForceAtPosition(dir * (GetSwingImpulse() * scale * impulseMult), point, ForceMode.Impulse);
            }
        }

        private static DestructionFeedback.SurfaceType ResolveSurfaceType(Collider hitCol)
        {
            if (hitCol == null)
            {
                return DestructionFeedback.SurfaceType.Generic;
            }

            var feedback = hitCol.GetComponentInParent<DestructionFeedback>();
            return feedback != null ? feedback.Surface : DestructionFeedback.SurfaceType.Generic;
        }

        private float GetSwingDamage()
        {
            return useUnifiedSwingProfile ? unifiedSwingDamage : _equipped.SwingDamage;
        }

        private float GetSwingImpulse()
        {
            return useUnifiedSwingProfile ? unifiedSwingImpulse : _equipped.SwingImpulse;
        }

        private float GetSwingRadius()
        {
            return useUnifiedSwingProfile ? unifiedSwingRadius : _equipped.SwingRadius;
        }

        private void HandleHoldTuning()
        {
            if (_equipped == null || !Input.GetKey(KeyCode.T))
            {
                return;
            }

            var moved = false;

            if (Input.GetKeyDown(KeyCode.J)) { _equipped.NudgeHoldPosition(new Vector3(-tunePositionStep, 0f, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.L)) { _equipped.NudgeHoldPosition(new Vector3(tunePositionStep, 0f, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.U)) { _equipped.NudgeHoldPosition(new Vector3(0f, tunePositionStep, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.O)) { _equipped.NudgeHoldPosition(new Vector3(0f, -tunePositionStep, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.I)) { _equipped.NudgeHoldPosition(new Vector3(0f, 0f, tunePositionStep)); moved = true; }
            if (Input.GetKeyDown(KeyCode.K)) { _equipped.NudgeHoldPosition(new Vector3(0f, 0f, -tunePositionStep)); moved = true; }

            if (Input.GetKeyDown(KeyCode.LeftArrow)) { _equipped.NudgeHoldEuler(new Vector3(0f, -tuneRotationStep, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { _equipped.NudgeHoldEuler(new Vector3(0f, tuneRotationStep, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.UpArrow)) { _equipped.NudgeHoldEuler(new Vector3(-tuneRotationStep, 0f, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { _equipped.NudgeHoldEuler(new Vector3(tuneRotationStep, 0f, 0f)); moved = true; }
            if (Input.GetKeyDown(KeyCode.Q)) { _equipped.NudgeHoldEuler(new Vector3(0f, 0f, -tuneRotationStep)); moved = true; }
            if (Input.GetKeyDown(KeyCode.E)) { _equipped.NudgeHoldEuler(new Vector3(0f, 0f, tuneRotationStep)); moved = true; }

            if (moved || Input.GetKeyDown(KeyCode.P))
            {
                _toolRestLocalRotation = _equipped.transform.localRotation;
                Debug.Log($"Tool hold tune [{_equipped.ToolName}] pos={_equipped.HoldPositionOffset} rot={_equipped.HoldEulerOffset}");
            }
        }

        private void HandleVrMountTuning(bool isRealVr)
        {
            if (!isRealVr || !Input.GetKey(KeyCode.V))
            {
                return;
            }

            var moved = false;

            if (Input.GetKeyDown(KeyCode.J)) { vrMountPositionOffset += new Vector3(-tunePositionStep, 0f, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.L)) { vrMountPositionOffset += new Vector3(tunePositionStep, 0f, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.U)) { vrMountPositionOffset += new Vector3(0f, tunePositionStep, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.O)) { vrMountPositionOffset += new Vector3(0f, -tunePositionStep, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.I)) { vrMountPositionOffset += new Vector3(0f, 0f, tunePositionStep); moved = true; }
            if (Input.GetKeyDown(KeyCode.K)) { vrMountPositionOffset += new Vector3(0f, 0f, -tunePositionStep); moved = true; }

            if (Input.GetKeyDown(KeyCode.LeftArrow)) { vrMountEulerOffset += new Vector3(0f, -tuneRotationStep, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { vrMountEulerOffset += new Vector3(0f, tuneRotationStep, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.UpArrow)) { vrMountEulerOffset += new Vector3(-tuneRotationStep, 0f, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { vrMountEulerOffset += new Vector3(tuneRotationStep, 0f, 0f); moved = true; }
            if (Input.GetKeyDown(KeyCode.Q)) { vrMountEulerOffset += new Vector3(0f, 0f, -tuneRotationStep); moved = true; }
            if (Input.GetKeyDown(KeyCode.E)) { vrMountEulerOffset += new Vector3(0f, 0f, tuneRotationStep); moved = true; }

            if (!moved)
            {
                return;
            }

            _toolMount = null;
            if (_equipped != null)
            {
                var mount = EnsureToolMount();
                _equipped.Equip(mount);
                _equipped.SetRuntimeHoldOverride(Vector3.zero, _equipped.HoldEulerOffset, _equipped.FlipViewYaw180);
                _equipped.ReapplyHoldPose();
                _toolRestLocalRotation = _equipped.transform.localRotation;
            }

            Debug.Log($"VR mount tune pos={vrMountPositionOffset} rot={vrMountEulerOffset}");
        }

        private void HandleVrControllerCalibration(bool isRealVr)
        {
            if (!vrControllerCalibrationEnabled || !isRealVr)
            {
                SetVrCalibText(false, string.Empty);
                return;
            }

            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!left.isValid || !right.isValid)
            {
                SetVrCalibText(false, "VR calib: controllers invalid");
                return;
            }

            left.TryGetFeatureValue(CommonUsages.gripButton, out var leftGrip);
            right.TryGetFeatureValue(CommonUsages.gripButton, out var rightGrip);
            var bothGrip = leftGrip && rightGrip;

            if (bothGrip)
            {
                _bothGripHeldTime += Time.deltaTime;
                if (_bothGripHeldTime >= vrCalibrateHoldSeconds)
                {
                    _bothGripHeldTime = 0f;
                    _vrCalibrating = !_vrCalibrating;
                    SetLocomotionBlocked(_vrCalibrating);
                    Debug.Log($"VR mount calibration mode: {(_vrCalibrating ? "ON" : "OFF")}");
                }
            }
            else
            {
                _bothGripHeldTime = 0f;
            }

            if (!_vrCalibrating)
            {
                SetVrCalibText(false, string.Empty);
                return;
            }

            right.TryGetFeatureValue(CommonUsages.primary2DAxis, out var rightAxis);
            left.TryGetFeatureValue(CommonUsages.primary2DAxis, out var leftAxis);
            right.TryGetFeatureValue(CommonUsages.triggerButton, out var rightTrigger);

            var changed = false;
            var dt = Time.deltaTime;

            if (rightTrigger)
            {
                var rotDelta = new Vector3(-rightAxis.y, rightAxis.x, leftAxis.x) * (vrRotAdjustSpeed * dt);
                if (rotDelta.sqrMagnitude > 0.000001f)
                {
                    vrMountEulerOffset += rotDelta;
                    changed = true;
                }
            }
            else
            {
                var posDelta = new Vector3(rightAxis.x, leftAxis.y, rightAxis.y) * (vrPosAdjustSpeed * dt);
                if (posDelta.sqrMagnitude > 0.0000001f)
                {
                    vrMountPositionOffset += posDelta;
                    changed = true;
                }
            }

            right.TryGetFeatureValue(CommonUsages.primaryButton, out var saveNow);
            if (saveNow && !_savePressed)
            {
                Debug.Log($"VR mount saved values pos={vrMountPositionOffset} rot={vrMountEulerOffset}");
            }
            _savePressed = saveNow;

            if (changed)
            {
                _toolMount = null;
                if (_equipped != null)
                {
                    var mount = EnsureToolMount();
                    _equipped.Equip(mount);
                    _equipped.SetRuntimeHoldOverride(Vector3.zero, _equipped.HoldEulerOffset, _equipped.FlipViewYaw180);
                    _equipped.ReapplyHoldPose();
                    _toolRestLocalRotation = _equipped.transform.localRotation;
                }
            }

            SetVrCalibText(true,
                "VR Tool Calibration\n"
                + "Hold both grips 1s: toggle\n"
                + "Move mode: right stick X/Z, left stick Y\n"
                + "Hold right trigger: rotate\n"
                + "Press right primary: log values\n"
                + $"pos {vrMountPositionOffset}\nrot {vrMountEulerOffset}");
        }

        private void SetVrCalibText(bool visible, string message)
        {
            if (!visible)
            {
                if (_vrCalibText != null)
                {
                    _vrCalibText.gameObject.SetActive(false);
                }
                return;
            }

            if (_vrCalibText == null)
            {
                var go = new GameObject("VRCalibText");
                _vrCalibText = go.AddComponent<TextMesh>();
                _vrCalibText.fontSize = 44;
                _vrCalibText.characterSize = 0.02f;
                _vrCalibText.anchor = TextAnchor.UpperLeft;
                _vrCalibText.color = Color.white;
            }

            var parent = head != null ? head : transform;
            _vrCalibText.transform.SetParent(parent, false);
            _vrCalibText.transform.localPosition = new Vector3(-0.26f, 0.16f, 1.25f);
            _vrCalibText.transform.localRotation = Quaternion.identity;
            _vrCalibText.gameObject.SetActive(true);
            _vrCalibText.text = message;
        }

        private void SetLocomotionBlocked(bool blocked)
        {
            if (_locomotionRoot == null)
            {
                var t = transform.Find("Locomotion");
                if (t != null)
                {
                    _locomotionRoot = t.gameObject;
                }
            }

            if (_locomotionRoot == null)
            {
                return;
            }

            if (blocked)
            {
                _locomotionWasActive = _locomotionRoot.activeSelf;
                _locomotionRoot.SetActive(false);
            }
            else
            {
                _locomotionRoot.SetActive(_locomotionWasActive);
            }
        }

        private void HandleVrTriggerPickup(bool isRealVr)
        {
            if (!isRealVr)
            {
                _rightTriggerWasPressed = false;
                return;
            }

            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!device.isValid)
            {
                return;
            }

            if (!device.TryGetFeatureValue(CommonUsages.triggerButton, out var triggerPressed))
            {
                return;
            }

            if (triggerPressed && !_rightTriggerWasPressed)
            {
                if (logVrInputState)
                {
                    var origin = rightHand != null ? rightHand.position : (head != null ? head.position : transform.position);
                    Debug.Log($"VR trigger press detected at {origin}, radius={vrInteractionRadius}");
                }

                if (_equipped == null)
                {
                    TryEquipNearestVr();
                }
            }

            _rightTriggerWasPressed = triggerPressed;

            var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftDevice.isValid && leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out var leftTriggerPressed))
            {
                if (leftTriggerPressed && !_leftTriggerWasPressed)
                {
                    DropEquipped();
                }

                _leftTriggerWasPressed = leftTriggerPressed;
            }
        }

        private void TryEquipNearestVr()
        {
            var origin = rightHand != null ? rightHand.position : (head != null ? head.position : transform.position);
            var hits = Physics.OverlapSphere(origin, vrInteractionRadius, ~0, QueryTriggerInteraction.Collide);

            DesktopMeleeTool best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < hits.Length; i++)
            {
                var tool = hits[i].GetComponentInParent<DesktopMeleeTool>();
                if (tool == null)
                {
                    continue;
                }

                var dist = Vector3.Distance(origin, tool.transform.position);
                if (dist < bestDist)
                {
                    best = tool;
                    bestDist = dist;
                }
            }

            if (best == null)
            {
                return;
            }

            _nearest = best;
            TryEquipNearest();
        }

        private void LogVrInputState(bool isRealVr)
        {
            if (!logVrInputState || !isRealVr)
            {
                return;
            }

            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!device.isValid)
            {
                return;
            }

            LogButtonChange(device, CommonUsages.triggerButton, ref _lastTriggerButton, "triggerButton");
            LogButtonChange(device, CommonUsages.gripButton, ref _lastGripButton, "gripButton");
            LogButtonChange(device, CommonUsages.primaryButton, ref _lastPrimaryButton, "primaryButton");
            LogButtonChange(device, CommonUsages.secondaryButton, ref _lastSecondaryButton, "secondaryButton");
            LogButtonChange(device, CommonUsages.menuButton, ref _lastMenuButton, "menuButton");
        }

        private void LogButtonChange(InputDevice device, InputFeatureUsage<bool> usage, ref bool lastValue, string label)
        {
            if (!device.TryGetFeatureValue(usage, out var value))
            {
                return;
            }

            if (value == lastValue)
            {
                return;
            }

            lastValue = value;
            Debug.Log($"VR input [{label}] = {value}");
        }

        private void HandleRealVrSwingHits(bool isRealVr)
        {
            if (!isRealVr || _equipped == null)
            {
                _hasLastTip = false;
                return;
            }

            var tip = _equipped.TipTransform != null ? _equipped.TipTransform.position : _equipped.transform.position;
            if (!_hasLastTip)
            {
                _lastTip = tip;
                _hasLastTip = true;
                return;
            }

            var delta = tip - _lastTip;
            var distance = delta.magnitude;
            if (distance < 0.001f)
            {
                return;
            }

            var speed = distance / Mathf.Max(Time.deltaTime, 0.0001f);
            if (speed < vrMinSwingSpeed)
            {
                _lastTip = tip;
                return;
            }

            var dir = delta / distance;
            var radius = GetSwingRadius();
            var hits = Physics.SphereCastAll(_lastTip, radius, dir, distance, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null)
                {
                    continue;
                }

                if (_vrNextHitTime.TryGetValue(col, out var nextTime) && Time.time < nextTime)
                {
                    continue;
                }

                _vrNextHitTime[col] = Time.time + vrHitCooldown;

                var point = hits[i].point;
                var damage = Mathf.Max(1f, (speed - vrMinSwingSpeed) * vrDamagePerSpeed);
                var impulse = Mathf.Max(0.5f, (speed - vrMinSwingSpeed) * vrImpulsePerSpeed);

                var breakable = col.GetComponentInParent<BreakablePiece>();
                if (breakable != null)
                {
                    breakable.ApplyDamage(damage, point, dir * impulse);
                }

                var rb = col.attachedRigidbody;
                if (rb != null)
                {
                    rb.AddForceAtPosition(dir * impulse, point, ForceMode.Impulse);
                }
            }

            _lastTip = tip;
        }

        private void RefreshNearestTool()
        {
            var origin = head != null ? head.position : transform.position;
            var hits = Physics.OverlapSphere(origin, interactionRadius, ~0, QueryTriggerInteraction.Collide);

            DesktopMeleeTool best = null;
            var bestDist = float.MaxValue;

            for (var i = 0; i < hits.Length; i++)
            {
                var tool = hits[i].GetComponentInParent<DesktopMeleeTool>();
                if (tool == null)
                {
                    continue;
                }

                var dist = Vector3.Distance(origin, tool.transform.position);
                if (dist < bestDist)
                {
                    best = tool;
                    bestDist = dist;
                }
            }

            _nearest = best;
            _nearestDistance = best != null ? bestDist : float.MaxValue;
        }

        private void OnGUI()
        {
            if (!showDebugOverlay)
            {
                return;
            }

            var toolText = _nearest != null
                ? $"Nearest: {_nearest.ToolName} ({_nearestDistance:0.00}m)"
                : "Nearest: none";
            var equippedText = _equipped != null ? _equipped.ToolName : "none";

            GUI.Box(new Rect(12f, 148f, 360f, 72f),
                "Tool Debug\n"
                + toolText + "\n"
                + $"Equipped: {equippedText}");
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmo)
            {
                return;
            }

            var origin = head != null ? head.position : transform.position;
            Gizmos.color = _nearest != null ? new Color(0.25f, 1f, 0.4f, 0.8f) : new Color(1f, 0.35f, 0.35f, 0.75f);
            Gizmos.DrawWireSphere(origin, interactionRadius);

            if (_nearest != null)
            {
                Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.9f);
                Gizmos.DrawLine(origin, _nearest.transform.position);
                Gizmos.DrawWireSphere(_nearest.transform.position, 0.1f);
            }
        }
    }
}
