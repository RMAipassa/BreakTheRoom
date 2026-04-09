using UnityEngine;

namespace BreakTheRoom.Player
{
    public class XrBodyPresenceDriver : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private bool hideControllerModels = true;
        [SerializeField] private bool createHeadAnchoredHands = false;
        [SerializeField] private bool usePhysicsBody = false;
        [SerializeField] private float bodyHeight = 1.75f;
        [SerializeField] private float bodyRadius = 0.22f;
        [SerializeField] private float followStrength = 18f;
        [SerializeField] private float groundProbeHeight = 1.2f;
        [SerializeField] private float minGroundDistance = 0.1f;

        private Transform _bodyRoot;
        private Transform _torso;
        private Transform _hips;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _leftArm;
        private Transform _rightArm;
        private bool _controllerVisualsHidden;
        private Rigidbody _bodyRb;

        private void Awake()
        {
            ResolveHandReferences();
            SetupPhysicsBody();
            BuildBody();
        }

        private void ResolveHandReferences()
        {
            if (leftHand == null)
            {
                leftHand = FindBestController("left");
            }

            if (rightHand == null)
            {
                rightHand = FindBestController("right");
            }
        }

        private void FixedUpdate()
        {
            if (!usePhysicsBody || _bodyRb == null || head == null)
            {
                return;
            }

            var current = _bodyRb.position;
            var targetY = current.y;

            var rayOrigin = new Vector3(head.position.x, head.position.y + groundProbeHeight, head.position.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                targetY = hit.point.y + minGroundDistance;
            }

            var target = new Vector3(head.position.x, targetY, head.position.z);
            var next = Vector3.Lerp(current, target, Time.fixedDeltaTime * followStrength);
            _bodyRb.MovePosition(next);
        }

        private void LateUpdate()
        {
            if (head == null || _bodyRoot == null)
            {
                return;
            }

            if (hideControllerModels && !_controllerVisualsHidden)
            {
                HideControllerVisuals(leftHand);
                HideControllerVisuals(rightHand);
                _controllerVisualsHidden = true;
            }

            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
            }

            var rootY = _bodyRb != null ? _bodyRb.position.y : transform.position.y;
            _bodyRoot.position = new Vector3(head.position.x, rootY, head.position.z);
            _bodyRoot.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            _torso.localPosition = new Vector3(0f, 1.02f, 0f);
            _hips.localPosition = new Vector3(0f, 0.76f, 0f);
            _leftLeg.localPosition = new Vector3(-0.1f, 0.34f, 0f);
            _rightLeg.localPosition = new Vector3(0.1f, 0.34f, 0f);

            if (leftHand != null)
            {
                _leftArm.position = Vector3.Lerp(_leftArm.position, leftHand.position, Time.deltaTime * 14f);
                _leftArm.rotation = leftHand.rotation;
            }

