# 오케스트레이션 규칙 — Autonomous Game Factory v2

이 문서는 전체 파이프라인의 **유일한 통합 오케스트레이션 명세**다.
9개 역할이 Feature Intake부터 Learning Recording까지 순서대로 바톤을 넘기는 방식을 정의한다.

**Cross-Project Intelligence Layer (CPIL)**: 파이프라인 위에서 프로젝트 간 지식을 축적하고 재사용하는 메타 레이어.
상세: `CROSS_PROJECT_INTELLIGENCE.md`

상태 전이 명세: `STATE_MACHINE.md`
태스크 필드 명세: `TASK_SCHEMA.md`
커밋 규칙: `COMMIT_RULES.md`
역할별 상세: `AGENT_ROLES.md`

---

## 1. 역할 체인 (Role Chain)

9개 역할이 정해진 순서로 실행된다. 각 역할은 자신의 단계를 완료한 후 다음 역할에 바톤을 넘긴다.

```
┌──────────────────────────────────────────────────────────────────────────┐
│ #   역할               트리거                  바톤 전달               │
│                                                                        │
│ 1   Feature Intake     사용자 요청              FEATURE_QUEUE.yaml     │
│ 2   Queue Generator    FEATURE_QUEUE intake     TASK_QUEUE + REGISTRY  │
│2.9  Diff Analyzer      TASK_QUEUE pending       arch_diff_risk 설정    │
│ 3   Orchestrator       TASK_QUEUE pending       Builder 배정           │
│ 4   Planner            TASK_QUEUE pending       status → planned       │
│ 5   Builder            status == planned        status → in_progress   │
│ 6   Human Validator    human_state == pending    human_state→validated  │
│ 7   Reviewer           human_state==validated    commit_state → ready   │
│ 8   Committer          commit_state == ready     commit_state→committed│
│ 9   Learning Recorder  commit_state==committed   learning_state→recorded│
│                                                                        │
│     ► 모든 차원 완료 시 Reviewer가 status → done으로 최종 마감        │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 2. End-to-End 파이프라인

### 2.1 전체 흐름도

```
사용자 요청 (자연어 / 디자인 문서)
 │
 ▼
