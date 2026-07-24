# Test fixtures

이 디렉터리는 카메라나 개인 영상 없이 Python과 Unity의 계약을 반복 검증할 합성 JSON을 보관한다. 실제 카메라 영상과 개인 식별 데이터는 fixture에 넣지 않는다.

## 보존된 gesture v1 fixture

기존 `hand_gesture` version 1 파일과 의미는 Triage Trace 전환 후에도 변경하지 않는다.

| 파일 | 기대 결과 |
|---|---|
| `hand_gesture_v1_rock.json` | 수락, `ROCK` |
| `hand_gesture_v1_scissors.json` | 수락, `SCISSORS` |
| `hand_gesture_v1_paper.json` | 수락, `PAPER` |
| `hand_gesture_v1_unknown.json` | 수락, `UNKNOWN` |
| `hand_gesture_v1_no_hand.json` | 수락, `NO_HAND` |
| `hand_gesture_v1_extra_field.json` | 수락, 추가 필드 무시 |
| `hand_gesture_invalid_version.json` | 거부, 지원하지 않는 버전 |
| `hand_gesture_invalid_gesture.json` | 거부, 정의되지 않은 gesture |

## Pose pointer v2 fixture

| 파일 | 시나리오 | 기대 결과 |
|---|---|---|
| `pose_pointer_v2_tracking.json` | 세 관절 정상, pointer 유효 | 수락, 포인터 활성 가능 |
| `pose_pointer_v2_lost.json` | Pose 추적 실패 | 수락, 포인터 비활성 |
| `pose_pointer_v2_partial.json` | 오른쪽 손목 좌표 누락 | 수락, 포인터 비활성 |

v2 의미 검증:

- `TRACKING`은 세 관절 좌표가 모두 존재한다.
- `PARTIAL`, `LOST`는 `pointing=false`, `pointer=null`이다.
- `LOST`는 세 관절이 모두 null이고 visibility가 모두 0이다.
- 누락 관절의 visibility는 0이다.
- visibility는 `0.0~1.0`이다.

정확한 계약은 [`docs/websocket-protocols.md`](../../../docs/websocket-protocols.md)를 따른다. Python 메시지 테스트와 Unity EditMode 테스트는 이 파일들을 공유한다.
