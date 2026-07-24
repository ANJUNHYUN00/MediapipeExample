# Triage Trace 프로젝트 기획서

## 1. 프로젝트 개요

Triage Trace는 웹캠 기반 MediaPipe Pose Landmarker 입력으로 Unity AR 화면의 모의 포인터를 조작하는 교육·시연용 시뮬레이션이다. Python이 오른쪽 어깨, 팔꿈치, 손목의 자세 정보를 처리해 WebSocket으로 보내고, Unity가 이를 가상의 시나리오 선택 인터페이스에 표시한다.

> **안전 고지:** Triage Trace는 실제 환자 평가, 응급도 분류, 진단, 치료 추천 또는 의료 자문을 수행하지 않는다. 화면의 모든 상태와 선택은 가상 데이터에 대한 모의 인터페이스이며 실제 의료 의사결정에 사용해서는 안 된다.

## 2. 전환 목표

- 기존 가위바위보 실습의 Python 서버 / Unity 클라이언트 구조를 유지한다.
- MediaPipe Hands 중심 처리 흐름을 Pose Landmarker 중심으로 전환한다.
- 오른쪽 어깨(12), 팔꿈치(14), 손목(16)을 MVP 입력으로 사용한다.
- 오른팔 방향을 정규화된 화면 포인터로 변환한다.
- 추적 품질과 입력 유효성을 명시적으로 전달한다.
- Unity에서 포인터와 모의 시나리오 UI를 표시한다.
- 기존 `hand_gesture` version 1 계약과 fixture는 변경 없이 보존한다.
- 새 데이터는 `pose_pointer` version 2 계약으로 분리한다.

## 3. 기존 완료 내용과 재사용 범위

| 자산 | 상태 | Triage Trace에서의 사용 |
|---|---|---|
| Python 3.11.9 가상환경과 의존성 | Task 03 완료 | OpenCV, MediaPipe, websockets, pytest 재사용 |
| Unity 6000.3.10f1 프로젝트 | Task 03 완료 | 같은 Unity 프로젝트와 책임별 폴더 재사용 |
| Python 서버 / Unity 클라이언트 책임 분리 | Task 01 완료 | 방향과 수명 주기 유지 |
| `ws://127.0.0.1:8765` 로컬 통신 기준 | Task 02 완료 | v2에서도 기본 URI 유지 |
| 최신 상태 우선 큐, 재연결, 메인 스레드 전달 설계 | 문서화 완료 | Pose 상태 수신에 재사용 |
| `hand_gesture` v1 문서와 fixture | Task 02 완료 | 레거시 호환 계약으로 동결 |
| Hand Landmarker 모델 | 설치·검증 완료 | 삭제하지 않지만 활성 Pose 경로에서는 사용하지 않음 |
| 실제 추적·WebSocket·Unity UI 코드 | 미구현 | Pose 기준으로 새로 구현 |

상세 전환 결정은 [`transition-plan.md`](./transition-plan.md)에 기록한다.

## 4. MVP 범위

### 포함

- 한 명의 상반신 Pose Landmarker 추적
- 오른쪽 어깨, 팔꿈치, 손목 좌표와 visibility 추출
- `TRACKING`, `PARTIAL`, `LOST` 상태 구분
- 오른팔 방향 기반 정규화 포인터 계산
- Python WebSocket 서버의 pose v2 게시
- Unity 클라이언트의 v2 검증과 최신 상태 처리
- Unity AR 모의 포인터와 비의료 안전 고지
- 연결 끊김, 데이터 만료, 불완전 좌표의 안전한 비활성화

### 제외

- 실제 의료 판단 또는 환자 우선순위 산출
- 신체 상태, 부상, 질환, 생체 신호 추론
- 카메라 영상 저장 또는 외부 전송
- 다중 사용자와 다중 카메라
- 양팔·손가락·시선·음성 입력
- 모바일 AR 최적화와 원격 서버
- 실제 병원 시스템 또는 환자 데이터 연동

## 5. 시스템 구성

```text
[Webcam]
    |
    v
[Python / OpenCV / MediaPipe Pose Landmarker]
  - 프레임 캡처
  - Pose 추론
  - 오른쪽 어깨·팔꿈치·손목 추출
  - tracking / pointing / pointer 계산
  - pose_pointer v2 생성
  - WebSocket Server
    |
    | UTF-8 JSON over ws://127.0.0.1:8765
    v
[Unity / WebSocket Client]
  - type/version별 DTO 파싱
  - 값과 불변 조건 검증
  - 최신 상태 큐와 메인 스레드 전달
  - AR 모의 포인터와 상태 UI
  - 비의료 안전 고지
```

영상 프레임은 Python 프로세스 밖으로 보내지 않는다.

## 6. Pose 입력 설계

MediaPipe Pose Landmarker의 표준 인덱스를 사용한다.

| 입력 | 인덱스 | 용도 |
|---|---:|---|
| `rightShoulder` | 12 | 상체 기준점과 오른팔 길이 품질 확인 |
| `rightElbow` | 14 | 포인팅 방향의 시작점 |
| `rightWrist` | 16 | 포인팅 방향의 끝점 |

각 관절 좌표는 이미지 정규화 좌표 `x`, `y`, 상대 깊이 `z`를 보존한다. `z`를 실제 거리나 환자 상태로 해석하지 않는다.

### 추적 상태

