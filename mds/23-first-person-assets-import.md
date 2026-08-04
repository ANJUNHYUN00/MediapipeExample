Task 23의 실제 임포트 결과를 확인하고 Task 24 구현 준비를 해줘.

대상 에셋:
- Assets/FirstPerson/Arms/arms2.fbx
- Assets/FirstPerson/MedicalBag/model.dae
- 각 Assets/FirstPerson 하위의 Materials, Textures

반드시 먼저 수행:
1. AGENTS.md와 관련 Task 문서를 읽는다.
2. arms2.fbx의 실제 Hierarchy, Rig, Renderer, bone 이름을 확인한다.
3. model.dae의 실제 Hierarchy, Renderer, 재질 연결 상태를 확인한다.
4. 에셋의 이름이나 bone 경로를 추측하지 않는다.
5. 현재 Unity 씬, Python Pose, WebSocket, Raycast, Dwell, 환자 카드 로직은 수정하지 않는다.

그 다음:
- Main Camera의 자식으로 사용할 FirstPersonPresentation 구조 설계를 작성한다.
- 월드 카메라와 별도 FirstPersonHands 전용 카메라/레이어가 필요한지 판단한다.
- Main Camera 태그는 하나만 유지한다.
- 왼손 + 의료가방, 오른손 + 기본 자세/포인팅 자세의 구현 가능성을 분석한다.
- 현재 Pose의 pointing 상태와 Unity 포인터 방향을 활용한 “대략적인” 오른팔 방향 연동 가능성을 판단한다.
- 손가락 정밀 추적, Hand Landmarker 추가, Python 프로토콜 변경은 범위에서 제외한다.
- 코드나 씬은 아직 수정하지 말고, 확인한 에셋 구조와 다음 구현 계획만 보고한다.