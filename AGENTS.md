# AGENTS.md

이 문서는 **MediaPipe–Unity 가위바위보 손 인식 프로젝트의 최상위 안내서이자 문서 목차**다. 사람과 AI 작업자는 이 저장소에서 작업을 시작하기 전에 반드시 이 문서를 먼저 읽고, 이어서 작업과 관련된 `docs` 문서를 확인해야 한다.

## 1. 문서 목차

| 문서 | 내용 | 읽어야 하는 시점 |
|---|---|---|
| [`AGENTS.md`](./AGENTS.md) | 프로젝트 목적, 디렉터리 책임, 공통 작업 원칙 | 모든 작업을 시작할 때 가장 먼저 |
| [`docs/project-plan.md`](./docs/project-plan.md) | 전체 기획, 아키텍처, 제스처 판정, WebSocket 계약, Unity UI, 구현·테스트 계획 | 설계 또는 구현을 시작하기 전 |
| [`Plan/01-architecture-and-environment.md`](./Plan/01-architecture-and-environment.md) | 전체 아키텍처, 책임 경계, 개발 환경과 프로젝트 구조 | 환경 구성과 구현을 시작할 때 |
| [`Plan/02-python-hand-tracking.md`](./Plan/02-python-hand-tracking.md) | Python 카메라 입력과 MediaPipe 손 랜드마크 추적 | 손 추적을 구현할 때 |
| [`Plan/03-gesture-classification.md`](./Plan/03-gesture-classification.md) | 손가락 상태, 가위바위보 분류와 결과 안정화 | 제스처 판정을 구현·조정할 때 |
| [`Plan/04-websocket-protocol-and-python-publisher.md`](./Plan/04-websocket-protocol-and-python-publisher.md) | WebSocket 버전 1 계약과 Python 게시자 | 통신 모델과 서버를 구현할 때 |
| [`Plan/05-unity-receiver-and-ui.md`](./Plan/05-unity-receiver-and-ui.md) | Unity 수신, 메인 스레드 전달과 결과 UI | Unity 클라이언트를 구현할 때 |
| [`Plan/06-integration-test-and-stabilization.md`](./Plan/06-integration-test-and-stabilization.md) | 끝단 통합, 복구, 성능 및 안정화 테스트 | 통합 검증과 완료 판단 시 |
| [`Tasks/01-python-unity-structure-and-responsibilities.md`](./Tasks/01-python-unity-structure-and-responsibilities.md) | Python·Unity 구조와 컴포넌트 책임 확정 | 최초 프로젝트 골격을 만들 때 |
| [`Tasks/02-message-spec-and-run-order.md`](./Tasks/02-message-spec-and-run-order.md) | 버전 1 메시지 계약과 실행·종료 순서 | 양쪽 데이터 계약을 고정할 때 |
| [`Tasks/03-development-environment-setup.md`](./Tasks/03-development-environment-setup.md) | Python·Unity 개발 환경 구성과 검증 | 의존성 설치와 도구 버전을 확정할 때 |
| [`Tasks/04-webcam-loop-and-preview.md`](./Tasks/04-webcam-loop-and-preview.md) | OpenCV 카메라 루프와 미리보기 | 웹캠 입력을 구현할 때 |
| [`Tasks/05-mediapipe-hand-landmark-detection.md`](./Tasks/05-mediapipe-hand-landmark-detection.md) | MediaPipe 한 손 21개 랜드마크 검출 | 손 추적기를 구현할 때 |
| [`Tasks/06-landmark-visualization-and-debug.md`](./Tasks/06-landmark-visualization-and-debug.md) | 랜드마크 연결선과 디버그 오버레이 | 손 추적 결과를 시각 검증할 때 |
| `mds/` 내 문서 | 보조 Markdown 자료와 작업 메모 | 관련 자료가 있을 때 |

현재 프로젝트의 상세 기준 문서는 [`docs/project-plan.md`](./docs/project-plan.md)다. 이후 `docs`에 문서가 추가되면 이 목차도 함께 갱신한다.

## 2. 프로젝트 목적

웹캠 영상을 Python MediaPipe로 분석해 한 손의 21개 랜드마크를 검출하고, 손 모양을 다음 상태 중 하나로 판정하는 앱을 개발한다.

