using UnityEngine;
using UnityEngine.UI;

namespace TriageTrace.Presentation
{
    public sealed class TriageTraceScenarioBootstrap : MonoBehaviour
    {
        [SerializeField]
        private PoseReceiverBehaviour poseReceiver;

        [SerializeField]
        private PosePointerLineRenderer pointerLine;

        [SerializeField]
        private PointerRaycaster pointerRaycaster;

        [SerializeField]
        private PatientDwellSelector dwellSelector;

        [SerializeField]
        private PatientStatusCardUI statusCard;

        [SerializeField]
        private WorldSpacePatientStatusCard worldSpaceStatusCard;

        [SerializeField]
        private Transform pointerOrigin;

        [SerializeField]
        private LayerMask patientLayerMask = ~0;

        [SerializeField]
        [Min(0.01f)]
        private float raycastDistance = 10.0f;

        [SerializeField]
        [Min(0.05f)]
        private float dwellSeconds = 0.7f;

        [SerializeField]
        private bool createStatusCardIfMissing = true;

        [SerializeField]
        private bool enableScreenSpaceStatusCard;

        [SerializeField]
        private bool createWorldSpaceStatusCardIfMissing = true;

        [SerializeField]
        private WorldSpaceCardDisplayMode worldSpaceDisplayMode =
            WorldSpaceCardDisplayMode.HoverOrSelected;

        [SerializeField]
        private Vector3 worldSpaceCardOffset = new Vector3(0.0f, 1.3f, 0.0f);

        [SerializeField]
        [Min(0.0001f)]
        private float worldSpaceCanvasScale = 0.003f;

        [SerializeField]
        private Vector2 worldSpaceCardPixelSize = new Vector2(320.0f, 180.0f);

        [SerializeField]
        private Camera worldSpaceCamera;

        private void Awake()
        {
            ResolveReferences();
            ConnectComponents();
        }

        private void OnValidate()
        {
            raycastDistance = Mathf.Max(0.01f, raycastDistance);
            dwellSeconds = Mathf.Max(0.05f, dwellSeconds);
            worldSpaceCanvasScale = Mathf.Max(0.0001f, worldSpaceCanvasScale);
            worldSpaceCardPixelSize.x = Mathf.Max(1.0f, worldSpaceCardPixelSize.x);
            worldSpaceCardPixelSize.y = Mathf.Max(1.0f, worldSpaceCardPixelSize.y);
        }

        public void ResolveReferences()
        {
            if (poseReceiver == null)
            {
                poseReceiver = GetComponent<PoseReceiverBehaviour>();
            }

            if (pointerLine == null)
            {
                pointerLine = GetComponent<PosePointerLineRenderer>();
            }

            if (pointerRaycaster == null)
            {
                pointerRaycaster = GetComponent<PointerRaycaster>();
            }

            if (dwellSelector == null)
            {
                dwellSelector = GetComponent<PatientDwellSelector>();
            }

            if (statusCard == null)
            {
                statusCard = FindFirstObjectByType<PatientStatusCardUI>();
            }

            if (worldSpaceStatusCard == null)
            {
                worldSpaceStatusCard = FindFirstObjectByType<WorldSpacePatientStatusCard>();
            }

            if (pointerOrigin == null)
            {
                pointerOrigin = transform;
            }

            if (statusCard == null && createStatusCardIfMissing)
            {
                statusCard = CreateStatusCard();
            }

            if (worldSpaceStatusCard == null &&
                createWorldSpaceStatusCardIfMissing)
            {
                worldSpaceStatusCard = CreateWorldSpaceStatusCard();
            }

            if (worldSpaceCamera == null)
            {
                worldSpaceCamera = Camera.main;
            }
            SetScreenSpaceCardEnabled(enableScreenSpaceStatusCard);
        }

