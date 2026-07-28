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
- End-to-End scenario 연결 helper 구현 완료
- `OnGUI` 연결/tracking/pointing 진단 표시 구현 완료
- Task 09 기준 EditMode 10개, PlayMode 3개, Python↔Unity 실소켓 PlayMode 4개 통과
- Task 12~15 PlayMode 테스트는 추가했으나 현재 환경의 Unity licensing 문제로 batchmode 실행 미확인

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

### World Space Patient Status Card

기본 데모 흐름은 `WorldSpacePatientStatusCard`를 사용해 현재 바라보는 Patient 또는
dwell로 선택된 Patient 위에 같은 `PatientStatusCardUI` 정보를 표시한다. 선택된
Patient가 hover Patient보다 우선하며, 둘 다 없으면 카드는 숨겨진다.

`TriageTraceScenarioBootstrap`의 주요 Inspector 옵션:

- `Enable Screen Space Status Card`: 기존 화면 고정 `StatusCardCanvas` 표시 여부.
  기본값은 꺼짐이며 기존 오브젝트와 바인딩은 삭제하지 않는다.
- `Create World Space Status Card If Missing`: World Space 카드가 없을 때 Play Mode에서
  기본 카드를 생성한다.
- `World Space Display Mode`: `Hover Or Selected`는 바라보기만 해도 표시하고,
  `Selected Only`는 dwell 선택 이후에만 표시한다.
- `World Space Card Offset`: Patient 또는 `Status Card Anchor` 기준 위치. 기본값
  `(0, 1.3, 0)`. 위아래 위치는 Y로 조정하고, 앞뒤 거리인 Z는 기본 `0`을 유지한다.
- `World Space Canvas Scale`: 기본 `0.003`.
- `World Space Card Pixel Size`: 기본 `320 x 180`, 실제 크기는 약 `0.96 x 0.54m`.
- `World Space Camera`: 비워 두면 `MainCamera` 태그 카메라를 사용한다.

씬에서 직접 만들 때:

1. `WorldSpacePatientStatusCard`라는 Canvas를 만들고 Render Mode를 `World Space`로
   설정한다.
2. Canvas 아래 panel과 Patient ID/Interaction/Checked Text, Mark Checked Button을
   만든다.
3. panel에 `PatientStatusCardUI`를 붙이고 `Card Root`, Text, Button,
   `Background Panel`을 연결한다.
4. Figma 배경을 넣을 때 `Background Panel` Image를 유지하고
   `Background Sprite` 슬롯에 export한 Sprite를 연결한다.
5. Canvas 루트에 `WorldSpacePatientStatusCard`를 붙이고 Status Card,
   Pointer Raycaster, Dwell Selector, Target Camera, World Space Canvas를 연결한다.
6. 카드 기준점을 세밀하게 조정하려면 Patient 자식으로 빈 Transform을 만들고
   `PatientView.Status Card Anchor`에 연결한다. 환자별 원점 차이가 있으면 이
   anchor로 개별 보정하고, 그렇지 않으면 Patient Transform과 `Patient Offset`을
   사용한다.
7. 기본 Y `1.3`에서 환자 모델 및 약 Y `0.9`의 `PatientMarker`와 겹치지 않는지
   확인하고, 필요한 경우 Y만 미세 조정한다.

카드는 LateUpdate에서 카메라 forward를 따라 회전한다. `Keep Upright`를 켜면
카메라가 위아래로 기울어도 카드가 수직을 유지한다.

### End-to-End Scenario Setup

`TriageTraceScenarioBootstrap`은 수동 Inspector 연결을 우선하고, 비어 있는 참조만
같은 GameObject나 현재 Scene에서 찾아 연결하는 편의 helper다. 자동 생성된 receiver
object에도 붙지만, 실제 데모 Scene에서는 명시적으로 설정하는 방식을 권장한다.

Unity Scene 체크리스트:

1. Main Camera 또는 AR Simulation Camera를 준비한다.
2. `PointerOrigin` 빈 GameObject를 만들고 카메라 앞쪽을 향하게 둔다.
3. 빈 GameObject를 만들고 `PoseReceiverBehaviour`, `PosePointerLineRenderer`,
   `PointerRaycaster`, `PatientDwellSelector`, `TriageTraceScenarioBootstrap`을 붙인다.
4. `PoseReceiverBehaviour.Pointer Line`에 같은 GameObject의
   `PosePointerLineRenderer`를 연결한다.
5. `PosePointerLineRenderer.Pointer Start`에 `PointerOrigin`을 연결한다.
6. `PointerRaycaster.Ray Origin`에 `PointerOrigin`을 연결하고
   `Patient Layer Mask`를 Patient 전용 Layer로 설정한다.
7. Canvas 아래 `PatientStatusCardUI`를 구성하고 `PatientDwellSelector.Status Card`
   또는 `TriageTraceScenarioBootstrap.Status Card`에 연결한다.
8. Patient 오브젝트를 여러 개 만들고 각 오브젝트에 `Collider`, `Renderer`,
   `PatientView`를 추가한다.
9. 각 Patient에 같은 Patient Layer를 적용한다.
10. `PatientView.Display Name`, interaction state 색상, `PatientDwellSelector.Dwell Seconds`
    를 Inspector에서 조정한다.
11. Play Mode에서 연결, pointer line, hover `Highlighted`, dwell `InProgress`,
    status card, Mark Checked, `Checked` 보호 동작을 순서대로 확인한다.

Patient 오브젝트는 raycast hit를 위해 Collider가 필요하고, 상태 색상 표시를 위해
Renderer가 필요하다. interaction state 색상은 cyan/blue/white/gray 계열을 사용하고
red/yellow/green/black은 triage severity와 혼동될 수 있으므로 interaction UI에
사용하지 않는다.

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

## Final Run Order

1. Python 환경을 준비한다.
   `Set-Location C:\Projects\MediapipeExample\Mediapipe`
2. 처음 실행하는 환경이면 의존성을 설치한다.
   `.\.venv\Scripts\python.exe -m pip install -e ".[dev]"`
3. MediaPipe Pose publisher를 실행한다.
   `.\.venv\Scripts\python.exe -m mediapipe_rps.app`
4. 카메라, Pose 모델, `ws://127.0.0.1:8765` 시작 로그를 확인한다.
5. Unity Hub에서 `C:\Projects\MediapipeExample\MediapipeUnity` 프로젝트를 연다.
6. Unity Scene 설정 체크리스트를 확인한다.
7. Unity Play Mode를 실행한다.
8. 오른팔을 카메라 안에서 펴고 pointing 상태에서 pointer line을 확인한다.
9. Patient 오브젝트를 가리켜 hover `Highlighted` 상태를 확인한다.
10. 같은 Patient를 `Dwell Seconds` 이상 가리켜 `InProgress`와 Status Card 표시를 확인한다.
11. Mark Checked 버튼을 눌러 `Checked` 상태가 유지되는지 확인한다.
12. 종료 시 Unity Play Mode를 중지하고 Python 앱에서 `q`, `Esc` 또는 `Ctrl+C`로 서버와 카메라를 정리한다.

씬에 별도 설정이 없어도 Play Mode에서 런타임 부트스트랩이
`PoseReceiverBehaviour`와 `PosePointerLineRenderer`를 생성한다. 연결 URI 기본값은
`ws://127.0.0.1:8765`, 재연결 간격은 1초, 데이터 만료 기준은 0.5초다.
배치 모드에서는 자동 부트스트랩을 생략한다.

씬에서 직접 설정하려면 빈 GameObject를 만들고 `PoseReceiverBehaviour`와
`PosePointerLineRenderer`를 붙인다. `PoseReceiverBehaviour`의 `Pointer Line`에
같은 GameObject의 `PosePointerLineRenderer`를 연결하고,
`PosePointerLineRenderer`의 `Pointer Start`에는 ray를 시작할 Transform을 지정한다.

## Setup Checklists

### Unity Editor setup