| 상태 | 의미 | 포인터 정책 |
|---|---|---|
| `TRACKING` | 세 관절의 좌표와 visibility가 품질 기준을 충족 | 추가 기하 검증 후 사용 가능 |
| `PARTIAL` | Pose는 있으나 하나 이상의 필수 관절이 누락되거나 품질 미달 | 비활성 |
| `LOST` | 사용할 Pose가 없음 | 비활성 |

### 포인팅

- 방향 벡터는 기본적으로 `rightWrist - rightElbow`를 사용한다.
- `rightShoulder`는 팔 길이, 비정상 축소 벡터와 관절 품질 검사에 사용한다.
- 정규화 포인터는 팔꿈치에서 손목 방향으로 연장한 2D 광선을 화면 상호작용 평면에 투영해 계산한다.
- 정확한 연장 계수, 스무딩, 경계 정책은 Task 08의 수동 데이터로 조정한다.
- 관절 좌표가 비유한 값이거나 길이가 너무 짧으면 `pointing=false`로 처리한다.

이 계산은 UI 입력만 생성하며 의료적 의미를 갖지 않는다.

## 7. WebSocket 계약

- 서버: Python
- 클라이언트: Unity
- 기본 URI: `ws://127.0.0.1:8765`
- 데이터: UTF-8 JSON 텍스트
- 레거시: `type="hand_gesture"`, `version=1`
- 활성: `type="pose_pointer"`, `version=2`
- 기본 전송 목표: 10~20Hz 이하, 최신 상태 우선

v1과 v2의 정확한 필드, 불변 조건과 fixture는 [`websocket-protocols.md`](./websocket-protocols.md)를 따른다.

## 8. Unity AR 모의 인터페이스

필수 화면 요소:

- `Triage Trace — Simulation Only` 제목
- 실제 의료 판단용이 아니라는 상시 고지
- WebSocket 연결 상태
- Pose 추적 상태
- 포인터 사용 가능 여부
- 정규화 포인터를 반영한 커서 또는 레이
- 가상의 시나리오 카드나 표적
- 데이터 만료·추적 실패 안내

Unity는 포인터로 선택할 수 있는 가상 시나리오를 표시할 수 있지만, 실제 환자 등급이나 치료 결론을 자동으로 생성하지 않는다. 샘플 라벨은 `Scenario A`, `Training Target 1`처럼 비임상 명칭을 우선한다.

## 9. 오류와 안전한 비활성화

- 잘못된 JSON, type, version, enum, 범위 밖 좌표·visibility는 적용하지 않는다.
- `PARTIAL`, `LOST`, `pointing=false`, `pointer=null`이면 포인터를 숨긴다.
- 연결이 끊기거나 데이터가 만료되면 마지막 포인터를 유지하지 않는다.
- Python은 손상된 내부 상태를 v2 메시지로 조용히 전송하지 않는다.
- 한 메시지 오류가 Python 또는 Unity 앱 전체 종료로 이어지지 않게 한다.

## 10. 구현 단계

1. Task 07: Pose Landmarker 모델과 VIDEO 모드 실행, 웹캠 프레임 처리
2. Task 08: 오른쪽 세 관절 추출, tracking 품질, pointing과 pointer 계산
3. Task 09: pose v2 메시지 빌더·WebSocket 게시와 Unity 수신 기반
4. 후속 Task: Unity AR 모의 포인터, 안전 고지, 시나리오 UI
5. 통합 Task: 추적 실패·재연결·데이터 만료·성능 검증

실제 Pose 추적 코드는 이번 문서 전환 작업에서 구현하지 않는다.

## 11. 검증 계획

- 모든 문서 링크와 활성 Task 순서 검사
- v1 fixture의 파일명·내용 불변 확인
- v2 정상, 추적 실패, 불완전 좌표 fixture JSON 구문 검사
- v2 상태별 불변 조건 검사
- Python 환경 smoke test 유지
- 구현 후 Pose 모델 초기화와 실제 카메라 수동 검증
- 구현 후 Unity EditMode DTO 파싱과 PlayMode 포인터 비활성화 테스트

## 12. 완료 기준

- Pose Landmarker가 오른쪽 세 관절을 안정적으로 제공한다.
- 세 추적 상태와 포인터 유효성이 계약과 일치한다.
- Python과 Unity가 v2 fixture를 동일하게 해석한다.
- Unity 포인터는 추적 실패, 데이터 만료와 연결 끊김에서 즉시 안전하게 비활성화된다.
- UI와 문서에 비의료 모의 인터페이스 고지가 명확하다.
- gesture v1 계약과 fixture가 그대로 유지된다.

## 13. 남은 위험

| 위험 | 대응 |
|---|---|
| Lite 모델의 실제 조명·거리·가림 조건 정확도와 장시간 안정성 미측정 | Task 08 품질 규칙과 대상 장치 수동 시나리오로 측정 |
| 웹캠 화각 밖 관절과 가림 | `PARTIAL` 처리와 포인터 비활성화 |
| 2D 포인터가 깊이·카메라 각도에 민감 | 보정 단계와 권장 사용자 위치를 후속 실험으로 확정 |
| visibility 임계값 미확정 | fixture 기준을 먼저 고정하고 실제 샘플로 조정 |
| Unity AR 좌표계와 이미지 좌표계 차이 | 명시적 y축 변환과 화면 투영 테스트 |
| 의료 앱으로 오인될 가능성 | 상시 고지, 비임상 라벨, 실제 판단 로직 금지 |
| v1/v2 라우팅 혼동 | type/version 조합을 엄격히 검증하고 DTO 분리 |
