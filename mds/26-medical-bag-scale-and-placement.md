# Task 26 - Medical Bag Scale and Placement

## 현재 문제
Assets/FirstPerson/MedicalBag/model.dae가 실제 Unity 단위보다 매우 크게 들어와,
FirstPersonPresentation의 MedicalBag이 지하철 전체를 덮을 만큼 커진다.

## 목표
- 구급상자를 왼손에 든 것처럼 보이는 현실적인 크기로 정규화한다.
- 모델 원점과 단위가 달라도 자동으로 크기를 계산한다.
- 이후 Inspector에서 위치와 회전을 미세 조정할 수 있게 한다.

## 요구사항
1. Python Pose, WebSocket, PointerOrigin, Raycast, Dwell, 환자 카드 코드는 변경하지 않는다.
2. 팔과 가방 재질/텍스처는 변경하지 않는다.
3. MedicalBag의 모든 Renderer bounds를 합산해 실제 최대 크기를 계산한다.
4. 목표 최대 크기 기본값을 약 0.32m로 두고, 자동 uniform scale을 적용한다.
5. 현재 left hand 부착 구조는 유지한다.
6. FirstPersonPresentationController Inspector에 다음을 추가한다.
   - Auto Normalize Medical Bag Scale
   - Medical Bag Max Dimension
   - Medical Bag Local Position
   - Medical Bag Local Rotation
   - Medical Bag Local Scale Multiplier
7. Triage Trace > Refresh First-Person Presentation 메뉴 실행 시
   기존 설치된 MedicalBag에도 크기 정규화 설정이 적용되게 한다.
8. 크기 보정 후에도 가방이 보이지 않으면 원인을 Console에 진단 로그로 남긴다.
9. 열린 Unity 씬은 직접 저장하거나 Play Mode 실행하지 않는다.
10. 변경 파일, Unity에서 실행할 메뉴, Inspector 조정 순서를 요약한다.