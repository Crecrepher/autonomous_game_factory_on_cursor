# Git 커밋 규칙 — Autonomous Game Factory v2

Committer 에이전트가 feature_group 단위로 검증 완료된 모듈을 안전하게 커밋하기 위한 정책이다.
이 문서는 커밋 판정의 **유일한 소스 오브 트루스**다.

v2 변경 사항:
- 4차원 상태 모델(`commit_state`, `human_state`, `learning_state`, `status`) 기반 커밋 게이트 도입
- Recommit 규칙 신규 추가
- 학습 기록 게이트 신규 추가
- `chore(ai-learning)` 커밋 타입 추가
- Reviewer/Committer 역할 분리 명시

---

## 1. Committer 역할의 범위

Committer는 **git 작업만** 담당한다. 코드 품질 판단은 Reviewer, 코드 수정은 Builder의 책임이다.

| Committer가 하는 것 | Committer가 하지 않는 것 |
|---------------------|------------------------|
| commit_state 확인 및 전이 | 코드 검증 / 품질 판단 |
| feature_group 완전성 확인 | 코드 수정 |
| 관련 파일만 선별 스테이징 | TASK_QUEUE status/human_state 변경 |
| 커밋 메시지 생성 | Validator 실행 |
| git commit 실행 | learning_state 전이 (Learning Recorder 역할) |
| 커밋 로그 작성 | feature_group에 무관한 파일 스테이징 |
| recommit 수행 (human fix 이후) | force push, amend |

---

## 2. 커밋 적격성 체크리스트 (Commit Eligibility)

Committer는 커밋 실행 전 **모든 조건**을 순서대로 확인한다. 하나라도 실패하면 커밋하지 않는다.

### 2.1 First Commit (feat)

```
COMMIT ELIGIBILITY — FIRST COMMIT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
□ 0. AIValidationReport.json이 존재하고 Passed == true (blocking error 0건)
□ 1. feature_group 내 모든 모듈의 commit_state == ready
□ 2. feature_group 내 모든 모듈의 status != blocked && != escalated
□ 3. feature_group 내 모든 모듈의 human_state == validated
□ 4. feature_group 내 사람 수정이 있었다면:
     □ 4a. human_fixes[]가 비어있지 않음
     □ 4b. learning_state == recorded (Learning Recorder 완료)
□ 5. feature_group 내 사람 수정이 없었다면:
     □ 5a. learning_state == none 또는 recorded (둘 다 허용)
□ 6. 스테이징 대상 파일이 1개 이상 존재
□ 7. 스테이징 대상에 관련 없는 파일이 없음
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
모두 통과 → git add + git commit 실행
```

### 2.2 Recommit (fix)

```
COMMIT ELIGIBILITY — RECOMMIT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
□ 0. AIValidationReport.json이 존재하고 Passed == true (blocking error 0건)
□ 1. commit_state == ready (Reviewer가 재검증 후 설정)
□ 2. human_state == validated
□ 3. human_fixes[]에 이번 수정 건이 기록되어 있음
□ 4. learning_state == recorded (사람 수정이 있었으므로 필수)
□ 5. status != blocked && != escalated
□ 6. 스테이징 대상에 수정된 파일이 포함됨
□ 7. 스테이징 대상에 관련 없는 파일이 없음
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
모두 통과 → git add + git commit 실행
```

### 2.3 핵심 게이트 요약

| 게이트 | 확인 대상 | 실패 시 |
|--------|-----------|---------|
| **Validation Report Gate** | **AIValidationReport.json 존재 + Passed == true** | **보고서 없거나 blocking error 존재 — 커밋 즉시 차단** |
| Reviewer Gate | `commit_state == ready` | Reviewer가 아직 검증하지 않았거나 blocked |
| Human Gate | `human_state == validated` | 사람이 아직 검증하지 않음 |
| Learning Gate | `learning_state == recorded` (사람 수정 시 필수) | Learning Recorder가 기록하지 않음 |
| Completeness Gate | feature_group 내 모든 모듈 ready | 일부 모듈이 아직 미완료 |
| Scope Gate | 스테이징 파일이 feature_group에만 속함 | 관련 없는 dirty 파일 혼입 |
| **Architecture Diff Gate (v2.3)** | **arch_diff_blocked != true** | **Critical 아키텍처 위험 미해결 — 커밋 차단** |

