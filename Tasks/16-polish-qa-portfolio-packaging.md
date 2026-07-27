# Task 16. Polish, QA, Portfolio Packaging

## Goal

Triage Trace MVP를 시연·제출 가능한 형태로 정리한다. README, 실행 순서, 데모 시나리오, 발표자료/포트폴리오 설명, known limitations를 정리하고 AR hardware 없이 Unity simulation MVP임을 명확히 설명한다.

## Scope

- README 정리
- Python/Unity 실행 순서 정리
- 데모 시나리오 정리
- 발표자료/포트폴리오용 설명 정리
- known limitations 정리
- 비의료 안전 고지 최종 점검
- AR hardware 없이 Unity simulation MVP임을 명확히 설명
- Task 10~15 구현 결과와 검증 결과 정리

## Out of Scope

- WebSocket `pose_pointer` v2 DTO 구조 변경
- 새 기능 추가
- AR hardware 배포
- 실제 의료 판단, 자동 진단, 중증도 판단 AI 구현
- 병원 시스템 또는 실제 환자 데이터 연동

## Implementation Notes

- README는 Python 실행, Unity 실행, 연결 확인, 데모 조작 순서를 분리한다.
- 포트폴리오 설명은 "MediaPipe Pose 기반 AR 시뮬레이션 입력"과 "가상 Patient 확인 추적"을 중심으로 작성한다.
- "중증도 판단 AI", "진단 자동화", "환자 평가 시스템"처럼 오해될 수 있는 표현을 피한다.
- known limitations에는 조명·카메라 각도·가림·Unity license 테스트 미확인 항목·AR hardware 미사용을 포함한다.
- triage 색상 red/yellow/green/black은 가상 시나리오의 severity label로만 조심스럽게 설명하고, interaction state 색상 원칙을 문서에 남긴다.

## Acceptance Criteria

- 신규 사용자가 README만 보고 Python과 Unity를 순서대로 실행할 수 있다.
- 데모 시나리오가 Task 15의 end-to-end 흐름을 설명한다.
- 프로젝트가 자동 의료 진단 시스템이 아니라 Unity AR simulation MVP임이 명확하다.
- known limitations와 테스트 미확인 항목이 숨겨지지 않는다.
- 활성 Plan/Tasks/Docs 링크가 깨지지 않는다.
- commit/push 없이 문서 변경으로 마무리한다.

## Test Notes

- 문서 링크와 파일 경로를 검사한다.
- 주요 Markdown 문서의 제목, Task 번호, 상태가 일관적인지 확인한다.
- 가능하면 JSON fixture와 기존 v2 parser 테스트가 DTO 변경 없이 유지되는지 확인한다.

## Status

완료. Unity simulation MVP를 발표와 포트폴리오에 사용할 수 있도록 README, 실행 순서, 씬 설정 체크리스트, demo scenario, known limitations, troubleshooting과 QA 기준을 정리했다.

정리 결과:

- README에 최종 실행 순서를 Python 환경 준비부터 Unity Play Mode 확인까지 초보자가 따라갈 수 있는 순서로 정리했다.
- Unity Editor setup, Patient object setup, UI setup 체크리스트를 추가했다.
- Patient에는 `Collider`, `Renderer`, `PatientView`, Patient Layer가 필요하다는 점을 명시했다.
- Patient Status Card Canvas와 Mark Checked 연결 방법을 정리했다.
- demo scenario를 pointer line, hover, dwell `InProgress`, status card, Mark Checked, `Checked` 보호 흐름으로 정리했다.
- pointer line과 interaction state 색상은 cyan/blue/white/gray 계열을 권장하고, red/yellow/green/black은 interaction state 색상으로 쓰지 않는 원칙을 유지했다.
- known limitations에 AR hardware 없음, PC webcam 기반 pose input, Unity simulation MVP, 의료 판단/자동 진단 아님, Unity license 테스트 제한을 기록했다.
- troubleshooting에 pointer line, hover, dwell selection, status card, Mark Checked, Unity license, Python publisher, WebSocket 연결 문제 확인 항목을 추가했다.
- Manual QA Checklist와 최소 데모 성공 기준을 추가했다.
- Portfolio Summary를 추가해 프로젝트를 "MediaPipe Pose 기반 Unity AR simulation interaction prototype"으로 설명할 수 있게 했다.
- WebSocket pose v2 DTO, 의료 판단 로직, AR hardware/mobile build는 변경하거나 추가하지 않았다.

검증 결과:

- Markdown 링크와 Task 16 필수 섹션을 검사했다.
- Unity batchmode PlayMode 테스트는 현재 환경의 Unity licensing 초기화 실패로 완료하지 못했다. 최신 실패 로그는 `MediapipeUnity/Logs/task15-playmode.log`와 동일한 licensing failure 계열이다.
