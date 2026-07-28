"""Standalone Triage Trace Pose Landmarker debug application."""

from __future__ import annotations

import argparse
import logging
import time
from contextlib import nullcontext
from pathlib import Path
from typing import Sequence

import cv2

from .camera import Camera, CameraError
from .config import (
    CameraConfig,
    DebugConfig,
    PointingConfig,
    PoseConfig,
    WebSocketConfig,
)
from .pointing import PointingPipeline
from .pose_debug import format_pointer_state, render_pose_debug
from .pose_tracker import PoseTracker, PoseTrackerError
from .websocket_server import PoseWebSocketPublisher, PublisherError

LOGGER = logging.getLogger(__name__)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description=(
            "Track right shoulder, elbow, and wrist for the non-medical "
            "Triage Trace simulation."
        )
    )
    parser.add_argument("--camera-index", type=int, default=0)
    parser.add_argument("--width", type=int)
    parser.add_argument("--height", type=int)
    parser.add_argument("--model", type=Path)
    parser.add_argument("--visibility-threshold", type=float, default=0.5)
    parser.add_argument("--min-elbow-angle", type=float, default=150.0)
    parser.add_argument("--pointer-extension", type=float, default=0.25)
    parser.add_argument("--smoothing-alpha", type=float, default=0.35)
    parser.add_argument("--activation-frames", type=int, default=2)
    parser.add_argument("--pointer-center-x", type=float, default=0.5)
    parser.add_argument("--pointer-center-y", type=float, default=0.5)
    parser.add_argument("--pointer-gain-x", type=float, default=1.0)
    parser.add_argument("--pointer-gain-y", type=float, default=1.0)
    parser.add_argument("--websocket-host", default="127.0.0.1")
    parser.add_argument("--websocket-port", type=int, default=8765)
    parser.add_argument("--publish-hz", type=float, default=15.0)
    parser.add_argument(
        "--no-websocket",
        action="store_true",
        help="disable pose v2 publishing for standalone camera diagnostics",
    )
    parser.add_argument("--log-interval", type=float, default=1.0)
    parser.add_argument(
        "--max-frames",
        type=int,
        help="stop after a fixed number of frames (useful for smoke tests)",
    )
    parser.add_argument(
        "--no-preview",
        action="store_true",
        help="disable the OpenCV window and report coordinates in the console only",
    )
    parser.add_argument(
        "--no-mirror",
        action="store_true",
        help="show the preview without horizontal mirroring",
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="enable detailed runtime logging",
    )
    return parser


def _update_fps(previous_fps: float, elapsed_seconds: float) -> float:
    if elapsed_seconds <= 0:
        return previous_fps
    instantaneous = 1.0 / elapsed_seconds
    if previous_fps == 0.0:
        return instantaneous
    return previous_fps * 0.9 + instantaneous * 0.1


