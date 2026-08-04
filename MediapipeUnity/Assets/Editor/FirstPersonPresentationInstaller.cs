using System.Collections.Generic;
using System.Linq;
using TriageTrace.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TriageTrace.EditorTools
{
    /// <summary>
    /// Creates the optional visual-only first-person presentation in the dedicated prototype scene.
    /// The action is deliberately menu-driven: importing this script never changes an open scene.
    /// </summary>
    public static class FirstPersonPresentationInstaller
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string RootName = "FirstPersonPresentation";
        private const string HandsCameraName = "FirstPersonHandsCamera";
        private const string HandsLayerName = "FirstPersonHands";
        private const string ArmsAssetPath = "Assets/FirstPerson/Arms/arms2.fbx";
        private const string BagAssetPath = "Assets/FirstPerson/MedicalBag/model.dae";
        private const string UndoLabel = "Install First-Person Presentation";
        private const string RefreshUndoLabel = "Refresh First-Person Presentation";

        [MenuItem("Triage Trace/Install First-Person Presentation")]
        public static void Install()
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            if (scene.GetRootGameObjects().Any(root => root.name == RootName))
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "FirstPersonPresentation already exists in this scene. No changes were made.",
                    "OK");
                return;
            }

            Camera mainCamera = FindOnlyMainCamera(scene);
            if (mainCamera == null)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "The prototype scene must contain exactly one active camera tagged MainCamera.",
                    "OK");
                return;
            }

            GameObject armsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ArmsAssetPath);
            GameObject bagAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BagAssetPath);
            if (armsAsset == null || bagAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "Arms or medical bag model asset is missing. No changes were made.",
                    "OK");
                return;
            }

            int handsLayer = EnsureHandsLayer();
            if (handsLayer < 0)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "No available user layer exists for FirstPersonHands. No scene changes were made.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                InstallPresentation(scene, mainCamera, handsLayer, armsAsset, bagAsset);
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch (System.Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "Installation failed and was reverted. See Console for details.",
                    "OK");
            }
        }

        [MenuItem("Triage Trace/Install First-Person Presentation", true)]
        private static bool ValidateInstall()
        {
            return TryGetTargetScene(out _, false);
        }

        [MenuItem("Triage Trace/Refresh First-Person Presentation")]
        public static void Refresh()
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            Camera mainCamera = FindOnlyMainCamera(scene);
            Transform root = mainCamera == null
                ? null
                : mainCamera.transform.Find(RootName);
            FirstPersonPresentationController controller = root == null
                ? null
                : root.GetComponent<FirstPersonPresentationController>();
            Camera handsCamera = controller == null
                ? null
                : controller.HandsCamera;
            if (handsCamera == null && mainCamera != null)
            {
                Transform handsCameraTransform = mainCamera.transform.Find(HandsCameraName);
                handsCamera = handsCameraTransform == null
                    ? null
                    : handsCameraTransform.GetComponent<Camera>();
            }

            if (mainCamera == null || controller == null || handsCamera == null)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "The prototype scene needs an installed FirstPersonPresentation, " +
                    "Main Camera, controller, and FirstPersonHandsCamera. No changes were made.",
                    "OK");
                return;
            }

            int handsLayer = EnsureHandsLayer();
            if (handsLayer < 0)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "No available user layer exists for FirstPersonHands. No scene changes were made.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(RefreshUndoLabel);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RecordObject(mainCamera, RefreshUndoLabel);
            Undo.RecordObject(handsCamera, RefreshUndoLabel);
            Undo.RecordObject(controller, RefreshUndoLabel);
            if (controller.MedicalBag != null)
            {
                Undo.RecordObject(controller.MedicalBag, RefreshUndoLabel);
            }
            ApplyHandsCameraConfiguration(mainCamera, handsCamera, handsLayer);
            controller.ConfigureHandsCamera(handsCamera);
            controller.ApplyMedicalBagPresentation();
            EditorUtility.SetDirty(mainCamera);
            EditorUtility.SetDirty(handsCamera);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root.gameObject;
        }

        [MenuItem("Triage Trace/Refresh First-Person Presentation", true)]
        private static bool ValidateRefresh()
        {
            return TryGetTargetScene(out _, false);
        }

        private static void InstallPresentation(
            Scene scene,
            Camera mainCamera,
            int handsLayer,
            GameObject armsAsset,
            GameObject bagAsset)
        {
            Undo.RecordObject(mainCamera, UndoLabel);
            mainCamera.cullingMask &= ~(1 << handsLayer);
            EditorUtility.SetDirty(mainCamera);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetParent(mainCamera.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            var handsCameraObject = new GameObject(HandsCameraName);
            Undo.RegisterCreatedObjectUndo(handsCameraObject, UndoLabel);
            SceneManager.MoveGameObjectToScene(handsCameraObject, scene);
            handsCameraObject.transform.SetParent(mainCamera.transform, false);
            handsCameraObject.transform.localPosition = Vector3.zero;
            handsCameraObject.transform.localRotation = Quaternion.identity;
            var handsCamera = handsCameraObject.AddComponent<Camera>();
            ApplyHandsCameraConfiguration(mainCamera, handsCamera, handsLayer);

            GameObject armsInstance = (GameObject)PrefabUtility.InstantiatePrefab(armsAsset, scene);
            Undo.RegisterCreatedObjectUndo(armsInstance, UndoLabel);
            armsInstance.name = "ArmsRig";
            armsInstance.transform.SetParent(root.transform, false);
            armsInstance.transform.localPosition = new Vector3(0.0f, -0.55f, 0.35f);
            armsInstance.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(armsInstance, handsLayer);

            Transform leftHand = FindBone(armsInstance.transform, "hand.L");
            Transform rightUpperArm = FindBone(armsInstance.transform, "arm.R");
            Transform rightForearm = FindBone(armsInstance.transform, "forearm.R");
            Transform rightHand = FindBone(armsInstance.transform, "hand.R");
            if (leftHand == null || rightUpperArm == null ||
                rightForearm == null || rightHand == null)
            {
                throw new System.InvalidOperationException(
                    "The imported arms rig does not contain the verified hand and right-arm bones.");
            }

            GameObject bagInstance = (GameObject)PrefabUtility.InstantiatePrefab(bagAsset, scene);
            Undo.RegisterCreatedObjectUndo(bagInstance, UndoLabel);
            bagInstance.name = "MedicalBag";
            bagInstance.transform.SetParent(leftHand, false);
            bagInstance.transform.localPosition = Vector3.zero;
            bagInstance.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(bagInstance, handsLayer);

            var controller = Undo.AddComponent<FirstPersonPresentationController>(root);
            List<FirstPersonPresentationController.FingerPoseOffset> fingerOffsets =
                CreateFingerOffsetBindings(armsInstance.transform);
            controller.Configure(
                handsCamera,
                armsInstance.transform,
                bagInstance.transform,
                leftHand,
                rightUpperArm,
                rightForearm,
                rightHand,
                Object.FindFirstObjectByType<PosePointerLineRenderer>(),
                fingerOffsets);
            controller.ApplyMedicalBagPresentation();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
        }

        private static List<FirstPersonPresentationController.FingerPoseOffset>
            CreateFingerOffsetBindings(Transform armsRoot)
        {
            string[] rightFingerBones =
            {
                "thumb1.R", "thumb2.R", "thumb3.R",
                "point1.R", "point2.R", "point3.R",
                "middle1.R", "middle2.R", "middle3.R",
                "ring1.R", "ring2.R", "ring3.R",
                "pink1.R", "pink2.R", "pink3.R"
            };
            var result = new List<FirstPersonPresentationController.FingerPoseOffset>();
            foreach (string boneName in rightFingerBones)
            {
                Transform bone = FindBone(armsRoot, boneName);
                if (bone != null)
                {
                    result.Add(new FirstPersonPresentationController.FingerPoseOffset(bone));
                }
            }
            return result;
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == boneName)
                {
                    return candidate;
                }
            }
            return null;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
            }
        }

        private static void ApplyHandsCameraConfiguration(
            Camera mainCamera,
            Camera handsCamera,
            int handsLayer)
        {
            mainCamera.cullingMask &= ~(1 << handsLayer);
            handsCamera.CopyFrom(mainCamera);
            handsCamera.clearFlags = CameraClearFlags.Depth;
            handsCamera.cullingMask = 1 << handsLayer;
            handsCamera.depth = mainCamera.depth + 1.0f;
            handsCamera.nearClipPlane = 0.01f;
            handsCamera.farClipPlane = 5.0f;
            handsCamera.tag = "Untagged";
        }

        private static Camera FindOnlyMainCamera(Scene scene)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Camera[] mainCameras = cameras.Where(camera =>
                camera.gameObject.scene == scene &&
                camera.gameObject.activeInHierarchy &&
                camera.CompareTag("MainCamera")).ToArray();
            return mainCameras.Length == 1 ? mainCameras[0] : null;
        }

        private static int EnsureHandsLayer()
        {
            int existingLayer = LayerMask.NameToLayer(HandsLayerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
            {
                return -1;
            }

            Undo.RegisterCompleteObjectUndo(assets[0], UndoLabel);
            var serializedTagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = serializedTagManager.FindProperty("layers");
            for (int index = 8; index < layers.arraySize; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = HandsLayerName;
                serializedTagManager.ApplyModifiedProperties();
                EditorUtility.SetDirty(assets[0]);
                return index;
            }
            return -1;
        }

        private static bool TryGetTargetScene(out Scene scene, bool showDialog)
        {
            scene = SceneManager.GetActiveScene();
            bool isCorrectScene = scene.IsValid() &&
                scene.name == TargetSceneName &&
                !EditorApplication.isPlayingOrWillChangePlaymode;
            if (!isCorrectScene && showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace first-person presentation",
                    "Open TriageTraceEnvironmentPrototype in Edit Mode before running this menu.",
                    "OK");
            }
            return isCorrectScene;
        }
    }
}
