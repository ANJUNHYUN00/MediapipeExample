# Task 06. 랜드마크 시각화 및 디버그 기능 구현

> 상태: Triage Trace 전환으로 대체됨 (2026-07-24)
>
> 이 문서는 기존 21개 손 랜드마크 시각화 계획을 이력으로 보존한다. 활성 순서는 Task 07 Pose 실행, Task 08 오른팔 포인팅, Task 09 pose v2 연동이다.

## 작업 목적

MediaPipe가 검출한 21개 손 랜드마크와 연결 구조를 OpenCV 미리보기 위에 표시하고, handedness, 검출 신뢰도, FPS와 손 미검출 상태를 함께 보여준다. 제스처 분류 전에 좌표·미러링·좌우 손·검출 품질을 육안으로 검증할 수 있는 디버그 도구를 만든다.

## 선행 조건

- Task 05가 완료되어 `HandTrackingResult`가 안정적으로 생성될 것
- Task 04의 원본 프레임과 표시용 프레임 분리 정책이 구현되어 있을 것
- 21개 MediaPipe 표준 랜드마크 인덱스가 상수 또는 enum으로 정의되어 있을 것
- 미리보기 미러링 여부와 handedness 해석 기준이 README에 기록되어 있을 것
- GUI를 사용할 수 없는 환경에서는 렌더링 함수 단위 테스트로 대체할 수 있을 것

## 작업 단계

1. 디버그 렌더러의 책임과 위치를 정한다.

   - 기본 파일은 `Mediapipe/src/mediapipe_rps/debug_renderer.py`로 한다.
   - 입력은 BGR 프레임 복사본과 `HandTrackingResult`다.
   - 출력은 오버레이가 적용된 새 BGR 프레임이다.
   - 렌더러는 MediaPipe 추론, 손가락 판정, WebSocket 송신을 수행하지 않는다.

2. 손 연결선 상수를 정의한다.

   표준 연결:

   ```text
   Wrist: 0-1, 0-5, 0-17
   Thumb: 1-2, 2-3, 3-4
   Index: 5-6, 6-7, 7-8
   Middle: 9-10, 10-11, 11-12
   Ring: 13-14, 14-15, 15-16
   Pinky: 17-18, 18-19, 19-20
   Palm: 5-9, 9-13, 13-17
   ```

   - 연결선과 인덱스를 매직 넘버로 렌더링 루프 곳곳에 반복하지 않는다.
   - 추후 분류 디버깅을 위해 손가락 그룹별 색상 확장이 가능하게 둔다.

3. 정규화 좌표를 픽셀 좌표로 변환한다.

   ```text
   pixel_x = round(x * (frame_width - 1))
   pixel_y = round(y * (frame_height - 1))
   ```

   - 그리기 직전에만 화면 범위로 제한한다.
   - 원본 `Landmark.x/y/z` 값은 수정하지 않는다.
   - 프레임 크기가 0이거나 이미지가 잘못된 경우 명확한 오류를 반환한다.
   - 미러링된 표시 프레임에 원본 좌표를 그리는 경우 x 좌표도 같은 기준으로 변환한다. 가능하면 검출 프레임과 표시 프레임의 좌표계를 일치시키고 정책을 주석으로 고정한다.

4. 21개 점과 연결선을 그린다.

   - 연결선을 먼저 그리고 랜드마크 점을 그 위에 표시한다.
   - 손목, MCP, TIP 등 주요 점을 색이나 크기로 구분할 수 있다.
   - 화면 밖 랜드마크 때문에 OpenCV 호출이 실패하지 않게 한다.
   - 손이 미검출이면 점과 연결선을 그리지 않는다.
   - 디버그 색상, 점 크기, 선 두께는 설정 또는 렌더러 상수 한곳에서 관리한다.

5. 상태 오버레이를 추가한다.

   최소 표시 항목:

   - `Hand: Left`, `Hand: Right`, `Hand: Unknown`
   - 검출 신뢰도
   - 처리 FPS
   - 현재 frame index
   - 손 미검출 시 `NO HAND`

   아직 제스처 분류 전이므로 `ROCK`, `SCISSORS`, `PAPER`를 추측해서 표시하지 않는다. 후속 Task가 확정 제스처를 선택적 입력으로 추가할 수 있는 확장 지점만 둔다.

6. 디버그 표시 설정을 추가한다.

   ```text
   debug_overlay_enabled: bool = true
   draw_landmark_indices: bool = false
   draw_connections: bool = true
   draw_fps: bool = true
   ```

   - 디버그 오버레이와 미리보기 창 활성화는 별도 설정으로 관리한다.
   - 오버레이를 꺼도 손 추적이 계속 동작해야 한다.
   - 인덱스 표시는 화면이 복잡해지므로 기본적으로 끈다.

