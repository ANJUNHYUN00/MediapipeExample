# AGENTS.md

이 문서는 **Triage Trace Pose 기반 Unity AR 시뮬레이션 프로젝트의 최상위 안내서이자 문서 목차**다. 사람과 AI 작업자는 저장소에서 어떤 작업을 시작하든 이 문서를 가장 먼저 읽어야 한다.

## 1. 프로젝트 목적과 안전 경계

Triage Trace는 웹캠 영상에서 MediaPipe Pose Landmarker로 사용자의 자세를 추적하고, 오른쪽 어깨·팔꿈치·손목을 이용해 Unity AR 화면의 모의 포인터를 조작하는 교육·시연용 시뮬레이션이다.

이 프로젝트는 실제 환자를 평가하거나 분류하지 않는다. 진단, 치료, 응급도 판단, 의료 자문 또는 임상 의사결정을 제공하지 않으며, Unity 화면의 모든 분류·선택·상태는 **가상의 시나리오를 위한 모의 인터페이스**다. 의료 현장, 응급 대응 또는 환자 안전에 영향을 주는 용도로 사용해서는 안 된다.

MVP 입력은 다음 세 Pose Landmarker 관절로 제한한다.

- `rightShoulder`: 오른쪽 어깨, MediaPipe Pose 인덱스 12
- `rightElbow`: 오른쪽 팔꿈치, MediaPipe Pose 인덱스 14
- `rightWrist`: 오른쪽 손목, MediaPipe Pose 인덱스 16

## 2. 활성 문서 목차

| 문서 | 역할 | 읽는 시점 |
|---|---|---|
| [`AGENTS.md`](./AGENTS.md) | 프로젝트 목적, 안전 경계, 문서 우선순위, 폴더 책임 | 모든 작업에서 가장 먼저 |
| [`docs/project-plan.md`](./docs/project-plan.md) | Triage Trace 전체 기획과 완료 기준 | 설계·구현 전에 |
| [`docs/transition-plan.md`](./docs/transition-plan.md) | 기존 RPS 완료 자산, 재사용 결정, 전환 범위 | 레거시 자산을 변경할 때 |
| [`docs/websocket-protocols.md`](./docs/websocket-protocols.md) | 보존된 gesture v1과 활성 pose v2 계약 | Python·Unity 데이터 변경 전에 |
| [`Plan/07-triage-trace-architecture-and-pose-input.md`](./Plan/07-triage-trace-architecture-and-pose-input.md) | Pose 기반 아키텍처와 오른팔 입력 설계 | Pose 구현 전에 |
| [`Plan/08-pose-v2-protocol-and-unity-ar.md`](./Plan/08-pose-v2-protocol-and-unity-ar.md) | pose v2 게시·수신·Unity AR 표시 설계 | 통신·Unity 구현 전에 |
| [`Plan/09-unity-scenario-interaction-and-completion.md`](./Plan/09-unity-scenario-interaction-and-completion.md) | Unity 포인터·Patient 상호작용·완료 흐름 설계 | Task 10 이후 Unity 시나리오 구현 전에 |
| [`Tasks/07-pose-landmarker-runtime.md`](./Tasks/07-pose-landmarker-runtime.md) | 완료: Pose Landmarker 실행과 오른팔 관절 추출 | Pose 런타임 변경 전 |
| [`Tasks/08-right-arm-pointing-and-quality.md`](./Tasks/08-right-arm-pointing-and-quality.md) | 완료: 오른팔 관절 품질과 안정화된 포인터 계산 | pointing 변경 전 |
| [`Tasks/09-pose-v2-publisher-and-unity-receiver.md`](./Tasks/09-pose-v2-publisher-and-unity-receiver.md) | 완료: pose v2 송수신과 Unity 연결 | 통신·Unity 수신 변경 전 |
| [`Tasks/10-unity-ar-pointer-visualization.md`](./Tasks/10-unity-ar-pointer-visualization.md) | 완료: Unity LineRenderer 기반 pose pointer 시각화 | 포인터 표시 변경 전 |
| [`Tasks/11-patient-raycast-hover-highlighting.md`](./Tasks/11-patient-raycast-hover-highlighting.md) | 완료: Patient raycast hover highlight | Patient hover 변경 전 |
| [`Tasks/12-dwell-selection.md`](./Tasks/12-dwell-selection.md) | 완료: dwell 기반 Patient 선택 | 선택 상호작용 변경 전 |
| [`Tasks/13-patient-state-machine.md`](./Tasks/13-patient-state-machine.md) | 완료: Patient interaction state machine | Patient 상태 변경 전 |
| [`Tasks/14-patient-status-card-ui.md`](./Tasks/14-patient-status-card-ui.md) | 완료: 선택 Patient 상태 카드 UI | HUD 변경 전 |
| [`Tasks/15-end-to-end-scenario-integration.md`](./Tasks/15-end-to-end-scenario-integration.md) | 완료: Pose부터 UI까지 통합 시나리오 | 통합 흐름 변경 전 |
| [`Tasks/16-polish-qa-portfolio-packaging.md`](./Tasks/16-polish-qa-portfolio-packaging.md) | 완료: QA, README, 포트폴리오 정리 | 완료 흐름 변경 전 |

