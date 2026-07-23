# Test fixtures

이 디렉터리는 카메라나 개인 영상 없이 반복 테스트할 수 있는 데이터를 보관한다. 실제 카메라에서 얻은 자료는 영상보다 비식별 수치 랜드마크를 우선하며, 출처와 기대 결과를 함께 기록한다.

## 버전 1 메시지 fixture

`messages/`의 모든 파일은 JSON 문법상 유효하다. `invalid` 파일은 파싱 실패가 아니라 **계약 의미 검증 실패**를 시험한다.

| 파일 | 기대 결과 |
|---|---|
| `hand_gesture_v1_rock.json` | 수락, `ROCK` |
| `hand_gesture_v1_scissors.json` | 수락, `SCISSORS` |
| `hand_gesture_v1_paper.json` | 수락, `PAPER` |
| `hand_gesture_v1_unknown.json` | 수락, `UNKNOWN` |
| `hand_gesture_v1_no_hand.json` | 수락, `NO_HAND` 불변 조건 충족 |
| `hand_gesture_v1_extra_field.json` | 수락, 알 수 없는 `source` 필드 무시 |
| `hand_gesture_invalid_version.json` | 거부, 지원하지 않는 `version=2` |
| `hand_gesture_invalid_gesture.json` | 거부, 정의되지 않은 `THUMBS_UP` |

이 fixture는 Python 메시지 생성 테스트와 Unity EditMode 파싱 테스트가 함께 사용해야 한다. 필드명이나 enum을 변경할 때 한쪽 사본을 만들지 말고 이 기준 파일과 양쪽 테스트를 함께 갱신한다.

## 예정된 추가 fixture

- MediaPipe 결과를 모사한 21개 정규화 랜드마크
- 제스처 전환과 손 미검출 안정화 시퀀스
