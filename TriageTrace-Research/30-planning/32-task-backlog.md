# 32. 작업 백로그

> 일정 배치: [`31-timeline.md`](./31-timeline.md)
> 기존 저장소의 `Tasks/` 문서 규약(목표·범위·제외·수용기준)을 따른다. 착수 시 해당 형식의 Task 문서를 `mds/`에 35번부터 이어서 만든다.

## 범례

| 표기 | 의미 |
|---|---|
| 🔴 | 크리티컬 패스 |
| ⬜ 미착수 · 🟡 진행 · ✅ 완료 | 상태 |
| 선행 | 이 작업 전에 끝나야 하는 것 |

---

## Level 1 — 계측 (트랙 A)

### 🔴 L1-4 · ExperimentLogger ⬜ · 1.5일 · 선행 없음
**목표** 모든 상호작용 이벤트를 파일로 남긴다.
**산출물** `Assets/Scripts/Experiment/ExperimentLogger.cs`
**범위** `PatientView.StateChanged` 구독, JSONL 이벤트 로그 + CSV 시행 요약, 이벤트마다 flush.
**제외** 기존 스크립트 수정. UI 추가.
**수용 기준**
- [ ] Play Mode 1회 실행 시 `events_*.jsonl`과 `trials_*.csv` 생성
- [ ] Play Mode 강제 중단 후에도 직전 이벤트까지 보존
- [ ] `dwell_start` 하나당 `dwell_complete` 또는 `dwell_abort`가 정확히 하나
- [ ] 기존 파일 diff가 0줄
**참조** [`22`](../20-engineering/22-instrumentation-spec.md) §3

> **먼저 해도 되는 작업이다.** 단서 레이어 없이도 착수 가능하므로, 일정 압박이 있으면 이것부터 한다.

### 🔴 L1-1 · PatientUrgencyProfile + 라벨 규칙 ⬜ · 1.5일 · 선행 없음
**목표** 관찰 가능한 단서 4차원과 그로부터 결정되는 우선순위 라벨.
**산출물** `PatientUrgencyProfile.cs`, `UrgencyLabelRules.cs`, `Tests/EditMode/UrgencyLabelRulesTests.cs`
**범위** enum 4종, 규칙표 구현(P4→P1→P2→P3 순), 읽기 전용 라벨 프로퍼티.
**제외** `PatientView` 수정. 애니메이션. UI.
**수용 기준**
- [ ] 규칙표의 모든 분기에 대한 EditMode 테스트 통과
- [ ] Inspector에서 라벨을 직접 설정할 수 없음
- [ ] `PatientView`와 별도 컴포넌트
**참조** [`14`](../10-research/14-severity-cue-spec.md) §2~3

### L1-2 · 단서 애니메이션 ⬜ · 2.5일 · 선행 L1-1
**목표** 4개 cue를 화면에서 판독 가능하게 표현.
**산출물** Animator Controller 1종, `PatientModelIntegrationMenu.cs` 수정
**범위** 상태 `Idle_Still` / `Idle_Breathing` / `Gesture_Wave` / `Walk_Slow`, Root Motion off, dwell 시 `Responds` 일회성 트리거.
**제외** 잔혹 표현. 얼굴 애니메이션.
**수용 기준**
- [ ] Animator 활성화 후 환자 위치·회전·collider 불변
- [ ] `TenPatientIdentityAndPlacementMenu` 검증 재실행 시 경고 0
- [ ] seated/prone/supine 자세에서 모든 cue 판독 가능
- [ ] 8m 거리에서 움직임 구분 가능
**리스크** 기존 설치 도구가 Animator를 의도적으로 비활성화한다. 이 결정을 뒤집는 것이므로 회귀 주의.

### L1-3 · ExperimentConfig ⬜ · 0.5일 · 선행 없음
**산출물** `ExperimentConfig.cs` (ScriptableObject)
**수용 기준** [ ] 런타임 변경 불가 [ ] 세션 헤더에 전 필드 직렬화

### 🔴 L1-5 · ExperimentRunner ⬜ · 2일 · 선행 L1-1, L1-3, L1-4
**목표** 블록 관리, 판단 입력 수집, SAGAT 프로브.
**산출물** `ExperimentRunner.cs`
**범위** 블록 경계, 최종 판단 입력(숫자키 1~4), SAGAT 화면 정지 + 질문, 조건별 HUD/카드 전환 지시.
**제외** 추정 생성(L2-1이 담당). 통계 계산.
**수용 기준**
- [ ] 3블록 연속 실행 시 조건이 정확히 전환됨
- [ ] 판단 입력 시각이 dwell 완료 시각과 분리 기록
- [ ] SAGAT 프로브가 로그에 남음
- [ ] 기존 선택 파이프라인 코드 무수정

