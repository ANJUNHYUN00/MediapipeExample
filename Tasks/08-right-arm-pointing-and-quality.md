# Task 08. 오른팔 포인팅 및 추적 품질

> 상태: 대기

## 작업 목적

Task 07의 오른쪽 어깨·팔꿈치·손목 좌표와 visibility를 검증해 `TRACKING`, `PARTIAL`, `LOST`를 결정하고, 유효한 오른팔 방향에서만 정규화 화면 pointer를 계산한다.

## 선행 조건

- Task 07이 완료되어 `PoseTrackingResult`와 카메라 수동 검증이 존재할 것
- [`Plan/07-triage-trace-architecture-and-pose-input.md`](../Plan/07-triage-trace-architecture-and-pose-input.md)를 읽을 것
- [`docs/websocket-protocols.md`](../docs/websocket-protocols.md)의 v2 불변 조건을 읽을 것
- 카메라 미러링과 Unity 화면 좌표 기준을 확정할 것

## 작업 단계

1. visibility 임계값과 최소 팔 벡터 길이를 설정으로 정의한다.
2. 세 관절의 존재, 유한 좌표, visibility를 검사한다.
3. Pose 없음은 `LOST`, 일부 누락·품질 미달은 `PARTIAL`, 모두 유효하면 `TRACKING`으로 만든다.
4. `rightWrist - rightElbow` 2D 방향 벡터를 계산한다.
5. 어깨–팔꿈치와 팔꿈치–손목 길이 비율로 퇴화 벡터를 거부한다.
6. 설정 가능한 연장 계수로 pointer 후보를 계산한다.
7. 경계 밖 pointer의 clamp 또는 무효 정책을 실험 후 하나로 확정한다.
8. tracking과 pointing을 분리하고 실패 시 pointer를 반드시 null로 만든다.
9. 시간 스무딩은 별도 클래스로 두고 tracking 실패를 숨기지 않게 한다.
10. 정상, 부분, 실패, 경계와 퇴화 벡터 단위 테스트를 작성한다.
11. 실제 카메라로 거리·각도·가림·화면 가장자리 조건을 반복 검증한다.
12. 임계값과 알려진 제약을 README와 Task 결과에 기록한다.

## 완료 기준

- 세 tracking 상태가 명시된 불변 조건과 일치한다.
- `pointing=true`는 세 관절과 pointer가 모두 유효할 때만 발생한다.
- `PARTIAL`, `LOST`, 계산 실패에서 `pointing=false`, pointer null이다.
- 경계 정책과 y축·미러링 기준이 테스트로 고정된다.
- 1~2프레임 흔들림을 완화하되 오래된 포인터를 유지하지 않는다.
- 입력은 UI 제어 용도로만 사용되고 의료 판단 로직이 없다.

## 예상 산출물

- `pointing.py`
- `PosePointerState`
- tracking·pointing 설정
- `tests/test_pointing.py`
- 실제 카메라 품질 검증 기록

## 다음 Task와의 연결

검증된 `PosePointerState`를 [`09-pose-v2-publisher-and-unity-receiver.md`](./09-pose-v2-publisher-and-unity-receiver.md)의 v2 메시지 빌더 입력으로 사용한다.
