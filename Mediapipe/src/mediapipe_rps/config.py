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
class PointingConfig:
    """Right-arm geometry and temporal stability settings."""

    min_joint_visibility: float = 0.5
    min_upper_arm_length: float = 0.05
    min_forearm_length: float = 0.05
    min_shoulder_wrist_length: float = 0.10
    min_segment_length_ratio: float = 0.25
    max_segment_length_ratio: float = 4.0
    min_elbow_angle_degrees: float = 150.0
    pointer_extension_factor: float = 0.25
    smoothing_alpha: float = 0.35
    smoothing_max_frame_gap: int = 2
    activation_frames: int = 2

    def __post_init__(self) -> None:
        if not 0.0 <= self.min_joint_visibility <= 1.0:
            raise ValueError("min_joint_visibility must be between 0.0 and 1.0")
        for name in (
            "min_upper_arm_length",
            "min_forearm_length",
            "min_shoulder_wrist_length",
        ):
            if getattr(self, name) <= 0.0:
                raise ValueError(f"{name} must be greater than zero")
        if self.min_segment_length_ratio <= 0.0:
            raise ValueError("min_segment_length_ratio must be greater than zero")
        if self.max_segment_length_ratio < self.min_segment_length_ratio:
            raise ValueError(
                "max_segment_length_ratio must not be less than the minimum"
            )
        if not 0.0 <= self.min_elbow_angle_degrees <= 180.0:
            raise ValueError("min_elbow_angle_degrees must be between 0 and 180")
        if self.pointer_extension_factor < 0.0:
            raise ValueError("pointer_extension_factor must be zero or greater")
        if not 0.0 < self.smoothing_alpha <= 1.0:
            raise ValueError("smoothing_alpha must be greater than 0 and at most 1")
        if self.smoothing_max_frame_gap <= 0:
            raise ValueError("smoothing_max_frame_gap must be greater than zero")
        if self.activation_frames <= 0:
            raise ValueError("activation_frames must be greater than zero")


@dataclass(frozen=True, slots=True)
class WebSocketConfig:
    """Local pose publisher settings."""

    enabled: bool = True
    host: str = "127.0.0.1"
    port: int = 8765
    publish_hz: float = 15.0
    startup_timeout_seconds: float = 5.0
    shutdown_timeout_seconds: float = 5.0

    def __post_init__(self) -> None:
        if self.host not in {"127.0.0.1", "localhost"}:
            raise ValueError("WebSocket host must remain local")
        if not 0 <= self.port <= 65535:
            raise ValueError("WebSocket port must be between 0 and 65535")
        if not 0.0 < self.publish_hz <= 60.0:
            raise ValueError("publish_hz must be greater than 0 and at most 60")
        if self.startup_timeout_seconds <= 0.0:
            raise ValueError("startup_timeout_seconds must be greater than zero")
        if self.shutdown_timeout_seconds <= 0.0:
            raise ValueError("shutdown_timeout_seconds must be greater than zero")

    @property
    def uri(self) -> str:
        return f"ws://{self.host}:{self.port}"


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