**Validation Report Gate는 가장 먼저 체크된다.** 이 게이트는 개별 task/module이 아닌 **저장소 전체의 정합성**을 검증한다.
`DependencyValidator.ValidateTaskQueueConsistency()` 등이 TASK_QUEUE ↔ MODULE_REGISTRY 불일치를 blocking error로 보고하면 커밋이 차단된다.

---

## 3. 커밋 범위 규칙 (Staging Policy)

### 3.1 포함 대상 (Allowed)

feature_group에 속하는 모듈의 다음 파일만 스테이징한다:

| 카테고리 | 경로 패턴 | 예시 |
|---------|-----------|------|
| 모듈 소스 | `Assets/Game/Modules/<Module>/**` | `HealthRuntime.cs`, `IHealth.cs` |
| 모듈 테스트 | `Assets/Game/Modules/<Module>/Tests/**` | `HealthTests.cs` |
| 모듈 메타 | `Assets/Game/Modules/<Module>/**/*.meta` | Unity .meta 파일 |
| 공유 인터페이스 | `Assets/Game/Shared/I<Interface>.cs` (이번 feature에서 신규 추가된 것만) | `IHealthProvider.cs` |
| Spec 문서 | `docs/ai/generated_specs/<Module>_SPEC.md` | `Health_SPEC.md` |
| Plan 문서 | `docs/ai/plans/<Module>_PLAN.md` | `Health_PLAN.md` |
| Review 문서 | `docs/ai/reviews/<Module>_REVIEW.md` | `Health_REVIEW.md` |

### 3.2 제외 대상 (Disallowed)

| 카테고리 | 경로 | 이유 |
|---------|------|------|
| 전역 큐 | `docs/ai/TASK_QUEUE.yaml` | 전체 시스템 상태 — 별도 커밋 |
| 전역 레지스트리 | `docs/ai/MODULE_REGISTRY.yaml` | 전체 모듈 목록 — 별도 커밋 |
| 전역 Feature 큐 | `docs/ai/FEATURE_QUEUE.yaml` | 전체 Feature 상태 — 별도 커밋 |
| 다른 feature_group 파일 | `Assets/Game/Modules/<OtherModule>/**` | feature_group 격리 |
| AI 인프라 | `Assets/Editor/AI/**` | 공유 검증 시스템 |
| Core | `Assets/Game/Core/**` | 핵심 시스템, 별도 관리 |
| 학습 파일 | `docs/ai/learning/**` | 별도 `chore(ai-learning)` 커밋 |
| Cursor 설정 | `.cursor/**` | 에디터 설정 |
| 환경 파일 | `.env`, `credentials.*` | 보안 |

### 3.3 관련 없는 dirty 파일 처리

```
1. git status로 모든 modified/untracked 파일 목록 확보
2. 포함 대상 경로 패턴에 매칭되는 파일만 필터링
3. 매칭되지 않는 dirty 파일이 있으면:
   → 경고 로그 출력: "[Committer] Skipping unrelated dirty file: <path>"
   → 해당 파일은 스테이징하지 않음
4. 필터링 후 파일이 0개면 커밋 중단
```

---

## 4. 커밋 타입 정의

| type | 용도 | 트리거 | commit_state 전이 |
|------|------|--------|-------------------|
| `feat` | 신규 feature_group 모듈 첫 커밋 | 모든 모듈 `commit_state: ready` (최초) | `ready → committed` |
| `fix` | 사람 수정 후 재커밋 | `commit_state: ready` (recommit 플래그) | `ready → recommitted` |
| `refactor` | 구조 개선 (코드 변경 없는 리팩토링) | 사용자 명시 요청 시 | `ready → committed` |
| `chore(ai-meta)` | 에이전트 메타 파일 커밋 (TASK_QUEUE, REGISTRY, FEATURE_QUEUE) | feat/fix 커밋 직후 자동 | 별도 커밋 (태스크 무관) |
| `chore(ai-learning)` | 학습 데이터 커밋 | Learning Recorder가 기록 완료 후 | 별도 커밋 (태스크 무관) |
| `docs` | 문서만 변경 | 사용자 명시 요청 시 | 별도 커밋 (태스크 무관) |

### 4.1 feat vs fix 판정 로직

```
if (feature_group 내 어떤 모듈이라도 이전에 committed 또는 recommitted 이력이 있음):
    type = fix
    recommit = true
else:
    type = feat
    recommit = false
```

### 4.2 chore(ai-meta) 커밋

에이전트 메타 파일은 feature 커밋과 **분리하여** 직후에 독립 커밋한다.

