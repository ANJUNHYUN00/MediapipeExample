"""Standalone Triage Trace Pose Landmarker debug application."""

from __future__ import annotations

import argparse
import logging
import time
from pathlib import Path
from typing import Sequence

import cv2

from .camera import Camera, CameraError
from .config import CameraConfig, DebugConfig, PoseConfig
from .pose_debug import format_pose_result, render_pose_debug
from .pose_tracker import PoseTracker, PoseTrackerError

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
    debug_config: DebugConfig,
) -> int:
    fps = 0.0
    previous_frame_time = time.monotonic()
    next_console_log_time = 0.0
    previous_state = None
    processed_frames = 0
    state_counts = {"TRACKING": 0, "PARTIAL": 0, "LOST": 0}

    try:
        with Camera(camera_config) as camera, PoseTracker(pose_config) as tracker:
            LOGGER.info("Pose tracking started. Press q or Esc to quit.")
            while True:
                frame = camera.read()
                result = tracker.process(frame)
                processed_frames += 1
                state_counts[result.tracking.value] += 1

                now = time.monotonic()
                fps = _update_fps(fps, now - previous_frame_time)
                previous_frame_time = now

                if result.tracking is not previous_state or now >= next_console_log_time:
                    LOGGER.info("%s", format_pose_result(result))
                    previous_state = result.tracking
                    next_console_log_time = (
                        now + debug_config.console_log_interval_seconds
                    )

                if camera_config.preview_enabled:
                    preview = render_pose_debug(
                        frame.image_bgr,
                        result,
                        mirror_preview=camera_config.mirror_preview,
                        fps=fps,
                    )
                    cv2.imshow(camera_config.window_name, preview)
                    if cv2.waitKey(1) & 0xFF in (ord("q"), 27):
                        break
                if (
                    debug_config.max_frames is not None
                    and processed_frames >= debug_config.max_frames
                ):
                    break
    except KeyboardInterrupt:
        LOGGER.info("Pose tracking stopped by user")
    except (CameraError, PoseTrackerError) as exc:
        LOGGER.error("%s", exc)
        return 1
    finally:
        if camera_config.preview_enabled:
            cv2.destroyAllWindows()

    LOGGER.info(
        "Processed %d frames: TRACKING=%d PARTIAL=%d LOST=%d",
        processed_frames,
        state_counts["TRACKING"],
        state_counts["PARTIAL"],
        state_counts["LOST"],
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
        debug_config = DebugConfig(
            console_log_interval_seconds=args.log_interval,
            max_frames=args.max_frames,
        )
    except ValueError as exc:
        LOGGER.error("Invalid configuration: %s", exc)
        return 2

    return run(camera_config, pose_config, debug_config)


if __name__ == "__main__":
    raise SystemExit(main())
