using System;
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
    /// Explicit editor-only installer for a patient model visual. It preserves the selected
    /// PatientView root and all interaction components, changing only its visual child and
    /// target-renderer binding in a single Undo group.
    /// </summary>
    public sealed class PatientModelInstallerWindow : EditorWindow
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string VisualRootName = "PatientVisual_Model";
        private const string LegacyVisualRootName = "PatientVisual_Lying_A";
        private const string UndoLabel = "Install Patient Model";

        [SerializeField] private GameObject patientModelFbx;
        [SerializeField] private GameObject targetPatientRoot;

        [MenuItem("Triage Trace/Patient Models/Open Patient Model Installer")]
        public static void Open()
        {
            PatientModelInstallerWindow window = GetWindow<PatientModelInstallerWindow>("Patient Model Installer");
            window.minSize = new Vector2(410.0f, 245.0f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Patient Model Installer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assign one FBX asset and one Patient_01 through Patient_10 root. " +
                "Only the selected patient's visual child and PatientView target renderers are updated.",
                MessageType.Info);

            patientModelFbx = (GameObject)EditorGUILayout.ObjectField(
                "Patient Model FBX",
                patientModelFbx,
                typeof(GameObject),
                false);
            targetPatientRoot = (GameObject)EditorGUILayout.ObjectField(
                "Target Patient Root",
                targetPatientRoot,
                typeof(GameObject),
                true);

            bool sceneValid = TryGetTargetScene(out _, false);
            bool modelValid = TryGetModelAsset(patientModelFbx, out _, out string modelMessage);
            bool patientValid = TryGetPatientRoot(targetPatientRoot, out _, out string patientMessage);

            EditorGUILayout.Space(6.0f);
            DrawValidation("Scene", sceneValid, sceneValid ? "Edit Mode prototype scene is active." : "Open TriageTraceEnvironmentPrototype in Edit Mode.");
            DrawValidation("Patient Model FBX", modelValid, modelMessage);
            DrawValidation("Target Patient Root", patientValid, patientMessage);

            EditorGUILayout.Space(10.0f);
            using (new EditorGUI.DisabledScope(!sceneValid || !modelValid || !patientValid))
            {
                if (GUILayout.Button("Install", GUILayout.Height(30.0f)))
                {
                    Install(patientModelFbx, targetPatientRoot);
                }
            }
        }

        private static void DrawValidation(string label, bool valid, string message)
        {
            EditorGUILayout.LabelField(
                label,
                valid ? "Ready" : message,
                valid ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel);
        }

        private static void Install(GameObject modelCandidate, GameObject patientCandidate)
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            bool modelValid = TryGetModelAsset(modelCandidate, out GameObject modelAsset, out string modelMessage);
            bool patientValid = TryGetPatientRoot(patientCandidate, out PatientView patient, out string patientMessage);
            if (!modelValid || !patientValid)
            {
                EditorUtility.DisplayDialog(
                    "Patient model integration",
                    modelValid ? patientMessage : modelMessage,
                    "OK");
                return;
            }

            if (patient.gameObject.scene != scene)
            {
                EditorUtility.DisplayDialog(
                    "Patient model integration",
                    "The target Patient root must belong to the active prototype scene.",
                    "OK");
                return;
            }

            if (patient.GetComponentInChildren<Collider>(true) == null)
            {
                EditorUtility.DisplayDialog(
                    "Patient model integration",
                    $"{patient.name} has no collider. The existing raycast target must be restored before installing a visual model.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                RemoveExistingVisuals(patient.transform);
                DisableExistingVisualRenderers(patient.transform);

                GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset, patient.transform) as GameObject;
                if (visual == null)
                {
                    throw new InvalidOperationException("The assigned FBX could not be instantiated as a GameObject.");
                }

                Undo.RegisterCreatedObjectUndo(visual, UndoLabel);
                visual.name = VisualRootName;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                SetLayerRecursively(visual.transform, patient.gameObject.layer);
                DisableAnimators(visual);

                Renderer[] modelRenderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Cast<Renderer>()
                    .ToArray();
                if (modelRenderers.Length == 0)
                {
                    throw new InvalidOperationException("The assigned FBX has no SkinnedMeshRenderer.");
                }

                AssignTargetRenderers(patient, modelRenderers);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = visual;
                Debug.Log(
                    $"Triage Trace: installed {modelAsset.name} under {patient.name}. " +
                    "Adjust only the selected visual child's Transform in the Inspector; PatientView and its collider were preserved.",
                    visual);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Patient model integration",
                    "Installation failed and was reverted. See Console for details.",
                    "OK");
            }
        }

        private static bool TryGetTargetScene(out Scene scene, bool showDialog)
        {
            scene = SceneManager.GetActiveScene();
            bool valid = scene.IsValid() &&
                scene.name == TargetSceneName &&
                !EditorApplication.isPlayingOrWillChangePlaymode;
            if (!valid && showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Patient model integration",
                    "Open TriageTraceEnvironmentPrototype in Edit Mode before using the installer.",
                    "OK");
            }

            return valid;
        }

        private static bool TryGetModelAsset(GameObject candidate, out GameObject modelAsset, out string message)
        {
            modelAsset = candidate;
            string path = candidate == null ? string.Empty : AssetDatabase.GetAssetPath(candidate);
            bool valid = !string.IsNullOrEmpty(path) && AssetImporter.GetAtPath(path) is ModelImporter;
            message = valid ? "FBX asset assigned." : "Drag a model FBX from the Project window.";
            if (!valid)
            {
                modelAsset = null;
            }

            return valid;
        }

        private static bool TryGetPatientRoot(GameObject candidate, out PatientView patient, out string message)
        {
            patient = candidate == null ? null : candidate.GetComponent<PatientView>();
            bool valid = patient != null &&
                patient.gameObject.scene == SceneManager.GetActiveScene() &&
                IsSupportedPatientRoot(patient.name);
            message = valid
                ? $"{patient.name} is ready."
                : "Drag a Patient_01 through Patient_10 root from the Hierarchy.";
            if (!valid)
            {
                patient = null;
            }

            return valid;
        }

        private static bool IsSupportedPatientRoot(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith("Patient_", StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(name.Substring("Patient_".Length), out int number) &&
                number >= 1 && number <= 10;
        }

        private static void RemoveExistingVisuals(Transform patientRoot)
        {
            for (int index = patientRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = patientRoot.GetChild(index);
                if (child.name == VisualRootName || child.name == LegacyVisualRootName)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        private static void DisableExistingVisualRenderers(Transform patientRoot)
        {
            foreach (Renderer renderer in patientRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.enabled)
                {
                    Undo.RecordObject(renderer, UndoLabel);
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static void DisableAnimators(GameObject visual)
        {
            foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            {
                if (animator != null && animator.enabled)
                {
                    Undo.RecordObject(animator, UndoLabel);
                    animator.enabled = false;
                    EditorUtility.SetDirty(animator);
                }
            }
        }

        private static void AssignTargetRenderers(PatientView patient, IReadOnlyList<Renderer> renderers)
        {
            Undo.RecordObject(patient, UndoLabel);
            var serializedPatient = new SerializedObject(patient);
            SerializedProperty targetRenderers = serializedPatient.FindProperty("targetRenderers");
            if (targetRenderers == null || !targetRenderers.isArray)
            {
                throw new InvalidOperationException("PatientView targetRenderers could not be resolved.");
            }

            targetRenderers.arraySize = renderers.Count;
            for (int index = 0; index < renderers.Count; index++)
            {
                targetRenderers.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
            }

            serializedPatient.ApplyModifiedProperties();
            EditorUtility.SetDirty(patient);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
            {
                SetLayerRecursively(child, layer);
            }
        }
    }
}
