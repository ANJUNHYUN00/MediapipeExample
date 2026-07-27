# Task 14. Patient Status Card UI

## Goal

선택된 가상 Patient의 상태 카드를 Unity UI로 표시한다. HUD는 AR 글래스 시뮬레이션 느낌을 주되, 실제 의료 판단이나 자동 진단처럼 보이지 않게 Patient ID, interaction state, checked 여부 중심으로 구성한다.

## Scope

- 선택된 `PatientView`의 상태 카드 UI 구현
- Patient ID 표시
- current interaction state 표시
- checked 여부 표시
- AR 글래스 시뮬레이션용 HUD 스타일 적용
- 비의료 시뮬레이션 고지 유지

## Out of Scope

- WebSocket `pose_pointer` v2 DTO 구조 변경
- Python Pose 또는 pointing 계산 변경
- 실제 vital sign, 질환, 부상 추론 표시
- 자동 진단, 치료 추천, 환자 우선순위 산출
- 최종 README/포트폴리오 패키징

## Implementation Notes

- HUD copy는 `Patient ID`, `Interaction`, `Checked`처럼 진행 상태 중심으로 둔다.
- 자동 의료 판단으로 오해될 수 있는 `diagnosis`, `risk score`, `AI severity` 같은 표현을 사용하지 않는다.
- card highlight와 progress 색상은 cyan/blue/white 계열을 사용한다.
- triage severity 색상 red/yellow/green/black이 필요하면 가상 시나리오의 미리 정한 label로만 표시하고 interaction state 색상과 분리한다.
- 작은 화면에서도 텍스트가 겹치지 않도록 Canvas layout 제약을 둔다.

## Acceptance Criteria

- selected Patient가 있으면 상태 카드가 표시된다.
- 카드에 Patient ID, current interaction state, checked 여부가 표시된다.
- selected Patient가 없거나 pointer/input이 invalid이면 카드가 안전한 빈 상태를 표시하거나 숨겨진다.
- 의료 판단, 자동 진단, 실제 환자 평가처럼 보이는 문구가 없다.
- 기존 `pose_pointer` v2 DTO 구조가 변경되지 않는다.

## Test Notes

- selected 없음, selected 있음, checked 완료, state 변경 시 UI 갱신을 검증한다.
- Canvas 스케일과 주요 해상도에서 텍스트 겹침이 없는지 수동 확인한다.
- 비의료 고지가 화면에 유지되는지 확인한다.

## Status

완료. `PatientStatusCardUI`를 Presentation 계층에 추가했고, dwell selection으로 `InProgress`가 된 Patient를 Canvas 기반 상태 카드에 바인딩할 수 있게 했다.

구현 결과:

- `PatientStatusCardUI`는 `PatientView`를 `Bind(PatientView)`로 표시하고 `Clear()`로 비운다.
- 카드에는 Patient ID, Interaction State, Checked 여부를 표시한다.
- `PatientView`에 `displayName` 필드와 `DisplayName` 속성을 추가했다.
- `PatientView.StateChanged` 이벤트를 추가해 상태 변경 시 UI가 갱신될 수 있게 했다.
- `PatientDwellSelector`에 선택적 `PatientStatusCardUI` 참조를 추가했고, 새 Patient가 `InProgress`가 되면 해당 카드를 바인딩한다.
- `PatientStatusCardUI.MarkChecked()`와 Mark Checked 버튼 listener가 현재 Patient의 `MarkChecked()`를 호출한다.
- null Patient는 예외 없이 empty 상태를 표시하고 버튼을 비활성화한다.
- UI 색상은 cyan/blue/white/gray 계열을 기본으로 하며 red/yellow/green/black triage severity 색상을 interaction state UI에 사용하지 않는 원칙을 유지했다.
- WebSocket 수신 구조와 `PosePointerState` DTO는 변경하지 않았다.

검증 결과:

- PlayMode 테스트를 추가해 Patient 바인딩, `InProgress` 표시, Mark Checked 버튼 동작, Checked UI 갱신, null 바인딩, dwell selection 후 카드 바인딩을 검증하도록 했다.
- Unity batchmode PlayMode 테스트는 라이선스 초기화 실패로 완료하지 못했다. 상세 로그는 `MediapipeUnity/Logs/task14-playmode.log`를 확인한다.
