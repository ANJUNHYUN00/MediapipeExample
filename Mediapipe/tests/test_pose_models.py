from __future__ import annotations

import math

import pytest

from mediapipe_rps.pose_models import Joint, PoseTrackingResult, TrackingState


def test_joint_rejects_non_finite_values() -> None:
    with pytest.raises(ValueError, match="finite"):
        Joint(x=math.nan, y=0.2, z=0.0, visibility=0.9)


def test_lost_result_cannot_contain_joints() -> None:
    joint = Joint(x=0.1, y=0.2, z=0.0, visibility=0.9)

    with pytest.raises(ValueError, match="must not contain"):
        PoseTrackingResult(
            tracking=TrackingState.LOST,
            frame_index=0,
            captured_at_ms=1,
            video_timestamp_ms=1,
            right_shoulder=joint,
            right_elbow=None,
            right_wrist=None,
        )


def test_lost_factory_returns_empty_safe_result() -> None:
    result = PoseTrackingResult.lost(
        frame_index=3,
        captured_at_ms=100,
        video_timestamp_ms=50,
    )

    assert result.tracking is TrackingState.LOST
    assert not result.pose_detected
    assert all(joint is None for joint in result.joints.values())
