# Task 05. MediaPipe 손 랜드마크 검출 구현

## 작업 목적

웹캠의 BGR 프레임을 MediaPipe 연속 영상 손 추적기에 전달해 한 손의 21개 정규화 랜드마크, handedness 및 검출 신뢰도를 추출한다. MediaPipe 라이브러리 객체를 후속 제스처 코드에 노출하지 않고 프로젝트 내부 `HandTrackingResult`로 변환한다.

이 Task에서는 손가락 펼침 여부와 가위바위보 제스처를 판정하지 않는다.

## 선행 조건

- Task 04의 카메라 루프와 `CapturedFrame`이 동작할 것
- Task 03에서 사용할 MediaPipe API, 패키지 버전, 모델 파일 경로가 확정되어 있을 것
- 기본 모델 경로가 `Mediapipe/models/hand_landmarker.task`라면 파일 존재와 출처가 확인되어 있을 것
- [`Plan/02-python-hand-tracking.md`](../Plan/02-python-hand-tracking.md)의 21개 인덱스와 handedness 정책을 읽었을 것
- 미러링 전후 어느 프레임을 검출에 사용할지 명시적으로 결정할 것

## 작업 단계

1. 내부 데이터 모델을 `models.py`에 구현한다.

   ```text
   Landmark:
     x: float
     y: float
     z: float

   Handedness:
     Left
     Right
     Unknown

   HandTrackingResult:
     hand_detected: bool
     landmarks: tuple/list[Landmark]
     handedness: Handedness
     detection_confidence: float
     frame_timestamp_ms: int
     frame_index: int
   ```

   불변 조건:

   - 손 검출 시 랜드마크는 정확히 21개
   - 미검출 시 빈 랜드마크, `Unknown`, 신뢰도 `0.0`
   - 신뢰도는 `0.0~1.0`
   - timestamp와 frame index는 입력 프레임에서 그대로 보존

2. MediaPipe 관련 설정을 `config.py`에 추가한다.

   ```text
   model_path
   num_hands = 1
   min_hand_detection_confidence
   min_hand_presence_confidence
   min_tracking_confidence
   detection_frame_mirrored: bool
   ```

   임계값은 한곳에서 설정하며, 실제 초기값과 조정 근거를 README에 기록한다.

3. `hand_tracker.py`에 `HandTracker` 수명 주기를 구현한다.

   권장 경계:

   ```text
   HandTracker.open() -> None
   HandTracker.process(CapturedFrame) -> HandTrackingResult
   HandTracker.close() -> None
   ```

   - 트래커 객체를 매 프레임 재생성하지 않는다.
   - 선택한 MediaPipe 연속 영상 모드를 사용한다.
   - 모델 파일 없음, 잘못된 옵션, 초기화 실패에 명확한 예외를 제공한다.
   - `close()`는 중복 호출에도 안전하게 만든다.

4. OpenCV 프레임을 MediaPipe 입력으로 변환한다.

   - BGR을 RGB로 변환한다.
   - 선택한 MediaPipe API가 요구하는 이미지 객체 또는 배열 형식으로 감싼다.
   - 원본 `CapturedFrame.image_bgr`를 수정하지 않는다.
   - 영상 모드 API가 밀리초 타임스탬프의 단조 증가를 요구하면 입력 timestamp 또는 별도 monotonic timestamp를 사용하고 같은 값이 반복되지 않게 한다.
   - API에 전달한 timestamp와 외부 Unix timestamp의 역할 차이를 코드 주석으로 설명한다.

5. 한 손 결과를 선택한다.

   - `num_hands=1`을 설정한다.
   - API가 여러 결과를 반환해도 신뢰도가 가장 높은 한 손을 선택한다.
   - handedness 점수와 손 랜드마크 목록의 인덱스 대응을 확인한다.
   - 결과가 없으면 예외 대신 정상적인 미검출 결과를 반환한다.

6. 21개 랜드마크를 내부 모델로 변환한다.

   - `x`, `y`, `z`를 Python float로 복사한다.
   - 정확히 21개인지 검증한다.
   - `NaN`이나 무한대가 있으면 손상 결과로 처리하고 경고를 제한적으로 기록한다.
   - 화면 밖 좌표가 약간 발생할 수 있으므로 무조건 `0~1`로 잘라 원본 정보를 왜곡하지 않는다.
   - MediaPipe 고유 Landmark 객체를 반환하지 않는다.

