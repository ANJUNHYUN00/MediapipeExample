# Task 18. Python Pointer Calibration

## Goal

기존 오른팔 포인팅 품질 판정과 pose v2 계약을 유지하면서 raw pointer를 Unity
전체 화면에 대응하도록 center/gain 기반으로 보정한다.

## Scope

- `pointer_center_x`, `pointer_center_y`
- `pointer_gain_x`, `pointer_gain_y`
- 최종 calibrated pointer의 `0.0~1.0` clamp
- CLI 옵션
- 미리보기 `C` 키 세션 center 캘리브레이션
- raw/calibrated 마커와 품질 진단 표시
- 단위 테스트와 실행 문서

## Preserved Behavior

- 오른쪽 어깨 12, 팔꿈치 14, 손목 16 입력
- visibility, 팔 길이·비율, elbow angle 판정
- joint EMA smoothing과 activation frames
- `pointing=false`일 때 `pointer=null`
- `pose_pointer` version 2 JSON 필드와 Unity 수신 코드

## Calibration

```text
calibrated_x = clamp(0.5 + (raw_x - center_x) * gain_x, 0.0, 1.0)
calibrated_y = clamp(0.5 + (raw_y - center_y) * gain_y, 0.0, 1.0)
```

기본 center는 `(0.5, 0.5)`, gain은 `(1.0, 1.0)`이며 기존 출력과 동일하다.
세션 중 `C`를 누르면 현재 유효 raw pointer를 center로 사용한다.

## Acceptance Criteria

- center 입력은 calibrated `(0.5, 0.5)`가 된다.
- gain 증가 시 center 주변 이동 범위가 확대된다.
- 최종 좌표는 항상 `0.0~1.0`으로 clamp된다.
- 기본값은 기존 pointer와 동일하다.
- pointing 실패 시 pointer가 null로 유지된다.
- WebSocket에는 calibrated pointer만 전송된다.
- raw/calibrated pointer가 서로 다른 색으로 보인다.
- 실패 reason, elbow angle, joint visibility, center/gain이 표시된다.
- Unity와 pose v2 fixture를 변경하지 않는다.

## Verification

- `tests/test_pointing.py`: 보정식, clamp, 기본 호환성, null 불변 조건, 세션 center
- `tests/test_pose_debug.py`: raw/calibrated 진단 문자열과 렌더 경로
- `tests/test_app.py`: CLI 기본값과 사용자 지정값
- `tests/test_message_builder.py`: pose v2 fixture 호환성
- 전체 Python pytest와 `pip check`

## Manual Integration Verification

2026-07-28 실제 카메라와 Unity Play Mode 통합 실행에서 다음 설정을 사용했다.

```powershell
Set-Location C:\Projects\MediapipeExample\Mediapipe

.\.venv\Scripts\python.exe -m mediapipe_rps.app `
  --min-elbow-angle 100 `
  --activation-frames 3 `
  --pointer-gain-x 2.0 `
  --pointer-gain-y 1.8
```

Pose 추적, WebSocket, `C` 키 중앙 보정, Unity 포인터, Patient raycast/hover/dwell,
환자 색상, World Space 카드와 ID/state/checked 갱신이 정상임을 확인했다.
포인팅 오작동은 관찰되지 않았다. 세션 center는 저장되지 않으므로 Python을
재실행할 때마다 `TRACKING POINTING` 상태에서 `C`를 눌러 중앙 보정한다.

## Status

완료. 현재 수동 검증 기준은 elbow angle `100`, activation frames `3`, gain
`x=2.0`, `y=1.8`이다. 다른 카메라/사용자 자세에서는 먼저 `C`로 center를 맞추고
gain을 조정한다.
