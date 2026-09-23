# 22. Level 1 — 계측 사양

> 목표: **시스템을 측정 가능하게 만든다.** 기능 추가가 아니라 관측 가능성 확보다.
> 선행: [`21-current-architecture.md`](./21-current-architecture.md) · 후속: [`23-uncertainty-hud-spec.md`](./23-uncertainty-hud-spec.md)

## 1. 왜 이것이 최우선인가

현재 시스템에는 **로그가 전혀 없다.** 실험을 아무리 잘 설계해도 기록되지 않으면 데이터가 0이다. 그리고 로깅 누락은 세션이 끝난 뒤에는 복구할 수 없다.

파일럿 전에 반드시 완료되어야 하는 유일한 항목이다.

## 2. 신규 컴포넌트

### 2.1 `ExperimentConfig` (ScriptableObject)

```
sessionId          : string    // "P01"
scenarioSetId      : string    // "S1" | "S2" | "S3"
scenarioSeed       : int
blockIndex         : int
inputMethod        : enum { PoseGesture, Mouse, HandRay, EyeGaze }
dwellSeconds       : float     // 기본 0.7
estimatorReliability : enum { R100, R85, R70 }
uncertaintyDisplay : enum { HardLabel, GradedConfidence, AbstainDefer, None }
hudEnabled         : bool
sagatProbeAfterN   : int       // 0이면 비활성
logDirectory       : string
```

- Inspector에서 조립하고, 세션 시작 시 로그 헤더에 전부 기록한다
- **런타임 중 변경 불가.** 변경하려면 세션을 새로 시작한다

### 2.2 `ExperimentRunner` (MonoBehaviour)

책임:
- 세션 시작/종료 시각 기록
- 블록 경계 관리
- `PatientUrgencyProfile`의 추정 결과 생성 (`scenarioSeed` 기반 결정론적)
- 오라벨 배치 결정 및 기록
- SAGAT 프로브 트리거 (화면 정지 + 질문 표시)
- 최종 판단 입력 수집

**절대 하지 않을 것:** 기존 `PatientDwellSelector`, `PointerRaycaster`, `PatientView`의 로직을 수정하지 않는다. 구독하고 관찰만 한다.

### 2.3 `ExperimentLogger` (MonoBehaviour)

- `PatientView.StateChanged` 구독 (이미 존재하는 이벤트)
- `PoseReceiverBehaviour`의 상태 폴링
- 파일 2개를 동시에 쓴다: 이벤트 로그(JSONL)와 시행 요약(CSV)
- **매 이벤트마다 flush.** 세션이 비정상 종료되어도 직전까지 남아야 한다

## 3. 로그 스키마

### 3.1 세션 헤더 (`session_<id>.json`)

```json
{
  "sessionId": "P01",
  "startedAtUtc": "2026-10-15T05:30:00Z",
  "unityVersion": "6000.3.10f1",
  "appBuildHash": "817e0db",
  "config": { "...ExperimentConfig 전체..." },
  "scenario": {
    "setId": "S1",
    "seed": 20261015,
    "patients": [
      {"id":"TR-001","truthLabel":"Immediate",
       "cues":{"motion":"None","posture":"Supine","respiration":"NotVisible","responsiveness":"None"},
       "estimatedLabel":"Immediate","isMislabeled":false,
       "position":[12.4,0.0,3.1]}
    ]
  },
  "participant": {"ageBand":"20-24","xrExperience":"low","glasses":true}
}
```

시나리오 전체가 헤더에 박제되므로, 로그 파일 하나만 있으면 시행을 완전히 재구성할 수 있다.

### 3.2 이벤트 로그 (`events_<id>.jsonl`) — 한 줄 = 한 이벤트

| 필드 | 설명 |
|---|---|
| `t` | 세션 시작 기준 경과 ms |
| `tUtc` | 절대 시각 |
| `block` | 블록 인덱스 |
| `event` | 아래 이벤트 종류 |
| `patientId` | 해당 시 |
| `payload` | 이벤트별 추가 필드 |

**이벤트 종류**

| 이벤트 | payload |
|---|---|
| `session_start` / `session_end` | — |
| `block_start` / `block_end` | `blockIndex`, `displayCondition` |
| `pose_state` | `tracking`, `pointing`, `pointerX`, `pointerY` (주기적, 5 Hz 다운샘플) |
| `hover_enter` / `hover_exit` | `patientId`, `distance` |
| `dwell_start` | `patientId` |
| `dwell_abort` | `patientId`, `elapsedMs` — **중단도 반드시 기록** |
| `dwell_complete` | `patientId`, `elapsedMs` |
| `card_shown` | `patientId`, `estimatedLabel`, `displayedConfidence` |
| `judgment_input` | `patientId`, `chosenLabel`, `truthLabel`, `estimatedLabel`, `isMislabeled`, `reactionMs` |
| `judgment_revised` | 재입력 시 |
| `sagat_probe` | `question`, `answer`, `correct`, `responseMs` |
| `camera_sample` | `position`, `forward` (2 Hz) |
| `hud_state` | `nearbyIds`, `uncheckedCount`, `checkedCount` |
| `error` | `message` |

