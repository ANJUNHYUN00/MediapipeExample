"""Console and OpenCV diagnostics for standalone Pose tracking."""

from __future__ import annotations

import cv2
import numpy as np
from numpy.typing import NDArray

from .pose_models import (
    Joint,
    NormalizedPointer,
    PosePointerState,
    PoseTrackingResult,
    TrackingState,
)

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


def format_pointer_state(
    state: PosePointerState,
    *,
    raw_pointer: NormalizedPointer | None = None,
    center_x: float = 0.5,
    center_y: float = 0.5,
    gain_x: float = 1.0,
    gain_y: float = 1.0,
) -> str:
    """Return Pose coordinates together with the pointing decision."""

    angle = (
        "missing"
        if state.elbow_angle_degrees is None
        else f"{state.elbow_angle_degrees:.1f}"
    )
    raw = (
        "null"
        if raw_pointer is None
        else f"x={raw_pointer.x:.3f},y={raw_pointer.y:.3f}"
    )
    calibrated = (
        "null"
        if state.pointer is None
        else f"x={state.pointer.x:.3f},y={state.pointer.y:.3f}"
    )
    return (
        f"{format_pose_result(state.pose)} "
        f"pointing={str(state.pointing).lower()} "
        f"reason={state.reason.value} angle={angle} "
        f"raw_pointer[{raw}] calibrated_pointer[{calibrated}] "
        f"center[x={center_x:.3f},y={center_y:.3f}] "
        f"gain[x={gain_x:.3f},y={gain_y:.3f}]"
    )


def render_pose_debug(
    image_bgr: NDArray[np.uint8],
    result: PoseTrackingResult,
    *,
    mirror_preview: bool,
    fps: float,
    pointer_state: PosePointerState | None = None,
    raw_pointer: NormalizedPointer | None = None,
    center_x: float = 0.5,
    center_y: float = 0.5,
    gain_x: float = 1.0,
    gain_y: float = 1.0,
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

    cv2.rectangle(canvas, (0, 0), (width, 118), (20, 20, 20), -1)

    def pointer_pixel(pointer: NormalizedPointer) -> tuple[int, int]:
        normalized_x = 1.0 - pointer.x if mirror_preview else pointer.x
        return (
            int(round(normalized_x * (width - 1))),
            int(round(pointer.y * (height - 1))),
        )

    if raw_pointer is not None:
        raw_point = pointer_pixel(raw_pointer)
        cv2.drawMarker(
            canvas,
            raw_point,
            (0, 165, 255),
            cv2.MARKER_TILTED_CROSS,
            22,
            2,
            cv2.LINE_AA,
        )
        cv2.putText(
            canvas,
            "RAW",
            (raw_point[0] + 8, max(20, raw_point[1] - 8)),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.45,
            (0, 165, 255),
            1,
            cv2.LINE_AA,
        )

    if pointer_state is not None and pointer_state.pointer is not None:
        calibrated_point = pointer_pixel(pointer_state.pointer)
        cv2.drawMarker(
            canvas,
            calibrated_point,
            (255, 80, 255),
            cv2.MARKER_CROSS,
            24,
            3,
            cv2.LINE_AA,
        )
        cv2.putText(
            canvas,
            "CAL",
            (calibrated_point[0] + 8, max(20, calibrated_point[1] - 8)),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.45,
            (255, 80, 255),
            1,
            cv2.LINE_AA,
        )

    pointing_label = ""
    reason_label = f"frame {result.frame_index}"
    if pointer_state is not None:
        pointing_label = "  POINTING" if pointer_state.pointing else "  NOT POINTING"
        angle = (
            "-"
            if pointer_state.elbow_angle_degrees is None
            else f"{pointer_state.elbow_angle_degrees:.1f} deg"
        )
        visibility = "/".join(
            "-"
            if joint is None
            else f"{joint.visibility:.2f}"
            for joint in (
                result.right_shoulder,
                result.right_elbow,
                result.right_wrist,
            )
        )
        reason_label = (
            f"Reason: {pointer_state.reason.value}  elbow: {angle}  "
            f"visibility S/E/W: {visibility}"
        )

    raw_label = (
        "null"
        if raw_pointer is None
        else f"({raw_pointer.x:.3f}, {raw_pointer.y:.3f})"
    )
    calibrated_pointer = None if pointer_state is None else pointer_state.pointer
    calibrated_label = (
        "null"
        if calibrated_pointer is None
        else f"({calibrated_pointer.x:.3f}, {calibrated_pointer.y:.3f})"
    )

    cv2.putText(
        canvas,
        f"{result.tracking.value}{pointing_label}  FPS {fps:.1f}",
        (12, 24),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.62,
        color,
        2,
        cv2.LINE_AA,
    )
    cv2.putText(
        canvas,
        reason_label,
        (12, 48),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.43,
        (230, 230, 230),
        1,
        cv2.LINE_AA,
    )
    cv2.putText(
        canvas,
        f"Raw Pointer: {raw_label}  Calibrated Pointer: {calibrated_label}",
        (12, 72),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.43,
        (230, 230, 230),
        1,
        cv2.LINE_AA,
    )
    cv2.putText(
        canvas,
        f"center=({center_x:.3f},{center_y:.3f})  "
        f"gain=({gain_x:.2f},{gain_y:.2f})  C: center  q/esc: quit",
        (12, 96),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.43,
        (230, 230, 230),
        1,
        cv2.LINE_AA,
    )
    return canvas
