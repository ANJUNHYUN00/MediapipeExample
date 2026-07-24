using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TriageTrace.Models;

namespace TriageTrace.Networking
{
    public enum PoseConnectionStatus
    {
        Connecting,
        Connected,
        Disconnected,
        Reconnecting,
        InvalidMessage,
        Error
    }

    public sealed class PoseConnectionEvent
    {
        public PoseConnectionEvent(PoseConnectionStatus status, string detail)
        {
            Status = status;
            Detail = detail ?? string.Empty;
        }

        public PoseConnectionStatus Status { get; }
        public string Detail { get; }
    }

    public sealed class PoseWebSocketClient : IDisposable
    {
        private readonly Uri _uri;
        private readonly TimeSpan _reconnectDelay;
        private readonly int _maxMessageBytes;
        private readonly LatestPoseStateQueue _poseQueue;
        private readonly ConcurrentQueue<PoseConnectionEvent> _statusQueue =
            new ConcurrentQueue<PoseConnectionEvent>();

        private CancellationTokenSource _cancellation;
        private Task _runTask;
        private ClientWebSocket _activeSocket;
        private volatile bool _isConnected;

        public PoseWebSocketClient(
            string uri,
            LatestPoseStateQueue poseQueue,
            double reconnectDelaySeconds = 1.0,
            int maxMessageBytes = 64 * 1024)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out _uri) ||
                (_uri.Scheme != "ws" && _uri.Scheme != "wss"))
            {
                throw new ArgumentException("A valid ws:// or wss:// URI is required.", nameof(uri));
            }

            if (poseQueue == null)
            {
                throw new ArgumentNullException(nameof(poseQueue));
            }

            if (reconnectDelaySeconds <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(reconnectDelaySeconds));
            }

            if (maxMessageBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMessageBytes));
            }

            _poseQueue = poseQueue;
            _reconnectDelay = TimeSpan.FromSeconds(reconnectDelaySeconds);
            _maxMessageBytes = maxMessageBytes;
        }

        public bool IsConnected => _isConnected;

        public void Start()
        {
            if (_runTask != null && !_runTask.IsCompleted)
            {
                return;
            }

            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            _runTask = RunReconnectLoopAsync(_cancellation.Token);
        }

        public async Task StopAsync()
        {
            CancellationTokenSource cancellation = _cancellation;
            Task runTask = _runTask;
            if (cancellation == null)
            {
                return;
            }

            cancellation.Cancel();
            _activeSocket?.Abort();
            if (runTask != null)
            {
                try
                {
                    await runTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            cancellation.Dispose();
            _cancellation = null;
            _runTask = null;
            _isConnected = false;
        }

        public bool TryDequeueStatus(out PoseConnectionEvent connectionEvent)
        {
            return _statusQueue.TryDequeue(out connectionEvent);
        }

        private async Task RunReconnectLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                EnqueueStatus(PoseConnectionStatus.Connecting, _uri.ToString());
                using (var socket = new ClientWebSocket())
                {
                    _activeSocket = socket;
                    socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                    try
                    {
                        await socket.ConnectAsync(_uri, token).ConfigureAwait(false);
                        _poseQueue.Reset();
                        _isConnected = true;
                        EnqueueStatus(PoseConnectionStatus.Connected, _uri.ToString());
                        await ReceiveLoopAsync(socket, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception) when (
                        exception is WebSocketException ||
                        exception is IOException ||
                        exception is InvalidOperationException)
                    {
                        EnqueueStatus(PoseConnectionStatus.Error, exception.Message);
                    }
                    finally
                    {
                        _isConnected = false;
                        _activeSocket = null;
                        EnqueueStatus(PoseConnectionStatus.Disconnected, _uri.ToString());
                    }
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }

                EnqueueStatus(
                    PoseConnectionStatus.Reconnecting,
                    _reconnectDelay.TotalSeconds.ToString("0.0"));
                await Task.Delay(_reconnectDelay, token).ConfigureAwait(false);
            }
        }

        private async Task ReceiveLoopAsync(
            ClientWebSocket socket,
            CancellationToken token)
        {
            var buffer = new byte[8 * 1024];
            while (!token.IsCancellationRequested &&
                   socket.State == WebSocketState.Open)
            {
                using (var message = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(
                                new ArraySegment<byte>(buffer),
                                token)
                            .ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            if (socket.State == WebSocketState.CloseReceived)
                            {
                                await socket.CloseOutputAsync(
                                        WebSocketCloseStatus.NormalClosure,
                                        "Triage Trace receiver closing",
                                        token)
                                    .ConfigureAwait(false);
                            }

                            return;
                        }

                        if (result.MessageType != WebSocketMessageType.Text)
                        {
                            EnqueueStatus(
                                PoseConnectionStatus.InvalidMessage,
                                "Only UTF-8 JSON text messages are supported.");
                            return;
                        }

                        message.Write(buffer, 0, result.Count);
                        if (message.Length > _maxMessageBytes)
                        {
                            EnqueueStatus(
                                PoseConnectionStatus.InvalidMessage,
                                "Message exceeded the configured size limit.");
                            return;
                        }
                    }
                    while (!result.EndOfMessage);

                    string json = Encoding.UTF8.GetString(message.ToArray());
                    HandleMessage(json);
                }
            }
        }

        private void HandleMessage(string json)
        {
            if (!PoseMessageParser.TryParse(
                    json,
                    out MessageKind kind,
                    out PosePointerState poseState,
                    out string error))
            {
                EnqueueStatus(PoseConnectionStatus.InvalidMessage, error);
                return;
            }

            if (kind == MessageKind.PosePointerV2)
            {
                if (!_poseQueue.TryEnqueue(poseState))
                {
                    EnqueueStatus(
                        PoseConnectionStatus.InvalidMessage,
                        "Ignored duplicate or out-of-order pose sequence.");
                }

                return;
            }

            EnqueueStatus(
                PoseConnectionStatus.InvalidMessage,
                "Legacy gesture v1 message ignored by the Pose receiver.");
        }

        private void EnqueueStatus(PoseConnectionStatus status, string detail)
        {
            _statusQueue.Enqueue(new PoseConnectionEvent(status, detail));
        }

        public void Dispose()
        {
            _cancellation?.Cancel();
            _activeSocket?.Abort();
        }
    }
}