def run(
    camera_config: CameraConfig,
    pose_config: PoseConfig,
    pointing_config: PointingConfig,
    websocket_config: WebSocketConfig,
    debug_config: DebugConfig,
) -> int:
    fps = 0.0
    previous_frame_time = time.monotonic()
    next_console_log_time = 0.0
    previous_state = None
    processed_frames = 0
    state_counts = {"TRACKING": 0, "PARTIAL": 0, "LOST": 0}
    pointing_frames = 0
    pointing_pipeline = PointingPipeline(pointing_config)
    publisher_context = (
        PoseWebSocketPublisher(websocket_config)
        if websocket_config.enabled
        else nullcontext(None)
    )

    try:
        with (
            publisher_context as publisher,
            Camera(camera_config) as camera,
            PoseTracker(pose_config) as tracker,
        ):
            LOGGER.info(
                "Pose tracking started. Press C to center; q or Esc to quit."
            )
            while True:
                frame = camera.read()
                result = tracker.process(frame)
                frame_height, frame_width = frame.image_bgr.shape[:2]
                pointer_state = pointing_pipeline.update(
                    result,
                    image_aspect_ratio=frame_width / frame_height,
                )
                processed_frames += 1
                state_counts[result.tracking.value] += 1
                if pointer_state.pointing:
                    pointing_frames += 1
                if publisher is not None:
                    publisher.submit(pointer_state)

                now = time.monotonic()
                fps = _update_fps(fps, now - previous_frame_time)
                previous_frame_time = now

                current_state = (
                    pointer_state.tracking,
                    pointer_state.pointing,
                    pointer_state.reason,
                )
                if current_state != previous_state or now >= next_console_log_time:
                    calibration = pointing_pipeline.calibration
                    LOGGER.info(
                        "%s",
                        format_pointer_state(
                            pointer_state,
                            raw_pointer=pointing_pipeline.raw_pointer,
                            center_x=calibration.center_x,
                            center_y=calibration.center_y,
                            gain_x=calibration.gain_x,
                            gain_y=calibration.gain_y,
                        ),
                    )
                    previous_state = current_state
                    next_console_log_time = (
                        now + debug_config.console_log_interval_seconds
                    )

                if camera_config.preview_enabled:
                    preview = render_pose_debug(
                        frame.image_bgr,
                        pointer_state.pose,
                        mirror_preview=camera_config.mirror_preview,
                        fps=fps,
                        pointer_state=pointer_state,
                        raw_pointer=pointing_pipeline.raw_pointer,
                        center_x=pointing_pipeline.calibration.center_x,
                        center_y=pointing_pipeline.calibration.center_y,
                        gain_x=pointing_pipeline.calibration.gain_x,
                        gain_y=pointing_pipeline.calibration.gain_y,
                    )
                    cv2.imshow(camera_config.window_name, preview)
                    key = cv2.waitKey(1) & 0xFF
                    if key in (ord("q"), 27):
                        break
                    if key in (ord("c"), ord("C")):
                        if pointing_pipeline.calibrate_center_from_current_raw_pointer():
                            calibration = pointing_pipeline.calibration
                            LOGGER.info(
                                "Session pointer center set to x=%.3f y=%.3f",
                                calibration.center_x,
                                calibration.center_y,
                            )
                        else:
                            LOGGER.warning(
                                "Center calibration ignored: "
                                "no valid raw pointer in the current frame"
                            )
                if (
                    debug_config.max_frames is not None
                    and processed_frames >= debug_config.max_frames
                ):
                    break
    except KeyboardInterrupt:
        LOGGER.info("Pose tracking stopped by user")
    except (CameraError, PoseTrackerError, PublisherError) as exc:
        LOGGER.error("%s", exc)
        return 1
    finally:
        if camera_config.preview_enabled:
            cv2.destroyAllWindows()

    LOGGER.info(
        "Processed %d frames: TRACKING=%d PARTIAL=%d LOST=%d POINTING=%d",
        processed_frames,
        state_counts["TRACKING"],
        state_counts["PARTIAL"],
        state_counts["LOST"],
        pointing_frames,
    )
    return 0


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    logging.basicConfig(
        level=logging.DEBUG if args.verbose else logging.INFO,
        format="%(asctime)s %(levelname)s %(name)s: %(message)s",
    )

    try:
        camera_config = CameraConfig(
            device_index=args.camera_index,
            width=args.width,
            height=args.height,
            preview_enabled=not args.no_preview,
            mirror_preview=not args.no_mirror,
        )
        default_pose_config = PoseConfig()
        pose_config = PoseConfig(
            model_path=args.model or default_pose_config.model_path,
            min_right_arm_visibility=args.visibility_threshold,
        )
        pointing_config = PointingConfig(
            min_joint_visibility=args.visibility_threshold,
            min_elbow_angle_degrees=args.min_elbow_angle,
            pointer_extension_factor=args.pointer_extension,
            smoothing_alpha=args.smoothing_alpha,
            activation_frames=args.activation_frames,
            pointer_center_x=args.pointer_center_x,
            pointer_center_y=args.pointer_center_y,
            pointer_gain_x=args.pointer_gain_x,
            pointer_gain_y=args.pointer_gain_y,
        )
        websocket_config = WebSocketConfig(
            enabled=not args.no_websocket,
            host=args.websocket_host,
            port=args.websocket_port,
            publish_hz=args.publish_hz,
        )
        debug_config = DebugConfig(
            console_log_interval_seconds=args.log_interval,
            max_frames=args.max_frames,
        )
    except ValueError as exc:
        LOGGER.error("Invalid configuration: %s", exc)
        return 2

    return run(
        camera_config,
        pose_config,
        pointing_config,
        websocket_config,
        debug_config,
    )


if __name__ == "__main__":
    raise SystemExit(main())
