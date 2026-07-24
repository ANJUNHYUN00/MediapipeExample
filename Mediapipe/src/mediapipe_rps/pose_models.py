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


class PointingReason(str, Enum):
    """Internal diagnostic reason for the current pointing decision."""

    POINTING = "POINTING"
    ACTIVATING = "ACTIVATING"
    TRACKING_REQUIRED = "TRACKING_REQUIRED"
    MISSING_JOINT = "MISSING_JOINT"
    LOW_VISIBILITY = "LOW_VISIBILITY"
    DEGENERATE_ARM = "DEGENERATE_ARM"
    IMBALANCED_SEGMENTS = "IMBALANCED_SEGMENTS"
    BENT_ELBOW = "BENT_ELBOW"


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
class Vector2:
    """Finite two-dimensional direction vector."""

    x: float
    y: float

    def __post_init__(self) -> None:
        if not math.isfinite(self.x) or not math.isfinite(self.y):
            raise ValueError("vector values must be finite")


@dataclass(frozen=True, slots=True)
class NormalizedPointer:
    """Pointer coordinate clamped to the normalized image boundary."""

    x: float
    y: float

    def __post_init__(self) -> None:
        if not math.isfinite(self.x) or not math.isfinite(self.y):
            raise ValueError("pointer values must be finite")
        if not 0.0 <= self.x <= 1.0 or not 0.0 <= self.y <= 1.0:
            raise ValueError("pointer values must be between 0.0 and 1.0")


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


@dataclass(frozen=True, slots=True)
class PosePointerState:
    """Pointing decision derived from one quality-preserving Pose result."""

    pose: PoseTrackingResult
    pointing: bool
    pointer: NormalizedPointer | None
    arm_direction: Vector2 | None
    elbow_angle_degrees: float | None
    reason: PointingReason

    def __post_init__(self) -> None:
        if self.elbow_angle_degrees is not None:
            if not math.isfinite(self.elbow_angle_degrees):
                raise ValueError("elbow angle must be finite")
            if not 0.0 <= self.elbow_angle_degrees <= 180.0:
                raise ValueError("elbow angle must be between 0 and 180")
        if self.pointing:
            if self.pose.tracking is not TrackingState.TRACKING:
                raise ValueError("pointing requires TRACKING input")
            if self.pointer is None:
                raise ValueError("pointing requires a pointer")
            if self.reason is not PointingReason.POINTING:
                raise ValueError("pointing state must use the POINTING reason")
        elif self.pointer is not None:
            raise ValueError("non-pointing state must not contain a pointer")
        elif self.reason is PointingReason.POINTING:
            raise ValueError("POINTING reason requires pointing=true")
        if self.pose.tracking is not TrackingState.TRACKING and self.pointing:
            raise ValueError("PARTIAL and LOST input cannot point")

    @property
    def tracking(self) -> TrackingState:
        return self.pose.tracking

    @property
    def frame_index(self) -> int:
        return self.pose.frame_index

    @property
    def captured_at_ms(self) -> int:
        return self.pose.captured_at_ms

    @property
    def right_shoulder(self) -> Joint | None:
        return self.pose.right_shoulder

    @property
    def right_elbow(self) -> Joint | None:
        return self.pose.right_elbow

    @property
    def right_wrist(self) -> Joint | None:
        return self.pose.right_wrist

    @property
    def joints(self) -> dict[str, Joint | None]:
        return self.pose.joints
