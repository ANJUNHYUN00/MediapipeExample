from __future__ import annotations

import pytest

from mediapipe_rps.config import PointingConfig
from mediapipe_rps.pointing import (
    PointerCalibration,
    PointingPipeline,
    PointingResolver,
    PointingStabilizer,
    RightArmSmoother,
)
from mediapipe_rps.pose_models import (
    Joint,
    NormalizedPointer,
    PointingReason,
    PoseTrackingResult,
    TrackingState,
)


def joint(
    x: float,
    y: float,
    *,
    visibility: float = 0.95,
) -> Joint:
    return Joint(x=x, y=y, z=-0.1, visibility=visibility)


def tracking_pose(
    frame_index: int = 0,
    *,
    shoulder: Joint | None = None,
    elbow: Joint | None = None,
    wrist: Joint | None = None,
) -> PoseTrackingResult:
    return PoseTrackingResult(
        tracking=TrackingState.TRACKING,
        frame_index=frame_index,
        captured_at_ms=1_000 + frame_index,
        video_timestamp_ms=100 + frame_index,
        right_shoulder=shoulder or joint(0.2, 0.5),
        right_elbow=elbow or joint(0.5, 0.5),
        right_wrist=wrist or joint(0.8, 0.5),
    )


def test_extended_arm_uses_shoulder_to_wrist_direction() -> None:
    resolver = PointingResolver(PointingConfig())

    state = resolver.resolve(
        tracking_pose(),
        image_aspect_ratio=1.0,
    )

    assert state.pointing
    assert state.reason is PointingReason.POINTING
    assert state.elbow_angle_degrees == pytest.approx(180.0)
    assert state.arm_direction.x == pytest.approx(1.0)
    assert state.arm_direction.y == pytest.approx(0.0)
    assert state.pointer.x == pytest.approx(0.95)
    assert state.pointer.y == pytest.approx(0.5)


def test_bent_elbow_disables_pointing() -> None:
    resolver = PointingResolver(PointingConfig(min_elbow_angle_degrees=150.0))
    pose = tracking_pose(wrist=joint(0.5, 0.2))

    state = resolver.resolve(pose, image_aspect_ratio=1.0)

    assert not state.pointing
    assert state.pointer is None
    assert state.reason is PointingReason.BENT_ELBOW
    assert state.elbow_angle_degrees == pytest.approx(90.0)


def test_low_visibility_disables_pointing_without_changing_tracking() -> None:
    resolver = PointingResolver(PointingConfig(min_joint_visibility=0.5))
    pose = tracking_pose(wrist=joint(0.8, 0.5, visibility=0.49))

    state = resolver.resolve(pose, image_aspect_ratio=1.0)

    assert state.tracking is TrackingState.TRACKING
    assert not state.pointing
    assert state.reason is PointingReason.LOW_VISIBILITY


@pytest.mark.parametrize("tracking", [TrackingState.PARTIAL, TrackingState.LOST])
def test_non_tracking_input_is_preserved_and_never_points(
    tracking: TrackingState,
) -> None:
    resolver = PointingResolver(PointingConfig())
    if tracking is TrackingState.LOST:
        pose = PoseTrackingResult.lost(
            frame_index=0,
            captured_at_ms=1,
            video_timestamp_ms=1,
        )
    else:
        pose = PoseTrackingResult(
            tracking=TrackingState.PARTIAL,
            frame_index=0,
            captured_at_ms=1,
            video_timestamp_ms=1,
            right_shoulder=joint(0.2, 0.5),
            right_elbow=joint(0.5, 0.5),
            right_wrist=None,
        )

    state = resolver.resolve(pose, image_aspect_ratio=1.0)

    assert state.tracking is tracking
    assert not state.pointing
    assert state.pointer is None
    assert state.reason is PointingReason.TRACKING_REQUIRED


def test_short_arm_vector_is_rejected() -> None:
    resolver = PointingResolver(PointingConfig())
    pose = tracking_pose(
        shoulder=joint(0.50, 0.50),
        elbow=joint(0.51, 0.50),
        wrist=joint(0.52, 0.50),
    )

    state = resolver.resolve(pose, image_aspect_ratio=1.0)

    assert not state.pointing
    assert state.reason is PointingReason.DEGENERATE_ARM


