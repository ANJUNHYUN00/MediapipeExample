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
    /// Places the three existing prototype patients without creating or changing scene content.
    /// Placement is an explicit, single Undo operation; saving remains the scene author's choice.
    /// </summary>
    public static class PrototypePatientPlacementMenu
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string UndoLabel = "Arrange Prototype Patients";
        private const float FloorClearance = 0.015f;
        private const float StructureClearance = 0.03f;
        private const float MinimumPatientGap = 0.75f;

        [MenuItem("Triage Trace/Arrange Prototype Patients")]
        public static void ArrangePrototypePatients()
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            List<PatientView> patients = FindPatients(scene);
            List<TrainArea> cars = FindTrainAreas(scene);
            if (patients.Count != 3 || cars.Count != 3)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace patient placement",
                    $"Expected Patient_01 through Patient_03 and three train cars; found {patients.Count} patients and {cars.Count} cars. No changes were made.",
                    "OK");
                return;
            }

            var placements = new List<Placement>(patients.Count);
            for (int index = 0; index < patients.Count; index++)
            {
                PatientView patient = patients[index];
                Bounds patientBounds = GetPatientBounds(patient);
                if (!TryFindSafePlacement(patient, patientBounds, cars[index], placements, out Placement placement))
                {
                    EditorUtility.DisplayDialog(
                        "Triage Trace patient placement",
                        $"No safe floor position was found for {patient.name} in {cars[index].Root.name}. No changes were made.",
                        "OK");
                    return;
                }

                placements.Add(placement);
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                foreach (Placement placement in placements)
                {
                    Undo.RecordObject(placement.Patient.transform, UndoLabel);
                    placement.Patient.transform.position = placement.Position;
                    EditorUtility.SetDirty(placement.Patient.transform);
                }

                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(scene);
                LogPlacementSummary(cars, placements);
                Selection.objects = placements.Select(placement => placement.Patient.gameObject).ToArray();
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Triage Trace patient placement",
                    "Placement failed and was reverted. See Console for details.",
                    "OK");
            }
        }

        [MenuItem("Triage Trace/Arrange Prototype Patients", true)]
        private static bool ValidateArrangePrototypePatients()
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
                    "Triage Trace patient placement",
                    $"Open {TargetSceneName}.unity in Edit Mode before arranging patients.",
                    "OK");
            }

            return valid;
        }

        private static List<PatientView> FindPatients(Scene scene)
        {
            string[] expectedNames = { "Patient_01", "Patient_02", "Patient_03" };
            var found = new Dictionary<string, PatientView>(StringComparer.Ordinal);
            foreach (PatientView patient in UnityEngine.Object.FindObjectsByType<PatientView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (patient.gameObject.scene == scene && expectedNames.Contains(patient.name))
                {
                    found[patient.name] = patient;
                }
            }

            return expectedNames
                .Where(found.ContainsKey)
                .Select(name => found[name])
                .ToList();
        }

        private static List<TrainArea> FindTrainAreas(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name.StartsWith("SubwayTrainEnvironment_Car", StringComparison.Ordinal))
                .Select(root => new TrainArea(root, GetRendererBounds(root)))
                .Where(area => area.Bounds.size.sqrMagnitude > 0.0f)
                .OrderBy(area => area.Root.name, StringComparer.Ordinal)
                .ToList();
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static Bounds GetPatientBounds(PatientView patient)
        {
            Renderer[] renderers = patient.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Collider collider = patient.GetComponentInChildren<Collider>(true);
                return collider == null
                    ? new Bounds(patient.transform.position, Vector3.one)
                    : collider.bounds;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static bool TryFindSafePlacement(
            PatientView patient,
            Bounds patientBounds,
            TrainArea car,
            List<Placement> previousPlacements,
            out Placement placement)
        {
            Vector3[] candidates = BuildCandidates(car.Bounds);
            Vector3 currentOffset = patientBounds.center - patient.transform.position;
            Placement warnedFallback = default;
            bool hasWarnedFallback = false;
            foreach (Vector3 candidate in candidates)
            {
                if (!TryFindFloor(candidate, car, out FloorSurface floor))
                {
                    continue;
                }

                Vector3 position = candidate;
                position.y = floor.Height + patientBounds.extents.y + FloorClearance - currentOffset.y;
                Bounds proposedBounds = new Bounds(position + currentOffset, patientBounds.size);
                if (!IsWithinCar(proposedBounds, car.Bounds) ||
                    IsTooCloseToPatient(proposedBounds, previousPlacements))
                {
                    continue;
                }

                bool structureOverlap = HasStructureOverlap(patient, proposedBounds, floor.Collider);
                Placement candidatePlacement = new Placement(
                    patient,
                    position,
                    proposedBounds,
                    car.Root,
                    structureOverlap);
                if (!structureOverlap)
                {
                    placement = candidatePlacement;
                    return true;
                }

                // Imported vehicle collider envelopes may cover the whole carriage even
                // when the visible aisle is clear. Retain the best floor-and-bounds-valid
                // candidate and report the warning instead of leaving all patients unplaced.
                if (!hasWarnedFallback)
                {
                    warnedFallback = candidatePlacement;
                    hasWarnedFallback = true;
                }
            }

            if (hasWarnedFallback)
            {
                placement = warnedFallback;
                return true;
            }

            placement = default;
            return false;
        }

        private static Vector3[] BuildCandidates(Bounds car)
        {
            bool longAxisIsX = car.size.x >= car.size.z;
            float[] longitudinal = { 0.38f, 0.62f, 0.50f, 0.26f, 0.74f };
            float[] lateral = { 0.50f, 0.42f, 0.58f };
            var candidates = new List<Vector3>(longitudinal.Length * lateral.Length);
            foreach (float along in longitudinal)
            {
                foreach (float across in lateral)
                {
                    candidates.Add(longAxisIsX
                        ? new Vector3(Mathf.Lerp(car.min.x, car.max.x, along), car.center.y, Mathf.Lerp(car.min.z, car.max.z, across))
                        : new Vector3(Mathf.Lerp(car.min.x, car.max.x, across), car.center.y, Mathf.Lerp(car.min.z, car.max.z, along)));
                }
            }

            return candidates.ToArray();
        }

        private static bool TryFindFloor(Vector3 position, TrainArea car, out FloorSurface floor)
        {
            Bounds carBounds = car.Bounds;
            Vector3 start = new Vector3(position.x, carBounds.center.y + carBounds.extents.y * 0.35f, position.z);
            float distance = Mathf.Max(1.0f, carBounds.size.y * 0.8f);
            RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore)
                .OrderBy(hit => hit.distance)
                .ToArray();
            foreach (RaycastHit hit in hits)
            {
                // A downward ray can first hit the underside of a roof or a wall edge.
                // Only upward-facing surfaces may support a patient placement.
                if (!IsPatientCollider(hit.collider) &&
                    IsPartOfCar(hit.collider, car.Root) &&
                    hit.normal.y > 0.5f &&
                    hit.point.y >= carBounds.min.y - 0.5f)
                {
                    floor = new FloorSurface(hit.point.y, hit.collider);
                    return true;
                }
            }

            // The imported train assets can use render-only floor meshes while their
            // collision representation is generated separately. Use the actual floor
            // renderer bounds as a conservative fallback rather than rejecting the car.
            Renderer rendererFloor = FindFloorRenderer(car.Root, position);
            if (rendererFloor != null)
            {
                floor = new FloorSurface(rendererFloor.bounds.max.y, null);
                return true;
            }

            floor = default;
            return false;
        }

        private static Renderer FindFloorRenderer(GameObject carRoot, Vector3 position)
        {
            return carRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(renderer => position.x >= renderer.bounds.min.x && position.x <= renderer.bounds.max.x &&
                                   position.z >= renderer.bounds.min.z && position.z <= renderer.bounds.max.z)
                .OrderByDescending(renderer => renderer.bounds.max.y)
                .FirstOrDefault();
        }

        private static bool IsPartOfCar(Collider collider, GameObject carRoot)
        {
            return collider != null &&
                   (collider.transform == carRoot.transform || collider.transform.IsChildOf(carRoot.transform));
        }

        private static bool IsWithinCar(Bounds patient, Bounds car)
        {
            const float edgeMargin = 0.15f;
            return patient.min.x >= car.min.x + edgeMargin && patient.max.x <= car.max.x - edgeMargin &&
                   patient.min.z >= car.min.z + edgeMargin && patient.max.z <= car.max.z - edgeMargin;
        }

        private static bool HasStructureOverlap(
            PatientView patient,
            Bounds proposedBounds,
            Collider floorCollider = null)
        {
            Collider[] overlaps = Physics.OverlapBox(
                proposedBounds.center,
                Vector3.Max(Vector3.zero, proposedBounds.extents - Vector3.one * StructureClearance),
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Ignore);
            return overlaps.Any(collider =>
                collider != null &&
                !collider.transform.IsChildOf(patient.transform) &&
                !IsPatientCollider(collider) &&
                !IsFloorSupport(collider, floorCollider, proposedBounds));
        }

        private static bool IsFloorSupport(
            Collider collider,
            Collider floorCollider,
            Bounds patientBounds)
        {
            if (collider == floorCollider)
            {
                return true;
            }

            if (collider.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                collider.name.IndexOf("ground", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // Some floor meshes are divided into adjacent collider pieces. Permit only
            // the pieces that end at the patient's feet; seats and walls remain blockers.
            return collider.bounds.max.y <= patientBounds.min.y + FloorClearance + StructureClearance;
        }

        private static bool IsTooCloseToPatient(Bounds proposedBounds, List<Placement> placements)
        {
            foreach (Placement existing in placements)
            {
                if (BoundsDistance(proposedBounds, existing.Bounds) < MinimumPatientGap)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPatientCollider(Collider collider)
        {
            return collider != null && collider.GetComponentInParent<PatientView>(true) != null;
        }

        private static float BoundsDistance(Bounds first, Bounds second)
        {
            float x = Mathf.Max(0.0f, Mathf.Max(first.min.x - second.max.x, second.min.x - first.max.x));
            float y = Mathf.Max(0.0f, Mathf.Max(first.min.y - second.max.y, second.min.y - first.max.y));
            float z = Mathf.Max(0.0f, Mathf.Max(first.min.z - second.max.z, second.min.z - first.max.z));
            return new Vector3(x, y, z).magnitude;
        }

        private static void LogPlacementSummary(List<TrainArea> cars, List<Placement> placements)
        {
            float minimumDistance = float.PositiveInfinity;
            for (int first = 0; first < placements.Count; first++)
            {
                for (int second = first + 1; second < placements.Count; second++)
                {
                    minimumDistance = Mathf.Min(minimumDistance, BoundsDistance(placements[first].Bounds, placements[second].Bounds));
                }
            }

            foreach (Placement placement in placements)
            {
                TrainArea nearest = cars.OrderBy(car => car.Bounds.SqrDistance(placement.Position)).First();
                Debug.Log(
                    $"Triage Trace patient placement: {placement.Patient.name} at {placement.Position:F3}; " +
                    $"nearest car {nearest.Root.name}; structure/floor overlap warning: {placement.StructureOverlapWarning}.",
                    placement.Patient);
            }

            Debug.Log($"Triage Trace patient placement: minimum patient-to-patient bounds distance: {minimumDistance:F3} m.");
        }

        private readonly struct TrainArea
        {
            public TrainArea(GameObject root, Bounds bounds)
            {
                Root = root;
                Bounds = bounds;
            }

            public GameObject Root { get; }
            public Bounds Bounds { get; }
        }

        private readonly struct Placement
        {
            public Placement(
                PatientView patient,
                Vector3 position,
                Bounds bounds,
                GameObject car,
                bool structureOverlapWarning)
            {
                Patient = patient;
                Position = position;
                Bounds = bounds;
                Car = car;
                StructureOverlapWarning = structureOverlapWarning;
            }

            public PatientView Patient { get; }
            public Vector3 Position { get; }
            public Bounds Bounds { get; }
            public GameObject Car { get; }
            public bool StructureOverlapWarning { get; }
        }

        private readonly struct FloorSurface
        {
            public FloorSurface(float height, Collider collider)
            {
                Height = height;
                Collider = collider;
            }

            public float Height { get; }
            public Collider Collider { get; }
        }
    }
}