### L1-6 · 시나리오 Export/Import ⬜ · 1.5일 · 선행 L1-1
**산출물** Editor 메뉴 2종
**수용 기준** [ ] Export→Import 왕복 후 배치가 동일 [ ] Import 직후 배치 검증 자동 실행 [ ] Undo 지원

### L1-7 · IPointerSource 추상화 + 마우스 베이스라인 ⬜ · 1일 · 선행 없음
**목표** 입력원을 교체 가능하게. **HL2 이식의 사전 작업이기도 하다.**
**산출물** `IPointerSource.cs`, `MousePointerSource.cs`, `PointerRaycaster` 최소 수정
**수용 기준**
- [ ] 마우스/제스처 조건의 raycast 거리·레이어·dwell이 동일
- [ ] `PosePointerLineRenderer`가 인터페이스를 구현
- [ ] 기존 동작 회귀 없음 (PlayMode 테스트)

> **이 작업이 HL2 이식 기간을 며칠 단축한다.** 생략하지 말 것.

### L1-8 · 로그 무결성 검증 스크립트 ⬜ · 1일 · 선행 L1-4
**산출물** `tools/validate_logs.py`
**범위** 행 수 검증, 이벤트 짝 검증, 결측 탐지, 요약 통계 출력
**수용 기준** [ ] 고의로 손상시킨 로그를 검출 [ ] 파일럿 로그 전건 통과

---

## Level 2 — 불확실성 표현 (트랙 A)

### 🔴 L2-1 · EstimatorSimulator ⬜ · 1일 · 선행 L1-1
**산출물** `EstimatorSimulator.cs` + 단위 테스트
**수용 기준**
- [ ] 동일 seed+reliability → 동일 오라벨 배치
- [ ] R100에서 오라벨 0
- [ ] 모든 조건에서 저추정 오라벨 ≥ 1
- [ ] P4가 오라벨 대상이 되지 않음
- [ ] 인접 등급으로만 교란
**참조** [`23`](../20-engineering/23-uncertainty-hud-spec.md) §1

### L2-2 · 월드 카드 3조건 ⬜ · 2일 · 선행 L2-1
**산출물** `WorldSpacePatientStatusCard.cs` 확장
**수용 기준**
- [ ] 3조건 카드의 물리적 크기가 동일
- [ ] 색 없이도 라벨 판독 가능 (이중 부호화)
- [ ] `SIMULATION ONLY` 상시 표시
- [ ] 조건 C의 보류 표시가 라벨을 노출하지 않음

### L2-3 · HUD nearby 패널 확장 ⬜ · 1일 · 선행 L2-1
**수용 기준** [ ] HUD가 입력에 관여하지 않음 (기존 계약) [ ] 조건별 열 구성 정확

### L2-4 · 이중 부호화 시각 자산 ⬜ · 0.5일
**수용 기준** [ ] 색각 시뮬레이터 통과 [ ] P4에 흑색 미사용(HL2 대비)

### L2-5 · 9조합 수동 검증 ⬜ · 0.5일 · 선행 L2-2, L2-3
**수용 기준** [ ] 3표현 × 3신뢰도 스크린샷 9장 확보 (논문 그림 자산이기도 함)

---

## 연구 운영 (트랙 A)

### 🔴 R-1 · IRB 신청 ⬜ · 2일 · 🔴 **10월 중순 제출 필수**
**산출물** 신청서, 동의서, 개인정보 처리방침, 디스트레스 프로토콜, 시나리오 스크린샷
**참조** [`15`](../10-research/15-ethics-and-irb.md) §7
**리스크** 심의 2~4주 → 늦으면 본 실험 전체가 밀린다

### R-2 · 설문지 구성 ⬜ · 0.5일
NASA-TLX, SUS, 신뢰 척도 3문항, SAGAT 프로브 문항, 인터뷰 가이드

### R-3 · 참가자 모집 ⬜ · 지속 · **10월 중순 시작**
목표 24명. 모집 공고, 일정 조율 시트

