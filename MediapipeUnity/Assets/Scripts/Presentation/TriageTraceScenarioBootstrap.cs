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

        private void Awake()
        {
            ResolveReferences();
            ConnectComponents();
        }

        private void OnValidate()
        {
            raycastDistance = Mathf.Max(0.01f, raycastDistance);
            dwellSeconds = Mathf.Max(0.05f, dwellSeconds);
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

            if (pointerOrigin == null)
            {
                pointerOrigin = transform;
            }

            if (statusCard == null && createStatusCardIfMissing)
            {
                statusCard = CreateStatusCard();
            }
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
