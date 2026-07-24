"""Console and OpenCV diagnostics for standalone Pose tracking."""

from __future__ import annotations

import cv2
import numpy as np
from numpy.typing import NDArray

from .pose_models import Joint, PoseTrackingResult, TrackingState

_STATE_COLORS = {
    TrackingState.TRACKING: (50, 205, 50),
    TrackingState.PARTIAL: (0, 191, 255),
    TrackingState.LOST: (60, 60, 230),
}


def format_pose_result(result: PoseTrackingResult) -> str:
    """Return a compact, stable line for terminal diagnostics."""

    def format_joint(joint: Joint | None) -> str:
        if joint is None:
            return "missing"
        return (
            f"x={joint.x:+.3f},y={joint.y:+.3f},"
            f"z={joint.z:+.3f},vis={joint.visibility:.2f}"
        )

    return (
        f"frame={result.frame_index} tracking={result.tracking.value} "
        f"shoulder[{format_joint(result.right_shoulder)}] "
        f"elbow[{format_joint(result.right_elbow)}] "
        f"wrist[{format_joint(result.right_wrist)}]"
    )


def render_pose_debug(
    image_bgr: NDArray[np.uint8],
    result: PoseTrackingResult,
    *,
    mirror_preview: bool,
    fps: float,
) -> NDArray[np.uint8]:
    """Draw the right-arm joints and current state on a preview frame."""

    canvas = cv2.flip(image_bgr, 1) if mirror_preview else image_bgr.copy()
    height, width = canvas.shape[:2]
    color = _STATE_COLORS[result.tracking]

    def pixel(joint: Joint) -> tuple[int, int]:
        normalized_x = 1.0 - joint.x if mirror_preview else joint.x
        x = int(round(normalized_x * (width - 1)))
        y = int(round(joint.y * (height - 1)))
        return (
            max(0, min(width - 1, x)),
            max(0, min(height - 1, y)),
        )

    named_joints = [
        ("R shoulder (12)", result.right_shoulder),
        ("R elbow (14)", result.right_elbow),
        ("R wrist (16)", result.right_wrist),
    ]
    points = [
        (name, joint, pixel(joint) if joint is not None else None)
        for name, joint in named_joints
    ]

    for (_, start_joint, start), (_, end_joint, end) in zip(
        points,
        points[1:],
        strict=False,
    ):
        if start_joint is not None and end_joint is not None:
            cv2.line(canvas, start, end, color, 3, cv2.LINE_AA)

    for name, joint, point in points:
        if joint is None or point is None:
            continue
        cv2.circle(canvas, point, 7, color, -1, cv2.LINE_AA)
        cv2.putText(
            canvas,
            f"{name} v={joint.visibility:.2f}",
            (point[0] + 8, max(20, point[1] - 8)),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.45,
            color,
            1,
            cv2.LINE_AA,
        )

    cv2.rectangle(canvas, (0, 0), (width, 72), (20, 20, 20), -1)
    cv2.putText(
        canvas,
        f"{result.tracking.value}  FPS {fps:.1f}",
        (12, 28),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        color,
        2,
        cv2.LINE_AA,
    )
    cv2.putText(
        canvas,
        f"frame {result.frame_index}  q/esc: quit",
        (12, 56),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.5,
        (230, 230, 230),
        1,
        cv2.LINE_AA,
    )
    return canvas
