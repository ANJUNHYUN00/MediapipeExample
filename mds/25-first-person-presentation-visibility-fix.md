# Task 25 Fix - First Person Arms Visibility

## 확인된 상태
- FirstPersonHandsCamera: Depth only, FirstPersonHands만 렌더, Near Clip 0.01
- ArmsRig와 실제 arm Skinned Mesh Renderer: FirstPersonHands 레이어
- arm 재질 연결 정상
- 그런데 Play Mode Game View에서 팔과 가방이 보이지 않음

## 수정 목표
팔 모델의 원점이나 bounds가 일반적인 1인칭 좌표와 다르더라도,
실제 렌더러 bounds 기준으로 화면 아래 중앙의 안전한 위치에 자동 배치한다.

## 요구사항
1. 기존 Python Pose, PointerOrigin, Raycast, Dwell, 환자 카드 로직은 변경 금지.
2. FirstPersonHandsCamera의 MainCamera 태그는 계속 Untagged.
3. 기존 FirstPersonHands 레이어 규칙을 유지.
4. FirstPersonPresentationController에 Inspector 조정 항목을 추가:
   - Visual Center Camera Local 기본값 (0, -0.42, 0.65)
   - 자동 bounds 정렬 사용 여부
5. Play Mode 시작 후 실제 SkinnedMeshRenderer bounds를 기준으로
   ArmsRig를 카메라 기준 화면 아래 중앙으로 한 번 정렬한다.
6. Hand camera의 Clear Flags, Culling Mask, Depth, Near/Far Clip을
   설치 및 갱신 시 명시적으로 다시 설정한다.
7. 기존 설치된 FirstPersonPresentation을 대상으로 설정을 갱신하는
   `Triage Trace > Refresh First-Person Presentation` 메뉴를 추가한다.
8. 기존 손/가방 재질을 수정하지 않는다.
9. Unity 씬은 직접 수정하거나 저장하지 않는다.
10. 코드 컴파일 확인과 Unity에서 실행할 메뉴/테스트 순서를 요약한다.