            if (rightHand != null)
            {
                _rightArm.position = Vector3.Lerp(_rightArm.position, rightHand.position, Time.deltaTime * 14f);
                _rightArm.rotation = rightHand.rotation;
            }
        }

        private void SetupPhysicsBody()
        {
            if (!usePhysicsBody)
            {
                return;
            }

            if (GetComponent<CharacterController>() != null)
            {
                usePhysicsBody = false;
                return;
            }

            var capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = gameObject.AddComponent<CapsuleCollider>();
            }

            capsule.height = bodyHeight;
            capsule.radius = bodyRadius;
            capsule.center = new Vector3(0f, bodyHeight * 0.5f, 0f);

            _bodyRb = GetComponent<Rigidbody>();
            if (_bodyRb == null)
            {
                _bodyRb = gameObject.AddComponent<Rigidbody>();
            }

            _bodyRb.mass = 75f;
            _bodyRb.interpolation = RigidbodyInterpolation.Interpolate;
            _bodyRb.constraints = RigidbodyConstraints.FreezeRotation;
            _bodyRb.linearDamping = 0f;
            _bodyRb.angularDamping = 1f;
            _bodyRb.useGravity = false;
            _bodyRb.isKinematic = true;
        }

        private void BuildBody()
        {
            _bodyRoot = new GameObject("XRBodyPresence").transform;
            _bodyRoot.SetParent(transform, false);

            _torso = CreatePart(_bodyRoot, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.02f, 0f), new Vector3(0.4f, 0.45f, 0.25f), new Color(0.22f, 0.28f, 0.35f));
            _hips = CreatePart(_bodyRoot, "Hips", PrimitiveType.Cube, new Vector3(0f, 0.76f, 0f), new Vector3(0.3f, 0.16f, 0.2f), new Color(0.16f, 0.19f, 0.24f));
            _leftLeg = CreatePart(_bodyRoot, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.1f, 0.34f, 0f), new Vector3(0.11f, 0.33f, 0.11f), new Color(0.16f, 0.19f, 0.24f));
            _rightLeg = CreatePart(_bodyRoot, "RightLeg", PrimitiveType.Capsule, new Vector3(0.1f, 0.34f, 0f), new Vector3(0.11f, 0.33f, 0.11f), new Color(0.16f, 0.19f, 0.24f));

            _leftArm = CreatePart(_bodyRoot, "LeftArmProxy", PrimitiveType.Capsule, new Vector3(-0.2f, 1.24f, 0.2f), new Vector3(0.07f, 0.2f, 0.07f), new Color(0.74f, 0.63f, 0.55f));
            _rightArm = CreatePart(_bodyRoot, "RightArmProxy", PrimitiveType.Capsule, new Vector3(0.2f, 1.24f, 0.2f), new Vector3(0.07f, 0.2f, 0.07f), new Color(0.72f, 0.6f, 0.53f));

            if (leftHand == null && _leftArm != null)
            {
                _leftArm.gameObject.SetActive(false);
            }

            if (rightHand == null && _rightArm != null)
            {
                _rightArm.gameObject.SetActive(false);
            }

            CreateSimpleHand(leftHand, true);
            CreateSimpleHand(rightHand, false);

            if (createHeadAnchoredHands && head != null)
            {
                CreateHeadHandPreview(true);
                CreateHeadHandPreview(false);
            }
        }

        private void CreateSimpleHand(Transform handParent, bool isLeft)
        {
            if (handParent == null)
            {
                return;
            }

            var hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hand.name = isLeft ? "BodyHand_Left" : "BodyHand_Right";
            hand.transform.SetParent(handParent, false);
            hand.transform.localPosition = new Vector3(0f, 0f, 0.055f);
            hand.transform.localRotation = Quaternion.identity;
            hand.transform.localScale = new Vector3(0.07f, 0.03f, 0.1f);

            var col = hand.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var renderer = hand.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var color = isLeft ? new Color(0.77f, 0.65f, 0.56f) : new Color(0.73f, 0.61f, 0.54f);
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                renderer.sharedMaterial = mat;
            }
        }

        private void CreateHeadHandPreview(bool isLeft)
        {
            if (head == null)
            {
                return;
            }

            var existing = head.Find(isLeft ? "HeadHand_Left" : "HeadHand_Right");
            if (existing != null)
            {
                return;
            }

            var hand = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hand.name = isLeft ? "HeadHand_Left" : "HeadHand_Right";
            hand.transform.SetParent(head, false);
            hand.transform.localPosition = new Vector3(isLeft ? -0.17f : 0.17f, -0.28f, 0.4f);
            hand.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
            hand.transform.localScale = new Vector3(0.09f, 0.1f, 0.11f);

            var col = hand.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var renderer = hand.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var color = isLeft ? new Color(0.78f, 0.66f, 0.57f) : new Color(0.73f, 0.61f, 0.54f);
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                renderer.sharedMaterial = mat;
            }
        }

        private void HideControllerVisuals(Transform hand)
        {
            if (hand == null)
            {
                return;
            }

            var renderers = hand.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name.StartsWith("BodyHand_"))
                {
                    continue;
                }

                if (renderers[i].GetComponentInParent<DesktopMeleeTool>() != null)
                {
                    continue;
                }

                if (renderers[i].gameObject.name.Contains("Tool", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                renderers[i].enabled = false;
            }
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
                if (name.Contains("visual")) score += 10;
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

        private static Transform CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            var col = part.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 0.16f);
                }

                renderer.sharedMaterial = mat;
            }

            return part.transform;
        }
    }
}
