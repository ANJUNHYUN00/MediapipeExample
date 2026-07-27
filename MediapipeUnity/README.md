# Triage Trace Unity 애플리케이션

이 디렉터리는 Python MediaPipe 서버의 `pose_pointer` version 2 메시지를 받아 모의 AR 포인터와 가상 시나리오 상태를 표시하는 **데이터 소비자**다.

> Triage Trace는 실제 의료 판단용이 아니다. Unity 화면은 교육·시연용 모의 인터페이스이며 환자 평가, 응급도 분류, 진단 또는 치료 결정을 제공하지 않는다.

## 현재 프로젝트 상태

`MediapipeUnity/`는 Unity Editor 6000.3.10f1로 생성하고 batch mode에서 초기 import와 컴파일을 검증한 유효한 Unity 프로젝트다.

- Unity Hub: 3.16.3
- Unity Editor: 6000.3.10f1 (`e35f0c77bd8e`)
- 프로젝트 버전: `ProjectSettings/ProjectVersion.txt`
- 패키지 기준: `Packages/manifest.json`, `Packages/packages-lock.json`
- Task 09 batch compile: C# 오류 없음
- WebSocket: .NET `ClientWebSocket` 사용, 별도 WebSocket 패키지 없음
- JSON: `com.unity.nuget.newtonsoft-json` 3.2.2
- pose v2 DTO·검증·재연결 수신기·최신 상태 큐 구현 완료
- `LineRenderer` 기반 모의 AR 포인터 시각화 구현 완료
- Patient interaction state와 Canvas 기반 Patient Status Card UI 구현 완료
- `OnGUI` 연결/tracking/pointing 진단 표시 구현 완료
- EditMode 10개, PlayMode 6개와 Python↔Unity 실소켓 PlayMode 4개 통과

## 활성 책임

- `ws://127.0.0.1:8765` 연결, 종료와 제한된 재연결
- type/version에 따른 gesture v1과 pose v2 분리
- pose v2 JSON 역직렬화와 불변 조건 검사
- 백그라운드 수신 결과를 최신 상태 큐로 전달
- Unity 메인 스레드에서 연결·tracking·pointer UI 갱신
- 데이터 만료, `PARTIAL`, `LOST`, 연결 끊김에서 포인터 숨김
- `Simulation Only / 실제 의료 판단용이 아님` 고지
- EditMode 계약 테스트와 PlayMode 수명 주기·UI 테스트

담당하지 않는 범위:

- 웹캠과 MediaPipe Pose 추론
- 오른쪽 관절 또는 pointer 재계산
- 실제 환자 분류, 진단, 치료 또는 위험도 판단
- Python 프로세스 자원 관리

## 프로젝트 구조

```text
MediapipeUnity/
  Assets/
    Scenes/
    Scripts/
      Configuration/
      Models/
      Networking/
      Presentation/
    Prefabs/
    UI/
    Tests/
      EditMode/
      PlayMode/
  Packages/
  ProjectSettings/
```

Unity `.meta`는 대응 자산과 함께 보존하고 `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`와 IDE 생성 파일은 버전 관리하지 않는다.

## 폴더별 책임

| 폴더 | 책임 |
|---|---|
| `Scripts/Configuration` | URI, 재연결, 데이터 만료, 좌표 변환 설정 |
| `Scripts/Models` | 분리된 v1/v2 DTO와 검증된 도메인 상태 |
| `Scripts/Networking` | 연결, 수신, 취소, 재연결, type/version 라우팅, 최신 상태 큐 |
| `Scripts/Presentation` | 수신 상태 진단과 `LineRenderer` 포인터 시각화 |
| `Tests/EditMode` | v1/v2 JSON 파싱, 값과 불변 조건 |
| `Tests/PlayMode` | 메인 스레드 적용, 포인터 숨김, 데이터 만료, 안전 고지 |

## 의존 방향

```text
Networking -> Models
Presentation -> Models
Configuration -> Networking / Presentation

Presentation -X-> WebSocket 라이브러리 직접 호출
Models -X-> Unity UI 또는 Pose 재계산
```

