using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TriageTrace.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TriageTrace.EditorTools
{
    /// <summary>
    /// Normalizes the ten scenario identifiers and reports interaction/placement readiness.
    /// It deliberately never changes patient transforms, models, colliders, or runtime systems.
    /// </summary>
    public static class TenPatientIdentityAndPlacementMenu
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string UndoLabel = "Normalize Ten Patient Identities";

        [MenuItem("Triage Trace/Patients/Normalize Ten Patient IDs and Validate Placement")]
        public static void NormalizeAndValidate()
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            if (!TryGetTenPatients(scene, out List<PatientView> patients, out string failure))
            {
                Debug.LogWarning($"Triage Trace patient validation: {failure}");
                EditorUtility.DisplayDialog("Ten patient validation", failure + " No changes were made.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                foreach (PatientView patient in patients)
                {
                    string id = GetScenarioId(patient.name);
                    SetDisplayName(patient, id);
                    UpdateExistingPatientMarkers(patient, id);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                LogValidationReport(patients);
                Selection.objects = patients.Select(patient => patient.gameObject).ToArray();
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Ten patient validation",
                    "Identity normalization failed and was reverted. See Console for details.",
                    "OK");
            }
        }

        [MenuItem("Triage Trace/Patients/Normalize Ten Patient IDs and Validate Placement", true)]
        private static bool ValidateNormalizeAndValidate()
        {
            return TryGetTargetScene(out _, false);
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
                    "Ten patient validation",
                    "Open TriageTraceEnvironmentPrototype in Edit Mode before using this menu.",
                    "OK");
            }

            return valid;
        }

        private static bool TryGetTenPatients(Scene scene, out List<PatientView> patients, out string failure)
        {
            var rootsByName = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
            foreach (Transform transform in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                if (!TryGetPatientNumber(transform.name, out _))
                {
                    continue;
                }

                if (!rootsByName.TryGetValue(transform.name, out List<GameObject> roots))
                {
                    roots = new List<GameObject>();
                    rootsByName.Add(transform.name, roots);
                }

                roots.Add(transform.gameObject);
            }

            patients = new List<PatientView>(10);
            for (int number = 1; number <= 10; number++)
            {
                string rootName = $"Patient_{number:00}";
                if (!rootsByName.TryGetValue(rootName, out List<GameObject> roots) || roots.Count != 1)
                {
                    failure = roots == null
                        ? $"Missing {rootName}."
                        : $"Expected one {rootName}, but found {roots.Count}.";
                    return false;
                }

                PatientView patient = roots[0].GetComponent<PatientView>();
                if (patient == null)
                {
                    failure = $"{rootName} is missing PatientView.";
                    return false;
                }

                patients.Add(patient);
            }

            failure = string.Empty;
            return true;
        }

        private static bool TryGetPatientNumber(string name, out int number)
        {
            number = 0;
            return !string.IsNullOrEmpty(name) &&
                name.StartsWith("Patient_", StringComparison.Ordinal) &&
                int.TryParse(name.Substring("Patient_".Length), out number) &&
                number >= 1 && number <= 10;
        }

        private static string GetScenarioId(string patientRootName)
        {
            TryGetPatientNumber(patientRootName, out int number);
            return $"TR-{number:000}";
        }

        private static void SetDisplayName(PatientView patient, string id)
        {
            var serializedPatient = new SerializedObject(patient);
            SerializedProperty displayName = serializedPatient.FindProperty("displayName");
            if (displayName == null || displayName.stringValue == id)
            {
                return;
            }

            Undo.RecordObject(patient, UndoLabel);
            displayName.stringValue = id;
            serializedPatient.ApplyModifiedProperties();
            EditorUtility.SetDirty(patient);
        }

        private static void UpdateExistingPatientMarkers(PatientView patient, string id)
        {
            foreach (TMP_Text marker in patient.GetComponentsInChildren<TMP_Text>(true)
                         .Where(text => text.gameObject.name == "PatientMarker"))
            {
                if (marker.text != id)
                {
                    Undo.RecordObject(marker, UndoLabel);
                    marker.text = id;
                    EditorUtility.SetDirty(marker);
                }
            }

            foreach (UnityEngine.UI.Text marker in patient.GetComponentsInChildren<UnityEngine.UI.Text>(true)
                         .Where(text => text.gameObject.name == "PatientMarker"))
            {
                if (marker.text != id)
                {
                    Undo.RecordObject(marker, UndoLabel);
                    marker.text = id;
                    EditorUtility.SetDirty(marker);
                }
            }
        }

        private static void LogValidationReport(IReadOnlyList<PatientView> patients)
        {
            int expectedPatientLayer = LayerMask.NameToLayer("Patient");
            var boundsByPatient = patients.ToDictionary(patient => patient, GetColliderBounds);
            foreach (PatientView patient in patients)
            {
                int rendererCount = GetAssignedRendererCount(patient);
                int colliderCount = patient.GetComponentsInChildren<Collider>(true).Length;
                bool layerValid = expectedPatientLayer >= 0 && patient.gameObject.layer == expectedPatientLayer;
                string anchor = GetAnchorSummary(patient);
                string placement = GetPlacementSummary(patient, boundsByPatient);
                string warnings = BuildWarnings(patient, rendererCount, colliderCount, layerValid);
                Debug.Log(
                    $"Triage Trace patient validation | ID={patient.DisplayName} | root={patient.name} | " +
                    $"modelRenderers={rendererCount} | colliders={colliderCount} | layer={LayerMask.LayerToName(patient.gameObject.layer)}({patient.gameObject.layer}) | " +
                    $"cardAnchor={anchor} | checked={patient.IsChecked} | expectedZone={GetExpectedZone(patient.name)} | placement={placement}{warnings}",
                    patient);
            }
        }

        private static int GetAssignedRendererCount(PatientView patient)
        {
            var serializedPatient = new SerializedObject(patient);
            SerializedProperty renderers = serializedPatient.FindProperty("targetRenderers");
            if (renderers == null || !renderers.isArray)
            {
                return 0;
            }

            int valid = 0;
            for (int index = 0; index < renderers.arraySize; index++)
            {
                if (renderers.GetArrayElementAtIndex(index).objectReferenceValue is Renderer)
                {
                    valid++;
                }
            }

            return valid;
        }

        private static string GetAnchorSummary(PatientView patient)
        {
            var serializedPatient = new SerializedObject(patient);
            SerializedProperty anchor = serializedPatient.FindProperty("statusCardAnchor");
            return anchor == null || anchor.objectReferenceValue == null
                ? "root fallback"
                : anchor.objectReferenceValue.name;
        }

        private static Bounds? GetColliderBounds(PatientView patient)
        {
            Collider[] colliders = patient.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                return null;
            }

            Bounds bounds = colliders[0].bounds;
            for (int index = 1; index < colliders.Length; index++)
            {
                bounds.Encapsulate(colliders[index].bounds);
            }

            return bounds;
        }

        private static string GetPlacementSummary(PatientView patient, IReadOnlyDictionary<PatientView, Bounds?> boundsByPatient)
        {
            Bounds? current = boundsByPatient[patient];
            if (!current.HasValue)
            {
                return "no collider bounds";
            }

            List<string> overlaps = boundsByPatient
                .Where(pair => pair.Key != patient && pair.Value.HasValue && current.Value.Intersects(pair.Value.Value))
                .Select(pair => pair.Key.DisplayName)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            return overlaps.Count == 0 ? "no patient overlap" : "overlap risk: " + string.Join(", ", overlaps);
        }

        private static string GetExpectedZone(string patientRootName)
        {
            TryGetPatientNumber(patientRootName, out int number);
            return number <= 3 ? "CAR 1" : number <= 6 ? "CAR 2" : "CAR 3 OR PLATFORM";
        }

        private static string BuildWarnings(PatientView patient, int rendererCount, int colliderCount, bool layerValid)
        {
            var warnings = new List<string>();
            if (rendererCount == 0) warnings.Add("missing target renderers");
            if (colliderCount == 0) warnings.Add("missing collider");
            if (!layerValid) warnings.Add("missing or incorrect Patient layer");
            if (patient.IsChecked) warnings.Add("already checked; expected initial unconfirmed state");
            return warnings.Count == 0 ? string.Empty : " | WARNING=" + string.Join("; ", warnings);
        }
    }
}