7. `app.py`에 렌더러를 연결한다.

   ```text
   captured = camera.read()
   tracking = tracker.process(captured)
   preview = captured.image_bgr.copy()
   preview = apply configured mirror policy
   preview = renderer.render(preview, tracking, fps)
   show preview
   ```

   - 원본 추론 프레임을 오버레이로 수정하지 않는다.
   - 미러링과 좌표 변환 순서를 테스트로 고정한다.
   - 렌더링 오류가 카메라와 트래커 자원 해제를 방해하지 않게 한다.

8. 렌더러 단위 테스트를 작성한다.

   `tests/test_debug_renderer.py`에서 합성 이미지와 랜드마크를 사용해 다음을 검증한다.

   - 21개 좌표가 예상 픽셀 위치로 변환됨
   - 좌상단 `(0,0)`과 우하단 `(1,1)` 경계
   - 화면 밖 좌표 처리
   - 손 없음에서 연결선이 그려지지 않음
   - 렌더링 후 원본 이미지 배열이 변경되지 않음
   - 미러링 정책에 따른 x 좌표
   - 오버레이 on/off
   - 잘못된 프레임 크기 처리

   픽셀 전체의 완전 일치보다 지정 위치 주변에 기대 색상이 존재하는지를 검사해 OpenCV 버전별 글꼴 렌더링 차이에 덜 민감하게 한다.

9. 실제 카메라 수동 검증을 수행한다.

   - 손목부터 다섯 손가락 끝까지 연결선이 자연스럽게 이어지는지 확인한다.
   - 왼손과 오른손 라벨이 미리보기 방향과 일치하는지 확인한다.
   - 손을 화면 모서리로 이동해도 렌더러가 중단되지 않는지 확인한다.
   - 손을 빠르게 넣고 빼 `NO HAND`와 랜드마크 표시가 정상 전환되는지 확인한다.
   - 손바닥과 손등에서 점이 같은 관절을 추적하는지 확인한다.
   - 오버레이를 껐을 때 영상 처리 루프가 계속되는지 확인한다.
   - 디버그 표시 전후 FPS 차이를 기록한다.

10. 문제 진단 기준을 README에 추가한다.

    | 증상 | 우선 확인 |
    |---|---|
    | 점이 손과 좌우 반대로 움직임 | 미러링 순서와 x 좌표 변환 |
    | handedness가 반대 | 검출 프레임 미러링 정책 |
    | 선이 잘못 연결됨 | 연결선 상수와 랜드마크 인덱스 |
    | 가장자리에서 오류 | 픽셀 좌표 제한 |
    | 손은 보이지만 점이 없음 | MediaPipe 임계값과 모델 경로 |
    | FPS 급락 | 인덱스 텍스트, 로그 빈도, 프레임 복사 |

11. 전체 Python 테스트를 실행하고 결과를 기록한다.

    ```powershell
    Set-Location Mediapipe
    .\.venv\Scripts\python.exe -m pytest
    ```

    카메라 수동 테스트의 환경, 성공 조건, 발견한 제약을 별도로 기록한다.

## 완료 기준

- 미리보기에서 한 손의 21개 점과 표준 연결선이 실제 관절 위치에 맞게 표시된다.
- handedness, 검출 신뢰도, FPS, frame index와 손 미검출 상태가 표시된다.
- 원본 추론 프레임과 내부 랜드마크 값이 렌더링으로 변경되지 않는다.
- 미러링된 화면에서도 점과 손 영상의 위치가 일치한다.
- 손 없음, 화면 밖 좌표, 잘못된 프레임이 안전하게 처리된다.
- 디버그 오버레이를 설정으로 끌 수 있고 추적은 계속 동작한다.
- 렌더러 단위 테스트와 전체 Python 테스트가 통과한다.
- 좌우 손, 손바닥/손등, 화면 가장자리 수동 테스트 결과가 기록되어 있다.
- 제스처 분류와 WebSocket 송신은 아직 구현되지 않았다.

## 예상 산출물

- `Mediapipe/src/mediapipe_rps/debug_renderer.py`
- 랜드마크 인덱스와 연결선 상수
- 디버그 설정이 추가된 `Mediapipe/src/mediapipe_rps/config.py`
- 렌더러가 연결된 `Mediapipe/src/mediapipe_rps/app.py`
- `Mediapipe/tests/test_debug_renderer.py`
- 갱신된 `Mediapipe/README.md`
- 랜드마크·미러링·handedness 수동 검증 기록

## 다음 Task와의 연결

이 Task의 시각화 결과로 21개 랜드마크와 좌표계가 올바름을 확인한 뒤, 다음 구현 Task에서는 [`Plan/03-gesture-classification.md`](../Plan/03-gesture-classification.md)에 따라 관절 각도와 손바닥 정규화 거리를 계산한다. 먼저 손가락별 펼침 상태와 `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND` 원시 분류를 구현하고, 이후 프레임 간 안정화와 WebSocket 게시 단계로 연결한다.
