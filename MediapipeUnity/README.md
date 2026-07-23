# MediaPipe Unity 애플리케이션

이 디렉터리는 Python MediaPipe 프로세스가 게시한 `hand_gesture` 메시지를 받아 연결 상태와 가위바위보 결과를 표시하는 **데이터 소비자**를 구현한다.

## 현재 프로젝트 상태

현재 `MediapipeUnity/`는 아직 유효한 Unity 프로젝트가 아니다. Unity 프로젝트의 필수 요소인 다음 항목이 생성되지 않은 상태다.

- `Assets/`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`

가짜 `ProjectSettings`나 임의의 Unity 버전 파일은 만들지 않았다. [`Tasks/03-development-environment-setup.md`](../Tasks/03-development-environment-setup.md)에서 설치된 Unity LTS Editor를 확인한 뒤 이 디렉터리에 정식 프로젝트를 생성해야 한다.

## 책임 범위

Unity 애플리케이션이 담당하는 범위:

- `ws://127.0.0.1:8765` WebSocket 연결과 종료
- 연결 실패 또는 종료 후 제한된 자동 재연결
- `hand_gesture` 버전 1 JSON 역직렬화와 유효성 검사
- 백그라운드 수신 결과를 스레드 안전한 큐로 전달
- Unity 메인 스레드에서 연결 상태와 결과 UI 갱신
- `주먹`, `가위`, `보`, 미인식, 손 없음 표시
- EditMode 파싱 테스트와 PlayMode 수명 주기·UI 테스트

Unity 애플리케이션이 담당하지 않는 범위:

- 웹캠 열기와 영상 처리
- MediaPipe 랜드마크 검출
- 손가락 상태 및 제스처 재판정
- Python 프로세스 내부 설정과 자원 관리

## Unity 프로젝트 생성 후 구조

Task 03에서 Unity Editor가 프로젝트를 생성한 뒤 다음 구조를 준비한다.

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

Unity의 `.meta` 파일은 대응하는 자산과 함께 보존한다. `Library/`, `Temp/`, `Logs/`, `obj/`와 사용자별 IDE 산출물은 버전 관리하지 않는다.

## 폴더별 책임

| 폴더 | 책임 | 직접 담당하지 않을 대상 |
|---|---|---|
| `Scripts/Configuration` | WebSocket URI, 재연결 간격, 데이터 만료 설정 | 연결 루프와 UI |
| `Scripts/Models` | 버전 1 네트워크 DTO와 검증된 도메인 상태 | WebSocket 연결 |
| `Scripts/Networking` | 연결, 수신, 취소, 재연결, 최신 메시지 큐 | 결과 한글 표시 |
| `Scripts/Presentation` | 검증된 상태를 텍스트, 아이콘, 신뢰도로 표현 | WebSocket 라이브러리 직접 호출 |
| `Tests/EditMode` | JSON 파싱, 값 검증, 표시 상태 매핑 | 실제 네트워크 연결 |
| `Tests/PlayMode` | 메인 스레드 큐, UI, Scene과 객체 수명 주기 | Python 판정 알고리즘 |

## 의존 방향

```text
Networking ─> Models
Presentation ─> Models
Configuration ─> Networking

Presentation -X-> WebSocket 라이브러리 직접 호출
Models -X-> Unity UI 또는 네트워크 구현
```

- WebSocket 콜백에서 TextMeshPro, Image, GameObject를 직접 변경하지 않는다.
- 수신 데이터는 스레드 안전한 최신 메시지 큐를 거쳐 메인 스레드에서 적용한다.
- Unity는 Python의 제스처 결과를 다시 계산하지 않는다.
- 지원하지 않는 메시지는 UI에 적용하지 않되 앱 전체를 종료하지 않는다.

## 다음 작업

1. Task 02에서 메시지 필드와 실행·종료 순서를 확정한다.
2. Task 03에서 Unity LTS 버전을 확인하고 이 폴더에 정식 프로젝트를 생성한다.
3. Unity 프로젝트 생성 후 이 문서의 권장 `Assets` 하위 구조를 만든다.