7. handedness를 정규화한다.

   - `Left`, `Right`만 프로젝트 enum으로 매핑한다.
   - 값 누락, 빈 값, 알 수 없는 문자열은 `Unknown`으로 처리한다.
   - 미러링 정책에 따른 의미를 좌우 손 수동 테스트로 확인한다.
   - handedness 신뢰도를 최종 제스처 confidence로 오해하지 않는다.

8. `app.py` 처리 루프에 트래커를 조립한다.

   ```text
   open camera
   open hand tracker
   for each captured frame:
       tracking_result = tracker.process(frame)
       update minimal debug status
       show preview
   finally:
       close tracker
       close camera
       destroy windows
   ```

   네트워크 호출과 제스처 판정은 추가하지 않는다.

9. 단위 테스트를 작성한다.

   `tests/test_hand_tracker.py`에서 MediaPipe 결과 어댑터를 모사해 다음을 검증한다.

   - 21개 좌표의 정확한 복사
   - timestamp와 frame index 보존
   - `Left`, `Right`, `Unknown` 매핑
   - 손 없음 결과
   - 랜드마크 수가 21개가 아닌 결과
   - `NaN` 또는 무한대 좌표
   - 여러 손 후보의 최고 신뢰도 선택
   - 중복 close 안전성

   실제 모델과 카메라가 필요한 테스트는 별도 marker 또는 수동 테스트로 분리한다.

10. 실제 카메라 수동 테스트를 수행한다.

    - 손이 없을 때 앱이 계속 실행되고 미검출 상태를 출력하는지 확인한다.
    - 오른손과 왼손을 번갈아 보여 handedness를 확인한다.
    - 손바닥과 손등, 화면 중앙과 가장자리, 가까운 거리와 먼 거리에서 21개 점 검출 여부를 확인한다.
    - 손을 빠르게 빼고 다시 넣어 추적이 복구되는지 확인한다.
    - 종료 후 카메라와 MediaPipe 자원이 해제되는지 확인한다.

11. 성능과 로그를 점검한다.

    - 정상 프레임마다 전체 랜드마크를 로그로 출력하지 않는다.
    - 주기적인 FPS와 손 검출 상태 변경만 로그로 남긴다.
    - Task 04 카메라 전용 FPS와 비교해 MediaPipe 처리 비용을 기록한다.

12. README와 검증 결과를 갱신한다.

    - 모델 자산 경로와 출처
    - MediaPipe API 모드와 핵심 임계값
    - 미러링 및 handedness 기준
    - 실행 명령
    - 자동 테스트 명령
    - 실제 좌우 손 수동 검증 결과

## 완료 기준

- 카메라 프레임에서 한 손의 정확히 21개 랜드마크를 추출한다.
- 손 검출 결과가 MediaPipe 객체가 아닌 `HandTrackingResult`로 반환된다.
- 손이 없을 때 빈 랜드마크, `Unknown`, 신뢰도 `0.0`의 정상 상태가 반환된다.
- timestamp와 frame index가 카메라 입력에서 보존된다.
- 좌우 손 handedness가 문서화된 미러링 기준과 일치한다.
- 잘못된 랜드마크 수와 비유한 좌표가 후속 단계로 전달되지 않는다.
- 트래커가 프레임마다 재생성되지 않고 종료 시 정리된다.
- 단위 테스트와 실제 카메라 수동 테스트 결과가 기록되어 있다.
- 손가락 상태, 제스처 분류, WebSocket 송신은 구현되지 않았다.

## 예상 산출물

- `Mediapipe/src/mediapipe_rps/models.py`의 랜드마크·추적 모델
- `Mediapipe/src/mediapipe_rps/hand_tracker.py`
- MediaPipe 설정이 추가된 `Mediapipe/src/mediapipe_rps/config.py`
- 트래커가 조립된 `Mediapipe/src/mediapipe_rps/app.py`
- `Mediapipe/tests/test_hand_tracker.py`
- 모델 자산 또는 재현 가능한 모델 배치 안내
- 좌우 손 및 검출 조건 수동 테스트 기록

## 다음 Task와의 연결

`HandTrackingResult.landmarks`, handedness, 검출 신뢰도를 [`06-landmark-visualization-and-debug.md`](./06-landmark-visualization-and-debug.md)의 디버그 렌더러에 전달한다. Task 06에서는 처리용 랜드마크를 변경하지 않고 미리보기 복사본 위에 점, 연결선, 상태와 FPS를 표시해 Task 05 결과를 사람이 확인할 수 있게 한다.
