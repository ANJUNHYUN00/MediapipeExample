using UnityEngine;

namespace TriageTrace.Presentation
{
    public sealed class PatientView : MonoBehaviour
    {
        [SerializeField]
        private Renderer[] targetRenderers;

        [SerializeField]
        private Color highlightColor = Color.yellow;

        [SerializeField]
        private Color selectedColor = Color.cyan;

        [SerializeField]
        private string primaryColorProperty = "_BaseColor";

        [SerializeField]
        private string fallbackColorProperty = "_Color";

        private Color[] _originalColors;
        private bool[] _hasColorProperty;
        private bool _highlighted;
        private bool _selected;

        public bool IsHighlighted => _highlighted;
        public bool IsSelected => _selected;

        public void ConfigureForTests(
            Renderer[] renderers,
            Color color,
            string primaryProperty = "_BaseColor",
            string fallbackProperty = "_Color",
            Color? selectionColor = null)
        {
            targetRenderers = renderers;
            highlightColor = color;
            selectedColor = selectionColor ?? Color.cyan;
            primaryColorProperty = primaryProperty;
            fallbackColorProperty = fallbackProperty;
            CacheOriginalColors();
        }

        public void HighlightOn()
        {
            EnsureReferences();
            EnsureColorCache();

            _highlighted = true;
            ApplyVisualState();
        }

        public void HighlightOff()
        {
            EnsureReferences();
            EnsureColorCache();

            _highlighted = false;
            ApplyVisualState();
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
            EnsureReferences();
            EnsureColorCache();

            _selected = selected;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_originalColors == null)
            {
                return;
            }

            if (_selected)
            {
                ApplyColor(selectedColor);
                return;
            }

            if (_highlighted)
            {
                ApplyColor(highlightColor);
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

                target.material.SetColor(property, _originalColors[i]);
            }
        }

        private void Awake()
        {
            EnsureReferences();
            CacheOriginalColors();
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
