using System.Threading.Tasks;
using TriageTrace.Models;
using TriageTrace.Networking;
using UnityEngine;

namespace TriageTrace.Presentation
{
    public sealed class PoseDebugPresenterState
    {
        public PosePointerState Latest { get; private set; }
        public float LastReceivedRealtime { get; private set; }
        public bool Connected { get; private set; }

        public void SetConnected(bool connected)
        {
            Connected = connected;
        }

        public void Apply(PosePointerState state, float receivedRealtime)
        {
            Latest = state;
            LastReceivedRealtime = receivedRealtime;
        }

        public bool IsFresh(float realtime, float staleAfterSeconds)
        {
            return Latest != null &&
                   realtime - LastReceivedRealtime <= staleAfterSeconds;
        }

        public bool IsPointerVisible(float realtime, float staleAfterSeconds)
        {
            return Connected &&
                   IsFresh(realtime, staleAfterSeconds) &&
                   Latest.Pointing &&
                   Latest.Pointer != null &&
                   Latest.Tracking == PoseTrackingState.Tracking;
        }
    }

    [DefaultExecutionOrder(-100)]
    public sealed class PoseReceiverBehaviour : MonoBehaviour
    {
        public const string SafetyNotice =
            "Simulation Only / 실제 의료 판단용이 아님";

        [SerializeField]
        private string websocketUri = "ws://127.0.0.1:8765";

        [SerializeField]
        [Min(0.1f)]
        private float reconnectDelaySeconds = 1.0f;

        [SerializeField]
        [Min(0.1f)]
        private float staleAfterSeconds = 0.5f;

        [SerializeField]
        private PosePointerLineRenderer pointerLine;

        private readonly LatestPoseStateQueue _poseQueue =
            new LatestPoseStateQueue();
        private readonly PoseDebugPresenterState _presenterState =
            new PoseDebugPresenterState();

        private PoseWebSocketClient _client;
        private PoseConnectionStatus _connectionStatus =
            PoseConnectionStatus.Disconnected;
        private string _connectionDetail = string.Empty;

        public PoseDebugPresenterState PresenterState => _presenterState;

        private void OnEnable()
        {
            if (pointerLine == null)
            {
                pointerLine = GetComponent<PosePointerLineRenderer>();
            }

            _client = new PoseWebSocketClient(
                websocketUri,
                _poseQueue,
                reconnectDelaySeconds);
            _client.Start();
        }

        private void Update()
        {
            if (_client == null)
            {
                return;
            }

            while (_client.TryDequeueStatus(out PoseConnectionEvent statusEvent))
            {
                _connectionStatus = statusEvent.Status;
                _connectionDetail = statusEvent.Detail;
                bool connected =
                    statusEvent.Status == PoseConnectionStatus.Connected;
                if (connected ||
                    statusEvent.Status == PoseConnectionStatus.Disconnected ||
                    statusEvent.Status == PoseConnectionStatus.Error)
                {
                    _presenterState.SetConnected(connected);
                    pointerLine?.SetConnected(connected);
                }

                if (statusEvent.Status == PoseConnectionStatus.InvalidMessage ||
                    statusEvent.Status == PoseConnectionStatus.Error)
                {
                    Debug.LogWarning(
                        $"Triage Trace receiver: {statusEvent.Status} - " +
                        statusEvent.Detail);
                }
                else
                {
                    Debug.Log(
                        $"Triage Trace receiver: {statusEvent.Status} - " +
                        statusEvent.Detail);
                }
            }

            if (_poseQueue.TryDequeue(out PosePointerState latest))
            {
                float receivedRealtime = Time.realtimeSinceStartup;
                _presenterState.Apply(latest, receivedRealtime);
                pointerLine?.Apply(latest, receivedRealtime);
                string pointerText = latest.Pointer == null
                    ? "null"
                    : $"({latest.Pointer.X:0.000}, {latest.Pointer.Y:0.000})";
                Debug.Log(
                    $"Pose v2 seq={latest.Sequence} " +
                    $"tracking={latest.Tracking} " +
                    $"pointing={latest.Pointing} pointer={pointerText}");
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 520, 150), GUI.skin.box);
            GUILayout.Label("Triage Trace — Simulation Only");
            GUILayout.Label(SafetyNotice);
            GUILayout.Label($"WebSocket: {_connectionStatus} {_connectionDetail}");

            PosePointerState latest = _presenterState.Latest;
            if (latest == null)
            {
                GUILayout.Label("Pose: waiting for pose_pointer v2");
            }
            else
            {
                bool fresh = _presenterState.IsFresh(
                    Time.realtimeSinceStartup,
                    staleAfterSeconds);
                GUILayout.Label(
                    $"Pose: {latest.Tracking} / pointing={latest.Pointing} / " +
                    $"sequence={latest.Sequence} / fresh={fresh}");
            }

            GUILayout.EndArea();
        }

        private void OnDisable()
        {
            _ = StopClientAsync();
        }

        private async Task StopClientAsync()
        {
            PoseWebSocketClient client = _client;
            _client = null;
            if (client == null)
            {
                return;
            }

            await client.StopAsync();
            client.Dispose();
            _presenterState.SetConnected(false);
        }

        private void OnApplicationQuit()
        {
            _client?.Dispose();
        }
    }

    public static class PoseReceiverBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateReceiver()
        {
            if (Application.isBatchMode ||
                Object.FindFirstObjectByType<PoseReceiverBehaviour>() != null)
            {
                return;
            }

            var receiverObject = new GameObject("Triage Trace Pose Receiver");
            Object.DontDestroyOnLoad(receiverObject);
            receiverObject.AddComponent<PosePointerLineRenderer>();
            receiverObject.AddComponent<PointerRaycaster>();
            receiverObject.AddComponent<PoseReceiverBehaviour>();
        }
    }
}
