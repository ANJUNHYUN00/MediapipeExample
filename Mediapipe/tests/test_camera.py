from __future__ import annotations

import cv2
import numpy as np
import pytest

from mediapipe_rps.camera import Camera, CameraOpenError, CameraReadError
from mediapipe_rps.config import CameraConfig


class FakeCapture:
    def __init__(
        self,
        *,
        opened: bool = True,
        reads: list[tuple[bool, np.ndarray | None]] | None = None,
    ) -> None:
        self.opened = opened
        self.reads = list(reads or [])
        self.released = False
        self.properties: dict[int, float] = {}

    def isOpened(self) -> bool:
        return self.opened and not self.released

    def set(self, property_id: int, value: float) -> bool:
        self.properties[property_id] = value
        return True

    def get(self, property_id: int) -> float:
        return self.properties.get(property_id, 0.0)

    def read(self) -> tuple[bool, np.ndarray | None]:
        if self.reads:
            return self.reads.pop(0)
        return False, None

    def release(self) -> None:
        self.released = True


def test_camera_reads_frames_in_order_and_applies_requested_size() -> None:
    image = np.zeros((3, 4, 3), dtype=np.uint8)
    capture = FakeCapture(reads=[(True, image), (True, image.copy())])
    camera = Camera(
        CameraConfig(width=1280, height=720),
        capture_factory=lambda _: capture,
    )

    with camera:
        first = camera.read()
        second = camera.read()

    assert first.frame_index == 0
    assert second.frame_index == 1
    assert first.captured_at_ms > 0
    assert first.image_bgr is image
    assert capture.properties[cv2.CAP_PROP_FRAME_WIDTH] == 1280
    assert capture.properties[cv2.CAP_PROP_FRAME_HEIGHT] == 720
    assert capture.released


def test_camera_releases_failed_capture() -> None:
    capture = FakeCapture(opened=False)
    camera = Camera(CameraConfig(), capture_factory=lambda _: capture)

    with pytest.raises(CameraOpenError, match="could not be opened"):
        camera.open()

    assert capture.released


def test_camera_retries_then_raises_on_read_failure() -> None:
    capture = FakeCapture(reads=[(False, None), (False, None)])
    camera = Camera(
        CameraConfig(max_read_attempts=2),
        capture_factory=lambda _: capture,
    )
    camera.open()

    with pytest.raises(CameraReadError, match="failed 2 times"):
        camera.read()

    camera.close()
    assert capture.released


def test_camera_context_releases_on_body_exception() -> None:
    capture = FakeCapture()
    camera = Camera(CameraConfig(), capture_factory=lambda _: capture)

    with pytest.raises(RuntimeError, match="body failed"):
        with camera:
            raise RuntimeError("body failed")

    assert capture.released
