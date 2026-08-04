using System;
using System.Linq;
using TMPro;
using TriageTrace.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TriageTrace.EditorTools
{
    /// <summary>Explicit, undoable installation of the single peripheral AR Glass HUD canvas.</summary>
    public static class ARGuidanceHudInstaller
    {
        private const string TargetSceneName = "TriageTraceEnvironmentPrototype";
        private const string RootName = "ARGuidanceHUD";
        private const string UndoLabel = "Install or Update AR Glass Operations HUD";
        private static readonly Color Panel = new Color(0.02f, 0.08f, 0.11f, 0.62f);
        private static readonly Color Text = new Color(0.84f, 0.95f, 0.98f, 1f);
        private static readonly Color Cyan = new Color(0.1f, 0.88f, 1f, 1f);
        private static readonly Color Amber = new Color(1f, 0.68f, 0.2f, 1f);

        [MenuItem("Triage Trace/TriageTraceEnvironmentPrototype/Install or Update AR Glass Operations HUD")]
        public static void InstallOrUpdate()
        {
            if (!TryGetTargetScene(out Scene scene, true)) return;
            Camera camera = FindOnlyMainCamera(scene);
            if (camera == null)
            {
                EditorUtility.DisplayDialog("AR Glass Operations HUD", "The prototype scene needs exactly one active MainCamera.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            int group = Undo.GetCurrentGroup();
            try
            {
                GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);
                if (root == null)
                {
                    root = new GameObject(RootName, typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(root, UndoLabel);
                    SceneManager.MoveGameObjectToScene(root, scene);
                }
                else
                {
                    Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                    foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
                }

                Build(root, camera);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = root;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("AR Glass Operations HUD", "HUD update was reverted. See Console for details.", "OK");
            }
        }

        [MenuItem("Triage Trace/TriageTraceEnvironmentPrototype/Install or Update AR Glass Operations HUD", true)]
        private static bool ValidateInstallOrUpdate() => TryGetTargetScene(out _, false);

        private static void Build(GameObject root, Camera camera)
        {
            Canvas canvas = root.GetComponent<Canvas>() ?? root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            if (root.GetComponent<CanvasScaler>() == null) root.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster != null) Undo.DestroyObjectImmediate(raycaster);

            TMP_Text zone = CreatePanel(root.transform, "TopCenter", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(280, 52), "DIRECTION  PLATFORM\nSIMULATION ONLY", TextAnchor.MiddleCenter, Cyan, 13);
            TMP_Text link = CreatePanel(root.transform, "Link", new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(220, 27), "LINK WAITING", TextAnchor.MiddleLeft, Cyan, 12);
            TMP_Text pose = CreatePanel(root.transform, "Pose", new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -47), new Vector2(220, 27), "POSE WAITING", TextAnchor.MiddleLeft, Amber, 12);
            TMP_Text left = CreatePanel(root.transform, "LeftGuidance", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(16, 0), new Vector2(170, 46), "", TextAnchor.MiddleLeft, Amber, 13);
            TMP_Text right = CreatePanel(root.transform, "RightGuidance", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-16, 0), new Vector2(170, 46), "", TextAnchor.MiddleRight, Amber, 13);
            GameObject nearbyPatientsPanel = CreatePanelContainer(root.transform, "NearbyPatientsPanel", new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 16), new Vector2(320, 156));
            TMP_Text status = CreateText(nearbyPatientsPanel.transform, "PatientStatus", new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -8), new Vector2(300, 40), "NEARBY PATIENTS\nUNCONFIRMED 0 / CHECKED 0", TextAnchor.UpperLeft, Cyan, 12, 0);
            TMP_Text rows = CreateText(nearbyPatientsPanel.transform, "PatientRows", new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -52), new Vector2(300, 96), "No scenario targets.", TextAnchor.UpperLeft, Text, 11, 5);
            TMP_Text sync = CreatePanel(root.transform, "LocalTeamSync", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(300, 92), "LOCAL TEAM SYNC\nNo local confirmations recorded.", TextAnchor.UpperLeft, Text, 11);

            ARGuidanceHud hud = root.GetComponent<ARGuidanceHud>() ?? root.AddComponent<ARGuidanceHud>();
            hud.Configure(UnityEngine.Object.FindFirstObjectByType<PoseReceiverBehaviour>(), camera, zone, link, pose, left, right, status, rows, sync);
            EditorUtility.SetDirty(hud);
        }

        private static TMP_Text CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, string value, TextAnchor alignment, Color color, float fontSize)
        {
            GameObject panel = CreatePanelContainer(parent, name, anchor, pivot, position, size);
            return CreateText(panel.transform, "Text", Vector2.zero, Vector2.zero, new Vector2(9, 3), size - new Vector2(18, 6), value, alignment, color, fontSize, 0);
        }

        private static GameObject CreatePanelContainer(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>(); image.color = Panel; image.raycastTarget = false;
            return panel;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, string value, TextAnchor alignment, Color color, float fontSize, float lineSpacing)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = anchor; textRect.anchorMax = anchor; textRect.pivot = pivot; textRect.anchoredPosition = position; textRect.sizeDelta = size;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value; text.fontSize = fontSize; text.alignment = ToTextMeshProAlignment(alignment); text.color = color; text.raycastTarget = false; text.enableWordWrapping = false;
            text.lineSpacing = lineSpacing;
            return text;
        }

        private static TextAlignmentOptions ToTextMeshProAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.TopLeft;
            }
        }

        private static Camera FindOnlyMainCamera(Scene scene)
        {
            Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item.gameObject.scene == scene && item.gameObject.activeInHierarchy && item.CompareTag("MainCamera")).ToArray();
            return cameras.Length == 1 ? cameras[0] : null;
        }

        private static bool TryGetTargetScene(out Scene scene, bool showDialog)
        {
            scene = SceneManager.GetActiveScene();
            bool valid = scene.IsValid() && scene.name == TargetSceneName && !EditorApplication.isPlayingOrWillChangePlaymode;
            if (!valid && showDialog) EditorUtility.DisplayDialog("AR Glass Operations HUD", "Open TriageTraceEnvironmentPrototype in Edit Mode before using this menu.", "OK");
            return valid;
        }
    }
}
