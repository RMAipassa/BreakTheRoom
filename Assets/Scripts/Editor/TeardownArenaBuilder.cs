using System;
using BreakTheRoom.Destruction;
using BreakTheRoom.Gameplay;
using BreakTheRoom.Optimization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakTheRoom.EditorTools
{
    public static class TeardownArenaBuilder
    {
        private const string MenuPath = "Tools/Break The Room/Build Teardown-Style Arena";
        private const string XriMenuPath = "Tools/Break The Room/Force Rebuild XRI + Teardown Arena";
        private const string WorldRootName = "__BTR_GeneratedWorld";
        private const string ArenaRootName = "TeardownArena";
        private const string GeneratedPath = "Assets/Generated";
        private const string MaterialsPath = GeneratedPath + "/Materials";
        private const string PrefabsPath = GeneratedPath + "/Prefabs";

        [MenuItem(MenuPath)]
        public static void BuildArena()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before building the arena.");
                return;
            }

            var worldRoot = GameObject.Find(WorldRootName);
            if (worldRoot == null)
            {
                ChaosWorldBuilder.BuildInActiveScene();
                worldRoot = GameObject.Find(WorldRootName);
                if (worldRoot == null)
                {
                    Debug.LogError("Break The Room: Could not create or find starter world root.");
                    return;
                }
            }

            var systemsRoot = worldRoot.transform.Find("GameSystems");
            if (systemsRoot == null)
            {
                var systems = new GameObject("GameSystems");
                Undo.RegisterCreatedObjectUndo(systems, "Create GameSystems");
                systems.transform.SetParent(worldRoot.transform);
                systemsRoot = systems.transform;
            }
            ChaosWorldBuilder.EnsureWearHealthBridge(systemsRoot);

            var existing = worldRoot.transform.Find(ArenaRootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            RemoveStarterOverlapContent(worldRoot.transform);

            EnsureAssetFolder(GeneratedPath);
            EnsureAssetFolder(MaterialsPath);
            EnsureAssetFolder(PrefabsPath);

            var concrete = GetOrCreateMaterial(MaterialsPath + "/Mat_Concrete.mat", new Color(0.48f, 0.49f, 0.5f), 0.08f);
            var rust = GetOrCreateMaterial(MaterialsPath + "/Mat_Rust.mat", new Color(0.42f, 0.24f, 0.14f), 0.06f);
            var glass = GetOrCreateMaterial(MaterialsPath + "/Mat_Glass.mat", new Color(0.72f, 0.82f, 0.9f), 0.85f);
            var wood = GetOrCreateMaterial(MaterialsPath + "/Mat_WoodDark.mat", new Color(0.31f, 0.22f, 0.14f), 0.15f);

            var fractureBrick = CreateFracturePrefab(PrefabsPath + "/Fracture_Brick.prefab", concrete, new Vector3(0.95f, 0.48f, 0.48f));
            var fractureGlass = CreateFracturePrefab(PrefabsPath + "/Fracture_Glass.prefab", glass, new Vector3(0.85f, 0.85f, 0.08f));
            var fractureWood = CreateFracturePrefab(PrefabsPath + "/Fracture_Wood.prefab", wood, new Vector3(0.9f, 0.2f, 0.5f));

            var arena = new GameObject(ArenaRootName);
            Undo.RegisterCreatedObjectUndo(arena, "Build Teardown Arena");
            arena.transform.SetParent(worldRoot.transform);

            CreateWarehouseEnvelope(arena.transform, concrete, rust);
            CreateDestructibleFacade(arena.transform, concrete, fractureBrick);
            CreateGlassStrip(arena.transform, glass, fractureGlass);
            CreateRacksAndFurniture(arena.transform, wood, rust, fractureWood);

            Selection.activeGameObject = arena;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Break The Room: Teardown-style arena generated.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateBuildArena()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(XriMenuPath)]
        public static void ForceRebuildXriAndArena()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before building the arena.");
                return;
            }

            ChaosWorldBuilder.ForceRebuildWithXriRig();
            BuildArena();
            ChaosWorldBuilder.FixXriDesktopHelpersInScene();
        }

        [MenuItem(XriMenuPath, true)]
        private static bool ValidateForceRebuildXriAndArena()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void CreateWarehouseEnvelope(Transform parent, Material concrete, Material rust)
        {
            var group = new GameObject("WarehouseEnvelope");
            group.transform.SetParent(parent);

            CreateStatic("BackWall", group.transform, new Vector3(0f, 3f, 8f), new Vector3(16f, 6f, 0.6f), concrete);
            CreateStatic("Catwalk", group.transform, new Vector3(4.5f, 1.8f, 1f), new Vector3(4f, 0.2f, 1.5f), rust);
        }

        private static void RemoveStarterOverlapContent(Transform worldRoot)
        {
            RemoveChildByName(worldRoot, "DestructibleProps");
            RemoveChildByName(worldRoot, "SupportStructures");
        }

        private static void RemoveChildByName(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(child.gameObject);
        }

        private static void CreateDestructibleFacade(Transform parent, Material concrete, GameObject fractureBrick)
        {
            var group = new GameObject("DestructibleFacade");
            group.transform.SetParent(parent);

            const int width = 14;
            const int height = 6;
            var start = new Vector3(-6.5f, 0.4f, 3.5f);
            var step = new Vector3(1f, 0.52f, 0f);

            var grid = new Rigidbody[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var hasWindowHole = y >= 2 && y <= 3 && x >= 5 && x <= 8;
                    if (hasWindowHole)
                    {
                        continue;
                    }

                    var pos = start + new Vector3(step.x * x, step.y * y, 0f);
                    var brick = CreateDestructible(
                        "Brick",
                        PrimitiveType.Cube,
                        pos,
                        new Vector3(0.95f, 0.48f, 0.48f),
                        group.transform,
                        concrete,
                        fractureBrick,
                        34f,
                        18,
                        y == 0,
                        DestructionFeedback.SurfaceType.Concrete);

                    var rb = brick.GetComponent<Rigidbody>();
                    grid[x, y] = rb;

                    if (x > 0 && grid[x - 1, y] != null)
                    {
                        var edgeT = Mathf.Abs((x - ((width - 1) * 0.5f)) / ((width - 1) * 0.5f));
                        var horizontalForce = Mathf.Lerp(3600f, 6600f, edgeT);
                        var horizontalTorque = Mathf.Lerp(2400f, 4400f, edgeT);
                        AddBreakableJoint(brick, grid[x - 1, y], horizontalForce, horizontalTorque);
                    }

                    if (y > 0 && grid[x, y - 1] != null)
                    {
                        var edgeT = Mathf.Abs((x - ((width - 1) * 0.5f)) / ((width - 1) * 0.5f));
                        var heightT = y / (float)(height - 1);
                        var verticalForce = Mathf.Lerp(4200f, 7600f, edgeT);
                        verticalForce *= Mathf.Lerp(1.05f, 0.9f, heightT);
                        var verticalTorque = Mathf.Lerp(2800f, 5200f, edgeT);
                        verticalTorque *= Mathf.Lerp(1.05f, 0.9f, heightT);
                        AddBreakableJoint(brick, grid[x, y - 1], verticalForce, verticalTorque);
                    }
                }
            }
        }

        private static void CreateGlassStrip(Transform parent, Material glass, GameObject fractureGlass)
        {
            var group = new GameObject("GlassStrip");
            group.transform.SetParent(parent);

            for (var i = 0; i < 8; i++)
            {
                var panel = CreateDestructible(
                    "GlassPanel",
                    PrimitiveType.Cube,
                    new Vector3(-3.5f + i, 2.25f, 3.5f),
                    new Vector3(0.85f, 0.85f, 0.08f),
                    group.transform,
                    glass,
                    fractureGlass,
                    10f,
                    30,
                    true,
                    DestructionFeedback.SurfaceType.Glass);

                var rb = panel.GetComponent<Rigidbody>();
                rb.mass = 0.8f;
                rb.linearDamping = 0.08f;
                rb.angularDamping = 0.08f;
            }
        }

        private static void CreateRacksAndFurniture(Transform parent, Material wood, Material rust, GameObject fractureWood)
        {
            var group = new GameObject("RacksAndFurniture");
            group.transform.SetParent(parent);

            for (var i = 0; i < 4; i++)
            {
                var basePos = new Vector3(-4f + i * 2.7f, 0.65f, -2.6f);
                CreateShelf(basePos, group.transform, wood, rust, fractureWood);
            }

            for (var i = 0; i < 3; i++)
            {
                var tablePos = new Vector3(2.5f + i * 2.1f, 0.9f, -5f);
                CreateTable(tablePos, group.transform, wood, fractureWood);
            }
        }

        private static void CreateShelf(Vector3 basePos, Transform parent, Material wood, Material rust, GameObject fractureWood)
        {
            var frameL = CreateDestructible(
                "ShelfFrame",
                PrimitiveType.Cube,
                basePos + new Vector3(-0.55f, 1f, 0f),
                new Vector3(0.1f, 2f, 0.6f),
                parent,
                rust,
                fractureWood,
                42f,
                35,
                true,
                DestructionFeedback.SurfaceType.Metal);

            _ = CreateDestructible(
                "ShelfFrame",
                PrimitiveType.Cube,
                basePos + new Vector3(0.55f, 1f, 0f),
                new Vector3(0.1f, 2f, 0.6f),
                parent,
                rust,
                fractureWood,
                42f,
                35,
                true,
                DestructionFeedback.SurfaceType.Metal);

            var frameLBody = frameL.GetComponent<Rigidbody>();

            for (var i = 0; i < 3; i++)
            {
                var plank = CreateDestructible(
                    "ShelfPlank",
                    PrimitiveType.Cube,
                    basePos + new Vector3(0f, 0.35f + i * 0.6f, 0f),
                    new Vector3(1.15f, 0.08f, 0.55f),
                    parent,
                    wood,
                    fractureWood,
                    22f,
                    25,
                    false,
                    DestructionFeedback.SurfaceType.Wood);

                AddBreakableJoint(plank, frameLBody, 1900f, 1300f);
            }
        }

        private static void CreateTable(Vector3 pos, Transform parent, Material wood, GameObject fractureWood)
        {
            var top = CreateDestructible(
                "TableTop",
                PrimitiveType.Cube,
                pos,
                new Vector3(1.4f, 0.12f, 0.8f),
                parent,
                wood,
                fractureWood,
                26f,
                30,
                false,
                DestructionFeedback.SurfaceType.Wood);
            var topBody = top.GetComponent<Rigidbody>();

            var legOffsets = new[]
            {
                new Vector3(-0.6f, -0.45f, -0.3f),
                new Vector3(0.6f, -0.45f, -0.3f),
                new Vector3(-0.6f, -0.45f, 0.3f),
                new Vector3(0.6f, -0.45f, 0.3f)
            };

            for (var i = 0; i < legOffsets.Length; i++)
            {
                var leg = CreateDestructible(
                    "TableLeg",
                    PrimitiveType.Cube,
                    pos + legOffsets[i],
                    new Vector3(0.1f, 0.9f, 0.1f),
                    parent,
                    wood,
                    fractureWood,
                    16f,
                    14,
                    false,
                    DestructionFeedback.SurfaceType.Wood);

                AddBreakableJoint(leg, topBody, 1700f, 1200f);
            }
        }

        private static GameObject CreateDestructible(
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Transform parent,
            Material material,
            GameObject fracturePrefab,
            float health,
            int score,
            bool isAnchored,
            DestructionFeedback.SurfaceType surfaceType)
        {
            var obj = GameObject.CreatePrimitive(primitiveType);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.localScale = scale;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.6f, scale.x * scale.y * scale.z * 16f);
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = isAnchored;

            obj.AddComponent<ImpactDamageDealer>();
            obj.AddComponent<ScoreOnBreak>();
            obj.AddComponent<ResettablePhysicsObject>();
            var feedback = obj.AddComponent<DestructionFeedback>();
            TryAddGrabInteractable(obj);

            var breakable = obj.AddComponent<BreakablePiece>();
            SetSerializedFloat(breakable, "maxHealth", health);
            SetSerializedFloat(breakable, "minImpulseToBreak", 0f);
            SetSerializedObject(breakable, "fracturePrefab", fracturePrefab);
            SetSerializedFloat(breakable, "chunkImpulseMultiplier", 1.05f);
            SetSerializedInt(breakable, "chaosValue", score);
            SetSerializedInt(feedback, "surfaceType", (int)surfaceType);

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

        private static void AddBreakableJoint(GameObject target, Rigidbody connectedBody, float forceThreshold, float torqueThreshold)
        {
            if (target == null || connectedBody == null)
            {
                return;
            }

            var joint = target.AddComponent<FixedJoint>();
            joint.connectedBody = connectedBody;
            joint.enableCollision = false;

            var link = target.AddComponent<StructuralLink>();
            SetSerializedFloat(link, "breakForceThreshold", forceThreshold);
            SetSerializedFloat(link, "breakTorqueThreshold", torqueThreshold);
            SetSerializedFloat(link, "supportDamageOnSnap", 14f);
        }

        private static GameObject CreateStatic(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool addKinematicBody = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.localScale = scale;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (addKinematicBody)
            {
                var rb = obj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
            else
            {
                GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            }

            return obj;
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
                        rb.mass = 0.18f + UnityEngine.Random.value * 0.18f;
                        rb.linearDamping = 0.16f;
                        rb.angularDamping = 0.12f;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                }
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
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

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Lit");
            var material = new Material(shader);
            ApplyMaterialLook(material, color, smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ApplyMaterialLook(Material material, Color color, float smoothness)
        {
            material.color = color;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
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
    }
}
