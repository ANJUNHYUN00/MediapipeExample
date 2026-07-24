from __future__ import annotations

import json
import socket

import pytest
from websockets.sync.client import connect

from mediapipe_rps.config import WebSocketConfig
from mediapipe_rps.websocket_server import PoseWebSocketPublisher, PublisherError
from test_message_builder import partial_state, tracking_state


def test_publisher_sends_latest_state_and_sequences_reconnections() -> None:
    publisher = PoseWebSocketPublisher(
        WebSocketConfig(port=0, publish_hz=30.0)
    )

    with publisher:
        publisher.submit(tracking_state())
        with connect(publisher.uri, open_timeout=3) as websocket:
            first = json.loads(websocket.recv(timeout=3))

        publisher.submit(partial_state())
        with connect(publisher.uri, open_timeout=3) as websocket:
            second = json.loads(websocket.recv(timeout=3))

    assert first["type"] == "pose_pointer"
    assert first["version"] == 2
    assert first["tracking"] == "TRACKING"
    assert first["sequence"] == 0
    assert second["tracking"] == "PARTIAL"
    assert second["sequence"] > first["sequence"]
    assert not publisher.is_running


def test_latest_state_slot_drops_older_unsent_values() -> None:
    publisher = PoseWebSocketPublisher(
        WebSocketConfig(port=0, publish_hz=10.0)
    )

    with publisher:
        publisher.submit(tracking_state())
        publisher.submit(partial_state())
        with connect(publisher.uri, open_timeout=3) as websocket:
            received = json.loads(websocket.recv(timeout=3))

    assert received["tracking"] == "PARTIAL"


def test_start_reports_port_conflict() -> None:
    blocker = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    blocker.bind(("127.0.0.1", 0))
    blocker.listen(1)
    port = blocker.getsockname()[1]
    publisher = PoseWebSocketPublisher(WebSocketConfig(port=port))

    try:
        with pytest.raises(PublisherError, match="failed to start"):
            publisher.start()
    finally:
        blocker.close()
        publisher.close()


def test_submit_before_start_is_rejected() -> None:
    publisher = PoseWebSocketPublisher(WebSocketConfig(port=0))

    with pytest.raises(PublisherError, match="must be running"):
        publisher.submit(tracking_state())
