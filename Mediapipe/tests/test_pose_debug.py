from __future__ import annotations

import numpy as np

from mediapipe_rps.pose_debug import format_pose_result, render_pose_debug
from mediapipe_rps.pose_models import Joint, PoseTrackingResult, TrackingState


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
