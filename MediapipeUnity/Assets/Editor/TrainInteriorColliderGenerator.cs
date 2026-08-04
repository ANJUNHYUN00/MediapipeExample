using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TriageTrace.EditorTools
{
    /// <summary>
    /// Creates collider-only children for the meaningful interior groups of each train car.
    /// Source meshes, renderers, materials, and existing colliders are left untouched.
    /// </summary>
    public static class TrainInteriorColliderGenerator
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string GeneratedRootName = "GeneratedTrainColliders";
        private const string UndoLabel = "Generate Train Interior Colliders";

        [MenuItem("Triage Trace/Generate Train Interior Colliders")]
        public static void GenerateTrainInteriorColliders()
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Triage Trace train collider generation was skipped because Play Mode is active or changing state.");
                EditorUtility.DisplayDialog(
                    "Triage Trace train colliders",
                    "Exit Play Mode before generating train colliders. No scene changes were made.",
                    "OK");
                return;
            }

            int beforeCount = CountSceneColliders(scene);
            List<GameObject> trainRoots = FindTrainRoots(scene);
            if (trainRoots.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace train colliders",
                    "No SubwayTrainEnvironment roots were found in the active prototype scene.",
                    "OK");
                return;
            }

            Camera mainCamera = Camera.main;
            CharacterController controller = mainCamera != null ? mainCamera.GetComponent<CharacterController>() : null;
            if (controller == null)
            {
                Debug.LogWarning("Train collider generation completed without a Main Camera CharacterController to validate.");
            }
            else if (!controller.detectCollisions)
            {
                Debug.LogWarning("Main Camera CharacterController has Detect Collisions disabled.", controller);
            }

            foreach (GameObject trainRoot in trainRoots)
            {
                GenerateForTrain(trainRoot);
            }

            int afterCount = CountSceneColliders(scene);
            ValidateLayerInteraction(mainCamera, trainRoots);
            ValidateStartingOverlap(controller, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Triage Trace train collider generation: scene colliders {beforeCount} -> {afterCount}; train cars processed: {trainRoots.Count}.");
        }

        [MenuItem("Triage Trace/Generate Train Interior Colliders", true)]
        private static bool ValidateGenerateTrainInteriorColliders()
        {
            return TryGetTargetScene(out _, false) && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static bool TryGetTargetScene(out Scene scene, bool showDialog)
        {
            scene = SceneManager.GetActiveScene();
            bool isTarget = scene.IsValid() && scene.name == TargetSceneName;
            if (!isTarget && showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace train colliders",
                    $"This tool only runs in {TargetSceneName}.unity.",
                    "OK");
            }

            return isTarget;
        }

        private static List<GameObject> FindTrainRoots(Scene scene)
        {
            List<GameObject> results = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.IndexOf("SubwayTrainEnvironment", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    results.Add(root);
                }
            }

            return results;
        }

        private static void GenerateForTrain(GameObject trainRoot)
        {
            Transform previousRoot = trainRoot.transform.Find(GeneratedRootName);
            if (previousRoot != null)
            {
                Undo.DestroyObjectImmediate(previousRoot.gameObject);
            }

            GameObject generatedRoot = new GameObject(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(generatedRoot, UndoLabel);
            generatedRoot.transform.SetParent(trainRoot.transform, false);
            generatedRoot.layer = trainRoot.layer;

            ColliderSummary summary = new ColliderSummary();
            foreach (Renderer renderer in trainRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || renderer.transform.IsChildOf(generatedRoot.transform) || HasExistingCollider(renderer))
                {
                    continue;
                }

                SourceGroup group = FindSourceGroup(renderer.transform, trainRoot.transform);
                switch (group)
                {
                    case SourceGroup.Floor:
                        CreateFloorCollider(generatedRoot.transform, renderer, ref summary);
                        break;
                    case SourceGroup.Walls:
                        CreateWallCollider(generatedRoot.transform, renderer, ref summary);
                        break;
                    case SourceGroup.Chair:
                        CreateSeatCollider(generatedRoot.transform, renderer, ref summary);
                        break;
                    case SourceGroup.Pole:
                        CreatePoleCollider(generatedRoot.transform, renderer, ref summary);
                        break;
                    case SourceGroup.Train:
                        CreateFrameOrDoorCollider(generatedRoot.transform, renderer, ref summary);
                        break;
                }
            }

            Debug.Log($"Triage Trace train colliders [{trainRoot.name}]: " +
                $"Box={summary.BoxCount}, Capsule={summary.CapsuleCount}, Mesh={summary.MeshCount}; " +
                $"groups FLOOR={summary.FloorSources}, WALLS={summary.WallSources}, CHAIR={summary.ChairSources}, " +
                $"POLE={summary.PoleSources}, TRAIN={summary.TrainSources}.", generatedRoot);
        }

        private static bool HasExistingCollider(Renderer renderer)
        {
            return renderer.GetComponent<Collider>() != null;
        }

        private static SourceGroup FindSourceGroup(Transform source, Transform trainRoot)
        {
            for (Transform current = source; current != null && current != trainRoot; current = current.parent)
            {
                string name = current.name;
                if (ContainsToken(name, "FLOOR"))
                {
                    return SourceGroup.Floor;
                }

                if (ContainsToken(name, "WALL"))
                {
                    return SourceGroup.Walls;
                }

                if (ContainsToken(name, "CHAIR") || ContainsToken(name, "SEAT"))
                {
                    return SourceGroup.Chair;
                }

                if (ContainsToken(name, "POLE"))
                {
                    return SourceGroup.Pole;
                }

                if (ContainsToken(name, "TRAIN"))
                {
                    return SourceGroup.Train;
                }
            }

            return SourceGroup.None;
        }

        private static bool ContainsToken(string value, string token)
        {
            return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void CreateFloorCollider(Transform parent, Renderer renderer, ref ColliderSummary summary)
        {
            Bounds bounds = renderer.bounds;
            if (bounds.size.y <= 0.6f)
            {
                CreateBoxCollider(parent, renderer.name + "_Floor", bounds, ref summary);
            }
            else
            {
                CreateStaticMeshCollider(parent, renderer, ref summary);
            }

            summary.FloorSources++;
        }

        private static void CreateWallCollider(Transform parent, Renderer renderer, ref ColliderSummary summary)
        {
            CreateStaticMeshCollider(parent, renderer, ref summary);
            summary.WallSources++;
        }

        private static void CreateSeatCollider(Transform parent, Renderer renderer, ref ColliderSummary summary)
        {
            Bounds bounds = renderer.bounds;
            // Very broad merged bounds would close the aisle. Those need explicit authoring instead.
            if (bounds.size.x > 4.5f || bounds.size.z > 1.8f || bounds.size.y < 0.25f)
            {
                return;
            }

            CreateBoxCollider(parent, renderer.name + "_Seat", bounds, ref summary);
            summary.ChairSources++;
        }

        private static void CreatePoleCollider(Transform parent, Renderer renderer, ref ColliderSummary summary)
        {
            Bounds bounds = renderer.bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            if (bounds.size.y < 1.0f || radius > 0.8f)
            {
                return;
            }

            GameObject colliderObject = CreateColliderObject(parent, renderer.name + "_Pole", bounds.center);
            CapsuleCollider collider = colliderObject.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = Mathf.Max(0.03f, radius);
            collider.height = Mathf.Max(bounds.size.y, collider.radius * 2.0f);
            collider.isTrigger = false;
            summary.CapsuleCount++;
            summary.PoleSources++;
        }

        private static void CreateFrameOrDoorCollider(Transform parent, Renderer renderer, ref ColliderSummary summary)
        {
            string name = renderer.name;
            if (!ContainsToken(name, "DOOR") && !ContainsToken(name, "FRAME") &&
                !ContainsToken(name, "THRESHOLD") && !ContainsToken(name, "ENTRY"))
            {
                return;
            }

            Bounds bounds = renderer.bounds;
            if (bounds.size.x > 3.0f || bounds.size.z > 1.2f || bounds.size.y < 0.25f)
            {
                return;
            }

            CreateBoxCollider(parent, renderer.name + "_Frame", bounds, ref summary);
            summary.TrainSources++;
        }

        private static void CreateBoxCollider(Transform parent, string name, Bounds bounds, ref ColliderSummary summary)
        {
            GameObject colliderObject = CreateColliderObject(parent, name, bounds.center);
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.size = bounds.size;
            collider.isTrigger = false;
            summary.BoxCount++;
        }

        private static void CreateStaticMeshCollider(Transform parent, Renderer renderer, ref ColliderSummary summary)
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            GameObject colliderObject = new GameObject(renderer.name + "_Mesh");
            Undo.RegisterCreatedObjectUndo(colliderObject, UndoLabel);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.position = meshFilter.transform.position;
            colliderObject.transform.rotation = meshFilter.transform.rotation;
            colliderObject.transform.localScale = meshFilter.transform.lossyScale;
            colliderObject.layer = parent.gameObject.layer;

            MeshCollider collider = colliderObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            summary.MeshCount++;
        }

        private static GameObject CreateColliderObject(Transform parent, string name, Vector3 worldPosition)
        {
            GameObject colliderObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(colliderObject, UndoLabel);
            colliderObject.transform.SetParent(parent, true);
            colliderObject.transform.position = worldPosition;
            colliderObject.layer = parent.gameObject.layer;
            return colliderObject;
        }

        private static int CountSceneColliders(Scene scene)
        {
            int count = 0;
            foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (collider.gameObject.scene == scene)
                {
                    count++;
                }
            }

            return count;
        }

        private static void ValidateLayerInteraction(Camera mainCamera, List<GameObject> trainRoots)
        {
            if (mainCamera == null)
            {
                return;
            }

            foreach (GameObject trainRoot in trainRoots)
            {
                if (Physics.GetIgnoreLayerCollision(mainCamera.gameObject.layer, trainRoot.layer))
                {
                    Debug.LogWarning($"Main Camera layer does not collide with train layer on {trainRoot.name}.", trainRoot);
                }
            }
        }

        private static void ValidateStartingOverlap(CharacterController controller, Scene scene)
        {
            if (controller == null)
            {
                return;
            }

            Vector3 center = controller.transform.TransformPoint(controller.center);
            float halfHeight = Mathf.Max(0.0f, controller.height * 0.5f - controller.radius);
            Vector3 bottom = center + Vector3.down * halfHeight;
            Vector3 top = center + Vector3.up * halfHeight;
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, controller.radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
            {
                if (overlap != controller)
                {
                    Debug.LogWarning($"Main Camera CharacterController starts overlapping {overlap.name}. Reposition the camera before Play Mode.", overlap);
                }
            }
        }

        private enum SourceGroup
        {
            None,
            Floor,
            Walls,
            Chair,
            Pole,
            Train,
        }

        private struct ColliderSummary
        {
            public int BoxCount;
            public int CapsuleCount;
            public int MeshCount;
            public int FloorSources;
            public int WallSources;
            public int ChairSources;
            public int PoleSources;
            public int TrainSources;
        }
    }
}