- `ROCK`: 주먹
- `SCISSORS`: 가위
- `PAPER`: 보
- `UNKNOWN`: 손은 검출됐지만 자세를 확정할 수 없음
- `NO_HAND`: 손이 검출되지 않음

MediaPipe 애플리케이션은 판정 결과와 손 정보를 WebSocket JSON 메시지로 실시간 전송한다. Unity 애플리케이션은 메시지를 수신해 연결 상태, 한글 판정 결과, 신뢰도 및 관련 시각 요소를 화면에 표시한다.

초기 범위는 한 명의 한 손 인식과 로컬 PC 내 통신이다. 다중 사용자, 원격 대전, 계정·전적, 모바일 최적화 및 별도 학습이 필요한 커스텀 제스처는 초기 범위에 포함하지 않는다.

## 3. 핵심 아키텍처

```text
Webcam
  -> Python / MediaPipe
     -> 손 랜드마크 검출
     -> 손가락 상태 및 가위바위보 판정
     -> 프레임 간 결과 안정화
     -> WebSocket Server
  -> JSON over ws://127.0.0.1:8765
  -> Unity WebSocket Client
     -> JSON 역직렬화
     -> 메인 스레드 전달
     -> 연결 상태 및 판정 결과 UI
```

기본 기술 방향은 다음과 같다.

- MediaPipe 측: Python 3.11, OpenCV, MediaPipe, `websockets`, `pytest`
- Unity 측: Unity LTS, C#, TextMeshPro, Unity 호환 WebSocket 라이브러리
- 통신 방향: MediaPipe가 WebSocket 서버, Unity가 클라이언트
- 메시지 스키마: `docs/project-plan.md`의 `hand_gesture` 버전 1 계약을 기준으로 함
- 제스처 값: `ROCK`, `SCISSORS`, `PAPER`, `UNKNOWN`, `NO_HAND`

통신 스키마나 제스처 enum을 변경할 때는 MediaPipe 송신 코드, Unity 수신 모델, 테스트 및 관련 문서를 같은 작업에서 함께 갱신한다.

## 4. 폴더별 역할

### `docs/`

프로젝트의 기획과 기술 기준을 보관한다.

- 프로젝트 목표와 범위
- 시스템 아키텍처
- 손 인식 및 판정 규칙
- WebSocket 메시지 계약
- Unity 화면과 동작 기준
- 테스트 계획과 완료 기준

구현과 문서가 충돌하면 임의로 한쪽을 선택하지 말고, 사용자의 최신 요구사항을 우선해 양쪽을 일치시킨다.

### `Plan/`

프로젝트를 수행하기 위한 단계별 구현 계획을 보관한다.

- 작업 단계와 선후 관계
- 기술 선택과 설계 결정
- 마일스톤
- 위험 요소와 대응 계획
- 단계별 검증 방법

계획 문서는 목표와 완료 조건이 검증 가능하도록 작성하고, 실제 진행 방향이 바뀌면 갱신한다.

### `Tasks/`

실제로 수행할 세부 작업과 진행 상태를 관리한다.

- 할 일과 우선순위
- 진행 상태
- 작업별 완료 조건
- 구현 또는 검증 결과
- 발견된 후속 작업

작업을 완료로 표시하기 전에 관련 테스트나 수동 검증 결과를 기록한다.

### `Mediapipe/`

Python 기반 손 인식 및 WebSocket 서버 애플리케이션을 구현한다.

- 웹캠 입력과 종료 처리
- MediaPipe 21개 손 랜드마크 검출
- 손가락 펼침 상태 계산
- 주먹, 가위, 보, 미인식 판정
- 프레임 간 판정 안정화
- WebSocket JSON 직렬화 및 송신
- 카메라·연결·판정 로그
- Python 단위 및 통합 테스트

화면의 단순 `y` 좌표 비교에만 의존하지 말고 손 방향과 기울기를 고려한 관절 벡터 또는 각도 기반 판정을 우선한다. 카메라 영상은 기본적으로 로컬에서만 처리하며 저장하거나 외부로 전송하지 않는다.

### `MediapipeUnity/`

Unity WebSocket 클라이언트와 사용자 인터페이스를 구현한다.

- WebSocket 연결, 종료 및 재연결
- JSON 메시지 역직렬화
- 수신 데이터 모델
- 백그라운드 수신 결과의 메인 스레드 전달
- 주먹, 가위, 보, 미인식 및 손 없음 UI
- 연결 상태, 신뢰도와 필요 시 좌우 손 정보 표시
- Unity 측 파싱 및 동작 테스트