```
포함 파일:
  - docs/ai/TASK_QUEUE.yaml
  - docs/ai/MODULE_REGISTRY.yaml
  - docs/ai/FEATURE_QUEUE.yaml

타이밍:
  - feat/fix 커밋 직후 자동 실행
  - 위 파일 중 dirty(modified/untracked)인 것만 스테이징
  - dirty 파일이 0개면 스킵

제외:
  - 기능 코드 파일 (Assets/)
  - learning/ 폴더 파일
  - Assets/Editor/AI/ (에이전트 인프라)
```

### 4.3 chore(ai-learning) 커밋

학습 파일은 feature 커밋과 **분리하여** 독립 커밋한다.

```
포함 파일:
  - docs/ai/learning/LEARNING_LOG.md
  - docs/ai/learning/HUMAN_FIX_EXAMPLES.md
  - docs/ai/learning/RULE_MEMORY.yaml
  - docs/ai/learning/VALIDATOR_FAILURE_PATTERNS.md
  - docs/ai/learning/RECURRING_MISTAKES.md
  - docs/ai/learning/CROSS_PROJECT_RULES.md
  - docs/ai/learning/CODING_PATTERNS.md

타이밍:
  - Learning Recorder가 learning_state: pending → recorded 전이 후
  - feature 커밋 직후 또는 독립 시점에
```

---

## 5. 커밋 메시지 규칙

### 5.1 형식

```
<type>(<scope>): <summary>

<body>
```

- **type**: §4 참조
- **scope**: feature_group slug (소문자, 하이픈 구분)
- **summary**: 50자 이내, 동사 시작 (`add`, `apply`, `fix`, `record`)
- **body**: 구조화된 메타데이터

### 5.2 feat 커밋 메시지

```
feat(<feature-group>): add <모듈 목록> modules

- modules:
  - <Module1>
  - <Module2>
- validation: PASS | FAIL (errors: N)
- validation_warnings: <count> (0이면 생략)
- human_validated: true
- human_fixes: <count>
- dependencies:
  - <Module1> → [<deps>]
  - <Module2> → [<deps>]
- learning_recorded: true | false
- generated by: Autonomous Game Factory v2
```

**validation 필드는 AIValidationReport.json의 실제 결과를 반영해야 한다. 하드코딩 금지.**

### 5.3 fix (recommit) 커밋 메시지

```
fix(<feature-group>): apply human validation fixes for <Module>

- modules:
  - <Module>
- fix_reason: <수정 이유 요약>
- human_modifications:
  - <파일>: <변경 요약>
- original_commit: <원본 커밋 해시>
- validation: PASS
- human_validated: true
- learning_recorded: true
- generated by: Autonomous Game Factory v2
```

### 5.4 chore(ai-meta) 커밋 메시지

```
chore(ai-meta): update pipeline state for <feature-group>

- feature_group: <group>
- updated_files:
  - <file1>
  - <file2>
- triggered_by: feat(<feature-group>) commit
- generated by: Autonomous Game Factory v2
```

### 5.5 chore(ai-learning) 커밋 메시지

```
chore(ai-learning): record learning from <feature-group> cycle

- feature_group: <group>
- learning_log_entries: <count>
- human_fix_examples: <count>
- rule_memory_rules: <count>
- validator_patterns: <count>
- recurring_mistakes_updated: true | false
- generated by: Autonomous Game Factory v2
```

### 5.5 예시

```
feat(combat-core): add HealthSystem, DamageSystem modules

- modules:
  - HealthSystem
  - DamageSystem
- validation: PASS
- human_validated: true
- human_fixes: 0
- dependencies:
  - HealthSystem → []
  - DamageSystem → [HealthSystem]
- learning_recorded: false
- generated by: Autonomous Game Factory v2
```

```
fix(combat-core): apply human validation fixes for HealthSystem

- modules:
  - HealthSystem
- fix_reason: Runtime이 MonoBehaviour를 상속하여 ArchitectureRuleValidator 실패
- human_modifications:
  - HealthRuntime.cs: MonoBehaviour 상속 제거, Init() 패턴으로 변환
- original_commit: a1b2c3d
- validation: PASS
- human_validated: true
- learning_recorded: true
- generated by: Autonomous Game Factory v2
```

```
feat(currency-economy): add CurrencyWallet runtime/config/factory/tests

- modules:
  - CurrencyWallet
- validation: PASS
- human_validated: true
- human_fixes: 2
- dependencies:
  - CurrencyWallet → [Economy]
- learning_recorded: true
- generated by: Autonomous Game Factory v2
```

