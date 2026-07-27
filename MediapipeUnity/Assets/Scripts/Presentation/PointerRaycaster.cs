using UnityEngine;

namespace TriageTrace.Presentation
{
    public sealed class PointerRaycaster : MonoBehaviour
    {
        private const float DirectionEpsilon = 0.0001f;

        [SerializeField]
        private PosePointerLineRenderer pointerLine;

        [SerializeField]
        private Transform rayOrigin;

        [SerializeField]
        private LayerMask patientLayerMask = ~0;

        [SerializeField]
        [Min(0.01f)]
        private float maxDistance = 10.0f;

        [SerializeField]
        private QueryTriggerInteraction triggerInteraction =
            QueryTriggerInteraction.Ignore;

        private PatientView _currentPatient;

        public PatientView CurrentPatient => _currentPatient;

        public void ConfigureForTests(
            PosePointerLineRenderer line,
            Transform origin,
            LayerMask layerMask,
            float distance,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore)
        {
            pointerLine = line;
            rayOrigin = origin;
            patientLayerMask = layerMask;
            maxDistance = Mathf.Max(0.01f, distance);
            triggerInteraction = triggers;
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            ClearCurrentPatient();
        }

        private void OnValidate()
        {
            maxDistance = Mathf.Max(0.01f, maxDistance);
        }

        private void Update()
        {
            EnsureReferences();

            if (pointerLine == null || !pointerLine.IsVisible)
            {
                ClearCurrentPatient();
                return;
            }

            Transform origin = rayOrigin == null ? transform : rayOrigin;
            Vector3 direction = pointerLine.CurrentDirection;
            if (!IsFinite(direction) ||
                direction.sqrMagnitude <= DirectionEpsilon)
            {
                ClearCurrentPatient();
                return;
            }

            if (!Physics.Raycast(
                    origin.position,
                    direction.normalized,
                    out RaycastHit hit,
                    maxDistance,
                    patientLayerMask,
                    triggerInteraction))
            {
                ClearCurrentPatient();
                return;
            }

            PatientView patient = hit.collider == null
                ? null
                : hit.collider.GetComponentInParent<PatientView>();
            SetCurrentPatient(patient);
        }

        private void EnsureReferences()
        {
            if (pointerLine == null)
            {
                pointerLine = GetComponent<PosePointerLineRenderer>();
            }

            if (rayOrigin == null)
            {
                rayOrigin = transform;
            }
        }

        private void SetCurrentPatient(PatientView patient)
        {
            if (_currentPatient == patient)
            {
                return;
            }

            ClearCurrentPatient();
            _currentPatient = patient;
            _currentPatient?.HighlightOn();
        }

        private void ClearCurrentPatient()
        {
            if (_currentPatient == null)
            {
                return;
            }

            _currentPatient.HighlightOff();
            _currentPatient = null;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
