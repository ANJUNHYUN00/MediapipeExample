"""Right-arm pointing geometry and temporal stabilization."""

from __future__ import annotations

import math
from dataclasses import replace

from .config import PointingConfig
from .pose_models import (
    Joint,
    NormalizedPointer,
    PointingReason,
    PosePointerState,
    PoseTrackingResult,
    TrackingState,
    Vector2,
)


def _length(vector: tuple[float, float]) -> float:
    return math.hypot(vector[0], vector[1])


def _screen_vector(
    start: Joint,
    end: Joint,
    image_aspect_ratio: float,
) -> tuple[float, float]:
    return (
        (end.x - start.x) * image_aspect_ratio,
        end.y - start.y,
    )


def _elbow_angle_degrees(
    shoulder: Joint,
    elbow: Joint,
    wrist: Joint,
    image_aspect_ratio: float,
) -> float:
    elbow_to_shoulder = _screen_vector(
        elbow,
        shoulder,
        image_aspect_ratio,
    )
    elbow_to_wrist = _screen_vector(
        elbow,
        wrist,
        image_aspect_ratio,
    )
    denominator = _length(elbow_to_shoulder) * _length(elbow_to_wrist)
    if denominator == 0.0:
        return 0.0
    cosine = (
        elbow_to_shoulder[0] * elbow_to_wrist[0]
        + elbow_to_shoulder[1] * elbow_to_wrist[1]
    ) / denominator
    return math.degrees(math.acos(max(-1.0, min(1.0, cosine))))


class PointerCalibration:
    """Map an existing normalized pointer into the calibrated Unity range."""

    def __init__(self, config: PointingConfig) -> None:
        self._center_x = config.pointer_center_x
        self._center_y = config.pointer_center_y
        self._gain_x = config.pointer_gain_x
        self._gain_y = config.pointer_gain_y

    @property
    def center_x(self) -> float:
        return self._center_x

    @property
    def center_y(self) -> float:
        return self._center_y

    @property
    def gain_x(self) -> float:
        return self._gain_x

    @property
    def gain_y(self) -> float:
        return self._gain_y

    def calibrate(self, pointer: NormalizedPointer) -> NormalizedPointer:
        calibrated_x = 0.5 + (pointer.x - self._center_x) * self._gain_x
        calibrated_y = 0.5 + (pointer.y - self._center_y) * self._gain_y
        return NormalizedPointer(
            x=max(0.0, min(1.0, calibrated_x)),
            y=max(0.0, min(1.0, calibrated_y)),
        )

    def set_center(self, pointer: NormalizedPointer) -> None:
        self._center_x = pointer.x
        self._center_y = pointer.y


class PointingResolver:
    """Convert one valid right-arm pose into a raw pointer decision."""

    def __init__(self, config: PointingConfig) -> None:
        self._config = config

    def resolve(
        self,
        pose: PoseTrackingResult,
        *,
        image_aspect_ratio: float,
    ) -> PosePointerState:
        if not math.isfinite(image_aspect_ratio) or image_aspect_ratio <= 0.0:
            raise ValueError("image_aspect_ratio must be finite and greater than zero")
        if pose.tracking is not TrackingState.TRACKING:
            return self._inactive(pose, PointingReason.TRACKING_REQUIRED)

        shoulder = pose.right_shoulder
        elbow = pose.right_elbow
        wrist = pose.right_wrist
        if shoulder is None or elbow is None or wrist is None:
            return self._inactive(pose, PointingReason.MISSING_JOINT)

        if any(
            joint.visibility < self._config.min_joint_visibility
            for joint in (shoulder, elbow, wrist)
        ):
            return self._inactive(pose, PointingReason.LOW_VISIBILITY)

        upper_arm = _screen_vector(shoulder, elbow, image_aspect_ratio)
        forearm = _screen_vector(elbow, wrist, image_aspect_ratio)
        shoulder_to_wrist = _screen_vector(
            shoulder,
            wrist,
            image_aspect_ratio,
        )
        upper_arm_length = _length(upper_arm)
        forearm_length = _length(forearm)
        total_arm_length = _length(shoulder_to_wrist)

        if (
            upper_arm_length < self._config.min_upper_arm_length
            or forearm_length < self._config.min_forearm_length
            or total_arm_length < self._config.min_shoulder_wrist_length
        ):
            return self._inactive(pose, PointingReason.DEGENERATE_ARM)

        segment_ratio = upper_arm_length / forearm_length
        if not (
            self._config.min_segment_length_ratio
            <= segment_ratio
            <= self._config.max_segment_length_ratio
        ):
            return self._inactive(pose, PointingReason.IMBALANCED_SEGMENTS)

        raw_direction_x = wrist.x - shoulder.x
        raw_direction_y = wrist.y - shoulder.y
        raw_direction_length = math.hypot(raw_direction_x, raw_direction_y)
        if raw_direction_length == 0.0:
            return self._inactive(pose, PointingReason.DEGENERATE_ARM)
        arm_direction = Vector2(
            x=raw_direction_x / raw_direction_length,
            y=raw_direction_y / raw_direction_length,
        )

        elbow_angle = _elbow_angle_degrees(
            shoulder,
            elbow,
            wrist,
            image_aspect_ratio,
        )
        if elbow_angle < self._config.min_elbow_angle_degrees:
            return self._inactive(
                pose,
                PointingReason.BENT_ELBOW,
                arm_direction=arm_direction,
                elbow_angle_degrees=elbow_angle,
            )

        candidate_x = wrist.x + (
            raw_direction_x * self._config.pointer_extension_factor
        )
        candidate_y = wrist.y + (
            raw_direction_y * self._config.pointer_extension_factor
        )
        pointer = NormalizedPointer(
            x=max(0.0, min(1.0, candidate_x)),
            y=max(0.0, min(1.0, candidate_y)),
        )
        return PosePointerState(
            pose=pose,
            pointing=True,
            pointer=pointer,
            arm_direction=arm_direction,
            elbow_angle_degrees=elbow_angle,
            reason=PointingReason.POINTING,
        )

    @staticmethod
    def _inactive(
        pose: PoseTrackingResult,
        reason: PointingReason,
        *,
        arm_direction: Vector2 | None = None,
        elbow_angle_degrees: float | None = None,
    ) -> PosePointerState:
        return PosePointerState(
            pose=pose,
            pointing=False,
            pointer=None,
            arm_direction=arm_direction,
            elbow_angle_degrees=elbow_angle_degrees,
            reason=reason,
        )


