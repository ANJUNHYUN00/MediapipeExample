"""Configuration for the standalone Triage Trace Pose runtime."""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path


def _default_pose_model_path() -> Path:
    return Path(__file__).resolve().parents[2] / "models" / "pose_landmarker_lite.task"


@dataclass(frozen=True, slots=True)
class CameraConfig:
    """OpenCV camera and preview settings."""

    device_index: int = 0
    width: int | None = None
    height: int | None = None
    max_read_attempts: int = 3
    preview_enabled: bool = True
    mirror_preview: bool = True
    window_name: str = "Triage Trace - Pose Debug"

    def __post_init__(self) -> None:
        if self.device_index < 0:
            raise ValueError("camera device_index must be zero or greater")
        if self.width is not None and self.width <= 0:
            raise ValueError("camera width must be greater than zero")
        if self.height is not None and self.height <= 0:
            raise ValueError("camera height must be greater than zero")
        if self.max_read_attempts <= 0:
            raise ValueError("max_read_attempts must be greater than zero")


@dataclass(frozen=True, slots=True)
class PoseConfig:
    """MediaPipe Pose Landmarker and right-arm quality settings."""

    model_path: Path = field(default_factory=_default_pose_model_path)
    num_poses: int = 1
    min_pose_detection_confidence: float = 0.5
    min_pose_presence_confidence: float = 0.5
    min_tracking_confidence: float = 0.5
    min_right_arm_visibility: float = 0.5

    def __post_init__(self) -> None:
        object.__setattr__(self, "model_path", Path(self.model_path))
        if self.num_poses != 1:
            raise ValueError("Triage Trace MVP supports exactly one pose")
        for name in (
            "min_pose_detection_confidence",
            "min_pose_presence_confidence",
            "min_tracking_confidence",
            "min_right_arm_visibility",
        ):
            value = getattr(self, name)
            if not 0.0 <= value <= 1.0:
                raise ValueError(f"{name} must be between 0.0 and 1.0")


@dataclass(frozen=True, slots=True)
class DebugConfig:
    """Human-readable diagnostics settings."""

    console_log_interval_seconds: float = 1.0
    max_frames: int | None = None

    def __post_init__(self) -> None:
        if self.console_log_interval_seconds <= 0:
            raise ValueError("console_log_interval_seconds must be greater than zero")
        if self.max_frames is not None and self.max_frames <= 0:
            raise ValueError("max_frames must be greater than zero")
