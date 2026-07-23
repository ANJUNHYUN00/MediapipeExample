"""Smoke tests for the declared development environment."""

from importlib import metadata
from importlib.util import find_spec


def test_declared_runtime_packages_are_installed() -> None:
    assert metadata.version("mediapipe") == "0.10.35"
    assert metadata.version("opencv-contrib-python") == "5.0.0.93"
    assert metadata.version("websockets") == "16.1.1"


def test_declared_runtime_modules_are_discoverable() -> None:
    assert find_spec("cv2") is not None
    assert find_spec("mediapipe") is not None
    assert find_spec("websockets") is not None
