using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BreakTheRoom.EditorTools
{
    public static class XrDeviceSimulatorInstaller
    {
        private const string MenuPath = "Tools/Break The Room/Add XR Device Simulator";
        private const string FixMenuPath = "Tools/Break The Room/Fix XR Device Simulator Action Assets";
        private const string SimulatorObjectName = "XR Device Simulator";

        [MenuItem(MenuPath)]
        public static void AddSimulatorToScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before adding XR Device Simulator.");
                return;
            }

            var existing = GameObject.Find(SimulatorObjectName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                Debug.Log("Break The Room: XR Device Simulator already exists in this scene.");
                return;
            }

            var simulatorType =
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit")
                ?? Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit.Samples");

            if (simulatorType == null)
            {
                Debug.LogError("Break The Room: XR Device Simulator type not found. Install XR Interaction Toolkit and re-open Unity.");
                return;
            }

            EnsureEventSystem();

            var go = new GameObject(SimulatorObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Add XR Device Simulator");
            var simulator = go.AddComponent(simulatorType);
            var assignedCount = TryAssignActionAssets(simulator);

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Break The Room: XR Device Simulator added. Assigned {assignedCount} action asset fields.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateAddSimulatorToScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(FixMenuPath)]
        public static void FixSimulatorAssetsInScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Break The Room: Stop Play mode before fixing simulator assets.");
                return;
            }

            var simulator = FindSimulatorComponent();
            if (simulator == null)
            {
                Debug.LogWarning("Break The Room: XR Device Simulator not found in this scene.");
                return;
            }

            Undo.RecordObject(simulator, "Fix XR Device Simulator Action Assets");
            var assignedCount = TryAssignActionAssets(simulator);
            EditorUtility.SetDirty(simulator);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Break The Room: Simulator action asset assignment updated ({assignedCount} fields set).");
        }

        [MenuItem(FixMenuPath, true)]
        private static bool ValidateFixSimulatorAssetsInScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                return;
            }

            var es = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            es.AddComponent<EventSystem>();

            var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                es.AddComponent(inputModuleType);
            }
            else
            {
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private static int TryAssignActionAssets(Component simulator)
        {
            if (simulator == null)
            {
                return 0;
            }

            var allAssets = AssetDatabase.FindAssets("t:InputActionAsset");
            if (allAssets.Length == 0)
            {
                Debug.LogWarning("Break The Room: No InputActionAsset found. Import XRI Starter Assets sample first.");
                return 0;
            }

            var deviceSimulatorActions = FindActionAsset("Device Simulator") ?? FindActionAsset("Simulator") ?? FindFirstActionAsset();
            var controllerActions = FindActionAsset("XRI Default") ?? FindActionAsset("Controller") ?? FindFirstActionAsset();
            var handActions = FindActionAsset("XRI Default") ?? FindActionAsset("Hands") ?? FindFirstActionAsset();

            var so = new SerializedObject(simulator);
            var assignedCount = 0;
            assignedCount += SetObjectProperty(so, "m_DeviceSimulatorActionAsset", deviceSimulatorActions);
            assignedCount += SetObjectProperty(so, "deviceSimulatorActionAsset", deviceSimulatorActions);
            assignedCount += SetObjectProperty(so, "m_ControllerActionAsset", controllerActions);
            assignedCount += SetObjectProperty(so, "controllerActionAsset", controllerActions);
            assignedCount += SetObjectProperty(so, "m_HandActionAsset", handActions);
            assignedCount += SetObjectProperty(so, "handActionAsset", handActions);
            so.ApplyModifiedPropertiesWithoutUndo();

            assignedCount += SetByReflection(simulator, "deviceSimulatorActionAsset", deviceSimulatorActions);
            assignedCount += SetByReflection(simulator, "controllerActionAsset", controllerActions);
            assignedCount += SetByReflection(simulator, "handActionAsset", handActions);
            return assignedCount;
        }

        private static UnityEngine.Object FindActionAsset(string nameContains)
        {
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        private static UnityEngine.Object FindFirstActionAsset()
        {
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length == 0)
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }

        private static Component FindSimulatorComponent()
        {
            var simulatorType =
                Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit")
                ?? Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit.Samples");
            if (simulatorType == null)
            {
                return null;
            }

            var go = GameObject.Find(SimulatorObjectName);
            if (go == null)
            {
                return null;
            }

            return go.GetComponent(simulatorType);
        }

        private static int SetObjectProperty(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || value == null)
            {
                return 0;
            }

            prop.objectReferenceValue = value;
            return 1;
        }

        private static int SetByReflection(Component target, string memberName, UnityEngine.Object value)
        {
            if (target == null || value == null)
            {
                return 0;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var property = target.GetType().GetProperty(memberName, flags);
            if (property != null && property.CanWrite && property.PropertyType.IsAssignableFrom(value.GetType()))
            {
                property.SetValue(target, value);
                return 1;
            }

            var field = target.GetType().GetField(memberName, flags);
            if (field != null && field.FieldType.IsAssignableFrom(value.GetType()))
            {
                field.SetValue(target, value);
                return 1;
            }

            return 0;
        }
    }
}