╔═══════════════════════════════════════════════╗
║  PHASE 1: 전처리 (Pre-Processing)             ║
║                                               ║
║  [1] Feature Intake                           ║
║      입력: 사용자 자연어/문서                 ║
║      출력: FEATURE_QUEUE.yaml (status: intake)║
║       ↓                                       ║
║  [2] Queue Generator                          ║
║      입력: FEATURE_QUEUE intake 엔트리        ║
║           + MODULE_REGISTRY.yaml              ║
║           + Assets/Game/Modules/ (스캔)       ║
║           + learning/RULE_MEMORY.yaml         ║
║           + learning/RECURRING_MISTAKES.md    ║
║      처리:                                    ║
║        [B.5] Module Discovery (★ v2.1)       ║
║        [B.7] Reuse Decision Engine (★ v2.1)  ║
║        → 분해 → 의존 추론 → 위험 주석        ║
║      출력: TASK_QUEUE.yaml (status: pending)  ║
║           + integration_strategy 필드 (v2.1) ║
║           + MODULE_REGISTRY.yaml 등록         ║
║           + generated_specs/*.md              ║
║           + FEATURE_QUEUE (status: queued)    ║
║       ↓                                       ║
║  [2.9] ★ Architecture Diff Analyzer (v2.3) ★ ║
║      입력: TASK_QUEUE pending 엔트리          ║
║           + integration_strategy              ║
║           + MODULE_REGISTRY.yaml (현재)       ║
║           + Assets/Game/Modules/ (기존 코드)  ║
║           + generated_specs/<Module>_SPEC.md  ║
║      처리:                                    ║
║        - 모듈 책임 겹침 분석                  ║
║        - 인터페이스 하위 호환성 검증          ║
║        - 의존성 그래프 diff + 순환 감지       ║
║        - Runtime/MonoBehaviour 계층 위반 감지 ║
║        - GC 위험 패턴 감지                    ║
║      출력: diff_reports/<Module>_DIFF.md      ║
║           + arch_diff_risk 필드 설정          ║
║           + critical → arch_diff_blocked=true ║
║      상세: ARCHITECTURE_DIFF_ANALYZER.md      ║
╚═══════════════════════════════════════════════╝
 │ (arch_diff_blocked == false만 진행)
 ▼
╔═══════════════════════════════════════════════╗
║  PHASE 2: 모듈 생산 루프 (per module)         ║
║                                               ║
║  [3] Orchestrator                             ║
║      입력: TASK_QUEUE.yaml + DependencyGraph  ║
║      처리: 실행 가능 모듈 선출, Builder 배정  ║
║       ↓                                       ║
║  [4] Planner                                  ║
║      입력: TASK_QUEUE pending 엔트리          ║
║           + generated_specs/<Module>_SPEC.md  ║
║           + learning/RULE_MEMORY.yaml         ║
║           + diff_reports/<Module>_DIFF.md ★   ║
║      출력: plans/<Module>_PLAN.md             ║
║           (medium+ diff → 경고 섹션 포함)     ║
║           + status: pending → planned         ║
║       ↓                                       ║
║  [5] Builder                                  ║
║      입력: PLAN + Spec + learning/            ║
║      출력: Assets/Game/Modules/<Module>/ 코드 ║
║           + status: planned → in_progress     ║
║           + human_state: none → pending       ║
║       ↓                                       ║
║  ┌────────────────────────────────────────┐   ║
║  │ ★ HUMAN GATE (파이프라인 정지) ★       │   ║
║  │                                        │   ║
║  │ [6] Human Validator                    │   ║
║  │     사람이 Unity Editor에서 Validator   │   ║
║  │     실행 + 코드 확인 + 필요 시 수정    │   ║
║  │     human_state: pending               │   ║
║  │      → in_review → (fixing →) validated│   ║
║  │     수정 시 human_fixes[] 기록         │   ║
║  └────────────────────────────────────────┘   ║
║       ↓                                       ║
║  [Builder] status: in_progress → review       ║
║       ↓                                       ║
║  [7] Reviewer                                 ║
║      입력: 코드 + AIValidationReport.json     ║
║           + human_fixes[]                     ║
║           + diff_reports/<Module>_DIFF.md ★   ║
║      처리: medium+ diff 항목 추가 검증        ║
║      통과 → commit_state: none → ready        ║
║      실패 → status → blocked                  ║
║       ↓ (blocked 시 [5]로 루프, 최대 3회)     ║
║                                               ║
╚═══════════════════════════════════════════════╝
 │ (commit_state == ready, feature_group 전체)
 ▼
╔═══════════════════════════════════════════════╗
║  PHASE 3: 후처리 (Post-Processing)            ║
║                                               ║
║  [8] Committer                                ║
║      입력: TASK_QUEUE (commit_state==ready)   ║
║           + COMMIT_RULES.md (5+1 Gate)        ║
║      처리: 6 Gate 체크 → git commit           ║
║        Gate 6: arch_diff_blocked!=true (v2.3) ║
║      출력: commit_state: ready → committed    ║
║           + commit_logs/<group>_COMMIT.md     ║
║       ↓                                       ║
║  [9] Learning Recorder                        ║
║      입력: human_fixes[] + Validator 보고서   ║
║           + Reviewer 보고서                   ║
║      출력: learning/ 폴더 파일 업데이트       ║
║           + learning_state: pending → recorded║
║       ↓                                       ║
║  [Reviewer] status: review → done             ║
║  (모든 4차원이 터미널 상태)                   ║
╚═══════════════════════════════════════════════╝
```

### 2.2 핵심 원칙

| # | 원칙 | 설명 |
|---|------|------|
| 1 | 순차 바톤 전달 | 각 역할은 이전 역할의 산출물이 있어야 시작 |
| 2 | Human Gate 정지 | Phase 2 중간에 파이프라인이 정지하고 사람을 기다림 |
| 3 | 역할 경계 불침범 | 각 역할은 자기 I/O 계약에 정의된 것만 읽고/쓴다 |
| 4 | 4차원 교차 조건 | status 전이 시 다른 차원의 guard를 반드시 확인 |
| 5 | feature_group 단위 커밋 | 모듈 단위가 아닌 feature_group 전체가 ready여야 커밋 |
| 6 | 회귀 방지 | Regression Guardian(CAP2)가 기존 시스템 파괴를 사전 감지 |
| 7 | 메타 자동 복구 | Self-Healing(CAP3)이 메타데이터 불일치를 자동 수정 (코드 금지) |
| 8 | 아키텍처 기억 | Knowledge Memory(CAP1)가 패턴/안티패턴을 영구 저장 |

---

## 3. 역할별 I/O 계약 (Per-Role Baton Pass)

### 3.1 [Role 1] Feature Intake

| 항목 | 값 |
|------|-----|
| 트리거 | 사용자가 기능 요청을 입력 |
| 읽기 | 사용자 입력 (자연어, 문서, YAML) |
| 쓰기 | `FEATURE_QUEUE.yaml` (새 엔트리, status: intake) |
| 바톤 전달 | FEATURE_QUEUE에 intake 엔트리가 생김 → Queue Generator 시작 |
| 구현 | `FeatureIntake.cs` |
| 상세 | `FEATURE_INTAKE.md` |

### 3.2 [Role 2] Queue Generator

| 항목 | 값 |
|------|-----|
| 트리거 | FEATURE_QUEUE에 status: intake 엔트리 존재 |
| 읽기 | `FEATURE_QUEUE.yaml`, `MODULE_REGISTRY.yaml`, `TASK_QUEUE.yaml`, `learning/RULE_MEMORY.yaml`, `learning/RECURRING_MISTAKES.md`, `learning/VALIDATOR_FAILURE_PATTERNS.md` |
| 쓰기 | `TASK_QUEUE.yaml` (신규 엔트리, status: pending), `MODULE_REGISTRY.yaml` (신규 모듈), `generated_specs/<Module>_SPEC.md`, `FEATURE_QUEUE.yaml` (status: intake → decomposed → queued) |
| 바톤 전달 | TASK_QUEUE에 pending 엔트리가 생김 → Orchestrator 시작 |
| 상태 전이 | FEATURE_QUEUE status: `intake → decomposed → queued` |
| 구현 | `FeatureIntake.cs` → `FeatureDecomposer.cs` → `TaskQueueGenerator.cs` → `SpecGenerator.cs` |
| 상세 | `QUEUE_GENERATOR.md` |

### 3.2.9 [Role 2.9] Architecture Diff Analyzer (v2.3)

| 항목 | 값 |
|------|-----|
| 트리거 | Queue Generator 완료 — TASK_QUEUE에 pending 엔트리가 존재하고 integration_strategy가 결정됨 |
| 읽기 | `TASK_QUEUE.yaml`, `MODULE_REGISTRY.yaml`, `Assets/Game/Modules/` (기존 코드), `generated_specs/<Module>_SPEC.md`, `I<Module>.cs` (기존 인터페이스) |
| 쓰기 | `diff_reports/<Module>_DIFF.md`, `TASK_QUEUE.yaml` (arch_diff_risk, arch_diff_blocked, arch_diff_report_path) |
| 바톤 전달 | arch_diff_blocked == false → Orchestrator 시작. arch_diff_blocked == true → 파이프라인 중단, 사용자에게 리포트 출력 |
| 상태 전이 | arch_diff_risk: `not_analyzed → low/medium/high/critical`, arch_diff_blocked: `false → true` (critical 시) |
| 구현 | `ArchitectureDiffAnalyzer.cs`, `ArchitectureDiffValidator.cs` |
| 상세 | `ARCHITECTURE_DIFF_ANALYZER.md` |

**Diff 분석 절차:**
1. 모듈별로 `integration_strategy`를 읽는다.
2. 기존 모듈이면 → 인터페이스/의존성/Runtime 코드를 스캔한다.
3. 제안된 변경과 기존 상태를 비교하여 `ArchDiff[]`를 생성한다.
4. 각 diff에 위험 등급(`low/medium/high/critical`)을 부여한다.
5. `critical` 조건이 하나라도 있으면 `arch_diff_blocked = true`.
6. `diff_reports/<Module>_DIFF.md`에 리포트를 기록한다.
7. TASK_QUEUE의 해당 엔트리에 `arch_diff_risk`, `arch_diff_blocked`, `arch_diff_report_path`를 설정한다.

**Critical 즉시 차단 조건:**
- 기존 인터페이스 메서드 삭제 (하위 호환성 파괴)
- 의존성 순환 도입
- Runtime이 MonoBehaviour를 상속
- foreach/LINQ/코루틴/람다 사용 (GC 위험)

### 3.3 [Role 3] Orchestrator

| 항목 | 값 |
|------|-----|
| 트리거 | TASK_QUEUE에 pending/planned 엔트리 존재 |
| 읽기 | `TASK_QUEUE.yaml`, `MODULE_REGISTRY.yaml` |
| 쓰기 | (직접 쓰기 없음 — 다른 역할에 배정 지시) |
| 처리 | DependencyGraph 빌드 → 토폴로지 정렬 → 실행 가능 모듈 선출 → Builder 배정 (최대 3개 동시) |
| 바톤 전달 | 각 모듈에 Planner → Builder 순서로 역할 배정 |
| 구현 | `DependencyGraphBuilder.cs`, `ParallelBuilderOrchestrator.cs`, `OrchestratorSimulator.cs` |

**실행 가능 모듈 판정:**
```
module.status == planned
AND
모든 depends_on 모듈의 status == done
```

### 3.4 [Role 4] Planner

| 항목 | 값 |
|------|-----|
| 트리거 | Orchestrator가 모듈을 배정, status == pending |
| 읽기 | `generated_specs/<Module>_SPEC.md`, `MODULE_REGISTRY.yaml`, `TASK_QUEUE.yaml`, `PROJECT_OVERVIEW.md`, `CODING_RULES.md`, `learning/RULE_MEMORY.yaml`, `learning/RECURRING_MISTAKES.md`, `MODULE_DISCOVERY.md`, `INTEGRATION_STRATEGY.md`, `MIGRATION_RULES.md`, `diff_reports/<Module>_DIFF.md` (v2.3) |
| 쓰기 | `plans/<Module>_PLAN.md`, `TASK_QUEUE.yaml` (status: pending → planned, owner 할당, impact_analysis/compatibility_review 업데이트) |
| 바톤 전달 | status == planned → Builder 시작 |
| 상태 전이 | `status: pending → planned` |

**v2.1 — Integration Strategy에 따른 Planner 추가 처리:**

| integration_strategy | Planner 추가 행동 |
|---------------------|-------------------|
| `reuse` | PLAN에 "기존 모듈 재사용" 명시, Builder는 코드 생성 불필요 |
| `extend` | Impact Analysis 실행, PLAN에 확장 범위 명시 |
| `adapt` | PLAN에 어댑터 설계 포함 |
| `replace` | Impact Analysis + Migration Plan 작성 |
| `create_new` | 표준 PLAN 작성 |

**PLAN 산출물:**
```
PLAN:
  module: <ModuleName>
  integration_strategy: <strategy>           # v2.1
  existing_module_candidates: [<modules>]    # v2.1
  target_files: [6파일 목록]
  dependencies: [<dep1>, <dep2>]
  core_access: false
  risk: low | medium | high
  known_risks_from_learning: [RM-XXXX, REC-XXX]
  impact_analysis: <summary>                 # v2.1, extend/replace 시
  migration_plan: <summary>                  # v2.1, replace 시
  architecture_diff_warnings: [<warnings>]   # v2.3, medium+ diff 항목
  notes: <한 줄 설명>
```

### 3.5 [Role 5] Builder

| 항목 | 값 |
|------|-----|
| 트리거 | status == planned, depends_on 모두 done |
| 읽기 | `plans/<Module>_PLAN.md`, `generated_specs/<Module>_SPEC.md`, `CODING_RULES.md`, `MODULE_TEMPLATES.md`, `learning/CODING_PATTERNS.md`, `learning/RULE_MEMORY.yaml`, `learning/HUMAN_FIX_EXAMPLES.md` |
| 쓰기 | `Assets/Game/Modules/<Module>/` (6파일), `TASK_QUEUE.yaml` (status, human_state) |
| 바톤 전달 | human_state == pending → 파이프라인 정지, Human Validator 대기 |
| 상태 전이 | `status: planned → in_progress`, `human_state: none → pending` |

**Builder는 human_state를 validated로 변경할 수 없다.** 사람만 가능하다.

### 3.6 [Role 6] Human Validator ★

| 항목 | 값 |
|------|-----|
| 트리거 | human_state == pending (Builder가 코드 생성 완료) |
| 읽기 | 생성된 코드, AIValidationReport.json |
| 쓰기 | `TASK_QUEUE.yaml` (human_state, human_fixes[]), 소스 코드 (직접 수정) |
| 바톤 전달 | human_state == validated → Builder가 status: in_progress → review 전이 |
| 주체 | **사람** (AI 에이전트가 아님) |

**Human Validator 절차:**
```
1. human_state: pending → in_review
2. Unity Editor에서 Tools/AI/Validate Generated Modules 실행
3. 코드 직접 확인
4. 에러 없으면:
   human_state: in_review → validated
5. 에러 있으면:
   human_state: in_review → fixing
   코드 직접 수정
   human_fixes[]에 수정 이유 기록
   human_state: fixing → validated
6. Builder가 status: in_progress → review 전이 가능
```

**human_fixes 기록 형식:**
```yaml
human_fixes:
  - file: "HealthRuntime.cs"
    change: "MonoBehaviour 상속 제거"
    rationale: "Runtime은 순수 C#이어야 함"
    timestamp: "2026-03-18T14:30:00"
```

**Gate 우회 절대 금지:**
- AI가 자동으로 validated 처리하는 것 금지
- "Validator PASS니까 바로 review" 금지
- 모든 모듈은 예외 없이 Human Gate 통과 필수

### 3.7 [Role 7] Reviewer

| 항목 | 값 |
|------|-----|
| 트리거 | status == review (human_state == validated가 전제) |
| 읽기 | 모든 소스 코드, `AIValidationReport.json`, `TASK_QUEUE.yaml` (human_fixes[]), `learning/VALIDATOR_FAILURE_PATTERNS.md`, `learning/RULE_MEMORY.yaml` |
| 쓰기 | `TASK_QUEUE.yaml` (commit_state, status), `reviews/<Module>_REVIEW.md` |
| 바톤 전달 (통과) | commit_state == ready → Committer 시작 (feature_group 전체 ready 시) |
| 바톤 전달 (실패) | status → blocked → Builder에게 수정 지시 (루프) |
| 상태 전이 (통과) | `commit_state: none → ready` |
| 상태 전이 (실패) | `status: review → blocked` + blocked_reason 기록 |
| 최종 마감 | Committer + Learning Recorder 완료 후 `status: review → done` |

**v2.1 — Reviewer의 Integration Strategy 검증:**

| integration_strategy | Reviewer 추가 검증 |
|---------------------|-------------------|
| `extend` | 기존 인터페이스 하위 호환성 확인, compatibility_review → passed/failed |
| `replace` | Impact Analysis 완료 확인, Migration Plan 존재 확인, 영향 모듈 업데이트 확인 |
| `adapt` | 어댑터가 기존 API를 올바르게 래핑하는지 확인 |
| `create_new` | 표준 검증 |
| `reuse` | 의존성 연결만 확인 |

**Reviewer는 코드를 수정하지 않고, git 작업을 하지 않는다.**

**REVIEW RESULT 산출물:**
```
REVIEW RESULT:
  module: <ModuleName>
  result: pass | fail
  commit_state_transition: none → ready (pass)
  status_transition: review → blocked (fail)
  errors: <count>
  warnings: <count>
  risk: low | medium | high
  human_modifications: [...]
  issues: [...] (fail 시)
  action_required: [...] (fail 시)
```

### 3.8 [Role 8] Committer

| 항목 | 값 |
|------|-----|
| 트리거 | feature_group 내 모든 모듈의 commit_state == ready |
| 읽기 | `TASK_QUEUE.yaml`, `MODULE_REGISTRY.yaml`, `COMMIT_RULES.md` |
| 쓰기 | `TASK_QUEUE.yaml` (commit_state), `commit_logs/<group>_COMMIT.md`, git staging + commit |
| 바톤 전달 | commit_state == committed → Learning Recorder 시작 |
| 상태 전이 | `commit_state: ready → committed` (feat) 또는 `ready → recommitted` (fix) |

**5 Gate 체크 (COMMIT_RULES.md §2 참조):**

| Gate | 확인 | 실패 시 |
|------|------|---------|
| Reviewer Gate | commit_state == ready | Reviewer 미완료 |
| Human Gate | human_state == validated | 사람 검증 미완료 |
| Learning Gate | learning_note_required == true → learning_state ∈ {recorded, recorded_existing_rule_reference} | Learning Recorder 미완료 (v2.2: AI fix도 포함) |
| Completeness Gate | feature_group 전체 ready | 부분 커밋 금지 |
| Scope Gate | 스테이징 파일이 feature_group만 | 관련 없는 파일 혼입 |

**Committer는 코드를 수정하지 않고, 검증 판정을 하지 않는다.**

### 3.9 [Role 9] Learning Recorder

| 항목 | 값 |
|------|-----|
| 트리거 | commit_state == committed (learning_state 자동 → pending) |
| 읽기 | `TASK_QUEUE.yaml` (human_fixes[], ai_post_validation_fixes[], learning_note_required), `AIValidationReport.json`, `reviews/<Module>_REVIEW.md`, `learning/` 전체 |
| 쓰기 | `learning/LEARNING_LOG.md`, `learning/HUMAN_FIX_EXAMPLES.md`, `learning/RULE_MEMORY.yaml`, `learning/VALIDATOR_FAILURE_PATTERNS.md`, `learning/RECURRING_MISTAKES.md`, `learning/CROSS_PROJECT_RULES.md`, `TASK_QUEUE.yaml` (learning_state) |
| 바톤 전달 | learning_state ∈ {recorded, recorded_existing_rule_reference} → Reviewer가 status: review → done으로 최종 마감 |
| 상태 전이 | `learning_state: pending → recorded` 또는 `pending → recorded_existing_rule_reference` (v2.2) |

**Learning Recorder 절차 (v2.2 강화):**
```
1. 수집: human_fixes[] + ai_post_validation_fixes[] + Validator 보고서 + Reviewer 보고서 + retry_count
2. learning_note_required 확인
   → false이고 fix 총합 == 0: learning_state → recorded (빈 기록)
   → true: 반드시 이벤트 분류 + 기록 진행
3. 각 fix에 대해 Learning Event 분류:
   a. related_rule 존재 → type: existing_rule_reinforced
   b. related_rule 없음 → type: new_rule
   c. 동일 validator 3회+ → type: failure_pattern
4. LEARNING_LOG.md에 시간순 엔트리 추가 (이벤트 목록 포함)
5. human_fixes 있으면 HUMAN_FIX_EXAMPLES.md에 Before/After 추가
6. existing_rule_reinforced이면 RULE_MEMORY.yaml의 reinforcement_count += 1
7. 새 실패 패턴이면 VALIDATOR_FAILURE_PATTERNS.md 업데이트
8. 새 규칙이면 RULE_MEMORY.yaml에 추가 (중복 확인)
9. 동일 유형 3회 이상이면 RECURRING_MISTAKES.md에 패턴 등록
10. scope: global이면 CROSS_PROJECT_RULES.md에 추가
11. learning_state 전이:
    → new_rule/failure_pattern 있으면: pending → recorded
    → existing_rule_reinforced만: pending → recorded_existing_rule_reference
```

**v2.2 핵심:** `learning_note_required == true`이면 Learning Recorder는 건너뛰지 않는다.
AI auto-fix도 교훈이다. 상세: `LEARNING_SYSTEM.md`, `PIPELINE_HARDENING.md`

---

## 4. 4차원 상태 모델

단일 status 필드 대신 4개 차원으로 태스크 상태를 추적한다.

| 차원 | 필드 | 추적 대상 | 전이 주체 |
|------|------|-----------|-----------|
| 빌드 진행 | `status` | AI 파이프라인 단계 | Planner, Builder, Reviewer |
| 사람 검증 | `human_state` | Human Validation Gate | Human, Builder(none→pending만) |
| 학습 기록 | `learning_state` | 학습 데이터 축적 | Learning Recorder, System(자동) |
| 커밋 상태 | `commit_state` | Git 커밋/재커밋 | Reviewer(none→ready), Committer |

### 4.1 status (빌드 진행)

```
pending ──► planned ──► in_progress ──► review ──► done
                              │            │
                              │            ▼
                              │         blocked ──► escalated
                              │            │
                              ◄────────────┘
```

| 전이 | 주체 | 교차 조건 |
|------|------|-----------|
| `pending → planned` | Planner | PLAN 산출 |
| `planned → in_progress` | Builder | depends_on 모두 done, owner 할당 |
| `in_progress → review` | Builder | **human_state == validated 필수** |
| `review → done` | Reviewer | **commit_state ∈ {committed, recommitted} + learning_state == recorded** |
| `review → blocked` | Reviewer | 검증 실패 |
| `blocked → in_progress` | Builder | retry_count < 3 |
| `blocked → escalated` | System | retry_count >= 3 |

### 4.2 human_state (사람 검증)

```
none ──► pending ──► in_review ──► validated
                        │
                        ▼
                      fixing ──► validated
```

| 전이 | 주체 |
|------|------|
| `none → pending` | Builder (코드 생성 완료) |
| `pending → in_review` | Human |
| `in_review → validated` | Human (수정 불필요) |
| `in_review → fixing` | Human (수정 필요) |
| `fixing → validated` | Human (수정 완료 + human_fixes 기록) |
| `validated → pending` | System (blocked → in_progress 시 자동 리셋) |

### 4.3 learning_state (학습 기록)

```
none ──► pending ──► recorded
```

| 전이 | 주체 |
|------|------|
| `none → pending` | System (commit_state → committed 시 자동) |
| `pending → recorded` | Learning Recorder |

### 4.4 commit_state (커밋 상태)

```
none ──► ready ──► committed ──► recommit_ready ──► recommitted
```

| 전이 | 주체 |
|------|------|
| `none → ready` | Reviewer (검증 통과) |
| `ready → committed` | Committer (feature_group 전체 ready) |
| `committed → recommit_ready` | Human/System (사후 이슈) |
| `recommit_ready → ready` | Reviewer (재검증 통과) |
| `ready → recommitted` | Committer (재커밋) |

### 4.5 교차 조건 (Cross-Dimension Guards)

| status 전이 | human_state | learning_state | commit_state |
|-------------|-------------|----------------|--------------|
| `in_progress → review` | == validated | (무관) | (무관) |
| `review → done` | == validated | == recorded | ∈ {committed, recommitted} |
| `blocked → in_progress` | 자동 → pending | 자동 → none | 자동 → none |

### 4.6 금지 전이

| 시도 | 이유 |
|------|------|
| `in_progress → review` (human_state != validated) | Human Gate 우회 |
| `review → done` (commit_state ∉ {committed, recommitted}) | 커밋 없이 완료 |
| `review → done` (learning_state != recorded) | 학습 미기록 |
| `blocked → in_progress` (retry_count >= 3) | escalated로 가야 함 |
| AI가 human_state를 validated로 변경 | 사람만 가능 |

---

## 5. 정상 경로 (Happy Path) — 상세 시퀀스

```
[Step 1] Feature Intake
  FEATURE_QUEUE.yaml: 새 엔트리 추가
  feature status: intake

[Step 2] Queue Generator
  learning/RULE_MEMORY.yaml + RECURRING_MISTAKES.md 읽기
  모듈 분해 + 의존성 추론 + 위험 주석
  TASK_QUEUE.yaml: N개 엔트리 추가 (status: pending)
  MODULE_REGISTRY.yaml: N개 모듈 등록
  generated_specs/: N개 Spec 생성
  feature status: intake → decomposed → queued

[Step 3] Orchestrator
  DependencyGraph 빌드 → 토폴로지 정렬
  실행 가능 모듈 선출 (depends_on 모두 done)
  Builder 배정 (최대 3개 동시, 단일 에이전트 시 직렬)

[Step 4] Planner (각 모듈)
  Spec + learning 읽기
  plans/<Module>_PLAN.md 작성
  status: pending → planned

[Step 5] Builder (각 모듈)
  PLAN + Spec + learning 읽기
  Assets/Game/Modules/<Module>/ 에 6파일 생성
  자체 점검 (구조, 코딩 규칙, GC, naming)
  status: planned → in_progress
  human_state: none → pending

  ─── 파이프라인 정지: 사람 대기 ───

[Step 6] Human Validator ★
  Unity Editor에서 Validator 실행
  코드 직접 확인 + 필요 시 수정
  human_fixes[] 기록 (수정한 경우)
  human_state: pending → in_review → (fixing →) validated

  ─── 파이프라인 재개 ───

[Step 5→7] Builder가 status: in_progress → review 전이

[Step 7] Reviewer
  코드 + Validator 보고서 + human_fixes 분석
  통과: commit_state: none → ready
  실패: status → blocked + blocked_reason → [Step 5]로 루프

  (feature_group 내 모든 모듈 commit_state == ready 대기)

[Step 8] Committer
  5 Gate 체크 (COMMIT_RULES.md)
  feature_group 파일만 선별 스테이징
  git commit
  commit_state: ready → committed
  commit_logs/<group>_COMMIT.md 기록
  learning_state: 자동 none → pending

[Step 9] Learning Recorder
  human_fixes[], Validator 보고서, Reviewer 보고서 수집
  learning/ 폴더에 데이터 기록
  learning_state: pending → recorded

[Final] Reviewer가 status: review → done

완료 상태:
  status: done
  human_state: validated
  learning_state: recorded
  commit_state: committed
```

---

## 6. 실패 경로 (Blocked Path)

### 6.1 검증 실패 → 재시도 루프

```
[Step 7] Reviewer: status → blocked, blocked_reason 기록
  ↓
[Step 5'] Builder: 실패 사유 확인 → 코드 수정
  status: blocked → in_progress
  retry_count += 1
  human_state: 자동 → pending (리셋)
  learning_state: 자동 → none (리셋)
  commit_state: 자동 → none (리셋)
  ↓
[Step 6'] Human Validator: 재검증
  human_state: pending → in_review → validated
  ↓
[Step 5'→7'] Builder: status → review
  ↓
[Step 7'] Reviewer: 재검증
  통과 → commit_state: none → ready → [Step 8]
  실패 → blocked (반복, retry_count < 3)
  ↓
retry_count >= 3 → status: blocked → escalated
  Learning Recorder가 RECURRING_MISTAKES에 패턴 등록
```

### 6.2 에스컬레이션

3회 검증 실패 시:
1. status → escalated
2. AI 에이전트는 escalated 태스크를 건드리지 않음
3. 실패 이력 요약을 사용자에게 제시
4. Learning Recorder가 반복 패턴을 `learning/RECURRING_MISTAKES.md`에 기록
5. 사용자가 직접 해결 후 status를 planned/in_progress로 수동 복원
6. 복원 시 retry_count를 0으로 리셋

### 6.3 의존 모듈 blocked 시

- 의존하는 하위 태스크는 planned 상태에서 대기 (진행 불가)
- 상위 모듈이 done이 되면 하위 태스크가 자동으로 진행 가능

---

## 7. 사후 재커밋 경로 (Recommit Path)

이미 done이고 커밋된 모듈에서 문제가 발견된 경우:

```
[R1] 사후 이슈 발견
  status: done → blocked
  commit_state: committed → recommit_ready
  human_state: validated → pending

[R2] Builder: 코드 수정
  status: blocked → in_progress

[R3] Human Validator: 재검증
  human_state: pending → validated

[R4] Builder: status → review

[R5] Reviewer: 재검증
  commit_state: recommit_ready → ready

[R6] Committer: fix 커밋
  commit_state: ready → recommitted

[R7] Learning Recorder: 학습 기록
  learning_state: pending → recorded

[R8] Reviewer: status → done
```

재커밋 메시지 형식:
```
fix(<feature-group>): apply human validation fixes for <Module>

- modules: [<Module>]
- fix_reason: <수정 이유>
- human_modifications:
  - <파일>: <변경 요약>
- original_commit: <원본 해시>
- validation: PASS
- learning_recorded: true
- generated by: Autonomous Game Factory v2
```

---

## 8. 의존성 규칙

### 8.1 모듈 의존성

- `TASK_QUEUE.yaml`의 `depends_on` 필드에 나열된 모듈이 **모두 done** 상태여야 `planned → in_progress` 전이 가능
- `MODULE_REGISTRY.yaml`의 `dependencies`와 `TASK_QUEUE.yaml`의 `depends_on`은 일치해야 함
- 순환 의존 절대 금지 (DFS 기반 검출)
- UnityEngine, System은 의존 목록에서 필터링

### 8.2 의존성 그래프 시스템

Orchestrator는 모듈 생성 전에 의존성 그래프를 빌드하여 실행 순서를 결정한다.

```
1. MODULE_REGISTRY.yaml 읽기
2. TASK_QUEUE.yaml 읽기
3. 의존성 그래프 구성
4. 토폴로지 정렬
5. 실행 가능 모듈 목록 산출
```

구현: `DependencyGraphBuilder.cs`

### 8.3 의존성 검증

| Validator | 검사 |
|-----------|------|
| `DependencyValidator` | 레지스트리 의존성 존재, using 정합성, QUEUE-REGISTRY 일치 |
| `CircularDependencyValidator` | DFS 순환 감지, 토폴로지 완전성 |

---

## 9. 병렬 실행 규칙

**Cursor는 단일 에이전트다.** 병렬은 사용자가 여러 Cursor 창을 열어 동시 작업할 때만 적용된다.
단일 창에서는 직렬로 실행한다. 멀티 에이전트를 시뮬레이션하지 않는다.

### 9.1 모듈 격리

- 각 Builder는 **자기 모듈 폴더만** 수정: `Assets/Game/Modules/<자기 모듈>/`
- 동일 모듈에 두 명 이상의 Builder 동시 할당 불가 (owner 필드)

### 9.2 제한

| 제한 | 값 |
|------|-----|
| 최대 동시 Builder | 3 |
| 최대 오케스트레이션 라운드 | 20 |
| 최대 검증 재시도 | 3 |

### 9.3 태스크 할당 절차

```
1. TASK_QUEUE.yaml 읽기
2. owner == null 확인
3. depends_on 모두 done 확인
4. owner에 에이전트 ID 기록
5. role에 현재 역할 기록
6. status 전이
7. 작업 시작
```

### 9.4 수정 금지 영역

| 파일/폴더 | 이유 |
|-----------|------|
| `Assets/Editor/AI/` | 검증 시스템 — 공유 인프라 |
| `Assets/Game/Core/` | editable: false |
| `Assets/Game/Modules/Template/` | 참조용 원본 |
| `.cursor/rules/` | 사용자만 수정 |

### 9.5 공유 파일 수정 규칙

| 파일 | 수정 가능 역할 | 조건 |
|------|----------------|------|
| `TASK_QUEUE.yaml` | 모든 역할 | 자기 태스크 항목만 |
| `MODULE_REGISTRY.yaml` | Queue Generator, Planner | 추가만, 삭제/수정 금지 |
| `FEATURE_QUEUE.yaml` | Feature Intake, Queue Generator, Committer | 상태 전이만 |
| `learning/` 전체 | Learning Recorder만 | append-only |

---

## 10. 학습 통합 포인트

학습 시스템은 파이프라인의 **입력과 출력 양쪽**에 통합된다.

### 10.1 학습 소비 (파이프라인 입력 측)

| 역할 | 읽는 학습 파일 | 활용 |
|------|---------------|------|
| Queue Generator | RULE_MEMORY.yaml, RECURRING_MISTAKES.md, VALIDATOR_FAILURE_PATTERNS.md | 위험 플래그, acceptance_criteria 생성 |
| Planner | RULE_MEMORY.yaml, RECURRING_MISTAKES.md | PLAN에 "회피할 규칙" 섹션 |
| Builder | CODING_PATTERNS.md, RULE_MEMORY.yaml, HUMAN_FIX_EXAMPLES.md | 코드 생성 시 패턴 적용 |
| Reviewer | VALIDATOR_FAILURE_PATTERNS.md, RULE_MEMORY.yaml | 알려진 패턴 집중 검증 |

### 10.2 학습 생산 (파이프라인 출력 측)

| 이벤트 | Learning Recorder가 기록하는 곳 |
|--------|-------------------------------|
| Validator 실패 | LEARNING_LOG.md + VALIDATOR_FAILURE_PATTERNS.md |
| 사람 수정 | LEARNING_LOG.md + HUMAN_FIX_EXAMPLES.md |
| 새 규칙 발견 | RULE_MEMORY.yaml |
| 3회 이상 반복 | RECURRING_MISTAKES.md |
| 범용 교훈 | CROSS_PROJECT_RULES.md |

### 10.3 학습 시스템 진입점

모든 에이전트는 작업 시작 전 `learning/LEARNING_INDEX.md`를 읽어 학습 시스템 구조를 파악한다.

### 10.4 프로젝트 간 재사용

`learning/` 폴더는 프로젝트 간 복사 가능. `scope: global` 항목은 다른 프로젝트에서도 유효.

---

## 11. 커밋 통합 포인트

커밋은 **feature_group 단위**로 실행된다. 상세 규칙은 `COMMIT_RULES.md` 참조.

### 11.1 커밋 타입

| 타입 | 용도 | commit_state 전이 |
|------|------|-------------------|
| `feat(<group>)` | 신규 모듈 첫 커밋 | ready → committed |
| `fix(<group>)` | 사람 수정 후 재커밋 | ready → recommitted |
| `chore(ai-learning)` | 학습 데이터 독립 커밋 | 태스크 무관 |

### 11.2 Learning Gate

사람 수정(human_fixes > 0)이 있으면 Learning Gate가 활성화된다:
- `learning_state == recorded` 없이 커밋 불가
- Learning Recorder가 기록 완료해야 Committer가 진행 가능

### 11.3 Reviewer의 최종 마감

Committer와 Learning Recorder가 모두 완료된 후, **Reviewer**가 `status: review → done`으로 최종 마감한다.
이것은 Reviewer가 커밋과 학습 완료를 확인하고 태스크를 공식적으로 닫는 것이다.

```
commit_state ∈ {committed, recommitted}
  AND learning_state == recorded
  → Reviewer: status → done
```

---

## 12. 검증 흐름

### 12.1 Builder 자체 점검 (human_state: pending 전이 전)

1. 모든 필수 파일 존재 (I, Runtime, Config, Factory, Bootstrap, Tests)
2. 네임스페이스 `Game`
3. Runtime: MonoBehaviour 미상속
4. Config: ScriptableObject 상속
5. Factory: static class
6. 매직넘버 없음
7. GC 유발 코드 없음 (코루틴, 람다, LINQ, foreach)
8. 테스트 최소 1개
9. 과거 학습 패턴 회피 확인

### 12.2 Human Validation (Step 6)

사람이 Unity Editor에서 수행:
1. Tools/AI/Validate Generated Modules 실행
2. AIValidationReport.json 확인
3. 코드 직접 확인 및 수정
4. human_fixes[] 기록

### 12.3 Reviewer 검증 (Step 7)

12개 Validator 결과 확인:

| 검증기 | 검사 |
|--------|------|
| CompileErrorValidator | 컴파일 에러 |
| ValidatorRegistrationValidator | 검증기 등록 정합성 |
| ForbiddenFolderValidator | 금지 영역 수정 |
| ModuleStructureValidator | 필수 파일 누락 |
| ModuleBoundaryValidator | 모듈 간 불법 참조 |
| ArchitectureRuleValidator | 아키텍처 패턴 위반 |
| ArchitecturePatternValidator | 구조 패턴 검증 |
| CodingStyleValidator | 코딩 스타일 |
| PerformanceValidator | 성능 규칙 |
| DependencyValidator | 의존성 선언 불일치 |
| CircularDependencyValidator | 순환 의존 |
| StringAndAnimatorValidator | 문자열/Animator 규칙 |

---

## 13. RUN_LOG 기록

각 오케스트레이션 실행은 `docs/ai/runs/RUN_LOG.md`에 기록:

```
## Run <timestamp>

### Round 1

**Executable:** [Economy, StatusEffect, Player]
**Skipped:** [Warriors → waiting for Economy]
**Builder Assignments:** [builder_1→Economy, ...]
**Human Validation:** [Economy→validated (fixes: 0), ...]
**Reviewer:** [Economy→PASS, ...]
**Commit:** feat(round-1): Economy, StatusEffect, Player — <hash>
**Learning:** [new patterns: 0, human fixes: 1, recurring: false]
**Final State:** [Economy→done, StatusEffect→done, Player→done]

### Round 2
...
```

---

## 14. 테스트 도구

| 메뉴 | 설명 |
|------|------|
| `Tools/AI/Run Dependency Graph Tests` | 의존성 검증 4개 케이스 |
| `Tools/AI/Simulate Orchestrator (Mini Queue)` | 의존 순서 시뮬레이션 |
| `Tools/AI/Simulate Parallel Builders (Independent Modules)` | 독립 모듈 병렬 빌드 |
| `Tools/AI/Simulate Builder Pool (Dependency-Mixed)` | 의존성 혼합 병렬 |
| `Tools/AI/Run Parallel Builder Orchestrator` | 실제 TASK_QUEUE 기반 오케스트레이션 |
| `Tools/AI/Validate Generated Modules` | 전체 Validator 실행 |
| `Tools/AI/Feature Pipeline/*` | Feature 파이프라인 도구 |

---

## 15. 10가지 보장

| # | 보장 | 메커니즘 |
|---|------|----------|
| 1 | 사람 검증 필수 | Human Gate — human_state == validated 없이 review 불가 |
| 2 | 태스크 소유권 | owner 필드 — 한 태스크에 한 에이전트 |
| 3 | 모듈 격리 | 각 Builder는 자기 모듈 폴더만 수정 |
| 4 | 아키텍처 검증 | 12개 Validator |
| 5 | 역할 분리 | Reviewer ≠ Committer ≠ Learning Recorder |
| 6 | 재커밋 흐름 | Recommit Path — committed → recommit_ready → recommitted |
| 7 | 학습 축적 | Learning Gate — 사람 수정 시 learning == recorded 없이 done 불가 |
| 8 | 에스컬레이션 | retry_count >= 3 → escalated |
| 9 | 4차원 독립성 | 각 차원이 독립 전이, 상태 폭발 방지 |
| 10 | 커밋 범위 격리 | 5 Gate — Scope Gate가 관련 없는 파일 차단 |

---

## 16. 역할별 상태 전이 권한 매트릭스

| 차원 | Feature Intake | Queue Gen | Planner | Builder | Human | Reviewer | Committer | Learning Rec |
|------|----------------|-----------|---------|---------|-------|----------|-----------|-------------|
| FEATURE_QUEUE | W | R/W | R | R | R | R | W(상태만) | R |
| TASK_QUEUE status | - | W(pending) | W(planned) | W(in_progress) | R | W(done,blocked) | R | R |
| human_state | - | - | R | W(pending) | W(전체) | R | R | R |
| commit_state | - | - | R | R | R | W(ready) | W(committed) | R |
| learning_state | - | - | R | R | R | R | R | W(recorded) |
| MODULE_REGISTRY | - | W(추가) | W(추가) | R | R | R | R | R |
| learning/ 폴더 | - | R | R | R | R | R | R | W(append) |
| 소스 코드 | - | - | - | W(자기 모듈) | W(수정) | R | - | R |
| git 작업 | - | - | - | - | - | - | W | - |

---

## 17. 참조 문서

| 문서 | 내용 |
|------|------|
| `STATE_MACHINE.md` | 4차원 상태 전이 명세, 교차 조건, 복합 상태 매핑 |
| `TASK_SCHEMA.md` | TASK_QUEUE.yaml 필드 정의, enum 값, 검증 규칙 |
| `AGENT_ROLES.md` | 9개 역할 상세, 허용/금지 액션 |
| `COMMIT_RULES.md` | 5 Gate, Staging Policy, Recommit, 커밋 메시지 규격 |
| `QUEUE_GENERATOR.md` | Queue Generator 분해/의존/위험 규칙, 전체 예시 |
| `FEATURE_INTAKE.md` | Feature Intake 입력 형식, 자연어 변환 예시 |
| `CODING_RULES.md` | C#/Unity 코딩 규칙 |
| `MODULE_TEMPLATES.md` | 모듈 템플릿 6파일 구조 |
| `generated_specs/README.md` | Spec 출력 규격 |
| `PROJECT_HANDOFF.md` | 핸드오프 문서 — 현재 상태, 갭, 다음 작업 |
| `MODULE_DISCOVERY.md` | v2.1 — Module Discovery 절차 |
| `INTEGRATION_STRATEGY.md` | v2.1 — Reuse Decision Engine |
| `MIGRATION_RULES.md` | v2.1 — 마이그레이션 규칙 |
| `PIPELINE_HARDENING.md` | v2.2 — 파이프라인 강화 |
| `LEARNING_SYSTEM.md` | v2.2 — Learning Recorder 강화 |
| `CONFIG_RULES.md` | v2.2 — Config Source-of-Truth 규칙 |
| `ARCHITECTURE_DIFF_ANALYZER.md` | v2.3 — Architecture Diff Analyzer 명세 |
| `GAME_FACTORY_ARCHITECTURE.md` | v2.3 — Game Factory 전체 아키텍처 |
| `PIPELINE_AUTOMATION.md` | v2.3 — 파이프라인 자동화 캡빌리티 명세 |
| `CROSS_PROJECT_INTELLIGENCE.md` | CPIL — Cross-Project Intelligence Layer 명세 |
| `GLOBAL_MODULE_LIBRARY.md` | CPIL — Global Module Library 상세 |
| `GLOBAL_LEARNING_SYSTEM.md` | CPIL — Cross-Project Learning System 상세 |
| `PROMPT_OS.md` | Prompt OS — 프롬프트 운영체제 아키텍처 |
| `COMMAND_PROFILES.yaml` | Prompt OS — 커맨드 프로파일 레지스트리 |
| `DEFAULT_EXECUTION_POLICY.yaml` | Prompt OS — 기본 실행 정책 |
| `AUTO_CONTEXT_RULES.md` | Prompt OS — 자동 컨텍스트 규칙 |
| `PARALLEL_EXECUTION_AUDIT.md` | v3.0 — 직렬/병렬 실행 감사 결과 |
| `PARALLEL_ORCHESTRATION.md` | v3.0 — 실행 단위 기반 병렬 아키텍처 |
| `TASK_EXECUTION_SCHEMA.md` | v3.0 — TaskExecutionUnit / AgentLease / DependencyReadyQueue |
| `WORKTREE_STRATEGY.md` | v3.0 — 작업 격리 전략 (L0~L3) |
| `JOIN_AND_MERGE_REVIEW.md` | v3.0 — 병렬 작업 합류/머지/리뷰 |
