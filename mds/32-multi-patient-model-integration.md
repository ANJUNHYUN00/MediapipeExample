# Task 32 - Multi Patient Model Integration

## Goal
서로 다른 환자 FBX 모델을 Patient_01~Patient_10에 각각 하나씩
안전하게 연결할 수 있는 Unity Editor 도구를 만든다.

## Rules
- 환자 루트마다 외형 모델은 하나만 둔다.
- PatientView, Collider, Patient Layer, Display Name,
  Status Card Anchor를 유지한다.
- 새 모델의 SkinnedMeshRenderer만 해당 PatientView Target Renderers에 연결한다.
- 기존 테스트 외형은 삭제하지 않고 Renderer만 비활성화한다.
- 기존 Pose, Raycast, Dwell, World Space Card, HUD는 변경하지 않는다.
- 씬 저장과 Play Mode 실행은 하지 않는다.
- Undo를 지원한다.

## Required Workflow
- Project 창에서 FBX를 선택한다.
- Hierarchy에서 대상 Patient_01~Patient_10을 선택한다.
- 메뉴 실행으로 선택한 FBX를 선택한 Patient의 유일한 외형으로 설치한다.
- 설치 후 모델 Transform만 사용자가 조정할 수 있게 한다.

## Acceptance Criteria
- Patient_01~Patient_04에 서로 다른 모델 하나씩 연결 가능
- 각 환자의 ID, 색상 변경, dwell 카드, Checked 상태가 독립적으로 동작
- 이후 복제해 TR-001~TR-010까지 확장 가능