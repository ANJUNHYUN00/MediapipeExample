"""Camera-side data models shared by runtime components."""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
from numpy.typing import NDArray


@dataclass(frozen=True, slots=True)
class CapturedFrame:
    """One BGR camera frame and its capture metadata."""

    image_bgr: NDArray[np.uint8]
    frame_index: int
    captured_at_ms: int

    def __post_init__(self) -> None:
        if self.frame_index < 0:
            raise ValueError("frame_index must be zero or greater")
        if self.captured_at_ms < 0:
            raise ValueError("captured_at_ms must be zero or greater")
        if self.image_bgr.ndim != 3 or self.image_bgr.shape[2] != 3:
            raise ValueError("image_bgr must have shape (height, width, 3)")
        if self.image_bgr.size == 0:
            raise ValueError("image_bgr must not be empty")