- Unity Editor 6000.3.10f1로 `MediapipeUnity`를 연다.
- Main Camera 또는 AR Simulation Camera가 Scene에 있는지 확인한다.
- `PointerOrigin` 빈 GameObject를 만들고 카메라 앞쪽을 향하게 한다.
- Scenario object에 `PoseReceiverBehaviour`, `PosePointerLineRenderer`,
  `PointerRaycaster`, `PatientDwellSelector`, `TriageTraceScenarioBootstrap`을 붙인다.
- `PoseReceiverBehaviour.Pointer Line`에 `PosePointerLineRenderer`를 연결한다.
- `PosePointerLineRenderer.Pointer Start`와 `PointerRaycaster.Ray Origin`에
  `PointerOrigin`을 연결한다.
- `PointerRaycaster.Patient Layer Mask`가 Patient 전용 Layer를 가리키는지 확인한다.

### Patient object setup

- 각 Patient는 가상 훈련 대상이며 실제 환자 데이터가 아니다.
- 각 Patient GameObject에 `Collider`, `Renderer`, `PatientView`를 추가한다.
- 모든 Patient에 같은 Patient Layer를 적용한다.
- `PatientView.Display Name`에는 `Training Target 1`처럼 비임상 이름을 쓴다.
- `Unseen`, `Highlighted`, `InProgress`, `Checked` 색상은 cyan/blue/white/gray 계열을 쓴다.
- red/yellow/green/black은 triage severity 색상과 혼동될 수 있으므로 interaction state 색상으로 쓰지 않는다.

### UI setup

- Canvas 아래 `PatientStatusCard` panel을 만든다.
- Patient ID Text, Interaction State Text, Checked Status Text, Mark Checked Button을 만든다.
- Panel에 `PatientStatusCardUI`를 붙이고 Text/Button/Image 필드를 연결한다.
- `PatientDwellSelector.Status Card` 또는 `TriageTraceScenarioBootstrap.Status Card`에
  `PatientStatusCardUI`를 연결한다.
- Status Card는 AR HUD 느낌의 반투명 카드로 유지하고, 정보는 Patient ID,
  Interaction State, Checked 여부로 제한한다.
- World Space Canvas의 `WorldSpacePatientStatusCard`에 Pointer Raycaster,
  Dwell Selector, Main Camera를 연결한다.
- 기존 화면 고정 카드는 `Enable Screen Space Status Card`로 선택적으로 유지한다.
- 환자별 위치 보정이 필요하면 `PatientView.Status Card Anchor`를 연결하고, 공통
  높이는 `World Space Card Offset`으로 조절한다.
- Figma 배경 Sprite는 `PatientStatusCardUI.Background Sprite`에 연결한다.
- `Severity`, `Diagnosis`, `AI Judgement`, `Risk Score`처럼 의료 판단처럼 보이는 표현을 쓰지 않는다.

### 2026-07-28 통합 수동 검증

실제 Python 카메라 입력과 Unity Play Mode 통합 실행에서 다음을 확인했다.

- WebSocket 연결과 Unity 포인터 이동
- 포인팅 오작동 없음
- Patient raycast, hover, dwell 선택
- 환자 interaction 색상 변경
- Patient 위 World Space 카드 표시
- Patient ID, Interaction State, Checked 상태 갱신

카드 기본 offset은 `(0, 1.3, 0)`으로 씬의 Play Mode 외 직렬화 설정에 반영했다.
씬의 `PatientMarker`는 약 Y `0.9`이고 World Space 카드 높이는 Y `1.3`이므로
중심 간 약 `0.4m` 간격을 둔다. 실제 모델 원점이나 카드 크기가 다른 환자는
`PatientView > Status Card Anchor`로 개별 보정한다.

## Demo Scenario

