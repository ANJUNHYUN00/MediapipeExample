"""MediaPipe Pose Landmarker wrapper for Triage Trace MVP input."""

from __future__ import annotations

import logging
import math
import time
from collections.abc import Callable
from typing import Any

import cv2
import mediapipe as mp
import numpy as np

from .config import PoseConfig
from .models import CapturedFrame
from .pose_models import Joint, PoseTrackingResult, TrackingState

LOGGER = logging.getLogger(__name__)

POSE_LANDMARK_COUNT = 33
RIGHT_SHOULDER_INDEX = 12
RIGHT_ELBOW_INDEX = 14
RIGHT_WRIST_INDEX = 16

LandmarkerFactory = Callable[[PoseConfig], Any]
ClockMilliseconds = Callable[[], int]


class PoseTrackerError(RuntimeError):
    """Raised when Pose Landmarker setup or inference fails."""


def _default_landmarker_factory(config: PoseConfig) -> Any:
    options = mp.tasks.vision.PoseLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(
            model_asset_path=str(config.model_path.resolve())
        ),
        running_mode=mp.tasks.vision.RunningMode.VIDEO,
        num_poses=config.num_poses,
        min_pose_detection_confidence=config.min_pose_detection_confidence,
        min_pose_presence_confidence=config.min_pose_presence_confidence,
        min_tracking_confidence=config.min_tracking_confidence,
        output_segmentation_masks=False,
    )
    return mp.tasks.vision.PoseLandmarker.create_from_options(options)


def _monotonic_ms() -> int:
    return time.monotonic_ns() // 1_000_000


class PoseTracker:
    """Run synchronous Pose Landmarker inference in VIDEO mode."""

    def __init__(
        self,
        config: PoseConfig,
        *,
        landmarker_factory: LandmarkerFactory = _default_landmarker_factory,
        clock_ms: ClockMilliseconds = _monotonic_ms,
    ) -> None:
        self._config = config
        self._landmarker_factory = landmarker_factory
        self._clock_ms = clock_ms
        self._landmarker: Any | None = None
        self._last_video_timestamp_ms = -1

    @property
    def is_open(self) -> bool:
        return self._landmarker is not None

    def open(self) -> None:
        if self.is_open:
            return
        if not self._config.model_path.is_file():
            raise PoseTrackerError(
                f"Pose Landmarker model not found: {self._config.model_path}"
            )
        try:
            self._landmarker = self._landmarker_factory(self._config)
        except Exception as exc:
            raise PoseTrackerError(f"failed to load Pose Landmarker model: {exc}") from exc
        self._last_video_timestamp_ms = -1
        LOGGER.info("Pose Landmarker loaded: %s", self._config.model_path)

    def process(self, frame: CapturedFrame) -> PoseTrackingResult:
        if self._landmarker is None:
            raise PoseTrackerError("PoseTracker must be opened before processing")

        video_timestamp_ms = max(
            self._clock_ms(),
            self._last_video_timestamp_ms + 1,
        )
        self._last_video_timestamp_ms = video_timestamp_ms

        rgb_image = cv2.cvtColor(frame.image_bgr, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(
            image_format=mp.ImageFormat.SRGB,
            data=np.ascontiguousarray(rgb_image),
        )
        try:
            raw_result = self._landmarker.detect_for_video(
                mp_image,
                video_timestamp_ms,
            )
        except Exception as exc:
            raise PoseTrackerError(f"Pose Landmarker inference failed: {exc}") from exc

        return self._convert_result(raw_result, frame, video_timestamp_ms)

    def _convert_result(
        self,
        raw_result: Any,
        frame: CapturedFrame,
        video_timestamp_ms: int,
    ) -> PoseTrackingResult:
        poses = getattr(raw_result, "pose_landmarks", None)
        if not poses:
            return PoseTrackingResult.lost(
                frame_index=frame.frame_index,
                captured_at_ms=frame.captured_at_ms,
                video_timestamp_ms=video_timestamp_ms,
            )

        landmarks = poses[0]
        if len(landmarks) != POSE_LANDMARK_COUNT:
            LOGGER.warning(
                "Ignoring malformed pose with %d landmarks; expected %d",
                len(landmarks),
                POSE_LANDMARK_COUNT,
            )
            return PoseTrackingResult.lost(
                frame_index=frame.frame_index,
                captured_at_ms=frame.captured_at_ms,
                video_timestamp_ms=video_timestamp_ms,
            )

        right_shoulder = self._joint_or_none(landmarks[RIGHT_SHOULDER_INDEX])
        right_elbow = self._joint_or_none(landmarks[RIGHT_ELBOW_INDEX])
        right_wrist = self._joint_or_none(landmarks[RIGHT_WRIST_INDEX])
        joints = (right_shoulder, right_elbow, right_wrist)

        is_tracking = all(
            joint is not None
            and joint.visibility >= self._config.min_right_arm_visibility
            for joint in joints
        )
        tracking = TrackingState.TRACKING if is_tracking else TrackingState.PARTIAL

        return PoseTrackingResult(
            tracking=tracking,
            frame_index=frame.frame_index,
            captured_at_ms=frame.captured_at_ms,
            video_timestamp_ms=video_timestamp_ms,
            right_shoulder=right_shoulder,
            right_elbow=right_elbow,
            right_wrist=right_wrist,
        )

    @staticmethod
    def _joint_or_none(landmark: Any) -> Joint | None:
        try:
            values = (
                float(landmark.x),
                float(landmark.y),
                float(landmark.z),
                float(landmark.visibility),
            )
        except (AttributeError, TypeError, ValueError):
            return None

        if not all(math.isfinite(value) for value in values):
            return None
        if not 0.0 <= values[3] <= 1.0:
            return None
        return Joint(
            x=values[0],
            y=values[1],
            z=values[2],
            visibility=values[3],
        )

    def close(self) -> None:
        if self._landmarker is not None:
            self._landmarker.close()
            self._landmarker = None
            LOGGER.info("Pose Landmarker closed")

    def __enter__(self) -> PoseTracker:
        self.open()
        return self

    def __exit__(self, exc_type: object, exc_value: object, traceback: object) -> None:
        self.close()
