from __future__ import annotations

import json
from pathlib import Path

import pytest

from mediapipe_rps.message_builder import (
    MessageValidationError,
    PoseMessageBuilder,
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

FIXTURES = Path(__file__).parent / "fixtures" / "messages"


def joint(x: float, y: float, z: float, visibility: float) -> Joint:
    return Joint(x=x, y=y, z=z, visibility=visibility)


def tracking_state() -> PosePointerState:
    pose = PoseTrackingResult(
        tracking=TrackingState.TRACKING,
        frame_index=0,
        captured_at_ms=1_750_000_100_123,
        video_timestamp_ms=100,
        right_shoulder=joint(0.58, 0.36, -0.12, 0.99),
        right_elbow=joint(0.66, 0.43, -0.16, 0.97),
        right_wrist=joint(0.76, 0.38, -0.20, 0.95),
    )
    return PosePointerState(
        pose=pose,
        pointing=True,
        pointer=NormalizedPointer(0.72, 0.34),
        arm_direction=Vector2(1.0, 0.0),
        elbow_angle_degrees=170.0,
        reason=PointingReason.POINTING,
    )


def partial_state() -> PosePointerState:
    pose = PoseTrackingResult(
        tracking=TrackingState.PARTIAL,
        frame_index=1,
        captured_at_ms=1_750_000_100_323,
        video_timestamp_ms=101,
        right_shoulder=joint(0.57, 0.36, -0.11, 0.98),
        right_elbow=joint(0.65, 0.44, -0.15, 0.91),
        right_wrist=None,
    )
    return PosePointerState(
        pose=pose,
        pointing=False,
        pointer=None,
        arm_direction=None,
        elbow_angle_degrees=None,
        reason=PointingReason.TRACKING_REQUIRED,
    )


def lost_state() -> PosePointerState:
    pose = PoseTrackingResult.lost(
        frame_index=2,
        captured_at_ms=1_750_000_100_223,
        video_timestamp_ms=102,
    )
    return PosePointerState(
        pose=pose,
        pointing=False,
        pointer=None,
        arm_direction=None,
        elbow_angle_degrees=None,
        reason=PointingReason.TRACKING_REQUIRED,
    )


@pytest.mark.parametrize(
    ("state_factory", "sequence", "fixture_name"),
    [
        (tracking_state, 100, "pose_pointer_v2_tracking.json"),
        (lost_state, 101, "pose_pointer_v2_lost.json"),
        (partial_state, 102, "pose_pointer_v2_partial.json"),
    ],
)
def test_builder_matches_pose_v2_fixtures(
    state_factory,
    sequence: int,
    fixture_name: str,
) -> None:
    expected = json.loads((FIXTURES / fixture_name).read_text(encoding="utf-8"))

    message = PoseMessageBuilder().build(
        state_factory(),
        sequence=sequence,
    )

    assert message.to_dict() == expected
    assert json.loads(message.to_json()) == expected


def test_message_contains_only_public_v2_fields() -> None:
    payload = PoseMessageBuilder().build(
        tracking_state(),
        sequence=0,
    ).to_dict()

    assert set(payload) == {
        "type",
        "version",
        "timestamp",
        "sequence",
        "tracking",
        "pointing",
        "pointer",
        "joints",
        "visibility",
    }
    assert "reason" not in payload
    assert "frame_index" not in payload


def test_partial_with_complete_high_quality_joints_is_rejected() -> None:
    pose = PoseTrackingResult(
        tracking=TrackingState.PARTIAL,
        frame_index=0,
        captured_at_ms=1,
        video_timestamp_ms=1,
        right_shoulder=joint(0.2, 0.3, 0.0, 0.9),
        right_elbow=joint(0.4, 0.3, 0.0, 0.9),
        right_wrist=joint(0.6, 0.3, 0.0, 0.9),
    )
    state = PosePointerState(
        pose=pose,
        pointing=False,
        pointer=None,
        arm_direction=None,
        elbow_angle_degrees=None,
        reason=PointingReason.LOW_VISIBILITY,
    )

    with pytest.raises(MessageValidationError, match="PARTIAL requires"):
        PoseMessageBuilder().build(state, sequence=0)


@pytest.mark.parametrize("sequence", [-1, 2**63])
def test_sequence_outside_signed_64_bit_range_is_rejected(sequence: int) -> None:
    with pytest.raises(MessageValidationError, match="sequence"):
        PoseMessageBuilder().build(tracking_state(), sequence=sequence)
