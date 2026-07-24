"""OpenCV camera capture lifecycle."""

from __future__ import annotations

import logging
import time
from collections.abc import Callable
from typing import Any

import cv2

from .config import CameraConfig
from .models import CapturedFrame

LOGGER = logging.getLogger(__name__)


class CameraError(RuntimeError):
    """Base exception for camera lifecycle failures."""


class CameraOpenError(CameraError):
    """Raised when the configured camera cannot be opened."""


class CameraReadError(CameraError):
    """Raised after repeated camera frame read failures."""


class Camera:
    """Own an OpenCV ``VideoCapture`` and release it deterministically."""

    def __init__(
        self,
        config: CameraConfig,
        capture_factory: Callable[[int], Any] = cv2.VideoCapture,
    ) -> None:
        self._config = config
        self._capture_factory = capture_factory
        self._capture: Any | None = None
        self._next_frame_index = 0

    @property
    def is_open(self) -> bool:
        return self._capture is not None and bool(self._capture.isOpened())

    def open(self) -> None:
        if self.is_open:
            return

        capture = self._capture_factory(self._config.device_index)
        if capture is None or not bool(capture.isOpened()):
            if capture is not None:
                capture.release()
            raise CameraOpenError(
                f"camera device {self._config.device_index} could not be opened"
            )

        if self._config.width is not None:
            capture.set(cv2.CAP_PROP_FRAME_WIDTH, self._config.width)
        if self._config.height is not None:
            capture.set(cv2.CAP_PROP_FRAME_HEIGHT, self._config.height)

        self._capture = capture
        self._next_frame_index = 0
        LOGGER.info(
            "Camera %d opened at %dx%d",
            self._config.device_index,
            int(capture.get(cv2.CAP_PROP_FRAME_WIDTH)),
            int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT)),
        )

    def read(self) -> CapturedFrame:
        if not self.is_open:
            raise CameraReadError("camera must be opened before reading")

        for attempt in range(1, self._config.max_read_attempts + 1):
            success, image_bgr = self._capture.read()
            if success and image_bgr is not None and image_bgr.size > 0:
                frame = CapturedFrame(
                    image_bgr=image_bgr,
                    frame_index=self._next_frame_index,
                    captured_at_ms=time.time_ns() // 1_000_000,
                )
                self._next_frame_index += 1
                return frame
            LOGGER.warning(
                "Camera frame read failed (%d/%d)",
                attempt,
                self._config.max_read_attempts,
            )

        raise CameraReadError(
            f"camera frame read failed {self._config.max_read_attempts} times"
        )

    def close(self) -> None:
        if self._capture is not None:
            self._capture.release()
            self._capture = None
            LOGGER.info("Camera closed")

    def __enter__(self) -> Camera:
        self.open()
        return self

    def __exit__(self, exc_type: object, exc_value: object, traceback: object) -> None:
        self.close()
