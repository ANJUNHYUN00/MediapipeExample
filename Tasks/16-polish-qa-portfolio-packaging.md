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

계획.
