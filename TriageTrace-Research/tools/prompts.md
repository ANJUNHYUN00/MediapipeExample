# Claude Code 세션 프롬프트 모음

> 저장소: `ANJUNHYUN00/MediapipeExample` · 기본 브랜치: `main` · 리모트: `origin`
> 복사해서 그대로 붙여 넣으면 된다.

---

## 🟢 세션 시작

### 매번 쓰는 것

```
CLAUDE.md 와 TriageTrace-Research/START-HERE.md 를 읽어줘.

그 다음 저장소 상태를 보고해줘:
- git status (변경/미추적 파일)
- 현재 브랜치와 origin/main 대비 앞뒤 커밋 수
- 최근 커밋 3개

마지막으로 "지금 해야 할 다음 작업"을 START-HERE 기준으로 한 줄로 말해줘.
보고만 하고, 내가 승인하기 전까지 파일은 수정하지 마.
```

마지막 두 줄이 핵심이다. 상태만 보고받고 무엇부터 할지는 직접 정한다.

### 작업이 큰 날 (브랜치를 파고 싶을 때)

```
CLAUDE.md 와 TriageTrace-Research/START-HERE.md 를 읽고 git status 를 보고해줘.

오늘은 [작업 내용]을 할 거야. main 에서 작업 브랜치를 만들어줘.
브랜치 이름은 feature/[짧은-이름] 형식으로.
만들기만 하고 작업은 내 승인 후에 시작해.
```

Level 1·2 구현처럼 여러 파일을 건드리는 날에 쓴다. 툴체인 아카이브나 문서 수정은 `main`에서 해도 된다.

---

## 🔵 작업 중

### 중간 저장 (한 덩어리 끝날 때마다)

```
지금까지 변경한 걸 커밋해줘.

- git status 와 git diff --stat 을 먼저 보여줘
- git add -A 는 쓰지 마. 변경한 파일만 명시적으로 add 해줘
- 커밋 메시지는 한 줄 요약 + 필요하면 본문
- 푸시는 아직 하지 마
```

### 작업 방향이 헷갈릴 때

```
TriageTrace-Research/20-engineering/[해당문서].md 의 수용 기준을 다시 확인하고,
지금 구현이 그걸 만족하는지 항목별로 체크해줘.
```

---

## 🔴 세션 종료

### 표준 (이걸 제일 많이 쓴다)

```
오늘 작업을 마무리하자.

1. git status 와 git diff --stat 을 보여줘
2. 실험 로그나 참가자 데이터가 스테이징에 섞이지 않았는지 확인해줘
   (events_*.jsonl, trials_*.csv, session_*.json, 동의서 — 이 저장소는 공개 GitHub 야)
3. 대용량 외부 에셋이 섞이지 않았는지 확인해줘 (Subway Full Package, Characters 등)
4. 변경한 파일만 명시적으로 add 하고 커밋해줘. git add -A 금지
5. origin main 에 푸시해줘
6. 마지막으로 오늘 한 일과 다음에 할 일을 3줄 이내로 정리해줘.
   START-HERE.md 나 32-task-backlog.md 의 체크박스 상태가 바뀌었으면 같이 업데이트해줘
```

### 짧게

```
오늘 작업 커밋하고 푸시해줘.
git add -A 쓰지 말고 변경 파일만. 로그/에셋 섞였는지 먼저 확인.
끝나면 다음 할 일 한 줄로.
```

### 중간에 끊어야 할 때 (미완성인데 저장은 하고 싶을 때)

```
작업이 아직 안 끝났는데 자리를 떠야 해.
WIP 커밋으로 남겨줘. 메시지 앞에 "WIP:" 붙이고,
본문에 지금 어디까지 했고 다음에 뭘 이어야 하는지 적어줘.
푸시는 해도 돼. 나중에 amend 하거나 squash 할 거야.
```

---

## ⚠️ 이 저장소에서 조심할 것

### 🔴 `git add -A` 를 쓰지 않는다

외부 에셋(`Subway Full Package`, `Characters` 등)이 `.gitignore`에 들어갔지만, 실수 하나로 수백 MB가 스테이징될 수 있다. GitHub는 **파일 하나가 100MB를 넘으면 푸시를 거부**하고, 그렇게 되면 히스토리를 되감아야 한다.

항상 `git status` → 목록 확인 → 파일 명시해서 `add`.

### 🔴 실험 데이터는 절대 커밋하지 않는다

이 저장소는 **공개 GitHub**에 있다. 참가자 데이터가 올라가면 IRB·개인정보 위반이다.

`.gitignore`가 `events_*.jsonl`, `trials_*.csv`, `session_*.json`, `questionnaire_*.csv`, `consent/`, `participants/` 를 막아두었지만, **로그 디렉터리를 저장소 밖에 두는 게 더 안전하다.** `ExperimentConfig.logDirectory`를 `D:\TriageTrace-Data\` 같은 저장소 바깥 경로로 잡을 것.

### 🟠 씬 파일은 충돌하면 손으로 못 고친다

`TriageTraceEnvironmentPrototype.unity`는 6.79MB YAML이다. 충돌 나면 병합이 사실상 불가능하다.
→ **씬을 만지는 작업은 커밋을 자주 하고, 브랜치를 오래 끌지 않는다.**

### 🟠 Claude Code에게 씬 저장·Play Mode를 맡기지 않는다

`CLAUDE.md` §6에 규칙으로 적혀 있지만, 새 세션에서 흐려질 수 있다. Unity Editor 조작은 직접 한다.

---

## 커밋 메시지 관례

기존 히스토리(`817e0db Finalize Triage Trace AR prototype`)를 따라 **영문 명령형 한 줄**.

```
Add ExperimentLogger with JSONL and CSV output
Add PatientUrgencyProfile and label rules
Fix patient card anchor to use renderer bounds
Update research docs with pilot findings
Archive HoloLens 2 toolchain manifest
```

본문이 필요하면 한 줄 비우고 "왜"를 적는다. "무엇"은 diff가 말해준다.

---

## 첫 세션에서 한 번만

아직 안 했다면 이것부터.

```
CLAUDE.md 를 읽어줘.

.gitignore 가 방금 업데이트됐고, CLAUDE.md 와 TriageTrace-Research/ 폴더가
아직 커밋되지 않았을 거야.

1. git status 로 확인
2. 외부 에셋이 이미 추적(tracked)되고 있는지 확인해줘
   (git ls-files 로 "Subway Full Package" 등을 검색)
   추적 중이면 .gitignore 가 소용없으니 알려줘
3. .gitignore, CLAUDE.md, TriageTrace-Research/ 를 커밋하고 푸시해줘
   메시지: "Add research planning docs and Claude Code guide"
```

2번을 꼭 확인시킬 것. 이미 추적 중인 파일에는 `.gitignore`가 적용되지 않는다.