기존 `Plan/01`~`06`, `Tasks/01`~`06`, `hand_gesture` fixture와 Hand Landmarker 자산은 삭제하지 않는다. 완료된 Task 01~03과 Task 07~16은 재사용 기반이며, 미완료 Hand/RPS Task 04~06은 레거시 참조로 보존한다. Task 10~16의 Unity simulation MVP 흐름은 완료 상태이며, 후속 확장은 새 Task로 분리한다.

## 3. 핵심 아키텍처

```text
Webcam
  -> Python / MediaPipe Pose Landmarker
     -> rightShoulder + rightElbow + rightWrist
     -> tracking quality / pointing / normalized pointer
     -> WebSocket Server
  -> pose_pointer v2 JSON over ws://127.0.0.1:8765
  -> Unity WebSocket Client
     -> DTO validation
     -> thread-safe latest-state queue
     -> main-thread AR simulation presenter
```

- Python은 데이터 생산자이자 WebSocket 서버다.
- Unity는 데이터 소비자이자 WebSocket 클라이언트다.
- 영상 프레임은 Unity 또는 외부 네트워크로 전송하지 않는다.
- `hand_gesture` version 1은 호환성 기준으로 동결한다.
- Triage Trace의 활성 메시지는 `pose_pointer` version 2다.
- v1과 v2는 필드 의미를 섞지 않고 `type`과 `version`으로 분기한다.

## 4. 폴더별 역할

### `docs/`

프로젝트 목적, 비의료 안전 고지, 확정 아키텍처, v1/v2 계약과 완료 기준을 보관한다. 활성 기준은 `project-plan.md`와 `websocket-protocols.md`다.

### `Plan/`

구현 단계별 설계와 선후 관계를 보관한다. `01`~`06`은 기존 RPS 설계 이력이고, `07` 이후가 Triage Trace 활성 계획이다.

### `Tasks/`

AI가 그대로 수행할 수 있는 작업 절차와 완료 조건을 보관한다. Task 01~03은 완료된 기반 작업, Task 04~06은 대체된 레거시 계획, Task 07부터는 활성 Pose 구현 작업이다.

### `Mediapipe/`

Python 3.11 기반 카메라 입력, Pose Landmarker 실행, 오른쪽 관절 추출, 포인터 계산, pose v2 직렬화와 WebSocket 게시를 담당한다. Unity UI, 모의 시나리오 상태 또는 의료 의미를 계산하지 않는다.

### `MediapipeUnity/`

Unity WebSocket 클라이언트, pose v2 검증, 메인 스레드 전달, AR 모의 포인터와 가상 시나리오 UI를 담당한다. Pose를 다시 추론하거나 실제 의료 판단을 수행하지 않는다.

### `mds/`

