using UnityEngine;

namespace TriageTrace.Presentation
{
    public sealed class PatientView : MonoBehaviour
    {
        [SerializeField]
        private Renderer[] targetRenderers;

        [SerializeField]
        private PatientInteractionState interactionState =
            PatientInteractionState.Unseen;

        // Interaction colors must stay separate from triage severity colors
        // such as red/yellow/green/black. These states track confirmation flow
        // only; they are not medical classification.
        [SerializeField]
        private Color unseenColor = Color.white;

        [SerializeField]
        private Color highlightedColor = Color.cyan;

        [SerializeField]
        private Color inProgressColor = new Color(0.2f, 0.45f, 1.0f);

        [SerializeField]
        private Color checkedColor = Color.white;

        [SerializeField]
        private string primaryColorProperty = "_BaseColor";

        [SerializeField]
        private string fallbackColorProperty = "_Color";

        private Color[] _originalColors;
        private bool[] _hasColorProperty;

        public PatientInteractionState InteractionState => interactionState;
        public bool IsHighlighted =>
            interactionState == PatientInteractionState.Highlighted;
        public bool IsSelected =>
            interactionState == PatientInteractionState.InProgress;
        public bool IsChecked =>
            interactionState == PatientInteractionState.Checked;

        public void ConfigureForTests(
            Renderer[] renderers,
            Color color,
            string primaryProperty = "_BaseColor",
            string fallbackProperty = "_Color",
            Color? selectionColor = null,
            Color? baseStateColor = null,
            Color? checkedStateColor = null)
        {
            targetRenderers = renderers;
            highlightedColor = color;
            inProgressColor = selectionColor ?? Color.cyan;
            unseenColor = baseStateColor ?? Color.white;
            checkedColor = checkedStateColor ?? Color.blue;
            primaryColorProperty = primaryProperty;
            fallbackColorProperty = fallbackProperty;
            CacheOriginalColors();
            ApplyVisualState();
        }

        public void HighlightOn()
        {
            if (interactionState == PatientInteractionState.Unseen)
            {
                SetState(PatientInteractionState.Highlighted);
            }
        }

        public void HighlightOff()
        {
            if (interactionState == PatientInteractionState.Highlighted)
            {
                SetState(PatientInteractionState.Unseen);
            }
        }

        public void SelectOn()
        {
            SetSelected(true);
        }

        public void SelectOff()
        {
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            SetState(
                selected
                    ? PatientInteractionState.InProgress
                    : PatientInteractionState.Unseen);
        }

        public void MarkChecked()
        {
            SetState(PatientInteractionState.Checked, force: true);
        }

        public void SetState(PatientInteractionState state)
        {
            SetState(state, force: false);
        }

        private void SetState(PatientInteractionState state, bool force)
        {
            if (!force &&
                interactionState == PatientInteractionState.Checked &&
                state != PatientInteractionState.Checked)
            {
                return;
            }

            EnsureReferences();
            EnsureColorCache();

            interactionState = state;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_originalColors == null)
            {
                return;
            }

            switch (interactionState)
            {
                case PatientInteractionState.Highlighted:
                    ApplyColor(highlightedColor);
                    return;
                case PatientInteractionState.InProgress:
                    ApplyColor(inProgressColor);
                    return;
                case PatientInteractionState.Checked:
                    ApplyColor(checkedColor);
                    return;
                default:
                    ApplyColor(unseenColor);
                    return;
            }
        }

        private void Awake()
        {
            EnsureReferences();
            CacheOriginalColors();
            ApplyVisualState();
        }

        private void OnValidate()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void EnsureReferences()
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                return;
            }

            targetRenderers = GetComponentsInChildren<Renderer>();
        }

        private void EnsureColorCache()
        {
            if (_originalColors != null &&
                targetRenderers != null &&
                _originalColors.Length == targetRenderers.Length)
            {
                return;
            }

            CacheOriginalColors();
        }

        private void CacheOriginalColors()
        {
            if (targetRenderers == null)
            {
                _originalColors = null;
                _hasColorProperty = null;
                return;
            }

            _originalColors = new Color[targetRenderers.Length];
            _hasColorProperty = new bool[targetRenderers.Length];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer target = targetRenderers[i];
                if (target == null || target.material == null)
                {
                    continue;
                }

                string property = ResolveColorProperty(target.material);
                if (string.IsNullOrEmpty(property))
                {
                    continue;
                }

                _hasColorProperty[i] = true;
                _originalColors[i] = target.material.GetColor(property);
            }
        }

        private void ApplyColor(Color color)
        {
            if (targetRenderers == null || _hasColorProperty == null)
            {
                return;
            }

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer target = targetRenderers[i];
                if (target == null ||
                    !_hasColorProperty[i] ||
                    target.material == null)
                {
                    continue;
                }

                string property = ResolveColorProperty(target.material);
                if (string.IsNullOrEmpty(property))
                {
                    continue;
                }

                target.material.SetColor(property, color);
            }
        }

        private string ResolveColorProperty(Material material)
        {
            if (material == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(primaryColorProperty) &&
                material.HasProperty(primaryColorProperty))
            {
                return primaryColorProperty;
            }

            return !string.IsNullOrWhiteSpace(fallbackColorProperty) &&
                   material.HasProperty(fallbackColorProperty)
                ? fallbackColorProperty
                : string.Empty;
        }
    }
}
