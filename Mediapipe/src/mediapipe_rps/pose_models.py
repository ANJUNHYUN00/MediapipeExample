"""MediaPipe-independent Pose tracking result models."""

from __future__ import annotations

import math
from dataclasses import dataclass
from enum import Enum


class TrackingState(str, Enum):
    """Quality state for the MVP right-arm joint set."""

    TRACKING = "TRACKING"
    PARTIAL = "PARTIAL"
    LOST = "LOST"


@dataclass(frozen=True, slots=True)
class Joint:
    """One normalized pose landmark with MediaPipe visibility."""

    x: float
    y: float
    z: float
    visibility: float

    def __post_init__(self) -> None:
        values = (self.x, self.y, self.z, self.visibility)
        if not all(math.isfinite(value) for value in values):
            raise ValueError("joint values must be finite")
        if not 0.0 <= self.visibility <= 1.0:
            raise ValueError("joint visibility must be between 0.0 and 1.0")


@dataclass(frozen=True, slots=True)
class PoseTrackingResult:
    """Right-arm pose result for one input frame."""

    tracking: TrackingState
    frame_index: int
    captured_at_ms: int
    video_timestamp_ms: int
    right_shoulder: Joint | None
    right_elbow: Joint | None
    right_wrist: Joint | None

    def __post_init__(self) -> None:
        if min(self.frame_index, self.captured_at_ms, self.video_timestamp_ms) < 0:
            raise ValueError("frame and timestamp values must be zero or greater")
        joints = (self.right_shoulder, self.right_elbow, self.right_wrist)
        if self.tracking is TrackingState.LOST and any(joint is not None for joint in joints):
            raise ValueError("LOST results must not contain joints")
        if self.tracking is TrackingState.TRACKING and any(
            joint is None for joint in joints
        ):
            raise ValueError("TRACKING results must contain all right-arm joints")

    @property
    def pose_detected(self) -> bool:
        return self.tracking is not TrackingState.LOST

    @property
    def joints(self) -> dict[str, Joint | None]:
        return {
            "rightShoulder": self.right_shoulder,
            "rightElbow": self.right_elbow,
            "rightWrist": self.right_wrist,
        }

    @classmethod
    def lost(
        cls,
        *,
        frame_index: int,
        captured_at_ms: int,
        video_timestamp_ms: int,
    ) -> PoseTrackingResult:
        return cls(
            tracking=TrackingState.LOST,
            frame_index=frame_index,
            captured_at_ms=captured_at_ms,
            video_timestamp_ms=video_timestamp_ms,
            right_shoulder=None,
            right_elbow=None,
            right_wrist=None,
        )
