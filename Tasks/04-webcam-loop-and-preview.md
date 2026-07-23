# Task 04. 웹캠 루프 및 미리보기 구현

## 작업 목적

OpenCV로 설정된 웹캠을 열고 프레임을 지속적으로 읽어 미리보기 창에 표시하는 독립적인 카메라 계층을 구현한다. 카메라 실패, 사용자 종료, 예외 상황에서도 장치와 창이 확실히 정리되도록 해 MediaPipe 추적 루프의 안정적인 입력을 만든다.

## 선행 조건

- Task 03이 완료되어 Python 3.11 가상 환경과 OpenCV import가 검증되어 있을 것
- `Mediapipe/src/mediapipe_rps/camera.py`, `config.py`, `app.py`의 책임이 확정되어 있을 것
- 사용할 기본 카메라 인덱스가 `0`임을 확인할 것
- 다른 프로그램이 카메라를 점유하고 있지 않을 것
- 카메라가 없는 자동 테스트 환경을 위해 캡처 객체를 대체할 수 있는 구조를 사용할 것

## 작업 단계

1. 카메라 설정 모델을 `config.py`에 구현한다.

   최소 설정:

   ```text
   camera_index: int = 0
   frame_width: int | None
   frame_height: int | None
   mirror_preview: bool = true
   preview_enabled: bool = true
   preview_window_name: str = "MediaPipe RPS"
   max_consecutive_read_failures: int
   ```

   - 환경 변수나 명령행 인자를 추가한다면 기본값과 우선순위를 README에 기록한다.
   - 설정값을 `camera.py`와 `app.py`에 중복 하드코딩하지 않는다.

2. `camera.py`에 카메라 추상 경계를 구현한다.

   권장 인터페이스:

   ```text
   CameraCapture.open() -> None
   CameraCapture.read() -> CapturedFrame
   CameraCapture.close() -> None
   CameraCapture.is_open: bool
   ```

   `CapturedFrame`은 최소한 다음을 가진다.

   ```text
   image_bgr
   timestamp_ms
   frame_index
   ```

   타임스탬프는 프레임을 성공적으로 읽은 직후 Unix epoch 밀리초로 만든다.

3. `cv2.VideoCapture(camera_index)`를 열고 상태를 검증한다.

   - `isOpened()`가 `false`이면 카메라 인덱스를 포함한 명확한 예외를 발생시킨다.
   - 너비와 높이가 설정된 경우 `CAP_PROP_FRAME_WIDTH`, `CAP_PROP_FRAME_HEIGHT`를 요청한다.
   - 실제 적용된 너비, 높이, 가능한 경우 FPS를 읽어 시작 로그에 남긴다.
   - 카메라 객체가 열린 뒤 예외가 나도 `release()`가 호출되도록 한다.

4. 프레임 읽기를 구현한다.

   - `read()`가 반환한 성공 여부와 프레임의 `None` 여부를 모두 검사한다.
   - 성공 시 연속 실패 카운터를 초기화하고 프레임 번호를 증가시킨다.
   - 일시적 실패는 제한된 경고와 함께 재시도한다.
   - 설정된 연속 실패 한도를 넘으면 무한 반복하지 않고 진단 가능한 오류로 루프를 종료한다.

5. 미리보기 변환을 별도 함수로 둔다.

   - `mirror_preview=true`이면 표시용 복사본만 수평 반전한다.
   - Task 05의 MediaPipe 입력에 원본과 반전 영상 중 무엇을 사용할지 아직 섞지 않는다.
   - 표시용 텍스트가 필요하면 현재 프레임 번호와 FPS 정도만 추가한다.
   - 원본 캡처 프레임을 미리보기 오버레이로 직접 오염시키지 않는다.