```
chore(ai-learning): record validator failure pattern for module boundary issue

- feature_group: combat-core
- learning_log_entries: 2
- human_fix_examples: 1
- rule_memory_rules: 1
- validator_patterns: 1
- recurring_mistakes_updated: false
- generated by: Autonomous Game Factory v2
```

---

## 6. 커밋 절차 (4차원 기반)

### 6.1 First Commit (feat) 절차

```
 1. TASK_QUEUE.yaml 읽기
 2. commit_state == ready인 모듈 수집
 3. feature_group별로 그룹화
 4. 대상 feature_group의 모든 모듈이 commit_state == ready인지 확인
    → 아니면 중단 (부분 커밋 금지)
 5. 적격성 체크리스트 §2.1 전체 확인
    → 하나라도 실패하면 중단 + 실패 사유 로그
 6. 해당 feature_group의 파일만 수집 (§3.1 포함 대상)
 7. 관련 없는 dirty 파일 필터링 (§3.3)
 8. 커밋 메시지 생성 (§5.2 feat 형식)
 9. git add <선별된 파일들>
10. git commit -m "<메시지>"
11. commit_state: ready → committed (각 모듈)
12. 커밋 로그를 docs/ai/commit_logs/<feature-group>_COMMIT.md에 기록
13. COMMIT RESULT 산출물 출력
```

### 6.2 Recommit (fix) 절차

```
 1. TASK_QUEUE.yaml 읽기
 2. commit_state == ready이면서 이전 커밋 이력이 있는 모듈 수집
    (committed → recommit_ready → ready 경로를 거친 모듈)
 3. 적격성 체크리스트 §2.2 전체 확인
    → learning_state == recorded 필수 (Learning Gate)
    → human_fixes[]에 이번 수정 건 존재 확인
 4. 수정된 파일 + 기존 모듈 파일 수집
 5. 커밋 메시지 생성 (§5.3 fix 형식)
    → original_commit 필드에 이전 커밋 해시 포함
 6. git add <선별된 파일들>
 7. git commit -m "<메시지>"
 8. commit_state: ready → recommitted (각 모듈)
 9. 커밋 로그에 recommit 사유와 human_modifications 기록
10. COMMIT RESULT 산출물 출력
```

### 6.3 Learning Commit (chore) 절차

```
1. Learning Recorder가 learning_state: pending → recorded 전이 완료 확인
2. learning/ 폴더에서 변경된 파일 수집
3. 커밋 메시지 생성 (§5.4 chore 형식)
4. git add docs/ai/learning/<변경된 파일들>
5. git commit -m "<메시지>"
6. (이 커밋은 태스크 상태 전이와 무관)
```

---

## 7. Recommit 트리거와 흐름

### 7.1 Recommit이 발생하는 3가지 시나리오

| # | 시나리오 | 트리거 | commit_state 경로 |
|---|---------|--------|-------------------|
| 1 | 첫 커밋 전 사람 수정 | 사람이 human_state: fixing → validated | `none → ready → committed` (feat이지만 human_fixes 포함) |
| 2 | 첫 커밋 후 Reviewer blocked | Builder 수정 → 사람 재검증 → Reviewer 재통과 | `committed → recommit_ready → ready → recommitted` |
| 3 | done 이후 사후 이슈 발견 | 사람이 이슈 보고 → status: done → blocked | `committed → recommit_ready → ready → recommitted` |

### 7.2 시나리오 1: 첫 커밋 전 사람 수정

```
Builder 코드 생성 → human_state: pending
 ↓
사람이 코드 수정 + human_fixes 기록
  human_state: pending → in_review → fixing → validated
 ↓
Builder: status: in_progress → review
 ↓
Reviewer: commit_state: none → ready
 ↓
Learning Recorder: learning_state: none → pending → recorded
  (사람 수정이 있었으므로 Learning Gate 활성)
 ↓
Committer: feat 커밋 (human_fixes > 0 이므로 커밋 메시지에 human_fixes: N 포함)
  commit_state: ready → committed
```

이 경우는 **feat** 타입이지만, 사람 수정이 있었으므로:
- `learning_state == recorded` 필수 (Learning Gate)
- 커밋 메시지에 `human_fixes: N` 포함

### 7.3 시나리오 2: 첫 커밋 후 blocked → recommit