WebSocket 콜백에서 TextMeshPro, Image, RectTransform, GameObject를 직접 변경하지 않는다.

## WebSocket 프로토콜

- Python 서버 / Unity 클라이언트
- `ws://127.0.0.1:8765`
- UTF-8 JSON 텍스트

### gesture v1

`hand_gesture` version 1 DTO와 fixture는 레거시 호환성 기준으로 보존한다. 필드를 Pose 의미로 재사용하지 않는다.

### pose v2

```text
PosePointerMessageV2
  type: string
  version: int
  timestamp: long
  sequence: long
  tracking: string
  pointing: bool
  pointer: PointerDto | null
  joints: RightArmJointsDto
  visibility: RightArmVisibilityDto
```

`tracking`은 `TRACKING`, `PARTIAL`, `LOST`만 허용한다. `PARTIAL`과 `LOST`에서는 `pointing=false`, `pointer=null`이어야 한다. 상세 계약은 [`docs/websocket-protocols.md`](../docs/websocket-protocols.md)를 따른다.

## 모의 AR 표시 정책

필수 요소:

- `Triage Trace — Simulation Only`
- `실제 의료 판단용이 아닙니다`
- WebSocket 연결 상태
- Pose tracking 상태
- 포인터 활성 여부
- 최신 유효 pointer의 `LineRenderer` ray
- 데이터 만료·추적 실패 안내

포인터는 `pointing=true`이고 데이터가 최신일 때만 활성화한다. 연결이 유지되더라도 메시지가 만료되면 마지막 위치를 유지하지 않는다.

가상 시나리오의 라벨은 `Scenario A`, `Training Target 1` 같은 비임상 표현을 사용한다. 실제 환자 등급이나 치료 결론을 자동 생성하지 않는다.

### LineRenderer 포인터

- `PoseReceiverBehaviour`는 WebSocket 수신과 검증된 최신 상태 전달만 담당한다.
- `PosePointerLineRenderer`는 검증된 `PosePointerState`를 받아 선 표시만 담당한다.
- `Pointer Start`에는 AR 카메라, 컨트롤러, 손목 앵커 등 ray 시작점으로 쓸 `Transform`을 지정한다.
- `Line Length`, `Line Thickness`, `Line Color`, `Interpolation Speed`, `Timeout Seconds`, `Invert Horizontal`, `Invert Vertical`은 Inspector에서 조정한다.
- 시작점이 비어 있으면 컴포넌트가 붙은 GameObject의 `Transform`을 사용한다.
- `LineRenderer`가 비어 있으면 같은 GameObject에 자동으로 생성한다.

### Patient interaction state

`PatientView`는 `Unseen`, `Highlighted`, `InProgress`, `Checked` 상태를 가진다.
이 상태는 가상 Patient의 확인 흐름을 추적하기 위한 interaction state이며 의료
중증도 판단이 아니다. `PointerRaycaster`가 가리키는 `Unseen` 대상은
`Highlighted`가 되고, `PatientDwellSelector`가 dwell 시간을 채우면
`InProgress`가 된다. `MarkChecked()`는 외부 호출이나 후속 UI에서 확인 완료를
표시하기 위한 최소 API다.

interaction state 색상은 Inspector의 `Unseen Color`, `Highlighted Color`,
`In Progress Color`, `Checked Color`에서 조정한다. red/yellow/green/black은
triage severity 라벨을 표현해야 할 때만 별도 데이터로 다루며, hover, dwell,
checked 같은 interaction state 색상과 섞지 않는다.

### Patient Status Card UI

`PatientStatusCardUI`는 선택되거나 `InProgress`가 된 가상 Patient의 확인 흐름을
보여 주는 Canvas 기반 카드다. 의료 진단 카드가 아니며 진단, 위험도, 치료 추천,
자동 중증도 판단을 표시하지 않는다.

기본 구성:

- Canvas
- `PatientStatusCard` panel
- Patient ID Text
- Interaction State Text
- Checked Status Text
- Mark Checked Button