### R-4 · 파일럿 5~8명 ⬜ · 3일 · 선행 L1 전체, L2 전체, R-1, R-2
**참조** [`13`](../10-research/13-experiment-design.md) §10

### R-5 · 본 실험 16~24명 ⬜ · 3주 · 선행 R-4
### R-6 · 분석 ⬜ · 1주 · 선행 R-5
**참조** [`13`](../10-research/13-experiment-design.md) §9 — **사전 확정된 계획을 따른다**

---

## 집필 (트랙 A)

| ID | 산출물 | 마감 | 선행 |
|---|---|---|---|
| W-1 | AH 2027 Short 8p | 2026-11-12 | R-6 (부분 데이터 가능) |
| W-2 | HCI Korea 3~6p 국문 | ~2026-11말 | R-6 |
| W-3 | IEEE VR Poster 2p | 2026-12-07 | W-1 |
| W-4 | CHI Poster/SRC 4p | 2027-01-21 | R-6 + HL2 파일럿 |
| W-5 | ISMAR 4~9p | ~2027-03 | HL2 본 실험 |

---

## HoloLens 2 이식 (트랙 B)

### 🔴 H-0 · 툴체인 아카이브 ⬜ · 0.5일 · **이번 주**
**참조** [`41`](../40-checklists/41-toolchain-archive.md) — 시간당 가치가 가장 높은 작업

### 🔴 H-1 · 빈 MRTK3 씬 실기 배포 ⬜ · 1일 · 선행 H-0 · **10월 안에**
**목표** 파이프라인이 실제로 동작하는지만 확인. 연구 로직 이전에.
**수용 기준**
- [ ] Unity 2022.3.62f1 + MRTK3 v3.3.0 프로젝트가 빌드됨
- [ ] HL2 실기에 배포되어 큐브 하나가 보이고 hand ray가 동작
- [ ] Holographic Remoting으로 에디터 Play Mode 연결 성공
- [ ] D3D11 강제 설정 완료
**실패 시** ISMAR 포기 또는 Quest 3 검토 → [`31`](./31-timeline.md) §4

### H-2 · 프로젝트 분기 + 다운그레이드 ⬜ · 2일 · 선행 H-1
### H-3 · 씬 이식 ⬜ · 3일 · 선행 H-2 · 외부 에셋 재설치 포함
### H-4 · hand ray IPointerSource ⬜ · 2일 · 선행 H-3, L1-7
### H-5 · eye gaze + 캘리브레이션 로깅 ⬜ · 2일 · 선행 H-4
### H-6 · HUD 52° 재설계 ⬜ · 3일 · 선행 H-3
### H-7 · 카드 가독성 조정 ⬜ · 2일 · 선행 H-3
### H-8 · 판단 입력 (제스처/음성) ⬜ · 2일 · 선행 H-4
### H-9 · 로깅 이식 (UWP 파일 권한) ⬜ · 1일 · 선행 H-2
### H-10 · HL2 파일럿 5명 ⬜ · 3일 · 선행 H-4~H-9

---

## 재현성 문서 (병렬, 낮은 우선순위)

| ID | 산출물 | 비고 |
|---|---|---|
| D-1 | `README.md` 갱신 | Unity/Python 버전, 포트, 실행 순서 |
| D-2 | `ASSET_SETUP.md` | **Git 제외 외부 에셋의 출처·라이선스·설치 경로** |
| D-3 | `EXPERIMENT_PROTOCOL.md` | 실험자 스크립트, 과업, 조건, 로그 위치 |
| D-4 | `KNOWN_LIMITATIONS.md` | 미확인 항목, 불안정 조건, 비의료 범위 |
| D-5 | `DEMO_CHECKLIST.md` | 데모 직전 확인 순서 |

**D-2가 가장 중요하다.** 외부 에셋이 Git에 없으므로 이 문서 없이는 아무도 재현할 수 없다. 논문 재현성 절이 여기에 의존한다.

---

## 합계

| 묶음 | 일수 |
|---|---|
| Level 1 | 11.5 |
| Level 2 | 5 |
| 연구 운영 (파일럿·본실험 제외) | 2.5 |
| 파일럿 + 본 실험 + 분석 | 약 25 |
| 집필 3건 | 약 12 |
| HL2 이식 | 21 |
| 재현성 문서 | 3 |
| **총계** | **약 80일** |

2026-09-16부터 2027-03 중순까지 약 26주. 주 5일 기준 130일. **여유는 있지만 크지 않다.** 특히 11월이 병목이다.
