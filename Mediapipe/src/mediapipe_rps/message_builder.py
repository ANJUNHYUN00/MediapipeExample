"""Versioned JSON message construction boundary."""

from __future__ import annotations

import json
import math
from dataclasses import dataclass
from typing import Any

from .pose_models import Joint, PosePointerState, TrackingState

POSE_POINTER_TYPE = "pose_pointer"
POSE_POINTER_VERSION = 2
MAX_SEQUENCE = 2**63 - 1


class MessageValidationError(ValueError):
    """Raised when an internal state violates the public v2 contract."""


@dataclass(frozen=True, slots=True)
class PosePointerMessageV2:
    """Serializable pose_pointer version 2 message."""

    timestamp: int
    sequence: int
    tracking: str
    pointing: bool
    pointer: dict[str, float] | None
    joints: dict[str, dict[str, float] | None]
    visibility: dict[str, float]

    def to_dict(self) -> dict[str, Any]:
        return {
            "type": POSE_POINTER_TYPE,
            "version": POSE_POINTER_VERSION,
            "timestamp": self.timestamp,
            "sequence": self.sequence,
            "tracking": self.tracking,
            "pointing": self.pointing,
            "pointer": self.pointer,
            "joints": self.joints,
            "visibility": self.visibility,
        }

    def to_json(self) -> str:
        return json.dumps(
            self.to_dict(),
            ensure_ascii=False,
            allow_nan=False,
            separators=(",", ":"),
        )


class PoseMessageBuilder:
    """Validate PosePointerState and build explicit camelCase v2 payloads."""

    def __init__(self, *, partial_visibility_threshold: float = 0.5) -> None:
        if not 0.0 <= partial_visibility_threshold <= 1.0:
            raise ValueError(
                "partial_visibility_threshold must be between 0.0 and 1.0"
            )
        self._partial_visibility_threshold = partial_visibility_threshold

    def build(
        self,
        state: PosePointerState,
        *,
        sequence: int,
    ) -> PosePointerMessageV2:
        if not 0 <= sequence <= MAX_SEQUENCE:
            raise MessageValidationError("sequence is outside signed 64-bit range")
        if state.captured_at_ms < 0:
            raise MessageValidationError("timestamp must be zero or greater")

        joints = {
            "rightShoulder": self._joint_payload(state.right_shoulder),
            "rightElbow": self._joint_payload(state.right_elbow),
            "rightWrist": self._joint_payload(state.right_wrist),
        }
        visibility = {
            "rightShoulder": self._visibility(state.right_shoulder),
            "rightElbow": self._visibility(state.right_elbow),
            "rightWrist": self._visibility(state.right_wrist),
        }
        self._validate_state(state, joints, visibility)

        pointer = None
        if state.pointer is not None:
            pointer = {
                "x": state.pointer.x,
                "y": state.pointer.y,
            }

        return PosePointerMessageV2(
            timestamp=state.captured_at_ms,
            sequence=sequence,
            tracking=state.tracking.value,
            pointing=state.pointing,
            pointer=pointer,
            joints=joints,
            visibility=visibility,
        )

    def _validate_state(
        self,
        state: PosePointerState,
        joints: dict[str, dict[str, float] | None],
        visibility: dict[str, float],
    ) -> None:
        missing_count = sum(joint is None for joint in joints.values())
        if state.tracking is TrackingState.TRACKING and missing_count:
            raise MessageValidationError("TRACKING requires all three joints")
        if state.tracking is TrackingState.PARTIAL:
            has_low_visibility = any(
                value < self._partial_visibility_threshold
                for value in visibility.values()
            )
            if missing_count == 0 and not has_low_visibility:
                raise MessageValidationError(
                    "PARTIAL requires a missing or low-visibility joint"
                )
            if state.pointing or state.pointer is not None:
                raise MessageValidationError("PARTIAL cannot contain a pointer")
        if state.tracking is TrackingState.LOST:
            if missing_count != 3:
                raise MessageValidationError("LOST requires all joints to be null")
            if any(value != 0.0 for value in visibility.values()):
                raise MessageValidationError("LOST visibility must be zero")
            if state.pointing or state.pointer is not None:
                raise MessageValidationError("LOST cannot contain a pointer")
        if state.pointing and state.pointer is None:
            raise MessageValidationError("pointing=true requires a pointer")
        if not state.pointing and state.pointer is not None:
            raise MessageValidationError("pointing=false requires pointer=null")

    @staticmethod
    def _joint_payload(joint: Joint | None) -> dict[str, float] | None:
        if joint is None:
            return None
        values = (joint.x, joint.y, joint.z)
        if not all(math.isfinite(value) for value in values):
            raise MessageValidationError("joint coordinates must be finite")
        return {
            "x": joint.x,
            "y": joint.y,
            "z": joint.z,
        }

    @staticmethod
    def _visibility(joint: Joint | None) -> float:
        if joint is None:
            return 0.0
        if not math.isfinite(joint.visibility):
            raise MessageValidationError("visibility must be finite")
        if not 0.0 <= joint.visibility <= 1.0:
            raise MessageValidationError("visibility must be between 0 and 1")
        return joint.visibility
