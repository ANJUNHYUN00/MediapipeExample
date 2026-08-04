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
    /// Creates low-complexity, non-medical scenario dressing from Unity primitives only.
    /// Interior objects stay visual-only. A few exterior, route-side obstacles use ordinary
    /// colliders so first-person inspection has physical scale without covering patients.
    /// </summary>
    public static class PrototypeDisasterDressingGenerator
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string GeneratedRootName = "GeneratedDisasterDressing";
        private const string UndoLabel = "Add Prototype Disaster Dressing";

        [MenuItem("Triage Trace/Add Prototype Disaster Dressing")]
        public static void AddPrototypeDisasterDressing()
        {
            if (!TryGetTargetScene(out Scene scene, true) ||
                !TryGetTrainBounds(scene, out Bounds trainBounds))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                Transform oldRoot = scene.GetRootGameObjects()
                    .Select(root => root.transform)
                    .FirstOrDefault(root => root.name == GeneratedRootName);
                if (oldRoot != null)
                {
                    Undo.DestroyObjectImmediate(oldRoot.gameObject);
                }

                GameObject root = new GameObject(GeneratedRootName);
                Undo.RegisterCreatedObjectUndo(root, UndoLabel);
                SceneManager.MoveGameObjectToScene(root, scene);

                Materials materials = CreateMaterials();
                BuildExterior(root.transform, trainBounds, materials);
                BuildInterior(root.transform, scene, materials);

                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = root;
                LogSummary(scene, root);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Triage Trace disaster dressing",
                    "Generation failed and was reverted. See Console for details.",
                    "OK");
            }
        }

        [MenuItem("Triage Trace/Add Prototype Disaster Dressing", true)]
        private static bool ValidateAddPrototypeDisasterDressing()
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
                    "Triage Trace disaster dressing",
                    $"Open {TargetSceneName}.unity in Edit Mode before adding dressing.",
                    "OK");
            }

            return valid;
        }

        private static bool TryGetTrainBounds(Scene scene, out Bounds bounds)
        {
            Renderer[] trainRenderers = scene.GetRootGameObjects()
                .Where(root => root.name.StartsWith("SubwayTrainEnvironment_Car", StringComparison.Ordinal))
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            if (trainRenderers.Length == 0)
            {
                bounds = default;
                EditorUtility.DisplayDialog(
                    "Triage Trace disaster dressing",
                    "No train-car renderers were found. No scene changes were made.",
                    "OK");
                return false;
            }

            bounds = trainRenderers[0].bounds;
            for (int index = 1; index < trainRenderers.Length; index++)
            {
                bounds.Encapsulate(trainRenderers[index].bounds);
            }

            return true;
        }

        private static void BuildExterior(Transform root, Bounds train, Materials materials)
        {
            Transform exterior = CreateGroup(root, "ExteriorScenarioDressing");
            float floorY = train.min.y + 0.12f;
            float platformSideZ = train.min.z - 2.7f;
            float outerPlatformZ = train.min.z - 5.0f;

            // The dressing lives alongside the platform edge, leaving the train doors,
            // centre walkway, and patient search route visually open.
            CreatePrimitive(
                exterior,
                PrimitiveType.Cube,
                "TippedSafetyBarrier",
                new Vector3(train.center.x - train.size.x * 0.25f, floorY + 0.38f, outerPlatformZ),
                new Vector3(2.4f, 0.72f, 0.14f),
                new Vector3(0.0f, 0.0f, 76.0f),
                materials.Warning,
                addCollider: true);
            CreatePrimitive(
                exterior,
                PrimitiveType.Cube,
                "DamagedPlatformPanel",
                new Vector3(train.center.x + train.size.x * 0.19f, floorY + 0.9f, outerPlatformZ + 0.25f),
                new Vector3(1.45f, 1.8f, 0.12f),
                new Vector3(0.0f, 18.0f, 12.0f),
                materials.DarkMetal,
                addCollider: true);
            CreatePrimitive(
                exterior,
                PrimitiveType.Cube,
                "UtilityCrate_A",
                new Vector3(train.center.x + train.size.x * 0.32f, floorY + 0.32f, platformSideZ),
                new Vector3(0.82f, 0.64f, 0.66f),
                new Vector3(4.0f, 22.0f, -8.0f),
                materials.Brown,
                addCollider: true);
            CreatePrimitive(
                exterior,
                PrimitiveType.Cube,
                "UtilityCrate_B",
                new Vector3(train.center.x + train.size.x * 0.36f, floorY + 0.22f, platformSideZ + 0.58f),
                new Vector3(0.54f, 0.44f, 0.48f),
                new Vector3(-12.0f, -18.0f, 10.0f),
                materials.Brown,
                addCollider: true);
            CreatePrimitive(
                exterior,
                PrimitiveType.Cube,
                "CollapsedCanopyBeam",
                new Vector3(train.center.x - train.size.x * 0.40f, floorY + 1.15f, outerPlatformZ + 0.62f),
                new Vector3(3.6f, 0.22f, 0.28f),
                new Vector3(0.0f, 16.0f, 18.0f),
                materials.DarkMetal,
                addCollider: true);
            CreatePrimitive(
                exterior,
                PrimitiveType.Cylinder,
                "BentSupportPole",
                new Vector3(train.center.x - train.size.x * 0.43f, floorY + 0.82f, outerPlatformZ + 0.75f),
                new Vector3(0.16f, 0.88f, 0.16f),
                new Vector3(0.0f, 0.0f, 58.0f),
                materials.DarkMetal,
                addCollider: true);
            CreateDebrisField(
                exterior,
                new Vector3(train.center.x + train.size.x * 0.04f, floorY + 0.10f, outerPlatformZ + 0.7f),
                materials);
            CreateSmokeColumn(
                exterior,
                "ExteriorSmoke",
                new Vector3(train.center.x - train.size.x * 0.07f, floorY + 0.6f, outerPlatformZ + 0.45f),
                materials.Smoke);
            CreateWarningBeacon(
                exterior,
                "ExteriorWarningBeacon",
                new Vector3(train.center.x + train.size.x * 0.42f, floorY + 0.45f, outerPlatformZ),
                materials.Warning);
        }

        private static void BuildInterior(Transform root, Scene scene, Materials materials)
        {
            Transform interior = CreateGroup(root, "InteriorScenarioDressing");
            GameObject[] cars = scene.GetRootGameObjects()
                .Where(car => car.name.StartsWith("SubwayTrainEnvironment_Car", StringComparison.Ordinal))
                .OrderBy(car => car.name, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < cars.Length; index++)
            {
                Bounds car = GetRendererBounds(cars[index]);
                bool longAxisIsX = car.size.x >= car.size.z;
                float floorY = FindFloorHeight(cars[index], car);
                Vector3 endPosition = longAxisIsX
                    ? new Vector3(Mathf.Lerp(car.min.x, car.max.x, index % 2 == 0 ? 0.16f : 0.84f), floorY, car.center.z)
                    : new Vector3(car.center.x, floorY, Mathf.Lerp(car.min.z, car.max.z, index % 2 == 0 ? 0.16f : 0.84f));

            // Keep each item at the end of the aisle and collider-free. This gives a
            // damaged-scene impression without changing navigation or patient targeting.
                CreatePrimitive(
                    interior,
                    PrimitiveType.Cube,
                    $"FallenInformationPanel_{index + 1:00}",
                    endPosition + new Vector3(0.0f, 0.23f, longAxisIsX ? 0.38f : 0.0f),
                    longAxisIsX ? new Vector3(0.16f, 0.46f, 0.88f) : new Vector3(0.88f, 0.46f, 0.16f),
                    new Vector3(0.0f, 0.0f, longAxisIsX ? 74.0f : 0.0f),
                    materials.DarkMetal);
                CreatePrimitive(
                    interior,
                    PrimitiveType.Cube,
                    $"SmallDebris_{index + 1:00}",
                    endPosition + new Vector3(longAxisIsX ? 0.52f : 0.28f, 0.08f, longAxisIsX ? -0.42f : 0.52f),
                    new Vector3(0.24f, 0.16f, 0.2f),
                    new Vector3(18.0f, 35.0f, 22.0f),
                    materials.Brown);
            CreateWarningBeacon(
                    interior,
                    $"InteriorWarningBeacon_{index + 1:00}",
                    endPosition + new Vector3(longAxisIsX ? -0.35f : 0.0f, 0.28f, longAxisIsX ? 0.42f : -0.35f),
                    materials.Warning);
                CreatePrimitive(
                    interior,
                    PrimitiveType.Cylinder,
                    $"LooseCable_{index + 1:00}",
                    endPosition + new Vector3(longAxisIsX ? 0.18f : 0.34f, 0.035f, longAxisIsX ? -0.52f : 0.18f),
                    new Vector3(0.42f, 0.025f, 0.42f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    materials.DarkMetal);
            }
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static float FindFloorHeight(GameObject car, Bounds carBounds)
        {
            Renderer floor = car.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(renderer => renderer.bounds.max.y)
                .FirstOrDefault();
            return floor == null ? carBounds.min.y : floor.bounds.max.y;
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(group, UndoLabel);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void CreatePrimitive(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 position,
            Vector3 scale,
            Vector3 eulerAngles,
            Material material,
            bool addCollider = false)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            Undo.RegisterCreatedObjectUndo(item, UndoLabel);
            item.name = name;
            item.transform.SetParent(parent, true);
            item.transform.SetPositionAndRotation(position, Quaternion.Euler(eulerAngles));
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = item.GetComponent<Collider>();
            if (!addCollider && collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void CreateSmokeColumn(Transform parent, string name, Vector3 position, Material material)
        {
            CreatePrimitive(parent, PrimitiveType.Sphere, name + "_Low", position,
                new Vector3(1.4f, 0.55f, 1.0f), new Vector3(8.0f, 0.0f, 12.0f), material);
            CreatePrimitive(parent, PrimitiveType.Sphere, name + "_High", position + new Vector3(0.32f, 0.62f, 0.08f),
                new Vector3(1.0f, 0.72f, 0.9f), new Vector3(-6.0f, 0.0f, -16.0f), material);
        }

        private static void CreateDebrisField(Transform parent, Vector3 origin, Materials materials)
        {
            Vector3[] offsets =
            {
                new Vector3(-0.70f, 0.0f, -0.18f),
                new Vector3(-0.32f, 0.0f, 0.26f),
                new Vector3(0.12f, 0.0f, -0.30f),
                new Vector3(0.48f, 0.0f, 0.22f),
                new Vector3(0.78f, 0.0f, -0.08f),
            };
            for (int index = 0; index < offsets.Length; index++)
            {
                float size = 0.16f + index * 0.035f;
                CreatePrimitive(
                    parent,
                    PrimitiveType.Cube,
                    $"PlatformFragment_{index + 1:00}",
                    origin + offsets[index] + Vector3.up * size * 0.4f,
                    new Vector3(size * 1.8f, size * 0.7f, size),
                    new Vector3(12.0f * index, 29.0f * index, 17.0f * index),
                    index % 2 == 0 ? materials.DarkMetal : materials.Brown);
            }
        }

        private static void CreateWarningBeacon(Transform parent, string name, Vector3 position, Material material)
        {
            CreatePrimitive(parent, PrimitiveType.Cylinder, name, position,
                new Vector3(0.13f, 0.08f, 0.13f), Vector3.zero, material);
            GameObject lightObject = new GameObject(name + "_Light");
            Undo.RegisterCreatedObjectUndo(lightObject, UndoLabel);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = position + Vector3.up * 0.18f;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.38f, 0.08f);
            light.intensity = 1.2f;
            light.range = 2.5f;
            light.shadows = LightShadows.None;
        }

        private static Materials CreateMaterials()
        {
            return new Materials
            {
                DarkMetal = CreateMaterial("Dressing Dark Metal", new Color(0.12f, 0.14f, 0.16f), 0.55f),
                Brown = CreateMaterial("Dressing Brown Debris", new Color(0.25f, 0.16f, 0.09f), 0.82f),
                Warning = CreateMaterial("Dressing Warning Orange", new Color(1.0f, 0.33f, 0.06f), 0.38f),
                Smoke = CreateTransparentMaterial("Dressing Smoke", new Color(0.16f, 0.18f, 0.2f, 0.30f)),
            };
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("Built-in Standard shader was not found.");
            }

            Material material = new Material(shader) { name = name, color = color };
            material.SetFloat("_Glossiness", smoothness);
            Undo.RegisterCreatedObjectUndo(material, UndoLabel);
            return material;
        }

        private static Material CreateTransparentMaterial(string name, Color color)
        {
            Material material = CreateMaterial(name, color, 0.05f);
            material.SetFloat("_Mode", 3.0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            return material;
        }

        private static void LogSummary(Scene scene, GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            int generatedColliderCount = root.GetComponentsInChildren<Collider>(true).Length;
            int visualPatientOverlapWarnings = 0;
            foreach (PatientView patient in UnityEngine.Object.FindObjectsByType<PatientView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (patient.gameObject.scene != scene)
                {
                    continue;
                }

                Collider patientCollider = patient.GetComponentInChildren<Collider>(true);
                if (patientCollider == null)
                {
                    continue;
                }

                foreach (Renderer renderer in renderers)
                {
                    if (renderer.bounds.Intersects(patientCollider.bounds))
                    {
                        visualPatientOverlapWarnings++;
                        Debug.LogWarning(
                            $"Triage Trace disaster dressing visual overlap warning: {renderer.name} intersects {patient.name}.",
                            renderer);
                    }
                }
            }

            Debug.Log(
                $"Triage Trace disaster dressing: generated {renderers.Length} visual primitives, " +
                $"{generatedColliderCount} generated colliders, and {visualPatientOverlapWarnings} patient visual-overlap warnings. " +
                "Interior dressing is visual-only; exterior obstacles have deliberate colliders away from patient targets.",
                root);
        }

        private sealed class Materials
        {
            public Material DarkMetal;
            public Material Brown;
            public Material Warning;
            public Material Smoke;
        }
    }
}
