from __future__ import annotations

from types import SimpleNamespace

import numpy as np
import pytest

from mediapipe_rps.config import PoseConfig
from mediapipe_rps.models import CapturedFrame
from mediapipe_rps.pose_models import TrackingState
from mediapipe_rps.pose_tracker import (
    RIGHT_ELBOW_INDEX,
    RIGHT_SHOULDER_INDEX,
    RIGHT_WRIST_INDEX,
    PoseTracker,
    PoseTrackerError,
)


class FakeLandmarker:
    def __init__(self, results: list[object]) -> None:
        self.results = list(results)
        self.calls: list[tuple[object, int]] = []
        self.closed = False

    def detect_for_video(self, image: object, timestamp_ms: int) -> object:
        self.calls.append((image, timestamp_ms))
        return self.results.pop(0)

    def close(self) -> None:
        self.closed = True


def make_landmarks(
    *,
    visibility: float = 0.9,
    overrides: dict[int, object] | None = None,
) -> list[object]:
    landmarks = [
        SimpleNamespace(
            x=index / 100.0,
            y=index / 200.0,
            z=-index / 300.0,
            visibility=visibility,
        )
        for index in range(33)
    ]
    for index, value in (overrides or {}).items():
        landmarks[index] = value
    return landmarks


def make_frame(index: int = 0) -> CapturedFrame:
    image = np.zeros((2, 3, 3), dtype=np.uint8)
    image[0, 0] = [10, 20, 30]
    return CapturedFrame(
        image_bgr=image,
        frame_index=index,
        captured_at_ms=1_700_000_000_000 + index,
    )


def make_tracker(
    tmp_path,
    landmarker: FakeLandmarker,
    clock_values: list[int] | None = None,
) -> PoseTracker:
    model_path = tmp_path / "pose.task"
    model_path.write_bytes(b"test model placeholder")
    clock = iter(clock_values or [100])
    return PoseTracker(
        PoseConfig(model_path=model_path),
        landmarker_factory=lambda _: landmarker,
        clock_ms=lambda: next(clock),
    )


def test_extracts_right_arm_indices_and_converts_bgr_to_rgb(tmp_path) -> None:
    landmarker = FakeLandmarker(
        [SimpleNamespace(pose_landmarks=[make_landmarks()])]
    )
    tracker = make_tracker(tmp_path, landmarker)

    with tracker:
        result = tracker.process(make_frame())

    assert result.tracking is TrackingState.TRACKING
    assert result.right_shoulder.x == pytest.approx(RIGHT_SHOULDER_INDEX / 100)
    assert result.right_elbow.x == pytest.approx(RIGHT_ELBOW_INDEX / 100)
    assert result.right_wrist.x == pytest.approx(RIGHT_WRIST_INDEX / 100)
    assert result.right_wrist.visibility == pytest.approx(0.9)
    mp_image, timestamp_ms = landmarker.calls[0]
    assert list(mp_image.numpy_view()[0, 0]) == [30, 20, 10]
    assert timestamp_ms == 100
    assert landmarker.closed


def test_no_pose_returns_lost_without_raising(tmp_path) -> None:
    landmarker = FakeLandmarker([SimpleNamespace(pose_landmarks=[])])
    tracker = make_tracker(tmp_path, landmarker)

    with tracker:
        result = tracker.process(make_frame())

    assert result.tracking is TrackingState.LOST
    assert result.right_shoulder is None
    assert result.right_elbow is None
    assert result.right_wrist is None


def test_low_visibility_right_arm_returns_partial(tmp_path) -> None:
    landmarks = make_landmarks()
    landmarks[RIGHT_WRIST_INDEX].visibility = 0.49
    landmarker = FakeLandmarker(
        [SimpleNamespace(pose_landmarks=[landmarks])]
    )
    tracker = make_tracker(tmp_path, landmarker)

    with tracker:
        result = tracker.process(make_frame())

    assert result.tracking is TrackingState.PARTIAL
    assert result.right_wrist is not None
    assert result.right_wrist.visibility == pytest.approx(0.49)


def test_non_finite_joint_is_removed_and_result_is_partial(tmp_path) -> None:
    invalid_elbow = SimpleNamespace(
        x=float("nan"),
        y=0.4,
        z=0.0,
        visibility=0.9,
    )
    landmarks = make_landmarks(overrides={RIGHT_ELBOW_INDEX: invalid_elbow})
    landmarker = FakeLandmarker(
        [SimpleNamespace(pose_landmarks=[landmarks])]
    )
    tracker = make_tracker(tmp_path, landmarker)

    with tracker:
        result = tracker.process(make_frame())

    assert result.tracking is TrackingState.PARTIAL
    assert result.right_shoulder is not None
    assert result.right_elbow is None
    assert result.right_wrist is not None


def test_wrong_landmark_count_returns_lost(tmp_path) -> None:
    landmarker = FakeLandmarker(
        [SimpleNamespace(pose_landmarks=[make_landmarks()[:17]])]
    )
    tracker = make_tracker(tmp_path, landmarker)

    with tracker:
        result = tracker.process(make_frame())

    assert result.tracking is TrackingState.LOST


def test_video_timestamps_are_strictly_increasing(tmp_path) -> None:
    raw_result = SimpleNamespace(pose_landmarks=[])
    landmarker = FakeLandmarker([raw_result, raw_result, raw_result])
    tracker = make_tracker(tmp_path, landmarker, [200, 200, 199])

    with tracker:
        tracker.process(make_frame(0))
        tracker.process(make_frame(1))
        tracker.process(make_frame(2))

    assert [call[1] for call in landmarker.calls] == [200, 201, 202]


def test_missing_model_fails_before_landmarker_creation(tmp_path) -> None:
    factory_called = False

    def factory(_: PoseConfig) -> FakeLandmarker:
        nonlocal factory_called
        factory_called = True
        return FakeLandmarker([])

    tracker = PoseTracker(
        PoseConfig(model_path=tmp_path / "missing.task"),
        landmarker_factory=factory,
    )

    with pytest.raises(PoseTrackerError, match="model not found"):
        tracker.open()

    assert not factory_called
