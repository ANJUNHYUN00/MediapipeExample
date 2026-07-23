# Task 02. 메시지 규격 및 실행 순서 확정

## 작업 목적

Python 게시자와 Unity 수신자가 독립적으로 구현되어도 같은 데이터를 해석하도록 `hand_gesture` 버전 1 JSON 계약을 실행 가능한 규격과 fixture로 고정한다. 개발·운영 시 Python과 Unity의 시작, 재연결, 종료 순서를 문서화해 다음 Task에서 환경과 기능을 일관되게 검증할 수 있게 한다.

이 Task에서는 WebSocket 서버나 Unity 수신기를 구현하지 않는다.

## 선행 조건

- Task 01이 완료되어 Python·Unity 책임과 기본 구조가 확정되어 있을 것
- [`Plan/04-websocket-protocol-and-python-publisher.md`](../Plan/04-websocket-protocol-and-python-publisher.md)를 읽었을 것
- [`Plan/05-unity-receiver-and-ui.md`](../Plan/05-unity-receiver-and-ui.md)를 읽었을 것
- [`docs/project-plan.md`](../docs/project-plan.md)의 WebSocket 필드와 enum을 확인했을 것
- 기존 계약 문서나 JSON fixture가 있으면 먼저 비교하고 단일 기준으로 통합할 것

## 작업 단계

1. 계약의 기준값을 확인하고 변경하지 않는다.

   - 서버: Python
   - 클라이언트: Unity
   - URI: `ws://127.0.0.1:8765`
   - 프레임: UTF-8 JSON 텍스트
   - `type`: `hand_gesture`
   - `version`: `1`
   - 제스처: `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND`
   - handedness: `Left`, `Right`, `Unknown`
   - 최대 전송 빈도: 설정 가능, 기본 목표 10~20Hz 이하

2. `Mediapipe/README.md` 또는 별도 계약 섹션에 아래 필드를 정확히 기록한다.

   | 필드 | JSON 형식 | 필수 | 유효성 |
   |---|---|---:|---|
   | `type` | string | 예 | `hand_gesture` |
   | `version` | integer | 예 | `1` |
   | `timestamp` | integer | 예 | 프레임 캡처 기준 Unix epoch 밀리초 |
   | `sequence` | integer | 예 | 프로세스 수명 동안 전송마다 증가 |
   | `handDetected` | boolean | 예 | 손 검출 여부 |
   | `handedness` | string | 예 | `Left`, `Right`, `Unknown` |
   | `gesture` | string | 예 | 정의된 다섯 값 중 하나 |
   | `confidence` | number | 예 | `0.0` 이상 `1.0` 이하 |
   | `fingerStates` | object | 예 | 다섯 boolean 필드 |

3. 정상 `SCISSORS` 기준 fixture를 만든다.

   ```json
   {
     "type": "hand_gesture",
     "version": 1,
     "timestamp": 1750000000123,
     "sequence": 42,
     "handDetected": true,
     "handedness": "Right",
     "gesture": "SCISSORS",
     "confidence": 0.94,
     "fingerStates": {
       "thumb": true,
       "index": true,
       "middle": true,
       "ring": false,
       "pinky": false
     }
   }
   ```

   fixture 위치는 `Mediapipe/tests/fixtures/messages/hand_gesture_v1_scissors.json`을 기본으로 한다.

4. 최소 계약 fixture 집합을 만든다.

   - `hand_gesture_v1_rock.json`
   - `hand_gesture_v1_scissors.json`
   - `hand_gesture_v1_paper.json`
   - `hand_gesture_v1_unknown.json`
   - `hand_gesture_v1_no_hand.json`
   - `hand_gesture_v1_extra_field.json`
   - `hand_gesture_invalid_version.json`
   - `hand_gesture_invalid_gesture.json`

   아직 테스트 코드가 없다면 fixture만 만들고 기대 결과를 README에 표로 기록한다. 실제 timestamp와 sequence에 의존하지 않도록 고정된 샘플 값을 사용한다.

5. 상태별 불변 조건을 fixture에 반영한다.

   - `NO_HAND`: `handDetected=false`, `handedness="Unknown"`, `gesture="NO_HAND"`, `confidence=0.0`, 다섯 손가락 모두 `false`
   - `UNKNOWN`: `handDetected=true`, `gesture="UNKNOWN"`, 실제 손가락 상태 유지
   - `ROCK`: 검지·중지·약지·소지 `false`
   - `SCISSORS`: 검지·중지 `true`, 약지·소지 `false`
   - `PAPER`: 검지·중지·약지·소지 `true`
   - 엄지는 세 제스처의 필수 분류 조건으로 사용하지 않음

