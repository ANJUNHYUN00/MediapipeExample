# Task 31 - Patient Model Integration

## Goal
기존 TriageTraceEnvironmentPrototype의 Patient_01 기능을 유지하면서
새로 임포트한 Patient_Lying_A FBX를 환자 외형으로 연결한다.

## Safety Rules
- Patient_01의 PatientView, Collider, Patient Layer, Display Name,
  Status Card Anchor, Raycast/Dwell 연결을 유지한다.
- Python Pose, WebSocket, Pointer, Dwell, World Space Card,
  AR Guidance HUD를 수정하지 않는다.
- 기존 테스트 환자 외형은 삭제하지 않고 비활성화한다.
- 씬 저장은 사용자가 결정한다.
- 작업 전후 Unity Console 컴파일 오류를 확인한다.

## Implementation
- 새 모델을 Patient_01 아래의 PatientVisual_Lying_A 자식으로 둔다.
- 새 모델의 실제 SkinnedMeshRenderer를 PatientView의 Target Renderers에 연결한다.
- 기존 환자 외형은 비활성화한다.
- 모델 높이, 회전, 크기와 Collider를 조정하되,
  Patient_01 루트의 포인터 선택 영역과 카드 Anchor는 유지한다.
- 모델의 기본/선택 애니메이션은 정지된 환자 자세로 표시한다.
- 결과가 맞으면 재사용 가능한 Patient_Lying_A 프리팹 구조를 제안한다.

## Acceptance Criteria
- 기존 포인터로 새 환자를 선택할 수 있다.
- dwell 후 World Space 카드가 표시된다.
- Mark Checked 후 색상/카드/HUD 상태가 갱신된다.
- 새 모델이 기존 지하철 내부 크기와 자연스럽게 맞는다.
- 씬을 자동 저장하지 않는다.