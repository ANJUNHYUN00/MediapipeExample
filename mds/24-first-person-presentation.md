# Task 24 - First Person Presentation

## 목표
TriageTraceEnvironmentPrototype에서 Main Camera 시점에
왼손의 의료가방과 양팔을 표시할 수 있는 안전한 1인칭 프레젠테이션 기반을 만든다.

## 확정된 에셋
- Arms: Assets/FirstPerson/Arms/arms2.fbx
- Medical Bag: Assets/FirstPerson/MedicalBag/model.dae

## 확인된 골격 경로
- root/chest/arm.L/elbow.L/forearm.L/hand.L
- root/chest/arm.R/elbow.R/forearm.R/hand.R
- 각 손에는 point, middle, ring, pinky, thumb bone이 존재함

## 반드시 지킬 조건
1. 기존 Main Camera 태그는 하나만 유지한다.
2. PointerOrigin, Python Pose, WebSocket, Raycast, Dwell, 환자 카드 로직을 변경하지 않는다.
3. 기존 환자 레이어와 FirstPersonHands 레이어를 섞지 않는다.
4. 현재 팔과 가방의 재질 및 텍스처 연결은 수정하지 않는다.
5. 씬을 직접 수정하거나 저장하지 않는다. Play Mode에서 실행하지 않는다.
6. 열린 Unity Editor의 씬을 덮어쓰지 않는다.

## 구현할 파일
- Assets/Scripts/Presentation/FirstPersonPresentationController.cs
- Assets/Editor/FirstPersonPresentationInstaller.cs
- 필요한 경우 테스트 또는 README/Task 문서

## 구현 요구사항
1. FirstPersonHands라는 전용 Layer를 사용한다.
2. Main Camera의 월드 렌더링에서는 FirstPersonHands 레이어를 제외한다.
3. FirstPersonHandsCamera는 Main Camera의 자식으로 만들되 MainCamera 태그를 부여하지 않는다.
4. Built-in Render Pipeline 방식으로 HandsCamera가 FirstPersonHands만 렌더하도록 구성한다.
5. HandsCamera는 월드 카메라보다 나중에 렌더되어 팔과 가방이 벽에 가려지지 않게 한다.
6. Main Camera 아래에 FirstPersonPresentation 루트를 만들 수 있는 Editor 메뉴를 구현한다.
7. ArmsRig와 MedicalBag을 배치할 구조를 만든다.
8. MedicalBag은 hand.L 기준으로 부착할 수 있게 한다.
9. 오른팔은 PosePointerLineRenderer의 현재 방향과 visible 상태를 읽어 제한된 yaw/pitch 범위 안에서 부드럽게 움직이게 한다.
10. 포인터가 유효하지 않으면 기본 자세로 부드럽게 복귀한다.
11. 기존 raycast 및 포인터 방향 자체는 절대 변경하지 않는다.
12. 손가락 정밀 추적, Hand Landmarker, Python 메시지 변경은 하지 않는다.
13. 포인팅 손가락 자세는 bone별 offset을 Inspector에서 조정할 수 있는 구조로만 준비한다. 실제 bone 축을 추측해 고정값을 강제하지 않는다.
14. 메뉴 실행은 TriageTraceEnvironmentPrototype 씬, Play Mode 아님 상태에서만 동작하도록 보호한다.
15. Undo를 지원한다.

## 완료 기준
- 코드 컴파일 오류 없음
- Unity 씬 파일은 변경하지 않음
- 설치 메뉴와 사용 순서를 README 또는 Task 문서에 기록
- 변경 파일, Unity에서 직접 할 설정, 테스트 순서를 요약

## 구현 메모 (2026-07-31)

- 설치 메뉴: `Triage Trace > Install First-Person Presentation`
- 메뉴는 `TriageTraceEnvironmentPrototype`이 활성 씬이고 Edit Mode일 때만 동작한다.
- 메뉴 실행 전에는 씬을 수정하지 않는다. 실행 후 생성되는 오브젝트와 카메라 culling mask 변경은 Undo 한 단계로 되돌릴 수 있으며, 저장은 사용자가 명시적으로 수행한다.
- 메뉴는 `FirstPersonHands` user layer를 만들거나 재사용하고, Main Camera에서는 해당 레이어를 제외한다. `FirstPersonHandsCamera`는 Main Camera의 자식이지만 `MainCamera` 태그를 갖지 않는다.
- 재질 또는 텍스처는 변경하지 않는다. 설치 후 Inspector에서 ArmsRig/MedicalBag transform, 손가락 pointing offset 및 오른팔 축을 실제 에셋 축에 맞춰 조정한다.
