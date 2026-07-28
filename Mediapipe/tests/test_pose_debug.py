from __future__ import annotations

import numpy as np

from mediapipe_rps.pose_debug import (
    format_pointer_state,
    format_pose_result,
    render_pose_debug,
)
from mediapipe_rps.pose_models import (
    Joint,
    NormalizedPointer,
    PointingReason,
    PosePointerState,
    PoseTrackingResult,
    TrackingState,
    Vector2,
)


def tracking_result() -> PoseTrackingResult:
    return PoseTrackingResult(
        tracking=TrackingState.TRACKING,
        frame_index=7,
        captured_at_ms=100,
        video_timestamp_ms=50,
        right_shoulder=Joint(0.2, 0.3, -0.1, 0.9),
        right_elbow=Joint(0.3, 0.4, -0.2, 0.8),
        right_wrist=Joint(0.4, 0.5, -0.3, 0.7),
    )


def test_console_format_contains_state_and_coordinates() -> None:
    text = format_pose_result(tracking_result())

    assert "frame=7 tracking=TRACKING" in text
    assert "shoulder[x=+0.200" in text
    assert "elbow[x=+0.300" in text
    assert "wrist[x=+0.400" in text


def test_preview_render_does_not_modify_source_frame() -> None:
    source = np.zeros((240, 320, 3), dtype=np.uint8)
    before = source.copy()

    rendered = render_pose_debug(
        source,
        tracking_result(),
        mirror_preview=True,
        fps=30.0,
    )

    assert rendered.shape == source.shape
    assert np.array_equal(source, before)
    assert np.any(rendered != source)


def test_pointer_diagnostics_include_decision_and_render_marker() -> None:
    pose = tracking_result()
    state = PosePointerState(
        pose=pose,
        pointing=True,
        pointer=NormalizedPointer(0.8, 0.4),
        arm_direction=Vector2(1.0, 0.0),
        elbow_angle_degrees=175.0,
        reason=PointingReason.POINTING,
    )
    source = np.zeros((240, 320, 3), dtype=np.uint8)

    raw_pointer = NormalizedPointer(0.7, 0.6)
    text = format_pointer_state(
        state,
        raw_pointer=raw_pointer,
        center_x=0.7,
        center_y=0.6,
        gain_x=2.0,
        gain_y=1.5,
    )
    rendered = render_pose_debug(
        source,
        pose,
        mirror_preview=False,
        fps=30.0,
        pointer_state=state,
        raw_pointer=raw_pointer,
        center_x=0.7,
        center_y=0.6,
        gain_x=2.0,
        gain_y=1.5,
    )

    assert "pointing=true" in text
    assert "reason=POINTING" in text
    assert "angle=175.0" in text
    assert "raw_pointer[x=0.700,y=0.600]" in text
    assert "calibrated_pointer[x=0.800,y=0.400]" in text
    assert "center[x=0.700,y=0.600]" in text
    assert "gain[x=2.000,y=1.500]" in text
    assert np.any(rendered != source)
