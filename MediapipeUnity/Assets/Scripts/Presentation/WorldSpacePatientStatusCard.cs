using UnityEngine;

namespace TriageTrace.Presentation
{
    public enum WorldSpaceCardDisplayMode
    {
        SelectedOnly,
        HoverOrSelected
    }

    /// <summary>
    /// Positions a PatientStatusCardUI relative to the active PatientView and
    /// keeps the World Space canvas facing the presentation camera.
    /// </summary>
    public sealed class WorldSpacePatientStatusCard : MonoBehaviour
    {
        [SerializeField]
        private PatientStatusCardUI statusCard;

        [SerializeField]
        private PointerRaycaster pointerRaycaster;

        [SerializeField]
        private PatientDwellSelector dwellSelector;

        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private WorldSpaceCardDisplayMode displayMode =
            WorldSpaceCardDisplayMode.HoverOrSelected;

        [SerializeField]
        [Tooltip("Default places the card above a Patient whose origin is near the floor.")]
        private Vector3 patientOffset = new Vector3(0.0f, 1.3f, 0.0f);

        [SerializeField]
        [Tooltip("Use the Patient/Card Anchor axes instead of world axes for the offset.")]
        private bool useLocalOffset;

        [SerializeField]
        private bool faceCamera = true;

        [SerializeField]
        [Tooltip("Keeps the card vertical while it turns toward the camera.")]
        private bool keepUpright = true;

        [SerializeField]
        private Canvas worldSpaceCanvas;

        private PatientView _visiblePatient;

        public PatientView VisiblePatient => _visiblePatient;
        public PatientStatusCardUI StatusCard => statusCard;

        public void Configure(
            PatientStatusCardUI card,
            PointerRaycaster raycaster,
            PatientDwellSelector selector,
            Camera camera,
            WorldSpaceCardDisplayMode mode,
            Vector3 offset,
            bool localOffset = false)
        {
            statusCard = card;
            pointerRaycaster = raycaster;
            dwellSelector = selector;
            targetCamera = camera;
            displayMode = mode;
            patientOffset = offset;
            useLocalOffset = localOffset;
            EnsureReferences();
            RefreshNow();
        }

        public void RefreshNow()
        {
            EnsureReferences();
            PatientView target = ResolveTargetPatient();
            if (_visiblePatient != target)
            {
                _visiblePatient = target;
                statusCard?.Bind(_visiblePatient);
            }
            else if (_visiblePatient != null &&
                     statusCard != null &&
                     statusCard.BoundPatient != _visiblePatient)
            {
                statusCard.Bind(_visiblePatient);
            }

            if (_visiblePatient == null)
            {
                statusCard?.Clear();
                return;
            }

            Transform anchor = _visiblePatient.StatusCardAnchor;
            if (!_visiblePatient.HasStatusCardAnchor &&
                _visiblePatient.TryGetVisualBounds(out Bounds visualBounds))
            {
                transform.position = new Vector3(
                    visualBounds.center.x + patientOffset.x,
                    visualBounds.max.y + 0.35f,
                    visualBounds.center.z + patientOffset.z);
            }
            else
            {
                transform.position = useLocalOffset
                    ? anchor.TransformPoint(patientOffset)
                    : anchor.position + patientOffset;
            }
            FaceCamera();
        }

        private void Awake()
        {
            EnsureReferences();
            RefreshNow();
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void OnDisable()
        {
            _visiblePatient = null;
            statusCard?.Clear();
        }

        private void EnsureReferences()
        {
            if (statusCard == null)
            {
                statusCard = GetComponentInChildren<PatientStatusCardUI>(true);
            }

            if (worldSpaceCanvas == null)
            {
                worldSpaceCanvas = GetComponent<Canvas>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (worldSpaceCanvas != null &&
                worldSpaceCanvas.renderMode == RenderMode.WorldSpace)
            {
                worldSpaceCanvas.worldCamera = targetCamera;
            }
        }

        private PatientView ResolveTargetPatient()
        {
            PatientView selected = dwellSelector == null
                ? null
                : dwellSelector.SelectedPatient;
            if (selected != null)
            {
                return selected;
            }

            return displayMode == WorldSpaceCardDisplayMode.HoverOrSelected &&
                   pointerRaycaster != null
                ? pointerRaycaster.CurrentPatient
                : null;
        }

        private void FaceCamera()
        {
            if (!faceCamera || targetCamera == null)
            {
                return;
            }

            Vector3 forward = targetCamera.transform.position - transform.position;
            Vector3 up = keepUpright ? Vector3.up : targetCamera.transform.up;
            if (keepUpright)
            {
                forward = Vector3.ProjectOnPlane(forward, Vector3.up);
                if (forward.sqrMagnitude < 0.0001f)
                {
                    forward = transform.forward;
                }
            }

            transform.rotation = Quaternion.LookRotation(
                forward.normalized,
                up);
        }
    }
}