def test_imbalanced_arm_segments_are_rejected() -> None:
    resolver = PointingResolver(PointingConfig())
    pose = tracking_pose(
        shoulder=joint(0.10, 0.50),
        elbow=joint(0.16, 0.50),
        wrist=joint(0.46, 0.50),
    )

    state = resolver.resolve(pose, image_aspect_ratio=1.0)

    assert not state.pointing
    assert state.reason is PointingReason.IMBALANCED_SEGMENTS


def test_out_of_bounds_pointer_is_clamped() -> None:
    resolver = PointingResolver(PointingConfig(pointer_extension_factor=0.5))
    pose = tracking_pose(
        shoulder=joint(0.35, 0.5),
        elbow=joint(0.65, 0.5),
        wrist=joint(0.95, 0.5),
    )

    state = resolver.resolve(pose, image_aspect_ratio=1.0)

    assert state.pointing
    assert state.pointer.x == 1.0
    assert state.pointer.y == 0.5


def test_aspect_ratio_must_be_positive_and_finite() -> None:
    resolver = PointingResolver(PointingConfig())

    with pytest.raises(ValueError, match="image_aspect_ratio"):
        resolver.resolve(tracking_pose(), image_aspect_ratio=0.0)


def test_joint_smoothing_reduces_single_frame_coordinate_jump() -> None:
    smoother = RightArmSmoother(PointingConfig(smoothing_alpha=0.25))
    first = tracking_pose(frame_index=0)
    jumped = tracking_pose(
        frame_index=1,
        shoulder=joint(0.3, 0.5),
        elbow=joint(0.6, 0.5),
        wrist=joint(0.9, 0.5),
    )

    assert smoother.update(first) is first
    smoothed = smoother.update(jumped)

    assert smoothed.right_shoulder.x == pytest.approx(0.225)
    assert smoothed.right_elbow.x == pytest.approx(0.525)
    assert smoothed.right_wrist.x == pytest.approx(0.825)
    assert smoothed.right_wrist.visibility == jumped.right_wrist.visibility


def test_smoothing_resets_immediately_on_partial_input() -> None:
    smoother = RightArmSmoother(PointingConfig(smoothing_alpha=0.25))
    smoother.update(tracking_pose(frame_index=0))
    partial = PoseTrackingResult(
        tracking=TrackingState.PARTIAL,
        frame_index=1,
        captured_at_ms=1_001,
        video_timestamp_ms=101,
        right_shoulder=joint(0.2, 0.5),
        right_elbow=joint(0.5, 0.5),
        right_wrist=None,
    )
    shifted = tracking_pose(
        frame_index=2,
        shoulder=joint(0.3, 0.5),
        elbow=joint(0.6, 0.5),
        wrist=joint(0.9, 0.5),
    )

    assert smoother.update(partial) is partial
    assert smoother.update(shifted) is shifted


def test_stabilizer_requires_consecutive_frames_and_deactivates_immediately() -> None:
    config = PointingConfig(activation_frames=2)
    resolver = PointingResolver(config)
    stabilizer = PointingStabilizer(config)

    first = stabilizer.update(
        resolver.resolve(tracking_pose(0), image_aspect_ratio=1.0)
    )
    second = stabilizer.update(
        resolver.resolve(tracking_pose(1), image_aspect_ratio=1.0)
    )
    bent = stabilizer.update(
        resolver.resolve(
            tracking_pose(2, wrist=joint(0.5, 0.2)),
            image_aspect_ratio=1.0,
        )
    )
    restarted = stabilizer.update(
        resolver.resolve(tracking_pose(3), image_aspect_ratio=1.0)
    )

    assert not first.pointing
    assert first.reason is PointingReason.ACTIVATING
    assert second.pointing
    assert not bent.pointing
    assert bent.reason is PointingReason.BENT_ELBOW
    assert not restarted.pointing
    assert restarted.reason is PointingReason.ACTIVATING


def test_pipeline_produces_stable_pointer_after_activation() -> None:
    pipeline = PointingPipeline(
        PointingConfig(smoothing_alpha=0.5, activation_frames=2)
    )

    first = pipeline.update(tracking_pose(0), image_aspect_ratio=1.0)
    second = pipeline.update(tracking_pose(1), image_aspect_ratio=1.0)

    assert not first.pointing
    assert second.pointing
    assert second.pointer is not None