6. `app.py`에 독립 실행 루프를 구현한다.

   ```text
   load config
   open camera
   while running:
       read frame
       render preview if enabled
       read keyboard
       stop on q or Esc
   finally:
       close camera
       destroy preview windows
   ```

   - `q`, `Q`, `Esc` 중 문서화한 키로 종료한다.
   - `KeyboardInterrupt`도 정상 종료 경로로 처리한다.
   - 카메라와 모든 OpenCV 창 정리는 `finally`에서 보장한다.

7. FPS 측정을 구현한다.

   - 매 프레임 즉시 로그를 남기지 않는다.
   - 최근 구간 또는 누적 구간의 FPS를 계산해 미리보기에 표시한다.
   - 0으로 나누는 경우와 첫 프레임을 처리한다.
   - FPS 측정은 손 추적 성능 기준의 기초값으로만 사용한다.

8. 자동 테스트를 작성한다.

   `tests/test_camera.py`에서 가짜 `VideoCapture`를 주입하거나 생성 함수를 대체해 다음을 검증한다.

   - 카메라 열기 성공과 실패
   - 설정된 해상도 요청
   - 성공 프레임의 timestamp와 증가 frame index
   - `read()` 실패와 연속 실패 한도
   - `close()`가 한 번 이상 호출되어도 안전한지
   - 컨텍스트 관리자 방식을 사용한다면 예외 중에도 release되는지

   실제 카메라와 GUI는 단위 테스트의 필수 조건으로 만들지 않는다.

9. 수동 검증을 수행한다.

   ```powershell
   Set-Location Mediapipe
   .\.venv\Scripts\python.exe -m mediapipe_rps.app
   ```

   다음을 확인한다.

   - 올바른 카메라 화면이 표시됨
   - 미러링 설정이 예상대로 동작함
   - 프레임이 지속 갱신되고 FPS가 비정상적으로 0에 머물지 않음
   - `q` 또는 `Esc`로 창과 프로세스가 종료됨
   - 종료 후 다른 앱이 카메라를 즉시 열 수 있음
   - 잘못된 카메라 인덱스에서 명확한 오류 후 종료됨

10. README를 갱신한다.

    - 실행 명령
    - 카메라 인덱스 변경 방법
    - 종료 키
    - 미러링의 의미
    - 카메라 점유와 GUI 미지원 환경의 문제 해결
    - 수동 검증 결과

## 완료 기준

- Python 앱이 기본 카메라를 열고 실시간 미리보기를 표시한다.
- 프레임마다 캡처 시각과 순서 번호가 생성된다.
- 표시용 미러링이 원본 처리 프레임과 명확히 분리되어 있다.
- `q`, `Esc`, `KeyboardInterrupt`, 예외 경로에서 카메라와 창이 정리된다.
- 카메라 열기 실패와 연속 프레임 읽기 실패가 무한 루프 없이 명확히 보고된다.
- 가짜 캡처 객체를 사용한 카메라 단위 테스트가 통과한다.
- 실제 카메라 수동 검증 결과와 환경이 기록되어 있다.
- MediaPipe 검출, 제스처 분류, WebSocket 코드는 아직 카메라 모듈에 포함되지 않았다.

## 예상 산출물

- `Mediapipe/src/mediapipe_rps/config.py`
- `Mediapipe/src/mediapipe_rps/camera.py`
- `Mediapipe/src/mediapipe_rps/app.py`
- `Mediapipe/src/mediapipe_rps/models.py`의 `CapturedFrame` 모델
- `Mediapipe/tests/test_camera.py`
- 실행·종료·문제 해결 내용이 추가된 `Mediapipe/README.md`
- 실제 카메라 미리보기 수동 검증 기록

## 다음 Task와의 연결

`CapturedFrame.image_bgr`, `timestamp_ms`, `frame_index`를 [`05-mediapipe-hand-landmark-detection.md`](./05-mediapipe-hand-landmark-detection.md)의 손 추적기에 전달한다. Task 05는 BGR 프레임을 MediaPipe 입력으로 변환하고 한 손의 21개 랜드마크를 내부 `HandTrackingResult`로 반환하며, 카메라 수명 주기는 변경하지 않는다.
