# Task 30 - AR Glass Operations HUD

## 목표
기존 ARGuidanceHUD를 확장해,
지하철 재난 시뮬레이션에 맞는 절제된 AR 글래스형 운영 HUD를 만든다.

## 디자인 원칙
- 중앙 시야를 가리는 큰 창을 만들지 않는다.
- 낮은 불투명도의 검정/청회색 패널, 흰색 텍스트,
  cyan 활성 상태, amber 미확인 상태만 사용한다.
- 게임 점수, 체력바, 무기 UI, 미니맵은 추가하지 않는다.
- 실제 의료 판단이나 응급도 판정처럼 보이는 표현은 하지 않는다.
- 기존 World Space 환자 카드는 유지한다.

## UI 구성
1. 상단 중앙
   - 간단한 방향 표시
   - 현재 구역: CAR 01 / CAR 02 / CAR 03 또는 PLATFORM
   - Simulation Only 작은 고지

2. 좌측 상단
   - LINK: CONNECTED / CONNECTING
   - POSE: TRACKING / WAITING

3. 좌우 가장자리
   - 기존 화면 밖 미확인 환자 방향 화살표 유지
   - 대상 ID와 간단한 거리 표시

4. 좌측 하단
   - PATIENT STATUS
   - UNCONFIRMED 수 / CHECKED 수
   - TR-001~003의 작은 상태 행

5. 우측 상단
   - LOCAL TEAM SYNC
   - 환자가 Checked가 될 때만 최근 이벤트 3개 표시
   - 예: TR-001 CONFIRMATION RECORDED
   - 실제 네트워크·다중 사용자 통신처럼 주장하지 않는다.

## 구현 조건
1. 기존 ARGuidanceHUD를 확장하거나 안전하게 갱신한다.
2. 별도의 중복 Canvas를 만들지 않는다.
3. Python Pose, WebSocket, PointerOrigin, Raycast, Dwell,
   PatientView 상태 전이, World Space 카드 구조는 변경하지 않는다.
4. UI는 raycastTarget을 끄고 기존 입력을 막지 않는다.
5. TextMeshPro를 사용한다.
6. 환자 Checked 상태 변화를 읽어 로컬 이벤트 피드를 갱신한다.
7. TriageTraceEnvironmentPrototype 전용 Editor 메뉴로 설치/갱신한다.
8. Play Mode에서 씬을 수정하지 않고, Undo를 지원한다.
9. 열린 Unity 씬은 직접 저장하지 않는다.
10. 변경 파일, Unity 메뉴, Inspector 조정 항목, 테스트 순서를 요약한다.

## 완료 기준
- Game View에서 화면 가장자리 중심의 AR HUD가 보인다.
- 기존 포인터, 카드, 환자 선택이 유지된다.
- 환자 확인 시 상태 카운트와 로컬 이벤트가 갱신된다.