`dwell_abort`를 빠뜨리기 쉬운데, 이것이 선택 난이도의 주 지표다. 반드시 넣는다.

### 3.3 시행 요약 (`trials_<id>.csv`)

분석 스크립트가 바로 읽을 수 있는 wide 포맷. 한 줄 = 환자 1명 처리 1회.

```
sessionId,block,displayCondition,reliability,scenarioSetId,patientId,
truthLabel,estimatedLabel,isMislabeled,chosenLabel,
isCorrect,isOverTriage,isUnderTriage,detectedMislabel,followedMislabel,
dwellStartMs,dwellCompleteMs,judgmentMs,timePerTargetMs,
dwellAbortCount,hoverCount,distanceAtSelect,trackingQualityPct
```

파생 필드 정의:
- `isOverTriage` = `chosenLabel`의 우선순위 > `truthLabel` (P1이 가장 높음)
- `isUnderTriage` = `chosenLabel`의 우선순위 < `truthLabel`
- `detectedMislabel` = `isMislabeled` **그리고** `chosenLabel == truthLabel`
- `followedMislabel` = `isMislabeled` **그리고** `chosenLabel == estimatedLabel`
- `timePerTargetMs` = `judgmentMs − dwellCompleteMs`
- `trackingQualityPct` = 해당 시행 구간에서 `tracking == TRACKING`인 비율

### 3.4 파일 배치

```
<logDirectory>/
  P01/
    session_P01.json
    events_P01.jsonl
    trials_P01.csv
    questionnaire_P01.csv
```

## 4. 마우스 베이스라인 조건

`inputMethod = Mouse`일 때, `PosePointerLineRenderer`를 대체하는 `MousePointerSource`가 화면 중앙 기준 레이를 생성한다.

**동일하게 유지해야 하는 것:** raycast 거리(10m), 레이어 마스크, dwell 시간, 카드 표시 방식. 입력원만 다르고 나머지가 같아야 비교가 성립한다.

⚠️ 마우스는 dwell 대신 클릭이 자연스럽지만, **dwell을 유지한다.** 입력 방식과 확정 방식을 동시에 바꾸면 교란이 생긴다. 클릭 조건은 별도 요인으로 두거나 아예 넣지 않는다.

## 5. 시나리오 파일화

현재 환자 배치는 씬에 하드코딩되어 있다. 재현성을 위해 JSON으로 분리한다.

```json
{
  "setId": "S1",
  "patients": [
    {"id":"TR-001","position":[12.4,0,3.1],"rotationY":135,
     "cues":{"motion":"None","posture":"Supine",
             "respiration":"NotVisible","responsiveness":"None"}}
  ]
}
```

- Editor 메뉴 `Triage Trace > Experiment > Export Current Scenario` — 현재 씬 → JSON
- Editor 메뉴 `Triage Trace > Experiment > Import Scenario` — JSON → 씬 (Undo 지원)
- **`TenPatientIdentityAndPlacementMenu`의 검증을 import 직후 자동 실행**한다

이 두 메뉴가 재현성 주장의 근거가 된다. 논문 §구현과 `EXPERIMENT_PROTOCOL.md`에 기록한다.

## 6. 작업 분해

| # | 작업 | 산출물 | 예상 |
|---|---|---|---|
| L1-1 | `PatientUrgencyProfile` + 라벨 규칙 + EditMode 테스트 | 스크립트 1, 테스트 1 | 1.5일 |
| L1-2 | Animator 4상태 + `PatientModelIntegrationMenu` 수정 | 수정 1, 컨트롤러 1 | 2.5일 |
| L1-3 | `ExperimentConfig` ScriptableObject | 스크립트 1 | 0.5일 |
| L1-4 | `ExperimentLogger` (JSONL + CSV, flush) | 스크립트 1 | 1.5일 |
| L1-5 | `ExperimentRunner` (블록·판단입력·SAGAT) | 스크립트 1 | 2일 |
| L1-6 | 시나리오 Export/Import 메뉴 | Editor 2 | 1.5일 |
| L1-7 | `MousePointerSource` 베이스라인 | 스크립트 1 | 1일 |
| L1-8 | 로그 무결성 자동 검증 스크립트 (Python) | 스크립트 1 | 1일 |
| | **합계** | | **약 11.5일** |

L1-8을 생략하지 말 것. 파일럿 로그를 이 스크립트로 검증해야 본 실험에서 데이터가 빠지지 않는다.

## 7. 수용 기준

- [ ] 세션 1회 실행 시 4개 파일이 모두 생성된다
- [ ] 비정상 종료(Play Mode 강제 중단) 후에도 직전 이벤트까지 남아 있다
- [ ] `trials_*.csv`의 행 수 == 블록 수 × 환자 수
- [ ] 동일 `scenarioSeed`로 두 번 실행하면 오라벨 배치가 동일하다
- [ ] 모든 `dwell_start`에 대응하는 `dwell_complete` 또는 `dwell_abort`가 존재한다
- [ ] `PatientDwellSelector`·`PointerRaycaster`·`PatientView`의 기존 코드가 **한 줄도 변경되지 않았다**
- [ ] 로그 파일에 개인 식별 정보가 없다
- [ ] 마우스 조건과 제스처 조건의 raycast 파라미터가 동일하다