def test_pipeline_keeps_noisy_extended_arm_pointing_after_activation() -> None:
    pipeline = PointingPipeline(
        PointingConfig(smoothing_alpha=0.35, activation_frames=2)
    )
    states = []

    for frame_index in range(30):
        jitter = 0.02 if frame_index % 2 == 0 else -0.02
        pose = tracking_pose(
            frame_index,
            elbow=joint(0.5 + jitter * 0.4, 0.5),
            wrist=joint(0.8 + jitter, 0.5),
        )
        states.append(pipeline.update(pose, image_aspect_ratio=1.0))

    assert states[0].reason is PointingReason.ACTIVATING
    assert all(state.pointing for state in states[1:])
    pointer_x_values = [state.pointer.x for state in states[5:]]
    assert max(pointer_x_values) - min(pointer_x_values) < 0.02


def test_pipeline_drops_stale_pointer_on_partial_and_reactivates() -> None:
    pipeline = PointingPipeline(PointingConfig(activation_frames=2))
    pipeline.update(tracking_pose(0), image_aspect_ratio=1.0)
    active = pipeline.update(tracking_pose(1), image_aspect_ratio=1.0)
    partial = PoseTrackingResult(
        tracking=TrackingState.PARTIAL,
        frame_index=2,
        captured_at_ms=1_002,
        video_timestamp_ms=102,
        right_shoulder=joint(0.2, 0.5),
        right_elbow=joint(0.5, 0.5),
        right_wrist=None,
    )

    inactive = pipeline.update(partial, image_aspect_ratio=1.0)
    restarted = pipeline.update(tracking_pose(3), image_aspect_ratio=1.0)

    assert active.pointing
    assert not inactive.pointing
    assert inactive.pointer is None
    assert inactive.tracking is TrackingState.PARTIAL
    assert not restarted.pointing
    assert restarted.reason is PointingReason.ACTIVATING


def test_calibration_center_maps_to_screen_center() -> None:
    calibration = PointerCalibration(
        PointingConfig(
            pointer_center_x=0.82,
            pointer_center_y=0.76,
            pointer_gain_x=2.0,
            pointer_gain_y=2.0,
        )
    )

    pointer = calibration.calibrate(NormalizedPointer(0.82, 0.76))

    assert pointer.x == pytest.approx(0.5)
    assert pointer.y == pytest.approx(0.5)


def test_calibration_gain_expands_motion_around_center() -> None:
    calibration = PointerCalibration(
        PointingConfig(pointer_gain_x=2.0, pointer_gain_y=3.0)
    )

    pointer = calibration.calibrate(NormalizedPointer(0.60, 0.40))

    assert pointer.x == pytest.approx(0.70)
    assert pointer.y == pytest.approx(0.20)


def test_calibration_clamps_final_pointer() -> None:
    calibration = PointerCalibration(
        PointingConfig(pointer_gain_x=4.0, pointer_gain_y=4.0)
    )

    pointer = calibration.calibrate(NormalizedPointer(0.90, 0.10))

    assert pointer.x == 1.0
    assert pointer.y == 0.0


def test_default_calibration_preserves_existing_pointer() -> None:
    config = PointingConfig(activation_frames=1)
    raw_state = PointingResolver(config).resolve(
        tracking_pose(),
        image_aspect_ratio=1.0,
    )
    calibrated_state = PointingPipeline(config).update(
        tracking_pose(),
        image_aspect_ratio=1.0,
    )

    assert raw_state.pointer is not None
    assert calibrated_state.pointer is not None
    assert calibrated_state.pointer.x == pytest.approx(raw_state.pointer.x)
    assert calibrated_state.pointer.y == pytest.approx(raw_state.pointer.y)


def test_calibration_keeps_pointer_null_when_pointing_is_false() -> None:
    pipeline = PointingPipeline(
        PointingConfig(
            activation_frames=1,
            pointer_center_x=0.8,
            pointer_center_y=0.8,
            pointer_gain_x=3.0,
            pointer_gain_y=3.0,
        )
    )

    state = pipeline.update(
        tracking_pose(wrist=joint(0.5, 0.2)),
        image_aspect_ratio=1.0,
    )

    assert not state.pointing
    assert state.pointer is None
    assert pipeline.raw_pointer is None


def test_session_center_uses_current_raw_pointer() -> None:
    pipeline = PointingPipeline(PointingConfig(activation_frames=1))
    initial = pipeline.update(tracking_pose(0), image_aspect_ratio=1.0)

    assert initial.pointer is not None
    assert pipeline.calibrate_center_from_current_raw_pointer()

    centered = pipeline.update(tracking_pose(1), image_aspect_ratio=1.0)

    assert centered.pointer is not None
    assert centered.pointer.x == pytest.approx(0.5)
    assert centered.pointer.y == pytest.approx(0.5)
