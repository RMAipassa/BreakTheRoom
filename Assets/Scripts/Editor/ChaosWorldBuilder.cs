using System;
using BreakTheRoom.Core;
using BreakTheRoom.Destruction;
using BreakTheRoom.Gameplay;
using BreakTheRoom.Integration;
using BreakTheRoom.Optimization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakTheRoom.EditorTools
{
    public static class ChaosWorldBuilder
    {
        private const string CreateNewMenuPath = "Tools/Break The Room/Create New Scene and Build Starter World";
        private const string BuildActiveMenuPath = "Tools/Break The Room/Build Starter World In Active Scene";
        private const string ForceXriMenuPath = "Tools/Break The Room/Force Rebuild With XRI Rig";
        private const string FixXriDesktopMenuPath = "Tools/Break The Room/Fix XRI Desktop Helpers In Scene";
        private const string RepairWearBridgeMenuPath = "Tools/Break The Room/Repair Wear Bridge In Scene";
        private const string DeleteMenuPath = "Tools/Break The Room/Delete Generated World";
        private const string RootName = "__BTR_GeneratedWorld";
        private const string GeneratedPath = "Assets/Generated";
        private const string MaterialsPath = GeneratedPath + "/Materials";
        private const string PrefabsPath = GeneratedPath + "/Prefabs";

        [MenuItem(CreateNewMenuPath)]
        public static void CreateNewSceneAndBuild()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before building a world.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildStarterWorld(scene, false);
        }

        [MenuItem(CreateNewMenuPath, true)]
        private static bool ValidateCreateNewSceneAndBuild()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(BuildActiveMenuPath)]
        public static void BuildInActiveScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before building a world.");
                return;
            }

            BuildStarterWorld(SceneManager.GetActiveScene(), false);
        }

        [MenuItem(BuildActiveMenuPath, true)]
        private static bool ValidateBuildInActiveScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(ForceXriMenuPath)]
        public static void ForceRebuildWithXriRig()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before building a world.");
                return;
            }

            BuildStarterWorld(SceneManager.GetActiveScene(), true);
        }

        [MenuItem(ForceXriMenuPath, true)]
        private static bool ValidateForceRebuildWithXriRig()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(FixXriDesktopMenuPath)]
        public static void FixXriDesktopHelpersInScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before fixing XRI helpers.");
                return;
            }

            var rig = FindXriRigInScene();
            if (rig == null)
            {
                Debug.LogWarning("Break The Room: No XRI rig found in scene.");
                return;
            }

            var cam = rig.GetComponentInChildren<Camera>(true);
            ConfigureXriBodyPresence(rig, cam != null ? cam.transform : null);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Break The Room: XRI desktop helpers repaired on current rig.");
        }

        [MenuItem(FixXriDesktopMenuPath, true)]
        private static bool ValidateFixXriDesktopHelpersInScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(RepairWearBridgeMenuPath)]
        public static void RepairWearBridgeInScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before repairing Wear bridge.");
                return;
            }

            var worldRoot = GameObject.Find(RootName);
            if (worldRoot == null)
            {
                Debug.LogWarning("Break The Room: No generated world found. Build a world first.");
                return;
            }

            var systemsRoot = worldRoot.transform.Find("GameSystems");
            if (systemsRoot == null)
            {
                var systems = new GameObject("GameSystems");
                Undo.RegisterCreatedObjectUndo(systems, "Create GameSystems");
                systems.transform.SetParent(worldRoot.transform);
                systemsRoot = systems.transform;
            }

            EnsureWearHealthBridge(systemsRoot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Break The Room: Wear bridge repaired in current scene.");
        }

        [MenuItem(RepairWearBridgeMenuPath, true)]
        private static bool ValidateRepairWearBridgeInScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(DeleteMenuPath)]
        public static void DeleteGeneratedWorld()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before deleting generated world.");
                return;
            }

            var existing = GameObject.Find(RootName);
            if (existing == null)
            {
                Debug.Log("Break The Room: No generated world found.");
                return;
            }

            Undo.DestroyObjectImmediate(existing);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Break The Room: Generated world removed.");
        }

        [MenuItem(DeleteMenuPath, true)]
        private static bool ValidateDeleteGeneratedWorld()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void BuildStarterWorld(Scene scene, bool requireXriRig)
        {
            if (!scene.IsValid())
            {
                Debug.LogError("Break The Room: Active scene is not valid.");
                return;
            }

            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                var replace = EditorUtility.DisplayDialog(
                    "Generated World Exists",
                    "A generated world already exists in this scene. Replace it?",
                    "Replace",
                    "Cancel");

                if (!replace)
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing);
            }

            EnsureAssetFolder(GeneratedPath);
            EnsureAssetFolder(MaterialsPath);
            EnsureAssetFolder(PrefabsPath);

            var roomMaterial = GetOrCreateMaterial(MaterialsPath + "/Mat_Room.mat", new Color(0.64f, 0.65f, 0.67f), 0.14f);
            var propMaterial = GetOrCreateMaterial(MaterialsPath + "/Mat_Prop.mat", new Color(0.47f, 0.31f, 0.18f), 0.08f);
            var supportMaterial = GetOrCreateMaterial(MaterialsPath + "/Mat_Support.mat", new Color(0.34f, 0.37f, 0.42f), 0.1f);

            var fractureCrate = CreateFracturePrefab(PrefabsPath + "/Fracture_Crate.prefab", propMaterial, new Vector3(0.8f, 0.8f, 0.8f));
            var fractureBarrel = CreateFracturePrefab(PrefabsPath + "/Fracture_Barrel.prefab", propMaterial, new Vector3(0.7f, 1.1f, 0.7f));
            var fractureSupport = CreateFracturePrefab(PrefabsPath + "/Fracture_Support.prefab", supportMaterial, new Vector3(0.6f, 0.4f, 0.6f));

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Break The Room World");

            CreateGameSystems(root.transform);
            var createdRig = CreatePlayerRig(root.transform, requireXriRig);
            if (!createdRig && requireXriRig)
            {
                Undo.DestroyObjectImmediate(root);
                Debug.LogError("Break The Room: XRI rig prefab not found. Import XR Interaction Toolkit Starter Assets, then run Force Rebuild again.");
                return;
            }
            CreateLighting(root.transform);
            CreateRoomShell(root.transform, roomMaterial);
            CreatePropField(root.transform, fractureCrate, fractureBarrel, propMaterial);
            CreateSupportStructures(root.transform, fractureSupport, supportMaterial);
            CreateDesktopToolStation(root.transform);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Break The Room: Starter VR world generated.");
        }

        private static void CreateGameSystems(Transform parent)
        {
            var systems = new GameObject("GameSystems");
            systems.transform.SetParent(parent);

            systems.AddComponent<ChaosGameManager>();
            systems.AddComponent<TargetValueObjective>();
            EnsureWearHealthBridge(systems.transform);
        }

        public static void EnsureWearHealthBridge(Transform systemsRoot)
        {
            if (systemsRoot == null)
            {
                return;
            }

            var bridgeTransform = systemsRoot.Find("WearBridge");
            GameObject bridge;
            if (bridgeTransform == null)
            {
                bridge = new GameObject("WearBridge");
                Undo.RegisterCreatedObjectUndo(bridge, "Create Wear Bridge");
                bridge.transform.SetParent(systemsRoot);
            }
            else
            {
                bridge = bridgeTransform.gameObject;
            }

            var receiver = bridge.GetComponent<WearHealthUdpReceiver>();
            if (receiver == null)
            {
                receiver = Undo.AddComponent<WearHealthUdpReceiver>(bridge);
            }

            var receiverSO = new SerializedObject(receiver);
            var listenPortProp = receiverSO.FindProperty("listenPort");
            if (listenPortProp != null)
            {
                listenPortProp.intValue = 7777;
            }
            var autoStartProp = receiverSO.FindProperty("autoStartOnEnable");
            if (autoStartProp != null)
            {
                autoStartProp.boolValue = true;
            }
            receiverSO.ApplyModifiedPropertiesWithoutUndo();

            var chaosBridge = bridge.GetComponent<WearHealthChaosBridge>();
            if (chaosBridge == null)
            {
                chaosBridge = Undo.AddComponent<WearHealthChaosBridge>(bridge);
            }

            var chaosSO = new SerializedObject(chaosBridge);
            var chaosReceiverProp = chaosSO.FindProperty("receiver");
            if (chaosReceiverProp != null)
            {
                chaosReceiverProp.objectReferenceValue = receiver;
            }
            chaosSO.ApplyModifiedPropertiesWithoutUndo();

            var hud = bridge.GetComponent<WearHealthHud>();
            if (hud == null)
            {
                hud = Undo.AddComponent<WearHealthHud>(bridge);
            }

            var hudSO = new SerializedObject(hud);
            var hudReceiverProp = hudSO.FindProperty("receiver");
            if (hudReceiverProp != null)
            {
                hudReceiverProp.objectReferenceValue = receiver;
            }
            hudSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool CreatePlayerRig(Transform parent, bool requireXriRig)
        {
            if (TryCreateXriRig(parent, out var prefabPath))
            {
                Debug.Log($"Break The Room: Using XRI rig prefab: {prefabPath}");
                return true;
            }

            if (requireXriRig)
            {
                return false;
            }

            var rig = new GameObject("XROrigin_Fallback");
            rig.transform.SetParent(parent);
            rig.transform.position = new Vector3(0f, 0f, -5f);

            var cameraRoot = new GameObject("CameraOffset");
            cameraRoot.transform.SetParent(rig.transform);
            cameraRoot.transform.localPosition = Vector3.zero;

            var cameraObj = new GameObject("Main Camera");
            cameraObj.transform.SetParent(cameraRoot.transform);
            cameraObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            cameraObj.tag = "MainCamera";
            cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            var controller = cameraObj.AddComponent<BreakTheRoom.Player.DesktopPlaytestController>();

            var bodyRoot = CreateFallbackBody(
                rig.transform,
                cameraObj.transform,
                out var rightArmPivot,
                out var rightFist,
                out var rightToolTip,
                out var rightHandMount,
                out var primaryHolster,
                out var secondaryHolster);
            SetSerializedObject(controller, "movementRoot", rig.transform);
            SetSerializedObject(controller, "rightArmPivot", rightArmPivot);
            SetSerializedObject(controller, "rightFistAnchor", rightFist);
            SetSerializedObject(controller, "swingToolTip", rightToolTip);
            SetSerializedObject(controller, "rightHandMount", rightHandMount);
            SetSerializedObject(controller, "primaryHolster", primaryHolster);
            SetSerializedObject(controller, "secondaryHolster", secondaryHolster);

            _ = bodyRoot;
            CreateHandVisual(cameraObj.transform, "LeftHand", new Vector3(-0.23f, -0.24f, 0.48f), true, true);
            CreateHandVisual(cameraObj.transform, "RightHand", new Vector3(0.23f, -0.24f, 0.48f), false, true);

            // Keep this as a neutral fallback camera rig for no-headset testing.
            // Use the XR Interaction Toolkit XROrigin prefab for real headset play.
            Debug.Log("Break The Room: XRI rig prefab not found, using fallback desktop rig.");
            return true;
        }

        private static GameObject CreateFallbackBody(
            Transform rigRoot,
            Transform cameraTransform,
            out Transform rightArmPivot,
            out Transform rightFist,
            out Transform rightToolTip,
            out Transform rightHandMount,
            out Transform primaryHolster,
            out Transform secondaryHolster)
        {
            var body = new GameObject("DesktopAvatarBody");
            body.transform.SetParent(rigRoot);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;

            var torso = CreateBodyPart(body.transform, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(0.38f, 0.46f, 0.24f), new Color(0.24f, 0.28f, 0.35f));
            _ = torso;
            CreateBodyPart(body.transform, "Hips", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.3f, 0.18f, 0.2f), new Color(0.16f, 0.18f, 0.22f));

            var headAnchor = new GameObject("HeadAnchor");
            headAnchor.transform.SetParent(body.transform);
            headAnchor.transform.localPosition = new Vector3(0f, 1.55f, 0f);

            var head = CreateBodyPart(headAnchor.transform, "Head", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), new Color(0.78f, 0.66f, 0.56f));
            _ = head;

            CreateBodyPart(body.transform, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.09f, 0.35f, 0f), new Vector3(0.11f, 0.33f, 0.11f), new Color(0.16f, 0.18f, 0.22f));
            CreateBodyPart(body.transform, "RightLeg", PrimitiveType.Capsule, new Vector3(0.09f, 0.35f, 0f), new Vector3(0.11f, 0.33f, 0.11f), new Color(0.16f, 0.18f, 0.22f));

            rightArmPivot = new GameObject("RightArmPivot").transform;
            rightArmPivot.SetParent(body.transform);
            rightArmPivot.localPosition = new Vector3(0.26f, 1.28f, 0.04f);
            rightArmPivot.localRotation = Quaternion.Euler(6f, 0f, 0f);
            CreateBodyPart(rightArmPivot, "RightUpperArm", PrimitiveType.Capsule, new Vector3(0.11f, -0.09f, 0.02f), new Vector3(0.08f, 0.2f, 0.08f), new Color(0.24f, 0.28f, 0.35f));
            CreateBodyPart(rightArmPivot, "RightForearm", PrimitiveType.Capsule, new Vector3(0.18f, -0.25f, 0.06f), new Vector3(0.07f, 0.17f, 0.07f), new Color(0.24f, 0.28f, 0.35f));
            rightFist = CreateBodyPart(rightArmPivot, "RightFist", PrimitiveType.Sphere, new Vector3(0.21f, -0.36f, 0.12f), new Vector3(0.1f, 0.08f, 0.1f), new Color(0.76f, 0.63f, 0.54f)).transform;
            rightToolTip = rightFist;

            var leftArmPivot = new GameObject("LeftArmPivot").transform;
            leftArmPivot.SetParent(body.transform);
            leftArmPivot.localPosition = new Vector3(-0.26f, 1.28f, 0.04f);
            leftArmPivot.localRotation = Quaternion.Euler(6f, 0f, 0f);
            CreateBodyPart(leftArmPivot, "LeftUpperArm", PrimitiveType.Capsule, new Vector3(-0.11f, -0.09f, 0.02f), new Vector3(0.08f, 0.2f, 0.08f), new Color(0.24f, 0.28f, 0.35f));
            CreateBodyPart(leftArmPivot, "LeftForearm", PrimitiveType.Capsule, new Vector3(-0.18f, -0.25f, 0.06f), new Vector3(0.07f, 0.17f, 0.07f), new Color(0.24f, 0.28f, 0.35f));
            CreateBodyPart(leftArmPivot, "LeftFist", PrimitiveType.Sphere, new Vector3(-0.21f, -0.36f, 0.12f), new Vector3(0.1f, 0.08f, 0.1f), new Color(0.79f, 0.67f, 0.56f));

            rightHandMount = new GameObject("RightHandMount").transform;
            rightHandMount.SetParent(rightFist);
            rightHandMount.localPosition = new Vector3(0f, -0.02f, 0.02f);
            rightHandMount.localRotation = Quaternion.identity;

            primaryHolster = new GameObject("PrimaryHolster").transform;
            primaryHolster.SetParent(body.transform);
            primaryHolster.localPosition = new Vector3(-0.22f, 0.82f, -0.1f);
            primaryHolster.localRotation = Quaternion.Euler(0f, 28f, 90f);

            secondaryHolster = new GameObject("SecondaryHolster").transform;
            secondaryHolster.SetParent(body.transform);
            secondaryHolster.localPosition = new Vector3(0.22f, 0.82f, -0.1f);
            secondaryHolster.localRotation = Quaternion.Euler(0f, -28f, -90f);

            if (cameraTransform != null)
            {
                body.transform.position = new Vector3(cameraTransform.position.x, rigRoot.position.y, cameraTransform.position.z);
            }

            return body;
        }

        private static GameObject CreateBodyPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPos, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            var col = part.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.DestroyImmediate(col);
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
                    mat.SetFloat("_Smoothness", 0.18f);
                }

                renderer.sharedMaterial = mat;
            }

            return part;
        }

        private static GameObject CreateToolPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPos, Vector3 localScale, Color color, bool keepCollider)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

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
                    mat.SetFloat("_Smoothness", 0.22f);
                }

                renderer.sharedMaterial = mat;
            }

            if (!keepCollider)
            {
                var col = part.GetComponent<Collider>();
                if (col != null)
                {
                    UnityEngine.Object.DestroyImmediate(col);
                }
            }

            return part;
        }

        private static void CreateDesktopToolStation(Transform parent)
        {
            var station = new GameObject("DesktopToolStation");
            station.transform.SetParent(parent);

            CreateStaticCube("ToolBench", station.transform, new Vector3(-7f, 0.45f, -7.4f), new Vector3(2.4f, 0.9f, 0.8f), GetOrCreateMaterial(MaterialsPath + "/Mat_Support.mat", new Color(0.34f, 0.37f, 0.42f), 0.1f));

            CreateToolBat(station.transform, new Vector3(-7.55f, 1.08f, -7.4f));
            CreateToolHammer(station.transform, new Vector3(-7.0f, 1.08f, -7.4f));
            CreateToolCrowbar(station.transform, new Vector3(-6.45f, 1.08f, -7.4f));
        }

        private static void CreateToolBat(Transform parent, Vector3 position)
        {
            var root = new GameObject("Tool_Bat");
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(8f, 0f, 90f);

            CreateToolPart(root.transform, "Handle", PrimitiveType.Cylinder, new Vector3(0f, -0.14f, 0f), new Vector3(0.03f, 0.18f, 0.03f), new Color(0.39f, 0.23f, 0.11f), true);
            CreateToolPart(root.transform, "Head", PrimitiveType.Cylinder, new Vector3(0f, -0.35f, 0f), new Vector3(0.045f, 0.12f, 0.045f), new Color(0.17f, 0.17f, 0.18f), true);

            var tip = new GameObject("ToolTip");
            tip.transform.SetParent(root.transform);
            tip.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            tip.transform.localRotation = Quaternion.identity;

            var grip = new GameObject("Grip");
            grip.transform.SetParent(root.transform);
            grip.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            grip.transform.localRotation = Quaternion.identity;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var tool = root.AddComponent<BreakTheRoom.Player.DesktopMeleeTool>();
            TryAddGrabInteractable(root);
            SetSerializedString(tool, "toolName", "Bat");
            SetSerializedFloat(tool, "swingDamage", 62f);
            SetSerializedFloat(tool, "swingImpulse", 8.6f);
            SetSerializedFloat(tool, "swingRadius", 0.22f);
            SetSerializedFloat(tool, "swingReachOffset", 0.46f);
            SetSerializedObject(tool, "tipTransform", tip.transform);
            SetSerializedObject(tool, "gripTransform", grip.transform);
            SetSerializedVector3(tool, "holdPositionOffset", new Vector3(-0.03f, -0.04f, 0.01f));
            SetSerializedVector3(tool, "holdEulerOffset", new Vector3(220f, 175f, -45f));
            SetSerializedBool(tool, "flipViewYaw180", true);
            SetSerializedBool(tool, "reverseSwingArc", false);
        }

        private static void CreateToolHammer(Transform parent, Vector3 position)
        {
            var root = new GameObject("Tool_Hammer");
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(14f, 0f, 88f);

            CreateToolPart(root.transform, "Handle", PrimitiveType.Cylinder, new Vector3(0f, -0.11f, 0f), new Vector3(0.02f, 0.16f, 0.02f), new Color(0.4f, 0.25f, 0.13f), true);
            CreateToolPart(root.transform, "Head", PrimitiveType.Cube, new Vector3(0f, -0.3f, 0f), new Vector3(0.12f, 0.06f, 0.06f), new Color(0.2f, 0.2f, 0.22f), true);

            var tip = new GameObject("ToolTip");
            tip.transform.SetParent(root.transform);
            tip.transform.localPosition = new Vector3(0f, -0.33f, 0.04f);
            tip.transform.localRotation = Quaternion.identity;

            var grip = new GameObject("Grip");
            grip.transform.SetParent(root.transform);
            grip.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            grip.transform.localRotation = Quaternion.identity;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1.25f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var tool = root.AddComponent<BreakTheRoom.Player.DesktopMeleeTool>();
            TryAddGrabInteractable(root);
            SetSerializedString(tool, "toolName", "Hammer");
            SetSerializedFloat(tool, "swingDamage", 74f);
            SetSerializedFloat(tool, "swingImpulse", 9.2f);
            SetSerializedFloat(tool, "swingRadius", 0.17f);
            SetSerializedFloat(tool, "swingReachOffset", 0.4f);
            SetSerializedObject(tool, "tipTransform", tip.transform);
            SetSerializedObject(tool, "gripTransform", grip.transform);
            SetSerializedVector3(tool, "holdPositionOffset", new Vector3(-0.03f, -0.04f, 0.01f));
            SetSerializedVector3(tool, "holdEulerOffset", new Vector3(220f, 175f, -45f));
            SetSerializedBool(tool, "flipViewYaw180", true);
            SetSerializedBool(tool, "reverseSwingArc", false);
        }

        private static void CreateToolCrowbar(Transform parent, Vector3 position)
        {
            var root = new GameObject("Tool_Crowbar");
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(12f, 0f, 90f);

            CreateToolPart(root.transform, "Shaft", PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f), new Vector3(0.017f, 0.23f, 0.017f), new Color(0.66f, 0.14f, 0.12f), true);
            CreateToolPart(root.transform, "Hook", PrimitiveType.Cube, new Vector3(0f, -0.46f, 0.02f), new Vector3(0.04f, 0.03f, 0.05f), new Color(0.68f, 0.15f, 0.13f), true);

            var tip = new GameObject("ToolTip");
            tip.transform.SetParent(root.transform);
            tip.transform.localPosition = new Vector3(0f, -0.48f, 0.02f);
            tip.transform.localRotation = Quaternion.identity;

            var grip = new GameObject("Grip");
            grip.transform.SetParent(root.transform);
            grip.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            grip.transform.localRotation = Quaternion.identity;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1.1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var tool = root.AddComponent<BreakTheRoom.Player.DesktopMeleeTool>();
            TryAddGrabInteractable(root);
            SetSerializedString(tool, "toolName", "Crowbar");
            SetSerializedFloat(tool, "swingDamage", 52f);
            SetSerializedFloat(tool, "swingImpulse", 6.8f);
            SetSerializedFloat(tool, "swingRadius", 0.14f);
            SetSerializedFloat(tool, "swingReachOffset", 0.52f);
            SetSerializedObject(tool, "tipTransform", tip.transform);
            SetSerializedObject(tool, "gripTransform", grip.transform);
            SetSerializedVector3(tool, "holdPositionOffset", new Vector3(-0.03f, -0.04f, 0.01f));
            SetSerializedVector3(tool, "holdEulerOffset", new Vector3(220f, 175f, -45f));
            SetSerializedBool(tool, "flipViewYaw180", true);
            SetSerializedBool(tool, "reverseSwingArc", false);
        }

        private static bool TryCreateXriRig(Transform parent, out string prefabPath)
        {
            prefabPath = string.Empty;
            EnsureInteractionManager(parent);

            var rigPrefab = FindXriRigPrefab(out prefabPath);
            if (rigPrefab == null)
            {
                return false;
            }

            var instance = PrefabUtility.InstantiatePrefab(rigPrefab) as GameObject;
            if (instance == null)
            {
                return false;
            }

            instance.name = "XROrigin_XRI";
            instance.transform.SetParent(parent);
            instance.transform.position = new Vector3(0f, 0f, -5f);
            instance.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(instance, "Create XRI Rig");

            var cameraTransform = FindChildByName(instance.transform, "Main Camera");
            if (cameraTransform != null)
            {
                var cameraGo = cameraTransform.gameObject;
                cameraGo.tag = "MainCamera";

                EnsureSingleAudioListener(cameraGo);
                EnsurePrimaryCamera(cameraGo);
            }

            EnsureXriHandVisuals(instance.transform);
            ConfigureXriBodyPresence(instance, cameraTransform);

            return true;
        }

        private static GameObject FindXriRigInScene()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                var lower = root.name.ToLowerInvariant();
                if (lower.Contains("xrorigin") || lower.Contains("xr origin"))
                {
                    return root;
                }

                var candidate = FindChildByContains(root.transform, "xrorigin") ?? FindChildByContains(root.transform, "xr origin");
                if (candidate != null)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static void EnsureSingleAudioListener(GameObject primaryCamera)
        {
            var primaryListener = primaryCamera != null ? primaryCamera.GetComponent<AudioListener>() : null;
            if (primaryListener == null && primaryCamera != null)
            {
                primaryListener = primaryCamera.AddComponent<AudioListener>();
            }

            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null)
                {
                    continue;
                }

                listener.enabled = listener == primaryListener;
            }
        }

        private static void EnsurePrimaryCamera(GameObject primaryCamera)
        {
            if (primaryCamera == null)
            {
                return;
            }

            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam == null)
                {
                    continue;
                }

                if (cam.gameObject == primaryCamera)
                {
                    cam.enabled = true;
                    continue;
                }

                if (cam.gameObject.name.Contains("Scene", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                cam.enabled = false;
            }
        }

        private static void ConfigureXriBodyPresence(GameObject rigInstance, Transform cameraTransform)
        {
            if (rigInstance == null)
            {
                return;
            }

            if (cameraTransform == null)
            {
                var anyCamera = rigInstance.GetComponentInChildren<Camera>(true);
                cameraTransform = anyCamera != null ? anyCamera.transform : null;
            }

            if (cameraTransform == null)
            {
                Debug.LogWarning("Break The Room: Could not find camera under XRI rig for desktop fallback bindings.");
                return;
            }

            var body = rigInstance.GetComponent<BreakTheRoom.Player.XrBodyPresenceDriver>();
            if (body == null)
            {
                body = rigInstance.AddComponent<BreakTheRoom.Player.XrBodyPresenceDriver>();
            }

            var left = FindChildByContains(rigInstance.transform, "left hand")
                ?? FindChildByContains(rigInstance.transform, "leftcontroller")
                ?? FindChildByContains(rigInstance.transform, "left controller")
                ?? FindChildByContains(rigInstance.transform, "left");

            var right = FindChildByContains(rigInstance.transform, "right hand")
                ?? FindChildByContains(rigInstance.transform, "rightcontroller")
                ?? FindChildByContains(rigInstance.transform, "right controller")
                ?? FindChildByContains(rigInstance.transform, "right");

            SetSerializedObject(body, "head", cameraTransform);
            SetSerializedObject(body, "leftHand", left);
            SetSerializedObject(body, "rightHand", right);
            SetSerializedBool(body, "hideControllerModels", true);
            SetSerializedBool(body, "usePhysicsBody", false);

            var fallbackLocomotion = rigInstance.GetComponent<BreakTheRoom.Player.DesktopXrFallbackLocomotion>();
            if (fallbackLocomotion == null)
            {
                fallbackLocomotion = rigInstance.AddComponent<BreakTheRoom.Player.DesktopXrFallbackLocomotion>();
            }

            SetSerializedObject(fallbackLocomotion, "head", cameraTransform);
            SetSerializedObject(fallbackLocomotion, "lookPivot", cameraTransform.parent);
            SetSerializedFloat(fallbackLocomotion, "turnSpeed", 230f);
            SetSerializedFloat(fallbackLocomotion, "lookSpeed", 175f);

            var toolInteractor = rigInstance.GetComponent<BreakTheRoom.Player.XrDesktopToolInteractor>();
            if (toolInteractor == null)
            {
                toolInteractor = rigInstance.AddComponent<BreakTheRoom.Player.XrDesktopToolInteractor>();
            }

            SetSerializedObject(toolInteractor, "head", cameraTransform);
            SetSerializedObject(toolInteractor, "rightHand", right);
            SetSerializedBool(toolInteractor, "firstPersonToolView", true);
            SetSerializedVector3(toolInteractor, "toolLocalPosition", new Vector3(0.18f, -0.18f, 0.36f));
            SetSerializedVector3(toolInteractor, "toolLocalEuler", new Vector3(6f, 12f, 52f));

            if (rigInstance.GetComponent<BreakTheRoom.Player.XrDesktopControlsOverlay>() == null)
            {
                rigInstance.AddComponent<BreakTheRoom.Player.XrDesktopControlsOverlay>();
            }

            Debug.Log("Break The Room: XRI desktop fallback controls + tool debug attached.");
        }

        private static void EnsureInteractionManager(Transform parent)
        {
            if (GameObject.Find("XR Interaction Manager") != null)
            {
                return;
            }

            var managerType =
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager, Unity.XR.Interaction.Toolkit")
                ?? Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interaction.XRIInteractionManager, Unity.XR.Interaction.Toolkit");

            if (managerType == null)
            {
                return;
            }

            var managerObject = new GameObject("XR Interaction Manager");
            managerObject.transform.SetParent(parent);
            managerObject.AddComponent(managerType);
        }

        private static GameObject FindXriRigPrefab(out string selectedPath)
        {
            selectedPath = string.Empty;
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var best = default(GameObject);
            var bestPath = string.Empty;

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var lower = path.ToLowerInvariant();

                var isLikelyRig =
                    lower.Contains("xri default xr rig")
                    || lower.Contains("xr origin (xr rig)")
                    || (lower.Contains("xr origin") && lower.Contains("starter"));

                if (!isLikelyRig)
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                if (best == null)
                {
                    best = prefab;
                    bestPath = path;
                }

                if (lower.Contains("xri default xr rig"))
                {
                    selectedPath = path;
                    return prefab;
                }
            }

            selectedPath = bestPath;
            return best;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildByName(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void EnsureXriHandVisuals(Transform rigRoot)
        {
            var left = FindChildByContains(rigRoot, "left") ?? FindChildByContains(rigRoot, "l hand");
            var right = FindChildByContains(rigRoot, "right") ?? FindChildByContains(rigRoot, "r hand");

            if (left != null && left.GetComponentInChildren<Renderer>() == null)
            {
                CreateHandVisual(left, "LeftHandMesh", new Vector3(0f, 0f, 0f), true, false);
            }

            if (right != null && right.GetComponentInChildren<Renderer>() == null)
            {
                CreateHandVisual(right, "RightHandMesh", new Vector3(0f, 0f, 0f), false, false);
            }
        }

        private static Transform FindChildByContains(Transform root, string token)
        {
            var lower = root.name.ToLowerInvariant();
            if (lower.Contains(token))
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildByContains(root.GetChild(i), token);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void CreateHandVisual(Transform parent, string handName, Vector3 localPosition, bool isLeft, bool addPoseDriver)
        {
            var hand = new GameObject(handName);
            hand.name = handName;
            hand.transform.SetParent(parent);
            hand.transform.localPosition = localPosition;
            hand.transform.localRotation = Quaternion.Euler(88f, 0f, 0f);
            hand.transform.localScale = Vector3.one;

            var palm = CreateHandPart(hand.transform, "Palm", new Vector3(0f, 0f, 0f), new Vector3(0.07f, 0.03f, 0.1f), isLeft);

            CreateFinger(hand.transform, "Index", new Vector3(isLeft ? -0.022f : 0.022f, 0.005f, 0.05f), isLeft);
            CreateFinger(hand.transform, "Middle", new Vector3(isLeft ? -0.007f : 0.007f, 0.008f, 0.055f), isLeft);
            CreateFinger(hand.transform, "Ring", new Vector3(isLeft ? 0.008f : -0.008f, 0.006f, 0.052f), isLeft);
            CreateFinger(hand.transform, "Pinky", new Vector3(isLeft ? 0.022f : -0.022f, 0.003f, 0.045f), isLeft);

            var thumb = new GameObject("Thumb");
            thumb.transform.SetParent(hand.transform);
            thumb.transform.localPosition = new Vector3(isLeft ? -0.038f : 0.038f, -0.01f, 0.012f);
            thumb.transform.localRotation = Quaternion.Euler(-15f, isLeft ? -32f : 32f, isLeft ? -25f : 25f);
            CreateHandPart(thumb.transform, "ThumbSegment", new Vector3(0f, 0f, 0.02f), new Vector3(0.018f, 0.018f, 0.04f), isLeft);

            if (addPoseDriver)
            {
                var driver = hand.AddComponent<BreakTheRoom.Player.SimpleHandPoseDriver>();
                SetSerializedBool(driver, "isLeftHand", isLeft);
            }
            _ = palm;
        }

        private static void CreateFinger(Transform handRoot, string name, Vector3 position, bool isLeft)
        {
            var fingerRoot = new GameObject(name);
            fingerRoot.transform.SetParent(handRoot);
            fingerRoot.transform.localPosition = position;
            fingerRoot.transform.localRotation = Quaternion.identity;

            CreateHandPart(fingerRoot.transform, name + "A", new Vector3(0f, 0f, 0.02f), new Vector3(0.015f, 0.015f, 0.04f), isLeft);
            CreateHandPart(fingerRoot.transform, name + "B", new Vector3(0f, 0f, 0.055f), new Vector3(0.013f, 0.013f, 0.03f), isLeft);
        }

        private static GameObject CreateHandPart(Transform parent, string name, Vector3 localPos, Vector3 localScale, bool isLeft)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            var col = part.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.DestroyImmediate(col);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var color = isLeft ? new Color(0.78f, 0.64f, 0.55f) : new Color(0.71f, 0.58f, 0.5f);
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 0.2f);
                }

                renderer.sharedMaterial = mat;
            }

            return part;
        }

        private static void CreateLighting(Transform parent)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.5f);

            var sunObj = new GameObject("Directional Light");
            sunObj.transform.SetParent(parent);
            sunObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.7f;
            sun.color = new Color(1f, 0.96f, 0.9f);

            var fillObj = new GameObject("Room Fill Light");
            fillObj.transform.SetParent(parent);
            fillObj.transform.position = new Vector3(0f, 5f, 0f);
            var fill = fillObj.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 30f;
            fill.intensity = 1.7f;
            fill.color = new Color(0.85f, 0.9f, 1f);

            var backFillObj = new GameObject("Back Fill Light");
            backFillObj.transform.SetParent(parent);
            backFillObj.transform.position = new Vector3(0f, 4f, -8f);
            var backFill = backFillObj.AddComponent<Light>();
            backFill.type = LightType.Point;
            backFill.range = 20f;
            backFill.intensity = 1.1f;
            backFill.color = new Color(0.78f, 0.82f, 0.95f);
        }

        private static void CreateRoomShell(Transform parent, Material roomMaterial)
        {
            var room = new GameObject("RoomShell");
            room.transform.SetParent(parent);

            CreateStaticCube("Floor", room.transform, new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f), roomMaterial);
            CreateStaticCube("Ceiling", room.transform, new Vector3(0f, 8.5f, 0f), new Vector3(20f, 1f, 20f), roomMaterial);
            CreateStaticCube("Wall_PosX", room.transform, new Vector3(10.5f, 4f, 0f), new Vector3(1f, 8f, 20f), roomMaterial);
            CreateStaticCube("Wall_NegX", room.transform, new Vector3(-10.5f, 4f, 0f), new Vector3(1f, 8f, 20f), roomMaterial);
            CreateStaticCube("Wall_PosZ", room.transform, new Vector3(0f, 4f, 10.5f), new Vector3(20f, 8f, 1f), roomMaterial);
            CreateStaticCube("Wall_NegZ", room.transform, new Vector3(0f, 4f, -10.5f), new Vector3(20f, 8f, 1f), roomMaterial);
        }

        private static void CreatePropField(Transform parent, GameObject crateFracture, GameObject barrelFracture, Material propMaterial)
        {
            var propGroup = new GameObject("DestructibleProps");
            propGroup.transform.SetParent(parent);

            var origin = new Vector3(-6f, 0.7f, -1f);
            var cols = 5;
            var rows = 4;

            for (var z = 0; z < rows; z++)
            {
                for (var x = 0; x < cols; x++)
                {
                    var isBarrel = (x + z) % 2 == 0;
                    var type = isBarrel ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                    var size = isBarrel ? new Vector3(0.7f, 1.1f, 0.7f) : new Vector3(0.8f, 0.8f, 0.8f);
                    var position = origin + new Vector3(x * 2.25f, 0f, z * 2.15f);

                    var obj = CreateDestructiblePrimitive(
                        isBarrel ? "Barrel" : "Crate",
                        type,
                        position,
                        Quaternion.identity,
                        size,
                        propGroup.transform,
                        propMaterial,
                        isBarrel ? barrelFracture : crateFracture,
                        isBarrel ? 85f : 65f,
                        isBarrel ? 70 : 45,
                        isBarrel ? DestructionFeedback.SurfaceType.Metal : DestructionFeedback.SurfaceType.Wood);

                    var rb = obj.GetComponent<Rigidbody>();
                    rb.mass = isBarrel ? 5.5f : 3.5f;
                    rb.linearDamping = 0.15f;
                    rb.angularDamping = 0.1f;
                }
            }
        }

        private static void CreateSupportStructures(Transform parent, GameObject fracturePrefab, Material supportMaterial)
        {
            var structureGroup = new GameObject("SupportStructures");
            structureGroup.transform.SetParent(parent);

            CreateSupportStack(structureGroup.transform, new Vector3(5f, 0.2f, -3.5f), fracturePrefab, supportMaterial, 8, 0.45f, 95f, 60);
            CreateSupportStack(structureGroup.transform, new Vector3(7.2f, 0.2f, -3.5f), fracturePrefab, supportMaterial, 8, 0.45f, 95f, 60);

            var beam = CreateDestructiblePrimitive(
                "CrossBeam",
                PrimitiveType.Cube,
                new Vector3(6.1f, 4.2f, -3.5f),
                Quaternion.identity,
                new Vector3(2.8f, 0.35f, 0.6f),
                structureGroup.transform,
                supportMaterial,
                fracturePrefab,
                140f,
                120,
                DestructionFeedback.SurfaceType.Concrete);

            beam.GetComponent<Rigidbody>().mass = 8.5f;
        }

        private static void CreateSupportStack(
            Transform parent,
            Vector3 basePosition,
            GameObject fracturePrefab,
            Material supportMaterial,
            int levels,
            float stepY,
            float health,
            int score)
        {
            Rigidbody previous = null;

            for (var i = 0; i < levels; i++)
            {
                var pos = basePosition + new Vector3(0f, i * stepY, 0f);
                var segment = CreateDestructiblePrimitive(
                    "SupportSegment",
                    PrimitiveType.Cube,
                    pos,
                    Quaternion.identity,
                    new Vector3(0.6f, 0.4f, 0.6f),
                    parent,
                    supportMaterial,
                    fracturePrefab,
                    health,
                    score,
                    DestructionFeedback.SurfaceType.Concrete);

                var rb = segment.GetComponent<Rigidbody>();
                rb.mass = 4f;
                rb.linearDamping = 0.05f;

                if (i == 0)
                {
                    rb.isKinematic = true;
                }

                if (previous != null)
                {
                    var joint = segment.AddComponent<FixedJoint>();
                    joint.connectedBody = previous;

                    var link = segment.AddComponent<StructuralLink>();
                    SetSerializedFloat(link, "breakForceThreshold", 1800f);
                    SetSerializedFloat(link, "breakTorqueThreshold", 900f);
                    SetSerializedFloat(link, "supportDamageOnSnap", 45f);
                }

                previous = rb;
            }
        }

        private static GameObject CreateDestructiblePrimitive(
            string baseName,
            PrimitiveType primitiveType,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Transform parent,
            Material material,
            GameObject fracturePrefab,
            float health,
            int scoreValue,
            DestructionFeedback.SurfaceType surfaceType)
        {
            var obj = GameObject.CreatePrimitive(primitiveType);
            obj.name = baseName;
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = scale;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var rb = obj.AddComponent<Rigidbody>();
            obj.AddComponent<ImpactDamageDealer>();
            obj.AddComponent<ScoreOnBreak>();
            obj.AddComponent<ResettablePhysicsObject>();
            var feedback = obj.AddComponent<DestructionFeedback>();
            var breakable = obj.AddComponent<BreakablePiece>();
            TryAddGrabInteractable(obj);

            SetSerializedFloat(breakable, "maxHealth", health);
            SetSerializedFloat(breakable, "minImpulseToBreak", 0f);
            SetSerializedObject(breakable, "fracturePrefab", fracturePrefab);
            SetSerializedFloat(breakable, "chunkImpulseMultiplier", 0.8f);
            SetSerializedInt(breakable, "chaosValue", scoreValue);
            SetSerializedInt(feedback, "surfaceType", (int)surfaceType);

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            return obj;
        }

        private static void TryAddGrabInteractable(GameObject target)
        {
            var grabType =
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit")
                ?? Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");

            if (grabType == null || target.GetComponent(grabType) != null)
            {
                return;
            }

            target.AddComponent(grabType);
        }

        private static void CreateStaticCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;

            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
        }

        private static GameObject CreateFracturePrefab(string prefabPath, Material material, Vector3 sourceSize)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            root.AddComponent<DespawnAfterTime>();

            const int grid = 2;
            var chunkSize = new Vector3(sourceSize.x / grid, sourceSize.y / grid, sourceSize.z / grid);

            for (var x = 0; x < grid; x++)
            {
                for (var y = 0; y < grid; y++)
                {
                    for (var z = 0; z < grid; z++)
                    {
                        var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        chunk.name = "Chunk";
                        chunk.transform.SetParent(root.transform);
                        var scaleJitter = 0.82f + UnityEngine.Random.value * 0.2f;
                        chunk.transform.localScale = chunkSize * scaleJitter;
                        chunk.transform.localPosition = new Vector3(
                            (x - 0.5f) * chunkSize.x,
                            (y - 0.5f) * chunkSize.y,
                            (z - 0.5f) * chunkSize.z);
                        chunk.transform.localPosition += UnityEngine.Random.insideUnitSphere * (chunkSize.magnitude * 0.08f);

                        var renderer = chunk.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.sharedMaterial = material;
                        }

                        var rb = chunk.AddComponent<Rigidbody>();
                        rb.mass = 0.2f + UnityEngine.Random.value * 0.2f;
                        rb.linearDamping = 0.15f;
                        rb.angularDamping = 0.1f;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                }
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parts = folderPath.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static Material GetOrCreateMaterial(string path, Color color, float smoothness)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                ApplyMaterialLook(existing, color, smoothness);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Lit");
            var material = new Material(shader) { color = color };
            ApplyMaterialLook(material, color, smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ApplyMaterialLook(Material material, Color color, float smoothness)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }
        }

        private static void SetSerializedObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedFloat(UnityEngine.Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedInt(UnityEngine.Object target, string fieldName, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedBool(UnityEngine.Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedString(UnityEngine.Object target, string fieldName, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedVector3(UnityEngine.Object target, string fieldName, Vector3 value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            prop.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
