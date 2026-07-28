from __future__ import annotations

from mediapipe_rps.app import build_parser


def test_pointer_calibration_cli_defaults_preserve_existing_mapping() -> None:
    args = build_parser().parse_args([])

    assert args.pointer_center_x == 0.5
    assert args.pointer_center_y == 0.5
    assert args.pointer_gain_x == 1.0
    assert args.pointer_gain_y == 1.0


def test_pointer_calibration_cli_accepts_axis_values() -> None:
    args = build_parser().parse_args(
        [
            "--pointer-center-x",
            "0.82",
            "--pointer-center-y",
            "0.76",
            "--pointer-gain-x",
            "2.4",
            "--pointer-gain-y",
            "1.8",
        ]
    )

    assert args.pointer_center_x == 0.82
    assert args.pointer_center_y == 0.76
    assert args.pointer_gain_x == 2.4
    assert args.pointer_gain_y == 1.8