class RightArmSmoother:
    """EMA-smooth valid joints without concealing PARTIAL or LOST input."""

    def __init__(self, config: PointingConfig) -> None:
        self._alpha = config.smoothing_alpha
        self._max_frame_gap = config.smoothing_max_frame_gap
        self._previous: PoseTrackingResult | None = None

    def update(self, pose: PoseTrackingResult) -> PoseTrackingResult:
        if pose.tracking is not TrackingState.TRACKING:
            self.reset()
            return pose
        if (
            pose.right_shoulder is None
            or pose.right_elbow is None
            or pose.right_wrist is None
        ):
            self.reset()
            return pose

        previous = self._previous
        if (
            previous is None
            or previous.right_shoulder is None
            or previous.right_elbow is None
            or previous.right_wrist is None
            or pose.frame_index <= previous.frame_index
            or pose.frame_index - previous.frame_index > self._max_frame_gap
        ):
            self._previous = pose
            return pose

        smoothed = PoseTrackingResult(
            tracking=pose.tracking,
            frame_index=pose.frame_index,
            captured_at_ms=pose.captured_at_ms,
            video_timestamp_ms=pose.video_timestamp_ms,
            right_shoulder=self._smooth_joint(
                previous.right_shoulder,
                pose.right_shoulder,
            ),
            right_elbow=self._smooth_joint(
                previous.right_elbow,
                pose.right_elbow,
            ),
            right_wrist=self._smooth_joint(
                previous.right_wrist,
                pose.right_wrist,
            ),
        )
        self._previous = smoothed
        return smoothed

    def _smooth_joint(self, previous: Joint, current: Joint) -> Joint:
        inverse = 1.0 - self._alpha
        return Joint(
            x=self._alpha * current.x + inverse * previous.x,
            y=self._alpha * current.y + inverse * previous.y,
            z=self._alpha * current.z + inverse * previous.z,
            visibility=current.visibility,
        )

    def reset(self) -> None:
        self._previous = None


class PointingStabilizer:
    """Require consecutive valid frames but deactivate immediately on failure."""

    def __init__(self, config: PointingConfig) -> None:
        self._activation_frames = config.activation_frames
        self._consecutive_pointing_frames = 0
        self._last_frame_index: int | None = None

    def update(self, state: PosePointerState) -> PosePointerState:
        if not state.pointing:
            self.reset()
            return state

        if (
            self._last_frame_index is None
            or state.frame_index != self._last_frame_index + 1
        ):
            self._consecutive_pointing_frames = 1
        else:
            self._consecutive_pointing_frames += 1
        self._last_frame_index = state.frame_index

        if self._consecutive_pointing_frames < self._activation_frames:
            return replace(
                state,
                pointing=False,
                pointer=None,
                reason=PointingReason.ACTIVATING,
            )
        return state

    def reset(self) -> None:
        self._consecutive_pointing_frames = 0
        self._last_frame_index = None


class PointingPipeline:
    """Compose smoothing, geometry validation, and safe activation."""

    def __init__(self, config: PointingConfig) -> None:
        self._smoother = RightArmSmoother(config)
        self._resolver = PointingResolver(config)
        self._stabilizer = PointingStabilizer(config)
        self._calibration = PointerCalibration(config)
        self._raw_pointer: NormalizedPointer | None = None

    @property
    def raw_pointer(self) -> NormalizedPointer | None:
        return self._raw_pointer

    @property
    def calibration(self) -> PointerCalibration:
        return self._calibration

    def calibrate_center_from_current_raw_pointer(self) -> bool:
        """Use the latest geometrically valid raw pointer as session center."""

        if self._raw_pointer is None:
            return False
        self._calibration.set_center(self._raw_pointer)
        return True

    def update(
        self,
        pose: PoseTrackingResult,
        *,
        image_aspect_ratio: float,
    ) -> PosePointerState:
        smoothed_pose = self._smoother.update(pose)
        raw_state = self._resolver.resolve(
            smoothed_pose,
            image_aspect_ratio=image_aspect_ratio,
        )
        self._raw_pointer = raw_state.pointer
        stabilized_state = self._stabilizer.update(raw_state)
        if not stabilized_state.pointing or stabilized_state.pointer is None:
            return stabilized_state
        return replace(
            stabilized_state,
            pointer=self._calibration.calibrate(stabilized_state.pointer),
        )

    def reset(self) -> None:
        self._smoother.reset()
        self._stabilizer.reset()
        self._raw_pointer = None
