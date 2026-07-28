# Task 17. World Space Patient Status Card

## Goal

기존 화면 고정 `PatientStatusCardUI` 데이터와 Patient interaction 흐름을 유지하면서,
현재 바라보거나 dwell로 선택한 가상 Patient 위에 World Space AR 상태 카드를
표시한다.

## Safety Boundary

카드는 Patient ID, interaction state, checked 여부만 표시한다. 실제 환자 평가,
진단, 치료 추천, 응급도 또는 중증도 자동 판단을 추가하지 않는다.

## Scope

- `PatientStatusCardUI`의 기존 ID/state/checked 갱신 재사용
- hover 또는 selected Patient 기준 World Space 카드 표시
- Patient별 선택적 카드 anchor와 Inspector offset
- Main Camera billboard와 upright 옵션
- 기존 Screen Space 카드 보존 및 표시 토글
- Figma export Sprite를 연결할 수 있는 background slot
- 기존 `PatientMarker`와 겹치지 않는 기본 높이와 크기
- PlayMode 테스트와 Unity Inspector 설정 문서

## Out of Scope

- Python Pose, WebSocket pose v2 DTO와 fixture 변경
- Pointer raycast 또는 dwell 판정 의미 변경
- 실제 AR hardware 최적화
- 의료 판단 또는 자동 분류

## Implementation

- `WorldSpacePatientStatusCard`가 selected Patient를 우선하고, 설정에 따라 hover
  Patient를 fallback으로 사용한다.
- 기본 offset은 `(0, 1.3, 0)`, Canvas scale은 `0.003`, pixel size는
  `320 x 180`이다.
- `PatientView.StatusCardAnchor`가 연결되면 해당 Transform을 기준으로 하고,
  없으면 PatientView Transform을 사용한다.
- `PatientStatusCardUI`는 Background Sprite와
  `HideCard`/`ShowWaitingState` 빈 상태 옵션을 제공한다.
- `TriageTraceScenarioBootstrap`은 World Space 카드가 없으면 Play Mode에 기본
  카드를 생성하며, 기존 Screen Space Canvas는 옵션으로 남긴다.

## Acceptance Criteria

- hover 또는 dwell 선택 시 해당 Patient 위에 카드가 표시된다.
- selected Patient가 있으면 hover가 사라져도 selected Patient 카드가 유지된다.
- Patient ID, interaction state, checked 여부와 Mark Checked가 기존처럼 갱신된다.
- target이 없으면 설정에 따라 카드가 숨겨지거나 대기 상태를 표시한다.
- 카드가 Main Camera를 향하고 Patient 이동을 따라간다.
- 기존 `StatusCardCanvas`, Pose/WebSocket/raycast/dwell 구조를 삭제하지 않는다.
- 기존 `PatientMarker`보다 높은 기본 위치를 사용한다.

## Verification

- Unity Roslyn으로 Runtime과 PlayMode test assembly 정적 컴파일
- PlayMode test에서 hover 표시, 1.3m offset, billboard, dwell 선택 후 유지 검증
- 데모 Scene Bootstrap 직렬화 값과 README Inspector 절차 확인
- 2026-07-28 실제 Unity Play Mode에서 raycast, hover, dwell, 환자 색상 변경,
  World Space 카드 표시와 Patient ID/state/checked 갱신 수동 확인

## Status

완료. 수동 검증 뒤 카드가 너무 높게 표시되는 문제를 반영해 Play Mode가 아닌
씬 설정의 기본 offset을 `(0, 1.3, 0)`으로 낮췄다. Y는 위아래, Z는 앞뒤 거리이므로
이번 조정에서 X/Z는 `0`으로 유지했다. 데모의 약 Y `0.9` `PatientMarker`와는
중심 간 약 `0.4m` 간격이며, 모델 원점이 다른 Patient는
`PatientView.Status Card Anchor`로 개별 보정한다.
