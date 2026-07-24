using TriageTrace.Models;
using UnityEngine;

namespace TriageTrace.Presentation
{
    public sealed class PosePointerLineRenderer : MonoBehaviour
    {
        private const float DirectionEpsilon = 0.0001f;

        [SerializeField]
        private Transform pointerStart;

        [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField]
        [Min(0.01f)]
        private float lineLength = 2.0f;

        [SerializeField]
        [Min(0.001f)]
        private float lineThickness = 0.025f;

        [SerializeField]
        private Color lineColor = Color.cyan;

        [SerializeField]
        [Min(0.0f)]
        private float interpolationSpeed = 12.0f;

        [SerializeField]
        [Min(0.05f)]
        private float timeoutSeconds = 0.5f;

        [SerializeField]
        private bool invertHorizontal;

        [SerializeField]
        private bool invertVertical;

        private PosePointerState _latest;
        private float _lastReceivedRealtime;
        private Vector3 _smoothedDirection = Vector3.forward;
        private bool _hasSmoothedDirection;
        private bool _connected;

        public bool IsVisible =>
            lineRenderer != null &&
            lineRenderer.enabled &&
            lineRenderer.positionCount == 2;

        public Vector3 CurrentDirection => _smoothedDirection;

        public void SetConnected(bool connected)
        {
            _connected = connected;
            if (!connected)
            {
                HideLine();
            }
        }

        public void Apply(PosePointerState state, float receivedRealtime)
        {
            _latest = state;
            _lastReceivedRealtime = receivedRealtime;

            if (!IsStateRenderable(state))
            {
                HideLine();
            }
        }

        public void ConfigureForTests(
            Transform start,
            LineRenderer renderer,
            float length,
            float thickness,
            Color color,
            float smoothing,
            float timeout,
            bool invertX,
            bool invertY)
        {
            pointerStart = start;
            lineRenderer = renderer;
            lineLength = Mathf.Max(0.01f, length);
            lineThickness = Mathf.Max(0.001f, thickness);
            lineColor = color;
            interpolationSpeed = Mathf.Max(0.0f, smoothing);
            timeoutSeconds = Mathf.Max(0.05f, timeout);
            invertHorizontal = invertX;
            invertVertical = invertY;
            ApplyLineSettings();
        }

        private void Awake()
        {
            EnsureReferences();
            ApplyLineSettings();
            HideLine();
        }

        private void OnValidate()
        {
            lineLength = Mathf.Max(0.01f, lineLength);
            lineThickness = Mathf.Max(0.001f, lineThickness);
            interpolationSpeed = Mathf.Max(0.0f, interpolationSpeed);
            timeoutSeconds = Mathf.Max(0.05f, timeoutSeconds);
            ApplyLineSettings();
        }

        private void Update()
        {
            EnsureReferences();
            ApplyLineSettings();

            if (lineRenderer == null)
            {
                return;
            }

            if (!_connected ||
                Time.realtimeSinceStartup - _lastReceivedRealtime >
                timeoutSeconds ||
                !IsStateRenderable(_latest))
            {
                HideLine();
                return;
            }

            Vector3 targetDirection = ToWorldDirection(_latest.Pointer);
            if (targetDirection.sqrMagnitude <= DirectionEpsilon)
            {
                HideLine();
                return;
            }

            targetDirection.Normalize();
            if (!_hasSmoothedDirection || interpolationSpeed <= 0.0f)
            {
                _smoothedDirection = targetDirection;
                _hasSmoothedDirection = true;
            }
            else
            {
                float t = 1.0f - Mathf.Exp(
                    -interpolationSpeed * Time.deltaTime);
                _smoothedDirection = Vector3.Slerp(
                    _smoothedDirection,
                    targetDirection,
                    t).normalized;
            }

            Vector3 start = ResolveStartPosition();
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, start + _smoothedDirection * lineLength);
        }

        private void EnsureReferences()
        {
            if (pointerStart == null)
            {
                pointerStart = transform;
            }

            if (lineRenderer != null)
            {
                return;
            }

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        private void ApplyLineSettings()
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = lineThickness;
            lineRenderer.endWidth = lineThickness;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }

        private Vector3 ToWorldDirection(PointerDto pointer)
        {
            if (pointer == null || pointerStart == null)
            {
                return Vector3.zero;
            }

            float x = (float)pointer.X;
            float y = (float)pointer.Y;
            if (!IsFinite(x) || !IsFinite(y))
            {
                return Vector3.zero;
            }

            float horizontal = (x - 0.5f) * 2.0f;
            float vertical = (0.5f - y) * 2.0f;
            if (invertHorizontal)
            {
                horizontal = -horizontal;
            }

            if (invertVertical)
            {
                vertical = -vertical;
            }

            Vector3 direction =
                pointerStart.forward +
                pointerStart.right * horizontal +
                pointerStart.up * vertical;
            return direction.sqrMagnitude <= DirectionEpsilon
                ? Vector3.zero
                : direction.normalized;
        }

        private Vector3 ResolveStartPosition()
        {
            return pointerStart == null ? transform.position : pointerStart.position;
        }

        private static bool IsStateRenderable(PosePointerState state)
        {
            return state != null &&
                   state.Tracking == PoseTrackingState.Tracking &&
                   state.Pointing &&
                   state.Pointer != null;
        }

        private void HideLine()
        {
            _hasSmoothedDirection = false;
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