        public void ConnectComponents()
        {
            if (pointerLine == null ||
                pointerRaycaster == null ||
                dwellSelector == null)
            {
                return;
            }

            poseReceiver?.SetPointerLine(pointerLine);
            pointerRaycaster.ConfigureForTests(
                pointerLine,
                pointerOrigin == null ? transform : pointerOrigin,
                patientLayerMask,
                raycastDistance);
            dwellSelector.ConfigureForTests(
                pointerRaycaster,
                dwellSeconds,
                statusCard);

            if (worldSpaceStatusCard != null)
            {
                RectTransform worldRect =
                    worldSpaceStatusCard.GetComponent<RectTransform>();
                if (worldRect != null)
                {
                    worldRect.sizeDelta = worldSpaceCardPixelSize;
                }
                worldSpaceStatusCard.transform.localScale =
                    Vector3.one * worldSpaceCanvasScale;
                worldSpaceStatusCard.Configure(
                    worldSpaceStatusCard.StatusCard,
                    pointerRaycaster,
                    dwellSelector,
                    worldSpaceCamera,
                    worldSpaceDisplayMode,
                    worldSpaceCardOffset);
            }
        }

        private void SetScreenSpaceCardEnabled(bool enabled)
        {
            if (statusCard == null)
            {
                return;
            }

            Canvas canvas = statusCard.GetComponentInParent<Canvas>(true);
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.enabled = enabled;
            }
        }

        private WorldSpacePatientStatusCard CreateWorldSpaceStatusCard()
        {
            var canvasObject = new GameObject(
                "WorldSpacePatientStatusCard",
                typeof(RectTransform));
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = worldSpaceCardPixelSize;
            rect.localScale = Vector3.one * worldSpaceCanvasScale;

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldSpaceCamera == null
                ? Camera.main
                : worldSpaceCamera;
            canvas.sortingOrder = 10;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject(
                "PatientStatusCard",
                typeof(RectTransform));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panelObject.AddComponent<Image>();

            Text patientText = CreateText(
                "Patient ID Text",
                panelObject.transform,
                new Vector2(12.0f, -14.0f));
            Text stateText = CreateText(
                "Interaction State Text",
                panelObject.transform,
                new Vector2(12.0f, -48.0f));
            Text checkedText = CreateText(
                "Checked Status Text",
                panelObject.transform,
                new Vector2(12.0f, -82.0f));
            Button button = CreateButton(panelObject.transform);

            var card = panelObject.AddComponent<PatientStatusCardUI>();
            card.ConfigureForTests(
                panelObject,
                patientText,
                stateText,
                checkedText,
                button,
                panelImage);

            var worldCard =
                canvasObject.AddComponent<WorldSpacePatientStatusCard>();
            worldCard.Configure(
                card,
                pointerRaycaster,
                dwellSelector,
                canvas.worldCamera,
                worldSpaceDisplayMode,
                worldSpaceCardOffset);
            return worldCard;
        }

        private PatientStatusCardUI CreateStatusCard()
        {
            var canvasObject = new GameObject("Triage Trace Status Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject("PatientStatusCard");
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1.0f, 1.0f);
            panelRect.anchorMax = new Vector2(1.0f, 1.0f);
            panelRect.pivot = new Vector2(1.0f, 1.0f);
            panelRect.anchoredPosition = new Vector2(-24.0f, -24.0f);
            panelRect.sizeDelta = new Vector2(280.0f, 132.0f);
            var panelImage = panelObject.AddComponent<Image>();

            Text patientText = CreateText(
                "Patient ID Text",
                panelObject.transform,
                new Vector2(12.0f, -14.0f));
            Text stateText = CreateText(
                "Interaction State Text",
                panelObject.transform,
                new Vector2(12.0f, -44.0f));
            Text checkedText = CreateText(
                "Checked Status Text",
                panelObject.transform,
                new Vector2(12.0f, -74.0f));
            Button button = CreateButton(panelObject.transform);

            var card = panelObject.AddComponent<PatientStatusCardUI>();
            card.ConfigureForTests(
                panelObject,
                patientText,
                stateText,
                checkedText,
                button,
                panelImage);
            return card;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Vector2 anchoredPosition)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.0f, 1.0f);
            rect.anchorMax = new Vector2(1.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(-24.0f, 24.0f);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private static Button CreateButton(Transform parent)
        {
            var buttonObject = new GameObject("Mark Checked Button");
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 0.0f);
            rect.pivot = new Vector2(0.5f, 0.0f);
            rect.anchoredPosition = new Vector2(0.0f, 12.0f);
            rect.sizeDelta = new Vector2(-24.0f, 28.0f);
            buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();

            Text label = CreateText(
                "Label",
                buttonObject.transform,
                new Vector2(0.0f, -2.0f));
            label.alignment = TextAnchor.MiddleCenter;
            label.text = "Mark Checked";
            return button;
        }
    }
}