정식 문서로 승격되기 전의 조사 자료와 메모를 보관한다. 확정 결정은 반드시 `docs/` 또는 활성 `Plan/`에 반영한다.

## 5. AI 필수 작업 절차

1. 루트 `AGENTS.md`를 가장 먼저 읽는다.
2. `docs/project-plan.md`, `docs/websocket-protocols.md`와 관련 Plan·Task를 읽는다.
3. 변경 대상 폴더의 기존 코드·테스트·설정과 하위 `AGENTS.md`를 확인한다.
4. 완료된 Task 01~03, gesture v1 fixture, Hand Landmarker 자산을 임의로 삭제하거나 의미 변경하지 않는다.
5. Pose 변경이 Python 내부 모델, v2 JSON, Unity DTO와 fixture에 미치는 영향을 함께 검토한다.
6. 의료 판단처럼 보이는 명칭·점수·자동 분류를 추가하지 않는다. 모든 UI는 모의 시나리오임을 유지한다.
7. 카메라 영상은 기본적으로 저장하거나 WebSocket으로 보내지 않는다.
8. 변경 후 문서 링크, JSON 구문, 필드명과 상태 불변 조건을 검증한다.
9. 완료 시 변경 파일, 설계 결정, 검증 결과, 남은 위험과 다음 Task를 보고한다.

## 6. Pose v2 핵심 원칙

- `tracking`: `TRACKING`, `PARTIAL`, `LOST` 중 하나다.
- `pointing`: 현재 프레임을 포인터 입력으로 사용할 수 있는지 나타낸다.
- `pointer`: 유효할 때 정규화 화면 좌표 `x`, `y`; 아니면 `null`이다.
- `joints`: `rightShoulder`, `rightElbow`, `rightWrist` 좌표 또는 `null`을 가진다.
- `visibility`: 세 관절 각각의 `0.0~1.0` 품질 값을 가진다.
- `TRACKING`만으로 `pointing=true`가 보장되지는 않는다. 관절 가시성, 유한 좌표, 팔 길이와 포인터 계산 유효성을 모두 통과해야 한다.
- `PARTIAL`과 `LOST`에서는 `pointing=false`, `pointer=null`이어야 한다.
- Unity는 유효하지 않거나 오래된 포인터를 화면에서 비활성화한다.

정확한 계약은 [`docs/websocket-protocols.md`](./docs/websocket-protocols.md)를 따른다.

## 7. 기본 완료 기준

- Pose Landmarker가 웹캠 연속 프레임에서 실행되고 자원을 정상 해제한다.
- 오른쪽 어깨·팔꿈치·손목의 좌표와 visibility가 내부 모델로 변환된다.
- 정상 추적, 부분 추적, 추적 실패가 서로 구분된다.
- Python이 pose v2 계약에 맞는 메시지를 게시한다.
- Unity가 v2를 검증하고 메인 스레드에서 모의 포인터를 표시한다.
- Unity가 LineRenderer pointer, Patient hover, dwell selection, interaction state와 HUD를 비의료 시뮬레이션으로 연결한다.
- 연결 끊김, 데이터 만료, `PARTIAL`, `LOST`에서 포인터가 안전하게 비활성화된다.
- 실제 의료 판단을 하지 않는다는 고지가 시작 화면과 프로젝트 문서에 존재한다.
- gesture v1 fixture와 의미가 그대로 유지된다.

## 8. 문서 유지 규칙

- 새 핵심 문서나 Task를 추가하면 이 목차를 갱신한다.
- v2 필드나 불변 조건을 바꾸면 Python·Unity 문서, fixture와 관련 Plan·Task를 같은 작업에서 갱신한다.
- v1 변경이 필요하면 호환성 검토와 별도 승인 없이 기존 필드를 삭제·재해석하지 않는다.
- 레거시 문서가 활성 설계로 오해되지 않도록 상태 배너와 활성 문서 링크를 유지한다.
- 구현 전환이 끝나도 기존 이력은 삭제하지 않고 Git 기록과 레거시 문서로 보존한다.