씬에서 직접 설정하려면 Canvas 아래 panel을 만들고 `PatientStatusCardUI`를 붙인
뒤 `Patient Id Text`, `Interaction State Text`, `Checked Status Text`,
`Mark Checked Button`, 선택적 `Background Panel`을 연결한다.
`PatientDwellSelector`의 `Status Card` 필드에 이 컴포넌트를 연결하면 dwell
selection으로 `InProgress`가 된 Patient가 자동으로 카드에 표시된다.

Mark Checked Button은 `PatientStatusCardUI.MarkChecked()`를 호출한다. 호출되면
현재 표시 중인 `PatientView.MarkChecked()`가 실행되고 카드의 Interaction State와
Checked 표시가 갱신된다. 표시할 Patient가 없으면 카드는 숨김 상태가 되며 버튼은
비활성화된다.

## 좌표 처리

- v2 pointer는 이미지 기준 정규화 좌표다.
- `PosePointerLineRenderer`가 정규화 pointer를 시작점 기준 world 방향으로 한 번 변환한다.
- 화면 중앙 `(0.5, 0.5)`은 시작점의 `forward` 방향이다.
- x축은 시작점의 `right`, y축은 이미지 좌표계 보정을 위해 기본적으로 시작점의 `up` 방향으로 변환한다.
- 좌우/상하 반전은 설정으로 명시하고 Python과 중복 적용하지 않는다.
- joints와 visibility는 디버그·상태 표시에 사용할 수 있지만 Unity에서 포인터를 다시 계산하지 않는다.

## 오류 처리

- 잘못된 JSON, 알 수 없는 type/version/tracking은 적용하지 않는다.
- 범위 밖 visibility, 누락 필수 필드와 상태 불변 조건 위반을 거부한다.
- 알 수 없는 추가 필드는 허용한다.
- 한 메시지 오류로 연결 또는 앱 전체를 종료하지 않는다.
- sequence 역행 메시지를 무시한다.
- 데이터 만료와 연결 끊김을 별도 상태로 진단한다.

## 실행 순서

1. Python Triage Trace 서버를 실행한다.
2. Pose 모델, 카메라와 `127.0.0.1:8765` 시작을 확인한다.
3. Unity Play Mode 또는 빌드를 실행한다.
4. 연결과 pose v2 수신, 포인터 표시를 확인한다.
5. Unity를 먼저 실행한 경우 재연결 상태에서 Python 시작 후 자동 복구되는지 확인한다.
6. 종료 시 Unity 수신 작업을 취소하고 Python 서버와 카메라를 정리한다.

씬에 별도 설정이 없어도 Play Mode에서 런타임 부트스트랩이
`PoseReceiverBehaviour`와 `PosePointerLineRenderer`를 생성한다. 연결 URI 기본값은
`ws://127.0.0.1:8765`, 재연결 간격은 1초, 데이터 만료 기준은 0.5초다.
배치 모드에서는 자동 부트스트랩을 생략한다.

씬에서 직접 설정하려면 빈 GameObject를 만들고 `PoseReceiverBehaviour`와
`PosePointerLineRenderer`를 붙인다. `PoseReceiverBehaviour`의 `Pointer Line`에
같은 GameObject의 `PosePointerLineRenderer`를 연결하고,
`PosePointerLineRenderer`의 `Pointer Start`에는 ray를 시작할 Transform을 지정한다.

## 테스트

Unity Editor Test Runner에서 EditMode와 PlayMode를 실행한다. 실제 Python
publisher까지 포함하는 조건부 PlayMode 테스트는 환경 변수
`TRIAGE_TRACE_INTEGRATION_URI=ws://127.0.0.1:8765`를 설정하고 합성 또는 실제
Python publisher를 먼저 실행한 뒤 수행한다.

## 다음 작업

후속 Unity AR UI Task에서 `LineRenderer` ray가 가리키는 비임상 가상 표적을
판별하고 hover 시각 피드백을 구현한다. 환자 선택, dwell 확정, 상태 카드와 실제
의료 판단 기능은 별도 승인 없이 추가하지 않는다.