WebSocket 콜백에서 Unity UI를 직접 변경하지 않는다. 수신 메시지를 스레드 안전한 방식으로 전달하고 Unity 메인 스레드에서 UI를 갱신한다.

### `mds/`

정식 기획서로 승격되기 전의 보조 Markdown 자료, 조사 결과 및 작업 메모를 보관한다. 프로젝트의 확정된 기준은 `docs/`에 반영하며, 중요한 결정이 `mds/`에만 남지 않도록 한다.

## 5. AI 필수 작업 절차

AI는 저장소에서 어떤 작업을 하든 다음 순서를 따른다.

1. 루트의 `AGENTS.md`를 가장 먼저 읽는다.
2. `docs/`의 문서 목록을 확인하고, 작업과 관련된 문서는 수정 전에 읽는다.
3. `Plan/`과 `Tasks/`에서 현재 계획, 작업 상태 및 완료 조건을 확인한다.
4. 변경 대상 폴더의 기존 코드, 설정, 테스트와 하위 `AGENTS.md` 유무를 확인한다.
5. 기존 사용자 변경 사항을 보존하고 요청 범위 안에서만 작업한다.
6. 구현 전에 MediaPipe와 Unity 사이의 메시지 계약에 미치는 영향을 확인한다.
7. 변경 후 영향 범위에 맞는 테스트 또는 수동 검증을 수행한다.
8. 코드, 통신 계약, 계획 또는 사용 방법이 바뀌었다면 관련 문서도 같은 작업에서 갱신한다.
9. 완료 시 변경 파일, 검증 결과 및 남은 위험 요소를 사용자에게 간결하게 보고한다.

관련 문서를 읽지 않은 상태에서 구현을 시작하거나, 검증하지 않은 작업을 완료로 간주하지 않는다.

## 6. 구현 원칙

- 손 인식, 제스처 판정, 결과 안정화, 통신 및 UI 책임을 분리한다.
- 포트, 카메라 번호, 신뢰도 임계값과 안정화 프레임 수를 코드 곳곳에 하드코딩하지 않고 설정으로 관리한다.
- 손 미검출은 `NO_HAND`, 불확실한 자세는 `UNKNOWN`으로 명확히 구분한다.
- 잘못된 JSON, 카메라 열기 실패, WebSocket 종료가 전체 앱의 예기치 않은 종료로 이어지지 않게 한다.
- 메시지에는 스키마 버전을 유지하고, Unity는 모르는 추가 필드를 허용할 수 있게 설계한다.
- 연결 상태, 카메라 상태, 판정 변경 및 예외 원인을 진단 가능한 수준으로 기록한다.
- 기능 변경에는 가능한 범위에서 자동화 테스트를 추가하고, 카메라가 필요한 항목은 재현 가능한 수동 테스트 절차를 문서화한다.

## 7. 기본 완료 기준

기능이 완료됐다고 판단하려면 최소한 다음 조건을 만족해야 한다.

- 좌우 손과 일반적인 손 방향에서 주먹, 가위, 보가 안정적으로 구분된다.
- 손 없음과 불확실한 자세가 각각 `NO_HAND`, `UNKNOWN`으로 처리된다.
- MediaPipe가 문서화된 JSON 계약에 맞춰 결과를 전송한다.
- Unity가 결과와 연결 상태를 실시간으로 올바르게 표시한다.
- 연결 순서가 달라도 정상 연결되고, 종료 후 재연결이 가능하다.
- 주요 판정과 메시지 파싱 로직이 테스트되었다.
- 실행 방법과 알려진 제약이 문서화되었다.

## 8. 문서 유지 규칙

- 새 핵심 문서를 추가하거나 이름을 바꾸면 이 파일의 문서 목차를 갱신한다.
- 아키텍처, 메시지 스키마, 제스처 규칙 또는 완료 기준이 바뀌면 `docs/project-plan.md`도 갱신한다.
- 계획 변경은 `Plan/`, 작업 상태 변경은 `Tasks/`에 반영한다.
- 문서 예시와 실제 코드의 필드명 및 enum 값은 항상 일치시킨다.
- 임시 메모가 프로젝트의 확정 기준이 되면 `mds/`에서 `docs/`로 정리한다.