6. Python과 Unity의 이름 매핑을 명시한다.

   - Python 내부 snake_case를 사용할 수 있지만 JSON 출력은 정확한 camelCase로 직렬화한다.
   - Unity DTO는 JSON 키와 일치하는 `handDetected`, `fingerStates` 필드를 사용하거나 직렬화 속성으로 명시 매핑한다.
   - enum 객체의 언어별 기본 문자열 변환에 의존하지 않고 계약 문자열을 명시한다.
   - `timestamp`와 `sequence`는 Unity에서 64비트 정수로 처리한다.

7. 잘못된 입력 처리 원칙을 문서화한다.

   - Python 메시지 빌더는 `NaN`, 무한대, 범위 밖 confidence, 잘못된 상태 조합을 전송하지 않는다.
   - Unity는 잘못된 JSON, 알 수 없는 type, 지원하지 않는 version과 enum을 UI에 적용하지 않는다.
   - Unity는 버전 1의 알 수 없는 추가 필드를 무시한다.
   - 한 메시지 오류로 연결이나 앱 전체를 종료하지 않는다.

8. 표준 실행 순서를 `Mediapipe/README.md`와 `MediapipeUnity/README.md`에 동일하게 기록한다.

   정상 실행:

   1. Python 가상 환경을 활성화한다.
   2. Python MediaPipe 앱을 실행한다.
   3. 카메라 열기와 `127.0.0.1:8765` 서버 시작 로그를 확인한다.
   4. Unity 프로젝트를 열고 Play Mode 또는 데스크톱 빌드를 실행한다.
   5. Unity의 `연결됨` 상태와 메시지 수신을 확인한다.

   Unity 선실행:

   1. Unity를 실행한다.
   2. `연결 중` 또는 `재연결 중` 상태를 확인한다.
   3. Python 앱을 실행한다.
   4. Scene 재시작 없이 Unity가 연결되는지 확인한다.

   정상 종료:

   1. Unity Play Mode 또는 앱을 종료해 수신 작업과 소켓을 정리한다.
   2. Python 앱에 종료 입력을 보내 프레임 루프를 멈춘다.
   3. 카메라, 창, WebSocket 서버와 포트가 해제되었는지 확인한다.

9. 초기 Python 단독 Task의 실행 순서를 별도로 기록한다.

   - Task 04~06에서는 WebSocket이 아직 없으므로 Python 앱만 실행한다.
   - 미리보기 창에서 `q` 또는 `Esc`로 종료한다.
   - Unity 연결 단계는 후속 WebSocket Task가 완료된 뒤 적용한다.

10. 계약 일치 검사를 수행한다.

   - `docs`, `Plan`, Python README, Unity README와 fixture에서 필드 대소문자를 검색한다.
   - `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND` 외의 철자 변형이 없는지 확인한다.
   - 주소가 `ws://127.0.0.1:8765`로 일치하는지 확인한다.
   - JSON 파서로 모든 정상 fixture가 문법적으로 유효한지 검사한다.
   - 변경이 기존 기획 계약과 다르면 사용자 요구 없이 임의 변경하지 말고 문서를 일치시킨다.

## 완료 기준

- 버전 1 필드, 형식, enum, 상태별 불변 조건이 한 문서에 명확히 정리되어 있다.
- 정상 상태 5개와 오류·호환성 사례 fixture가 존재하며 JSON 문법 검사를 통과한다.
- Python 내부 이름과 JSON 이름, Unity DTO 이름의 매핑이 설명되어 있다.
- Python 우선 실행, Unity 우선 실행, 정상 종료, Python 단독 개발 순서가 문서화되어 있다.
- 모든 관련 문서와 fixture에서 URI, 필드명, enum 값이 일치한다.
- 이 Task에서는 WebSocket 서버·수신기 구현이나 제스처 판정 구현을 하지 않았다.

## 예상 산출물

- `Mediapipe/tests/fixtures/messages/*.json`
- 메시지 규격과 실행 순서가 추가된 `Mediapipe/README.md`
- 연결·종료 순서와 Unity 처리 규칙이 추가된 `MediapipeUnity/README.md`
- 필요 시 계약 일치 검사 결과 또는 간단한 JSON fixture 검증 스크립트
- 후속 Python·Unity 테스트가 재사용할 버전 1 샘플 데이터

## 다음 Task와의 연결

고정된 프로젝트 구조와 메시지 계약을 바탕으로 [`03-development-environment-setup.md`](./03-development-environment-setup.md)에서 Python 3.11 가상 환경과 의존성을 설치하고, Unity LTS 프로젝트의 실제 생성 여부와 패키지 호환성을 검증한다. Task 03에서 선택한 정확한 버전은 README와 잠금 가능한 설정에 기록한다.