```
[이미 committed 상태]
 ↓
사후 검증에서 이슈 발견
  status: done → blocked
  commit_state: committed → recommit_ready
  human_state: validated → pending (리셋)
 ↓
Builder: 코드 수정
  status: blocked → in_progress
 ↓
사람: 재검증
  human_state: pending → in_review → validated
 ↓
Builder: status: in_progress → review
 ↓
Reviewer: commit_state: recommit_ready → ready (재검증 통과)
 ↓
Learning Recorder: learning_state → recorded (필수)
 ↓
Committer: fix 커밋 (5 Gate 체크)
  commit_state: ready → recommitted
  커밋 메시지: fix(<group>): apply human validation fixes for <Module>
```

### 7.4 시나리오 3: done 이후 사후 이슈

시나리오 2와 동일한 흐름이다. 차이점은 시작 상태가 `done`이라는 것.

### 7.5 Recommit 시 Learning Gate가 필수인 이유

```
사람이 코드를 수정했다
 → 그 수정에는 이유가 있다
 → 그 이유가 기록되지 않으면 같은 실수가 반복된다
 → Learning Recorder가 기록할 때까지 커밋을 막는다
 → 이것이 Learning Gate다
```

| 조건 | Learning Gate |
|------|---------------|
| human_fixes 없음 (AI 코드 그대로 통과) | 불필요 (`learning_state: none` 허용) |
| human_fixes 있음 (사람이 수정) | 필수 (`learning_state == recorded` 필수) |

---

## 8. 커밋 로그 형식

커밋 후 `docs/ai/commit_logs/<feature-group>_COMMIT.md`에 기록한다.
기존 로그가 있으면 하단에 `---` 구분자 후 append한다.

### 8.1 feat 로그

```markdown
# Commit Log: <feature_group>

## <timestamp>

- Type: feat
- Commit Hash: <hash>
- Modules:
  - <Module1> (<path>)
  - <Module2> (<path>)
- Validation: PASS | FAIL (errors: N)
- Validation Warnings: <count> (0이면 생략)
- Human Validated: true
- Human Fixes: <count>
- Learning Recorded: true | false
- Files Staged: <count>
  - <file1>
  - <file2>
  - ...
- Dependencies:
  - <Module1> → [<deps>]
```

### 8.2 fix (recommit) 로그

```markdown
---

## <timestamp> — RECOMMIT

- Type: fix
- Commit Hash: <hash>
- Original Commit: <original hash>
- Modules:
  - <Module>
- Fix Reason: <수정 이유>
- Human Modifications:
  - <파일>: <변경 요약>
- Validation: PASS
- Human Validated: true
- Learning Recorded: true
- Files Staged: <count>
  - <file1>
  - ...
```

---

## 9. 금지 사항

| # | 금지 | 이유 | 검출 방법 |
|---|------|------|-----------|
| 1 | 전체 파일 한 번에 커밋 (`git add .`) | feature_group 격리 위반 | 스테이징 파일 경로 검증 |
| 2 | 검증 미통과 모듈 커밋 | `commit_state != ready` | 적격성 체크리스트 §2 |
| 3 | blocked/escalated 모듈 포함 커밋 | 미완성 코드 유입 | status 체크 |
| 4 | TASK_QUEUE/REGISTRY를 feature 커밋에 포함 | 전체 시스템 상태 오염 | 제외 대상 §3.2 |
| 5 | force push | 히스토리 파괴 | 절대 금지 |
| 6 | amend | 추적성 훼손 | 절대 금지 |
| 7 | 사람 수정이 있는데 Learning 미기록 상태로 커밋 | 학습 누락 | Learning Gate §2.1/§2.2 |
| 8 | feature_group 일부만 커밋 | 완전성 위반 | Completeness Gate |
| 9 | Reviewer가 직접 커밋 | 역할 혼재 | 역할 분리 원칙 |
| 10 | 다른 feature_group의 파일 스테이징 | 격리 위반 | Scope Gate |

---

## 10. Committer 산출물

### 10.1 COMMIT RESULT

커밋 성공 시:

```
COMMIT RESULT:
  feature_group: <group>
  type: feat | fix
  modules: [<list>]
  commit_hash: <hash>
  files_staged: <count>
  recommit: true | false
  recommit_reason: <사유> (recommit인 경우)
  human_fixes_included: <count>
  learning_recorded: true | false
  timestamp: <ISO 8601>
```

### 10.2 COMMIT BLOCKED

커밋 불가 시:

```
COMMIT BLOCKED:
  feature_group: <group>
  reason: <실패 게이트 이름>
  detail: <구체적 사유>
  blocked_modules: [<목록>] (해당 시)
  action_required: <어떤 역할이 무엇을 해야 하는가>
```

| 실패 게이트 | action_required |
|-------------|-----------------|
| **Validation Report Gate** | **Tools/AI/Validate Generated Modules를 실행하고, 모든 blocking error를 수정한 후 재실행** |
| Reviewer Gate | Reviewer가 commit_state를 ready로 전이해야 함 |
| Human Gate | 사람이 human_state를 validated로 전이해야 함 |
| Learning Gate | Learning Recorder가 learning_state를 recorded로 전이해야 함 |
| Completeness Gate | feature_group 내 미완료 모듈의 빌드/검증이 필요함 |
| Scope Gate | 관련 없는 dirty 파일을 별도 관리해야 함 |
| **Architecture Diff Gate** | **arch_diff_blocked == true인 모듈의 아키텍처 위험을 해결하거나, ARCHITECTURE_DIFF_ANALYZER.md에 따라 재분석 필요** |

---

## 11. commit_state 전이 규칙

Committer가 전이할 수 있는 `commit_state` 값:

| 전이 | 조건 | 트리거 |
|------|------|--------|
| `ready → committed` | 적격성 §2.1 전체 통과 + 이전 커밋 이력 없음 | feat 커밋 성공 |
| `ready → recommitted` | 적격성 §2.2 전체 통과 + 이전 커밋 이력 존재 | fix 커밋 성공 |

Committer가 전이할 수 **없는** 값:

| 전이 | 담당 역할 |
|------|-----------|
| `none → ready` | Reviewer (검증 통과 시) |
| `committed → recommit_ready` | 사람 또는 System (이슈 발견 시) |
| `recommit_ready → ready` | Reviewer (재검증 통과 시) |

---

## 12. 구현 훅 (Implementation Hooks)

현재 `GitCommitStage.cs`에 다음 업데이트가 필요하다:

### 12.1 CommitCandidate 확장

```
기존: AllDone, HasBlocked 체크
추가 필요:
  - AllCommitReady: 모든 모듈의 commit_state == ready
  - AllHumanValidated: 모든 모듈의 human_state == validated
  - LearningRecorded: human_fixes가 있으면 learning_state == recorded
  - IsRecommit: 이전 커밋 이력 존재 여부
  - OriginalCommitHash: recommit 시 원본 커밋 해시
  - HumanFixCount: human_fixes 건수
```

### 12.2 FeatureGroupTracker 확장

```
기존: status == "done" 기반 AllDone 체크
추가 필요:
  - CommitState 필드 파싱
  - HumanState 필드 파싱
  - LearningState 필드 파싱
  - AllCommitReady: 모든 모듈의 CommitState == "ready"
  - HasHumanFixes: human_fixes 존재 여부
  - LearningComplete: human_fixes가 있으면 LearningState == "recorded"
```

### 12.3 BuildCommitMessage 확장

```
기존: feat 타입만 지원, validation PASS만 출력
추가 필요:
  - type 선택 (feat vs fix)
  - human_validated 필드
  - human_fixes 건수
  - learning_recorded 필드
  - original_commit (recommit 시)
  - fix_reason (recommit 시)
  - human_modifications (recommit 시)
```

### 12.4 우선순위

| 순위 | 작업 | 이유 |
|------|------|------|
| 1 | FeatureGroupTracker에 commit_state 파싱 추가 | 커밋 판정의 기반 |
| 2 | CommitCandidate에 4차원 게이트 필드 추가 | 적격성 체크리스트 구현 |
| 3 | BuildCommitMessage에 fix 타입 + 확장 메타데이터 | 커밋 메시지 규격 |
| 4 | PrepareCommit에 Learning Gate 체크 | 학습 누락 방지 |

---

## 13. 참조 문서

| 문서 | 관련 내용 |
|------|-----------|
| `STATE_MACHINE.md` §5 | commit_state 전이 명세 |
| `AGENT_ROLES.md` §4 | Committer 역할 상세 |
| `ORCHESTRATION_RULES.md` §7 | Recommit Path |
| `ORCHESTRATION_RULES.md` §11 | 커밋 통합 포인트 |
| `TASK_SCHEMA.md` §3 | commit_state enum 값 |
| `learning/LEARNING_INDEX.md` | 학습 시스템 구조 |
