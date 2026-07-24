"""Local WebSocket connection management and latest-state publishing."""

from __future__ import annotations

import asyncio
import logging
import threading
from collections.abc import Iterable
from typing import Any

from websockets.asyncio.server import ServerConnection, serve

from .config import WebSocketConfig
from .message_builder import MAX_SEQUENCE, MessageValidationError, PoseMessageBuilder
from .pose_models import PosePointerState

LOGGER = logging.getLogger(__name__)


class PublisherError(RuntimeError):
    """Raised when the local WebSocket publisher cannot start or stop."""


class PoseWebSocketPublisher:
    """Publish the newest Pose state without blocking the camera loop."""

    def __init__(
        self,
        config: WebSocketConfig,
        *,
        message_builder: PoseMessageBuilder | None = None,
    ) -> None:
        self._config = config
        self._message_builder = message_builder or PoseMessageBuilder()
        self._thread: threading.Thread | None = None
        self._started = threading.Event()
        self._startup_error: BaseException | None = None
        self._loop: asyncio.AbstractEventLoop | None = None
        self._stop_event: asyncio.Event | None = None
        self._queue: asyncio.Queue[PosePointerState] | None = None
        self._clients: set[ServerConnection] = set()
        self._latest_state: PosePointerState | None = None
        self._send_lock: asyncio.Lock | None = None
        self._next_sequence = 0
        self._bound_port: int | None = None

    @property
    def is_running(self) -> bool:
        return self._thread is not None and self._thread.is_alive()

    @property
    def bound_port(self) -> int:
        if self._bound_port is None:
            raise PublisherError("publisher has not started")
        return self._bound_port

    @property
    def uri(self) -> str:
        return f"ws://{self._config.host}:{self.bound_port}"

    def start(self) -> None:
        if self.is_running:
            return
        if not self._config.enabled:
            raise PublisherError("cannot start a disabled publisher")

        self._started.clear()
        self._startup_error = None
        self._thread = threading.Thread(
            target=self._thread_main,
            name="triage-trace-websocket",
            daemon=True,
        )
        self._thread.start()
        if not self._started.wait(self._config.startup_timeout_seconds):
            raise PublisherError("WebSocket publisher startup timed out")
        if self._startup_error is not None:
            error = self._startup_error
            self._thread.join(timeout=self._config.shutdown_timeout_seconds)
            self._thread = None
            raise PublisherError(f"WebSocket publisher failed to start: {error}") from error
        LOGGER.info("Pose WebSocket publisher listening on %s", self.uri)

    def submit(self, state: PosePointerState) -> None:
        loop = self._loop
        if not self.is_running or loop is None:
            raise PublisherError("publisher must be running before submit")
        loop.call_soon_threadsafe(self._offer_state, state)

    def close(self) -> None:
        thread = self._thread
        loop = self._loop
        stop_event = self._stop_event
        if thread is None:
            return
        if loop is not None and stop_event is not None:
            loop.call_soon_threadsafe(stop_event.set)
        thread.join(timeout=self._config.shutdown_timeout_seconds)
        if thread.is_alive():
            raise PublisherError("WebSocket publisher shutdown timed out")
        self._thread = None
        LOGGER.info("Pose WebSocket publisher closed")

    def _thread_main(self) -> None:
        try:
            asyncio.run(self._serve())
        except BaseException as exc:
            self._startup_error = exc
            self._started.set()
            LOGGER.exception("Pose WebSocket publisher stopped unexpectedly")

    async def _serve(self) -> None:
        self._loop = asyncio.get_running_loop()
        self._stop_event = asyncio.Event()
        self._queue = asyncio.Queue(maxsize=1)
        self._send_lock = asyncio.Lock()
        self._next_sequence = 0
        self._latest_state = None
        self._clients.clear()

        async with serve(
            self._handle_client,
            self._config.host,
            self._config.port,
            compression=None,
        ) as server:
            sockets = server.sockets
            if not sockets:
                raise PublisherError("WebSocket server did not bind a socket")
            self._bound_port = int(sockets[0].getsockname()[1])
            publish_task = asyncio.create_task(self._publish_loop())
            self._started.set()
            try:
                await self._stop_event.wait()
            finally:
                publish_task.cancel()
                await asyncio.gather(publish_task, return_exceptions=True)

        self._clients.clear()
        self._queue = None
        self._stop_event = None
        self._loop = None

    def _offer_state(self, state: PosePointerState) -> None:
        queue = self._queue
        if queue is None:
            return
        self._latest_state = state
        if queue.full():
            try:
                queue.get_nowait()
            except asyncio.QueueEmpty:
                pass
        queue.put_nowait(state)

    async def _handle_client(self, connection: ServerConnection) -> None:
        self._clients.add(connection)
        LOGGER.info("Unity client connected; clients=%d", len(self._clients))
        try:
            if self._latest_state is not None:
                await self._send_state(self._latest_state, (connection,))
            await connection.wait_closed()
        finally:
            self._clients.discard(connection)
            LOGGER.info("Unity client disconnected; clients=%d", len(self._clients))

    async def _publish_loop(self) -> None:
        queue = self._queue
        if queue is None:
            return
        interval = 1.0 / self._config.publish_hz
        next_send_time = 0.0

        while True:
            state = await queue.get()
            now = asyncio.get_running_loop().time()
            if now < next_send_time:
                await asyncio.sleep(next_send_time - now)
            while not queue.empty():
                try:
                    state = queue.get_nowait()
                except asyncio.QueueEmpty:
                    break
            if self._clients:
                await self._send_state(state, tuple(self._clients))
                next_send_time = asyncio.get_running_loop().time() + interval

    async def _send_state(
        self,
        state: PosePointerState,
        connections: Iterable[ServerConnection],
    ) -> None:
        send_lock = self._send_lock
        if send_lock is None:
            return
        async with send_lock:
            try:
                message = self._message_builder.build(
                    state,
                    sequence=self._next_sequence,
                ).to_json()
            except MessageValidationError as exc:
                LOGGER.warning("Dropped invalid Pose state: %s", exc)
                return
            targets = tuple(connections)
            if not targets:
                return
            results = await asyncio.gather(
                *(connection.send(message) for connection in targets),
                return_exceptions=True,
            )
            successful = False
            for connection, result in zip(targets, results, strict=True):
                if isinstance(result, BaseException):
                    self._clients.discard(connection)
                    LOGGER.warning("WebSocket send failed: %s", result)
                else:
                    successful = True
            if successful:
                if self._next_sequence >= MAX_SEQUENCE:
                    raise PublisherError("WebSocket sequence exhausted")
                self._next_sequence += 1

    def __enter__(self) -> PoseWebSocketPublisher:
        self.start()
        return self

    def __exit__(self, exc_type: object, exc_value: object, traceback: object) -> None:
        self.close()
