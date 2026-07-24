"""Smoke tests for the declared development environment."""

import hashlib
from importlib import metadata
from importlib.util import find_spec

from mediapipe_rps.config import PoseConfig


def test_declared_runtime_packages_are_installed() -> None:
    assert metadata.version("mediapipe") == "0.10.35"
    assert metadata.version("opencv-contrib-python") == "5.0.0.93"
    assert metadata.version("websockets") == "16.1.1"


def test_declared_runtime_modules_are_discoverable() -> None:
    assert find_spec("cv2") is not None
    assert find_spec("mediapipe") is not None
    assert find_spec("websockets") is not None


def test_pose_landmarker_api_is_available() -> None:
    from mediapipe.tasks.python import vision

    assert hasattr(vision, "PoseLandmarker")
    assert hasattr(vision, "PoseLandmarkerOptions")


def test_pose_model_asset_matches_recorded_checksum() -> None:
    model_path = PoseConfig().model_path

    assert model_path.is_file()
    assert model_path.stat().st_size == 5_777_746
    assert hashlib.sha256(model_path.read_bytes()).hexdigest().upper() == (
        "59929E1D1EE95287735DDD833B19CF4AC46D29BC7AFDDBBF6753C459690D574A"
    )
