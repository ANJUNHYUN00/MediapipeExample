"""Short-lived synthetic Pose v2 publisher for Unity bridge verification."""

from __future__ import annotations

import argparse
import time

from mediapipe_rps.config import WebSocketConfig
from mediapipe_rps.websocket_server import PoseWebSocketPublisher
from test_message_builder import tracking_state


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--duration", type=float, default=30.0)
    args = parser.parse_args()

    publisher = PoseWebSocketPublisher(
        WebSocketConfig(port=args.port, publish_hz=30.0)
    )
    deadline = time.monotonic() + args.duration
    with publisher:
        while time.monotonic() < deadline:
            publisher.submit(tracking_state())
            time.sleep(1.0 / 30.0)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