1. Python publisher가 pose v2 메시지를 보낸다.
2. Unity가 WebSocket으로 pose v2를 수신한다.
3. 오른팔 pointing이 유효하면 얇은 cyan 계열 pointer line이 보인다.
4. pointer line이 Patient collider를 향하면 해당 Patient가 `Highlighted`가 된다.
5. 같은 Patient를 dwell 시간 이상 계속 가리키면 `InProgress`가 된다.
6. Status Card가 Patient ID, Interaction State, Checked 여부를 표시한다.
7. Mark Checked를 누르면 Patient가 `Checked`가 된다.
8. `Checked` Patient는 다시 hover/dwell해도 `Unseen`이나 `InProgress`로 되돌아가지 않는다.

이 시나리오는 누가 확인되었고 누가 아직 확인되지 않았는지 추적하는 보조 UI다.
의료 중증도 판단, 자동 진단, 환자 우선순위 산출을 하지 않는다.

## Design Polish Guide

- Pointer line은 얇고 차분한 cyan 계열을 권장한다.
- Patient interaction state 색상은 cyan/blue/white/gray 계열을 권장한다.
- red/yellow/green/black은 triage severity 색상과 혼동될 수 있으므로 interaction state에는 쓰지 않는다.
- Status Card는 AR HUD 느낌의 반투명 카드로 유지한다.
- 정보는 Patient ID, Interaction State, Checked 여부 중심으로 최소화한다.
- 의료 판단처럼 보이는 문구와 점수, 자동 분류 표현을 피한다.
- 복잡한 최종 레이아웃, 발표 화면 polish, 포트폴리오용 캡처 정리는 별도 최종 다듬기에서 수행한다.

## Manual QA Checklist

- Unity Play Mode에서 pointer line이 보인다.
- Patient 오브젝트를 가리키면 `Highlighted` 상태가 된다.
- 같은 Patient를 `Dwell Seconds` 이상 가리키면 `InProgress` 상태가 된다.
- Status Card가 해당 Patient 정보를 표시한다.
- Mark Checked 실행 후 Patient가 `Checked` 상태가 된다.
- `Checked` Patient는 hover/dwell로 되돌아가지 않는다.
- `PARTIAL`, `LOST`, `pointing=false`, 연결 끊김, 데이터 만료에서 stale pointer나 hover가 남지 않는다.
- UI 문구가 interaction tracking만 설명하고 의료 판단으로 읽히지 않는다.

최소 데모 성공 기준은 pointer line, hover, dwell `InProgress`, Status Card, Mark Checked,
`Checked` 보호 동작이 한 번의 Play Mode 세션에서 순서대로 확인되는 것이다.

## Known limitations

- 실제 AR hardware 없이 Unity desktop simulation MVP로 동작한다.
- Pose 입력은 PC webcam 기반이며 조명, 거리, 카메라 각도, 오른팔 가림에 민감하다.
- Unity는 Python이 보낸 pose v2 pointer를 소비하며 Pose를 다시 추론하지 않는다.
- 이 UI는 interaction tracking UI이며 의료 판단, 자동 진단, 중증도 판단 AI가 아니다.
- Unity licensing 문제로 batchmode PlayMode 테스트 실행이 제한될 수 있다.
- 모바일 AR, AR Foundation, 실제 병원 시스템, 실제 환자 데이터 연동은 포함하지 않는다.
- 디자인은 MVP 수준이며 발표용 최종 화면 구성은 상황에 맞춰 수동 조정할 수 있다.

## Troubleshooting

### Pointer line이 안 보일 때

- Python publisher가 실행 중이고 Unity WebSocket 상태가 Connected인지 확인한다.
- Pose 상태가 `TRACKING`이고 `pointing=true`인지 확인한다.
- 오른쪽 어깨, 팔꿈치, 손목이 카메라 안에 충분히 보이는지 확인한다.
- `PosePointerLineRenderer.Pointer Start`, `Line Length`, `Timeout Seconds`를 확인한다.
- `PARTIAL`, `LOST`, 데이터 만료 상태에서는 line이 숨겨지는 것이 정상이다.

### Patient가 highlight되지 않을 때

