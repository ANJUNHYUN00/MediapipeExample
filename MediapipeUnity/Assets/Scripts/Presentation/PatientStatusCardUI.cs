using UnityEngine;
using UnityEngine.UI;

namespace TriageTrace.Presentation
{
    public enum EmptyPatientCardDisplay
    {
        HideCard,
        ShowWaitingState
    }

    public sealed class PatientStatusCardUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject cardRoot;

        [SerializeField]
        private Text patientIdText;

        [SerializeField]
        private Text interactionStateText;

        [SerializeField]
        private Text checkedStatusText;

        [SerializeField]
        private Button markCheckedButton;

        [SerializeField]
        private Image backgroundPanel;

        [SerializeField]
        [Tooltip("Optional sprite slot for a future Figma-exported card background.")]
        private Sprite backgroundSprite;

        [SerializeField]
        private EmptyPatientCardDisplay emptyPatientDisplay =
            EmptyPatientCardDisplay.HideCard;

        // These are interaction tracking colors, not triage severity colors.
        // Do not use red/yellow/green/black here.
        [SerializeField]
        private Color backgroundColor = new Color(0.05f, 0.12f, 0.16f, 0.72f);

        [SerializeField]
        private Color textColor = Color.white;

        [SerializeField]
        private Color accentColor = Color.cyan;

        [SerializeField]
        private string emptyPatientText = "No patient selected";

        private PatientView _boundPatient;

        public PatientView BoundPatient => _boundPatient;

        public void Bind(PatientView patient)
        {
            if (_boundPatient == patient)
            {
                Refresh();
                return;
            }

            if (_boundPatient != null)
            {
                _boundPatient.StateChanged -= HandlePatientStateChanged;
            }

            _boundPatient = patient;
            if (_boundPatient != null)
            {
                _boundPatient.StateChanged += HandlePatientStateChanged;
            }

            Refresh();
        }

        public void Clear()
        {
            Bind(null);
        }

        public void MarkChecked()
        {
            if (_boundPatient == null)
            {
                Refresh();
                return;
            }

            _boundPatient.MarkChecked();
            Refresh();
        }

        public void ConfigureForTests(
            GameObject root,
            Text patientText,
            Text stateText,
            Text checkedText,
            Button button,
            Image background = null)
        {
            cardRoot = root;
            patientIdText = patientText;
            interactionStateText = stateText;
            checkedStatusText = checkedText;
            markCheckedButton = button;
            backgroundPanel = background;
            WireButton();
            ApplyStyle();
            Refresh();
        }

        private void Awake()
        {
            EnsureReferences();
            WireButton();
            ApplyStyle();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_boundPatient != null)
            {
                _boundPatient.StateChanged -= HandlePatientStateChanged;
            }

            if (markCheckedButton != null)
            {
                markCheckedButton.onClick.RemoveListener(MarkChecked);
            }
        }

        private void OnValidate()
        {
            ApplyStyle();
        }

        private void HandlePatientStateChanged(PatientView patient)
        {
            if (patient == _boundPatient)
            {
                Refresh();
            }
        }

        private void EnsureReferences()
        {
            if (cardRoot == null)
            {
                cardRoot = gameObject;
            }
        }

        private void WireButton()
        {
            if (markCheckedButton == null)
            {
                return;
            }

            markCheckedButton.onClick.RemoveListener(MarkChecked);
            markCheckedButton.onClick.AddListener(MarkChecked);
        }

        private void ApplyStyle()
        {
            if (backgroundPanel != null)
            {
                backgroundPanel.color = backgroundColor;
                if (backgroundSprite != null)
                {
                    backgroundPanel.sprite = backgroundSprite;
                }
            }

            ApplyTextStyle(patientIdText);
            ApplyTextStyle(interactionStateText);
            ApplyTextStyle(checkedStatusText);

            if (markCheckedButton != null)
            {
                var colors = markCheckedButton.colors;
                colors.normalColor = accentColor;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(0.7f, 0.9f, 1.0f);
                markCheckedButton.colors = colors;
            }
        }

        private void ApplyTextStyle(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.color = textColor;
        }

        private void Refresh()
        {
            bool hasPatient = _boundPatient != null;
            if (cardRoot != null)
            {
                cardRoot.SetActive(
                    hasPatient ||
                    emptyPatientDisplay ==
                    EmptyPatientCardDisplay.ShowWaitingState);
            }

            if (!hasPatient)
            {
                SetText(patientIdText, emptyPatientText);
                SetText(interactionStateText, "Interaction: None");
                SetText(checkedStatusText, "Checked: No");
                if (markCheckedButton != null)
                {
                    markCheckedButton.interactable = false;
                }

                return;
            }

            SetText(patientIdText, $"Patient ID: {_boundPatient.DisplayName}");
            SetText(
                interactionStateText,
                $"Interaction: {_boundPatient.InteractionState}");
            SetText(
                checkedStatusText,
                $"Checked: {(_boundPatient.IsChecked ? "Yes" : "No")}");

            if (markCheckedButton != null)
            {
                markCheckedButton.interactable = !_boundPatient.IsChecked;
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
