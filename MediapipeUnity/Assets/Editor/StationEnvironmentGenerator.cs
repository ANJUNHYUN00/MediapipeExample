using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TriageTrace.Presentation;

namespace TriageTrace.EditorTools
{
    /// <summary>
    /// Builds the disposable station shell for the dedicated environment prototype scene.
    /// It never creates cameras or changes the train, Patient, pose, or interaction objects.
    /// </summary>
    public static class StationEnvironmentGenerator
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string GeneratedRootName = "GeneratedStationEnvironment";
        private const string UndoLabel = "Generate Station Environment";
        private const string ControllerUndoLabel = "Configure Grounded First-Person Controller";

        [MenuItem("Triage Trace/Generate Station Environment")]
        public static void GenerateStationEnvironment()
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            Generate(scene);
        }

        [MenuItem("Triage Trace/Generate Station Environment", true)]
        private static bool ValidateGenerateStationEnvironment()
        {
            return TryGetTargetScene(out _, false);
        }

        [MenuItem("Triage Trace/Configure Grounded First-Person Controller")]
        public static void ConfigureGroundedFirstPersonController()
        {
            if (!TryGetTargetScene(out Scene scene, true))
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null || mainCamera.gameObject.scene != scene)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace grounded controller",
                    "The prototype scene must contain one active Main Camera.",
                    "OK");
                return;
            }

            FirstPersonCameraController controller = mainCamera.GetComponent<FirstPersonCameraController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace grounded controller",
                    "Main Camera does not have FirstPersonCameraController. No changes were made.",
                    "OK");
                return;
            }

            CharacterController characterController = mainCamera.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = Undo.AddComponent<CharacterController>(mainCamera.gameObject);
            }
            else
            {
                Undo.RecordObject(characterController, ControllerUndoLabel);
            }

            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            // The camera transform is the eye position, so the capsule extends from eye level to the feet.
            characterController.center = new Vector3(0.0f, -0.9f, 0.0f);
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45.0f;

            Undo.RecordObject(controller, ControllerUndoLabel);
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("requireRightMouseButton").boolValue = false;
            serializedController.FindProperty("lockCursorWhileLooking").boolValue = true;
            serializedController.FindProperty("allowFlyMode").boolValue = false;
            serializedController.FindProperty("gravity").floatValue = 20.0f;
            serializedController.FindProperty("jumpHeight").floatValue = 1.60f;
            serializedController.ApplyModifiedProperties();

            EditorUtility.SetDirty(characterController);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = mainCamera.gameObject;
        }

        [MenuItem("Triage Trace/Configure Grounded First-Person Controller", true)]
        private static bool ValidateConfigureGroundedFirstPersonController()
        {
            return TryGetTargetScene(out _, false);
        }

        /// <summary>
        /// Intended for the Unity batch-mode verification command. It is deliberately separate
        /// from the menu action so the menu leaves saving under the scene author's control.
        /// </summary>
        public static void GenerateAndSaveForBatchMode()
        {
            if (!TryGetTargetScene(out Scene scene, false))
            {
                throw new InvalidOperationException(
                    $"Open {TargetSceneName}.unity before generating the station environment.");
            }

            Generate(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("The generated station environment scene could not be saved.");
            }
        }

        private static bool TryGetTargetScene(out Scene scene, bool showDialog)
        {
            scene = SceneManager.GetActiveScene();
            bool isTarget = scene.IsValid() && scene.name == TargetSceneName;
            if (!isTarget && showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Triage Trace station generator",
                    $"This tool only runs in {TargetSceneName}.unity. Open that prototype scene and try again.",
                    "OK");
            }

            return isTarget;
        }

        private static void Generate(Scene scene)
        {
            if (!TryGetTrainBounds(scene, out Bounds trainBounds))
            {
                throw new InvalidOperationException(
                    "No train renderers were found. Train roots must include 'Train' in their name.");
            }

            GameObject previousRoot = GameObject.Find(GeneratedRootName);
            if (previousRoot != null && previousRoot.scene == scene)
            {
                Undo.DestroyObjectImmediate(previousRoot);
            }

            GameObject root = new GameObject(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            SceneManager.MoveGameObjectToScene(root, scene);

            StationMaterials materials = CreateMaterials();
            BuildStation(root.transform, trainBounds, materials);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool TryGetTrainBounds(Scene scene, out Bounds bounds)
        {
            bool foundRenderer = false;
            bounds = new Bounds();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.IndexOf("train", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled || renderer.gameObject == null)
                    {
                        continue;
                    }

                    if (!foundRenderer)
                    {
                        bounds = renderer.bounds;
                        foundRenderer = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return foundRenderer;
        }

        private static void BuildStation(Transform root, Bounds train, StationMaterials materials)
        {
            float length = Mathf.Max(train.size.x + 12.0f, 32.0f);
            float centerX = train.center.x;
            float floorY = train.min.y - 0.15f;
            float platformWidth = 6.5f;
            float platformNearEdge = train.min.z - 0.2f;
            float platformCenterZ = platformNearEdge - platformWidth * 0.5f;
            float platformOuterEdge = platformNearEdge - platformWidth;
            float emergencyWidth = 2.4f;
            float emergencyCenterZ = train.max.z + 1.8f;
            float rearWallZ = emergencyCenterZ + emergencyWidth * 0.5f + 0.35f;
            float ceilingY = Mathf.Max(train.max.y + 3.5f, floorY + 7.0f);
            float tunnelCenterZ = (platformOuterEdge + rearWallZ) * 0.5f;
            float tunnelWidth = rearWallZ - platformOuterEdge;

            Transform structure = CreateGroup(root, "StationStructure");
            Transform route = CreateGroup(root, "RouteAndSearchZones");
            Transform lighting = CreateGroup(root, "StationLighting");

            CreateBox(structure, "PlatformFloor", new Vector3(centerX, floorY, platformCenterZ),
                new Vector3(length, 0.3f, platformWidth), materials.Concrete, true);
            CreateBox(structure, "EmergencyWalkway", new Vector3(centerX, floorY, emergencyCenterZ),
                new Vector3(length, 0.25f, emergencyWidth), materials.Concrete, true);
            CreateBox(structure, "PlatformEndSafetyWall_West", new Vector3(train.min.x - 6.0f, floorY + 1.5f, tunnelCenterZ),
                new Vector3(0.35f, 3.0f, tunnelWidth), materials.Concrete, true);
            CreateBox(structure, "PlatformEndSafetyWall_East", new Vector3(train.max.x + 6.0f, floorY + 1.5f, tunnelCenterZ),
                new Vector3(0.35f, 3.0f, tunnelWidth), materials.Concrete, true);
            CreateBox(structure, "TunnelWall_PlatformSide", new Vector3(centerX, floorY + 3.0f, platformOuterEdge - 0.25f),
                new Vector3(length, 6.0f, 0.5f), materials.ConcreteDark, true);
            CreateBox(structure, "TunnelWall_TrackSide", new Vector3(centerX, floorY + 3.0f, rearWallZ),
                new Vector3(length, 6.0f, 0.5f), materials.ConcreteDark, true);
            CreateBox(structure, "TunnelCeiling", new Vector3(centerX, ceilingY, tunnelCenterZ),
                new Vector3(length, 0.35f, tunnelWidth), materials.ConcreteDark, true);

            CreateBox(route, "YellowSafetyMarking", new Vector3(centerX, floorY + 0.17f, platformNearEdge - 0.55f),
                new Vector3(length - 1.0f, 0.035f, 0.32f), materials.SafetyYellow, false);
            CreateZone(route, "StartZone", train.min.x + 3.0f, floorY + 0.18f, platformCenterZ, materials.StartZone);
            CreateZone(route, "PatientSearchZone_01", Mathf.Lerp(train.min.x, train.max.x, 0.25f), floorY + 0.18f, platformCenterZ, materials.SearchZone);
            CreateZone(route, "PatientSearchZone_02", Mathf.Lerp(train.min.x, train.max.x, 0.50f), floorY + 0.18f, platformCenterZ, materials.SearchZone);
            CreateZone(route, "PatientSearchZone_03", Mathf.Lerp(train.min.x, train.max.x, 0.75f), floorY + 0.18f, platformCenterZ, materials.SearchZone);

            float firstColumn = train.min.x - 3.0f;
            float lastColumn = train.max.x + 3.0f;
            for (float x = firstColumn; x <= lastColumn + 0.01f; x += 6.0f)
            {
                CreateColumn(structure, $"PlatformColumn_{Mathf.RoundToInt(x * 10.0f)}", x, floorY, platformOuterEdge + 0.55f, ceilingY, materials.Concrete);
            }

            CreateLight(lighting, "NeutralPlatformLight_01", new Vector3(train.min.x + 4.0f, ceilingY - 0.45f, platformCenterZ), Color.white, 5.0f, 16.0f);
            CreateLight(lighting, "NeutralPlatformLight_02", new Vector3(train.center.x, ceilingY - 0.45f, platformCenterZ), Color.white, 5.0f, 16.0f);
            CreateLight(lighting, "NeutralPlatformLight_03", new Vector3(train.max.x - 4.0f, ceilingY - 0.45f, platformCenterZ), Color.white, 5.0f, 16.0f);
            CreateLight(lighting, "EmergencyRouteLight_West", new Vector3(train.min.x + 2.0f, floorY + 2.3f, emergencyCenterZ), new Color(1.0f, 0.22f, 0.12f), 3.0f, 8.0f);
            CreateLight(lighting, "EmergencyRouteLight_East", new Vector3(train.max.x - 2.0f, floorY + 2.3f, emergencyCenterZ), new Color(1.0f, 0.22f, 0.12f), 3.0f, 8.0f);
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void CreateZone(Transform parent, string name, float x, float y, float z, Material material)
        {
            CreateBox(parent, name, new Vector3(x, y, z), new Vector3(3.2f, 0.035f, 3.2f), material, false);
        }

        private static void CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool includeCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(box, UndoLabel);
            box.name = name;
            box.transform.SetParent(parent, true);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;

            if (!includeCollider)
            {
                UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            }
        }

        private static void CreateColumn(Transform parent, string name, float x, float floorY, float z, float ceilingY, Material material)
        {
            GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(column, UndoLabel);
            column.name = name;
            column.transform.SetParent(parent, true);
            column.transform.position = new Vector3(x, (floorY + ceilingY) * 0.5f, z);
            column.transform.localScale = new Vector3(0.45f, (ceilingY - floorY) * 0.5f, 0.45f);
            column.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(lightObject, UndoLabel);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        private static StationMaterials CreateMaterials()
        {
            return new StationMaterials
            {
                Concrete = CreateStandardMaterial("Station Concrete", new Color(0.20f, 0.22f, 0.24f), 0.72f),
                ConcreteDark = CreateStandardMaterial("Station Dark Concrete", new Color(0.09f, 0.10f, 0.12f), 0.82f),
                SafetyYellow = CreateStandardMaterial("Station Safety Yellow", new Color(0.95f, 0.68f, 0.05f), 0.45f),
                StartZone = CreateStandardMaterial("Station Start Zone", new Color(0.15f, 0.42f, 0.62f), 0.55f),
                SearchZone = CreateStandardMaterial("Station Search Zone", new Color(0.16f, 0.55f, 0.58f), 0.55f),
            };
        }

        private static Material CreateStandardMaterial(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("Built-in Standard shader was not found.");
            }

            Material material = new Material(shader) { name = name };
            Undo.RegisterCreatedObjectUndo(material, UndoLabel);
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        private sealed class StationMaterials
        {
            public Material Concrete;
            public Material ConcreteDark;
            public Material SafetyYellow;
            public Material StartZone;
            public Material SearchZone;
        }
    }
}