- Patient GameObject에 Collider와 `PatientView`가 있는지 확인한다.
- Patient Layer가 `PointerRaycaster.Patient Layer Mask`에 포함되는지 확인한다.
- `PointerRaycaster.Ray Origin`과 pointer direction이 Patient collider를 향하는지 확인한다.
- Collider가 너무 작거나 raycast distance 밖에 있지 않은지 확인한다.

### dwell selection이 안 될 때

- `PatientDwellSelector.Pointer Raycaster`가 연결되어 있는지 확인한다.
- `Dwell Seconds`가 너무 길지 않은지 확인한다.
- 같은 Patient를 dwell 시간 동안 계속 가리키고 있는지 확인한다.
- pointer가 숨겨지거나 `CurrentPatient`가 null이면 dwell timer가 초기화된다.
- 이미 `Checked` 상태인 Patient는 dwell로 `InProgress`로 되돌아가지 않는다.

### Status Card가 안 뜰 때

- `PatientStatusCardUI`의 Text/Button 필드가 연결되어 있는지 확인한다.
- World Space 카드의 `Pointer Raycaster`, `Dwell Selector`, `Target Camera`가
  연결되어 있는지 확인한다.
- Main Camera가 `MainCamera` 태그를 가지는지 확인한다.
- `World Space Display Mode`가 `Selected Only`이면 dwell 선택 전에는 숨김이 정상이다.
- `PatientDwellSelector.Status Card` 또는 `TriageTraceScenarioBootstrap.Status Card`가 연결되어 있는지 확인한다.
- Status Card가 null이면 `TriageTraceScenarioBootstrap.Create Status Card If Missing`을 켤 수 있다.
- World Space 카드가 null이면 `Create World Space Status Card If Missing`을 켠다.
- 표시할 Patient가 없으면 card가 숨김 상태가 되는 것이 정상이다.

### Mark Checked가 안 될 때

- Mark Checked Button이 `PatientStatusCardUI.MarkChecked()`에 연결되어 있는지 확인한다.
- 현재 `PatientStatusCardUI.BoundPatient`가 null이 아닌지 확인한다.
- 이미 `Checked` 상태이면 버튼이 비활성화되는 것이 정상이다.

### Unity PlayMode test가 안 될 때

- Unity Hub license 상태와 로그인 상태를 확인한다.
- batchmode 로그의 `Licensing initialization failed` 여부를 기록한다.
- 이 저장소에서는 Task 12~15 batchmode PlayMode 테스트가 licensing 문제로 결과 XML까지 진행하지 못한 이력이 있다.

### Python publisher가 실행되지 않을 때

- Python 3.11 가상환경이 만들어졌는지 확인한다.
- `.\.venv\Scripts\python.exe -m pip install -e ".[dev]"`를 다시 실행한다.
- `Mediapipe/models/pose_landmarker_lite.task`가 있는지 확인한다.
- 카메라가 다른 앱에서 사용 중인지 확인한다.

### WebSocket 연결이 안 될 때

- Python 로그에서 `ws://127.0.0.1:8765`가 열렸는지 확인한다.
- Unity `PoseReceiverBehaviour.websocketUri`가 같은 URI인지 확인한다.
- 포트 8765를 다른 프로세스가 사용 중인지 확인한다.
- Unity를 먼저 실행한 경우 재연결 로그를 확인하고 Python publisher를 다시 시작한다.

## 테스트

Unity Editor Test Runner에서 EditMode와 PlayMode를 실행한다. 실제 Python
publisher까지 포함하는 조건부 PlayMode 테스트는 환경 변수
`TRIAGE_TRACE_INTEGRATION_URI=ws://127.0.0.1:8765`를 설정하고 합성 또는 실제
Python publisher를 먼저 실행한 뒤 수행한다.

## Portfolio Summary

Triage Trace는 MediaPipe Pose의 오른팔 pointing을 Unity AR simulation 입력으로
변환해, 가상 Patient 대상의 hover, dwell selection, checked 상태를 추적하는
비의료 시뮬레이션 MVP다. 실제 의료 판단이나 자동 중증도 분류가 아니라,
확인 흐름을 시각화하는 interaction prototype이다.
