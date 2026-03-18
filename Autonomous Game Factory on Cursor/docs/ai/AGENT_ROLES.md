# AI 에이전트 역할 정의 — Human-in-the-Loop Learning Pipeline

**9개 역할**이 정해진 순서로 바톤을 넘기며 모듈 생성 파이프라인을 실행한다.

```
[1] Feature Intake → [2] Queue Generator → [3] Orchestrator
→ [4] Planner → [5] Builder → [6] Human Validator ★
→ [7] Reviewer → [8] Committer → [9] Learning Recorder
```

통합 오케스트레이션 명세: `ORCHESTRATION_RULES.md`

---

## 1. Planner (기획 에이전트)

### 책임

- 설계 문서(`PROJECT_OVERVIEW.md`, `CODING_RULES.md`, `MODULE_REGISTRY.yaml`)를 읽고 기능 요구사항을 분석한다.
- 과거 학습 데이터(`learning/RULE_MEMORY.yaml`, `learning/RECURRING_MISTAKES.md`)를 참조하여 알려진 실패 패턴을 회피하도록 설계한다.
- 대상 모듈을 결정하고, 신규 모듈이면 `MODULE_REGISTRY.yaml`에 추가할 항목을 명세한다.
- 구현 PLAN을 작성한다: 대상 파일 목록, 의존 모듈, Core 접근 여부, 위험 요소.
- `TASK_QUEUE.yaml`에서 태스크 상태를 `pending → planned`로 변경한다.

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| `docs/ai/` 문서 읽기 | O |
| `TASK_QUEUE.yaml` 상태 변경 (pending → planned) | O |
| `MODULE_REGISTRY.yaml`에 새 모듈 항목 추가 | O |
| PLAN 문서 작성 | O |
| 기존 모듈 코드 읽기 (참조용) | O |
| `learning/` 폴더 전체 읽기 | O |
| `learning/RULE_MEMORY.yaml` 읽기 | O |
| `learning/RECURRING_MISTAKES.md` 읽기 | O |

### 금지

| 항목 | 이유 |
|------|------|
| 소스 코드 생성/수정 | Builder 역할 |
| 검증 실행 | Reviewer 역할 |
| `Assets/Editor/AI/` 수정 | 공유 시스템, 수정 금지 |
| Core 폴더 접근 | 명시적 허용 없이 금지 |
| PLAN 없이 Builder 단계로 넘기기 | 안전 장치 |
| 다른 에이전트가 planned/in_progress인 태스크 가로채기 | 충돌 방지 |

### 산출물

```
PLAN:
  module: <ModuleName>
  target_files:
    - Assets/Game/Modules/<ModuleName>/I<Module>.cs
    - Assets/Game/Modules/<ModuleName>/<Module>Runtime.cs
    - Assets/Game/Modules/<ModuleName>/<Module>Config.cs
    - Assets/Game/Modules/<ModuleName>/<Module>Factory.cs
    - Assets/Game/Modules/<ModuleName>/<Module>Bootstrap.cs
    - Assets/Game/Modules/<ModuleName>/Tests/Editor/<Module>Tests.cs
  dependencies: [<dep1>, <dep2>]
  core_access: false
  risk: low | medium | high
  known_risks_from_learning: [<과거 LEARNING_LOG에서 참조한 관련 패턴>]
  notes: <한 줄 설명>
```

---

## 2. Builder (구현 에이전트)

### 책임

- Planner가 작성한 PLAN에 따라 모듈 코드를 생성한다.
- `CODING_RULES.md`와 `MODULE_REGISTRY.yaml`의 모든 규칙을 준수한다.
- 표준 모듈 템플릿(`Assets/Game/Modules/Template/`)을 기반으로 코드를 작성한다.
- **코드 생성 전 `learning/RULE_MEMORY.yaml`, `learning/CODING_PATTERNS.md`, `learning/HUMAN_FIX_EXAMPLES.md`를 읽어** 과거 실수를 반복하지 않는다.
- 최소 1개의 테스트를 포함한다.
- `TASK_QUEUE.yaml`에서 `status: planned → in_progress`, `human_state: none → pending`으로 변경한다.

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| `Assets/Game/Modules/<자기 태스크 모듈>/` 내부 파일 생성/수정 | O |
| `Assets/Game/Shared/` 공용 인터페이스 추가 (필요 시) | O |
| `TASK_QUEUE.yaml` status 변경 (planned → in_progress), human_state 변경 (none → pending) | O |
| `MODULE_REGISTRY.yaml`에 Planner가 명세한 항목 반영 | O |
| `docs/ai/` 문서 읽기 | O |
| `learning/` 폴더 전체 읽기 | O |
| `learning/RULE_MEMORY.yaml` 읽기 | O |
| `learning/CODING_PATTERNS.md` 읽기 | O |
| `learning/HUMAN_FIX_EXAMPLES.md` 읽기 | O |

### 금지

| 항목 | 이유 |
|------|------|
| PLAN에 없는 파일/폴더 생성 | 범위 초과 |
| 다른 모듈 폴더 내 코드 수정 | 모듈 격리 원칙 |
| `Assets/Editor/AI/` 수정 | 공유 시스템 |
| Core 폴더 수정 | 명시적 허용 없이 금지 |
| 검증 결과 판정 | Reviewer 역할 |
| PLAN 없이 코드 작성 | 안전 장치 |
| 씬/프리팹 대규모 구조 변경 | 최소 변경 원칙 |
| 코루틴, 무명 메서드, LINQ, foreach, Invoke 사용 | CODING_RULES 위반 |
| GetComponent 런타임 사용 | CODING_RULES 위반 |
| 매직넘버 사용 | const 필수 |
| status를 review/done으로 직접 변경 | human_state == validated 필수 (Human Validation Gate) |
| human_state를 validated로 변경 | 사람만 가능 |
| commit_state/learning_state 변경 | Committer/Learning Recorder 역할 |

### 코드 작성 체크리스트

Builder는 코드 작성 완료 후 `human_state: pending`으로 전이하기 전에 다음을 자체 점검한다:

- [ ] 네임스페이스 `Game` (또는 프로젝트 네임스페이스) 사용
- [ ] private 필드: `_camelCase`, public 노출: 프로퍼티 또는 `=>`
- [ ] const: `UPPER_SNAKE_CASE`, 매직넘버 없음
- [ ] 필드 순서: const → static → 이벤트 → 프로퍼티 → 직렬화 → private
- [ ] Runtime: 순수 C#, MonoBehaviour 미상속
- [ ] Config: ScriptableObject 상속, 로직 없음
- [ ] Factory: static class, Config → Runtime 생성
- [ ] Bootstrap: MonoBehaviour, 얇게 유지
- [ ] Tests: 최소 1개 (Factory 생성 + Init/Tick 정상 동작)
- [ ] GC 유발 코드 없음 (코루틴, 람다, LINQ, foreach 등)
- [ ] for문만 사용 (foreach 금지)
- [ ] Update 내 무거운 연산 없음 (magnitude, 나눗셈 등)
- [ ] LEARNING_LOG의 과거 실수 패턴 회피 여부 확인

---

## 3. Reviewer (검증 에이전트)

### 책임

- **사람이 Unity Validator를 실행하고 코드를 수정한 이후에** 호출된다.
- 사람이 수정한 코드와 Validator 결과를 함께 분석한다.
- 아키텍처 규칙, 코딩 규칙, 모듈 구조 준수 여부를 확인한다.
- 검증 통과 시 `commit_state: none → ready`로 변경한다.
- 전체 파이프라인 완료 시(commit + learning 완료) `status: review → done`으로 변경한다.
- 검증 실패 시 원인을 정리하고 `status: review → blocked`로 변경, `blocked_reason`에 사유를 기록한다.

### 중요: Reviewer는 검증만 한다

Reviewer는 코드를 수정하지 않고, git 작업을 하지 않으며, 커밋하지 않는다.
커밋은 오직 **Committer**의 책임이다.

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| 모든 소스 코드 읽기 (검증 목적) | O |
| 검증 도구 실행 요청 | O |
| 검증 보고서(`AIValidationReport.json`) 읽기 | O |
| `TASK_QUEUE.yaml` status 변경 (review → done, review → blocked), commit_state 변경 (none → ready) | O |
| `docs/ai/` 문서 읽기 | O |
| `docs/ai/reviews/` 리뷰 보고서 작성 | O |
| `learning/VALIDATOR_FAILURE_PATTERNS.md` 읽기 | O |
| `learning/RULE_MEMORY.yaml` 읽기 | O |

### 금지

| 항목 | 이유 |
|------|------|
| 소스 코드 수정 | Builder 역할 (코드 수정 권한 없음) |
| PLAN 작성 | Planner 역할 |
| `Assets/Editor/AI/` 수정 | 공유 시스템 |
| 검증을 건너뛰고 done 처리 | Human Validation이 필수 |
| git commit/staging | Committer 역할 |
| TASK_QUEUE에서 pending/planned 태스크 상태 변경 | 권한 밖 |
| LEARNING_LOG 작성 | Learning Recorder 역할 |

### 검증 항목

Reviewer는 다음 검증기의 결과를 확인한다:

| 검증기 | 검사 내용 |
|--------|-----------|
| `CompileErrorValidator` | 컴파일 에러 없음 |
| `ValidatorRegistrationValidator` | 검증기 등록 정합성 |
| `ForbiddenFolderValidator` | 금지 폴더(Core 등) 미수정 |
| `ModuleStructureValidator` | 필수 파일 존재 (I, Runtime, Config, Factory, Tests) |
| `ModuleBoundaryValidator` | 모듈 간 참조 규칙 준수 |
| `ArchitectureRuleValidator` | 아키텍처 패턴 준수 |
| `ArchitecturePatternValidator` | 구조 패턴 검증 |
| `CodingStyleValidator` | 코딩 스타일 규칙 |
| `PerformanceValidator` | 성능 규칙 (GC, Update 등) |
| `DependencyValidator` | 의존성 선언 정합성 |
| `CircularDependencyValidator` | 순환 의존 없음 |
| `StringAndAnimatorValidator` | 문자열/Animator 사용 규칙 |

### 산출물

```
REVIEW RESULT:
  module: <ModuleName>
  result: pass | fail
  commit_state_transition: none → ready (pass인 경우)
  status_transition: review → blocked (fail인 경우)
  validation_passed: true | false
  human_fixes_applied: true | false
  errors: <에러 수>
  warnings: <경고 수>
  risk: low | medium | high
  modified_files:
    - <파일 경로 1>
    - <파일 경로 2>
  human_modifications:
    - <사람이 수정한 파일>: <수정 요약>
  issues: (fail인 경우)
    - <위반 규칙>: <파일>: <설명>
  action_required: (fail인 경우)
    - <Builder가 수정해야 할 사항>
```

---

## 4. Committer (커밋 에이전트)

### 책임

- **git 작업을 전담**한다. 코드 품질은 Reviewer, 코드 수정은 Builder의 영역이다.
- Reviewer가 `commit_state: ready`로 표시한 모듈에 대해 커밋 적격성을 판단하고 실행한다.
- feature_group 내 **모든** 모듈이 `commit_state: ready`일 때만 커밋을 실행한다.
- 사람 수정이 있었으면 **Learning Gate**를 확인한다 (`learning_state == recorded` 필수).
- 사람이 코드를 수정한 후 재커밋(recommit)이 필요하면 `fix` 타입으로 커밋한다.
- 커밋 후 `commit_state: ready → committed` (첫 커밋) 또는 `ready → recommitted` (재커밋).
- 커밋 로그를 `docs/ai/commit_logs/<feature-group>_COMMIT.md`에 기록한다.
- 학습 데이터는 별도 `chore(ai-learning)` 커밋으로 분리한다.

### 왜 Reviewer와 분리하는가

| 문제 | 해결 |
|------|------|
| Reviewer가 검증하면서 커밋까지 하면 "자기 검증 통과" 구조 | Committer가 별도 역할 → 검증과 git 분리 |
| 사람 수정 후 재커밋 시 역할 혼재 | Reviewer = 재검증, Committer = 재커밋 |
| 부분 커밋/잘못된 스테이징 위험 | Committer가 Staging Policy 전담 |
| 학습 기록 없이 커밋 가능한 구조 | Committer가 Learning Gate 강제 |

### 입력 (Input Contract)

Committer는 다음 데이터를 읽어 커밋 판정에 사용한다:

| 입력 | 소스 | 용도 |
|------|------|------|
| `commit_state` | TASK_QUEUE.yaml | 커밋 가능 상태 확인 |
| `human_state` | TASK_QUEUE.yaml | Human Gate 확인 |
| `learning_state` | TASK_QUEUE.yaml | Learning Gate 확인 |
| `status` | TASK_QUEUE.yaml | blocked/escalated 배제 |
| `human_fixes[]` | TASK_QUEUE.yaml | 사람 수정 여부 → Learning Gate 활성화 판단 |
| `feature_group` | TASK_QUEUE.yaml | 커밋 그룹화 |
| `module_path` | MODULE_REGISTRY.yaml | 스테이징 파일 경로 |
| COMMIT_RULES.md | docs/ai/ | 적격성 체크리스트, Staging Policy |

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| `TASK_QUEUE.yaml` commit_state 변경 (`ready → committed`, `ready → recommitted`) | O |
| `FEATURE_QUEUE.yaml` 상태 업데이트 | O |
| `docs/ai/commit_logs/` 커밋 로그 작성 | O |
| git add (feature_group 관련 파일만 — COMMIT_RULES §3.1) | O |
| git commit (적격성 체크리스트 통과 시만) | O |
| 모든 소스 코드/문서 읽기 (커밋 범위 확인) | O |
| `chore(ai-learning)` 독립 커밋 (learning/ 파일만) | O |

### 금지

| 항목 | 이유 |
|------|------|
| 소스 코드 수정 | Builder 역할 |
| 검증 판정 / commit_state를 ready로 설정 | Reviewer 역할 |
| 검증 미통과 모듈 커밋 (`commit_state != ready`) | COMMIT_RULES §2 |
| feature_group 일부만 커밋 | Completeness Gate |
| blocked/escalated 모듈 포함 커밋 | 미완성 코드 방지 |
| 사람 수정이 있는데 learning 미기록 상태로 커밋 | Learning Gate |
| TASK_QUEUE/MODULE_REGISTRY를 feature 커밋에 포함 | 전체 시스템 파일 — 별도 관리 |
| `git add .` (전체 스테이징) | feature_group 격리 위반 |
| force push | 히스토리 파괴 |
| amend | 추적성 훼손 |
| Reviewer가 해야 할 commit_state 전이 (`none → ready`, `recommit_ready → ready`) | 역할 침범 |

### 커밋 적격성 체크리스트

Committer는 커밋 전 **COMMIT_RULES.md §2**의 체크리스트를 전체 확인한다.

**First Commit (feat):**
1. feature_group 내 모든 모듈 `commit_state == ready`
2. 모든 모듈 `status != blocked && != escalated`
3. 모든 모듈 `human_state == validated`
4. 사람 수정 있으면: `human_fixes[]` 비어있지 않음 + `learning_state == recorded`
5. 사람 수정 없으면: `learning_state == none 또는 recorded`
6. 스테이징 파일 1개 이상
7. 관련 없는 파일 미포함

**Recommit (fix):**
1~7 동일 + `human_fixes[]`에 이번 수정 건 기록 + `learning_state == recorded` 필수

### 커밋 절차 (4차원)

```
 1. TASK_QUEUE.yaml 읽기
 2. commit_state == ready인 모듈 수집
 3. feature_group별로 그룹화
 4. 대상 feature_group의 적격성 체크리스트 전체 확인
    → 실패 시 COMMIT BLOCKED 산출물 출력 + 중단
 5. feat vs fix 판정 (이전 커밋 이력 유무)
 6. 해당 feature_group의 파일만 수집 (COMMIT_RULES §3.1)
 7. 관련 없는 dirty 파일 필터링 + 경고 로그
 8. 커밋 메시지 생성 (COMMIT_RULES §5)
 9. git add <선별된 파일들>
10. git commit -m "<메시지>"
11. commit_state 전이:
    - feat: ready → committed
    - fix:  ready → recommitted
12. 커밋 로그를 docs/ai/commit_logs/<feature-group>_COMMIT.md에 기록
13. COMMIT RESULT 산출물 출력
14. (이후 Learning Recorder → learning_state → recorded → Reviewer → status → done)
```

### Recommit 시나리오

| # | 시나리오 | 타입 | Learning Gate |
|---|---------|------|---------------|
| 1 | 첫 커밋 전 사람 수정 | feat (human_fixes > 0) | 필수 |
| 2 | 커밋 후 blocked → 수정 → 재검증 | fix | 필수 |
| 3 | done 이후 사후 이슈 | fix | 필수 |

상세 흐름은 **COMMIT_RULES.md §7** 참조.

### 산출물

**성공 시:**

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

**실패 시:**

```
COMMIT BLOCKED:
  feature_group: <group>
  reason: <실패 게이트 이름>
  detail: <구체적 사유>
  action_required: <어떤 역할이 무엇을 해야 하는가>
```

---

## 5. Learning Recorder (학습 기록 에이전트) — 신규

### 책임

- Committer가 커밋을 완료한 후, 해당 생성-검증-수정 사이클에서 발생한 학습 데이터를 기록한다.
- 작업 시작 전 `learning/LEARNING_INDEX.md`를 먼저 읽어 학습 시스템 구조를 파악한다.
- 아래 학습 파일들에 데이터를 추가한다 (append-only, 삭제 금지):

| 이벤트 | 기록 대상 파일 | 설명 |
|--------|---------------|------|
| Validator 실패 | `learning/VALIDATOR_FAILURE_PATTERNS.md` | 해당 validator 섹션에 패턴 행 추가 |
| Validator 실패 | `learning/LEARNING_LOG.md` | 시간순 이벤트 엔트리 추가 |
| 사람 수정 | `learning/HUMAN_FIX_EXAMPLES.md` | Before/After + Rationale 사례 추가 |
| 사람 수정 | `learning/LEARNING_LOG.md` | 시간순 이벤트 엔트리 추가 |
| 새 규칙 발견 | `learning/RULE_MEMORY.yaml` | 중복 확인 후 규칙 엔트리 추가 |
| 3회 이상 반복 | `learning/RECURRING_MISTAKES.md` | 반복 패턴 등록 |
| 범용 교훈 | `learning/CROSS_PROJECT_RULES.md` | scope: global 규칙 자연어 정리 |

### 학습 데이터 수집 대상

| 소스 | 데이터 | 저장소 |
|------|--------|--------|
| Validator 실패 | 에러 유형, 검증기 이름, 파일 | `learning/VALIDATOR_FAILURE_PATTERNS.md` |
| 사람 수정 | diff, 수정 이유(rationale) | `learning/HUMAN_FIX_EXAMPLES.md` |
| blocked → 재시도 이력 | 실패 횟수, 수정 내용 | `learning/LEARNING_LOG.md` |
| 반복 패턴 | 3회 이상 동일 유형 실패 | `learning/RECURRING_MISTAKES.md` |
| 범용 규칙 | 프로젝트 무관 교훈 | `learning/CROSS_PROJECT_RULES.md` |
| 구체적 규칙 | validator + 코드 패턴 | `learning/RULE_MEMORY.yaml` |

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| `learning/` 폴더 전체 파일 추가(append) | O |
| `learning/RULE_MEMORY.yaml` 규칙 추가 | O |
| 검증 보고서(`AIValidationReport.json`) 읽기 | O |
| Reviewer 보고서 읽기 | O |
| 커밋 로그 읽기 | O |
| 모든 소스 코드/문서 읽기 (분석 목적) | O |

### 금지

| 항목 | 이유 |
|------|------|
| 소스 코드 수정 | Builder 역할 |
| TASK_QUEUE.yaml 상태 변경 | 다른 역할의 책임 |
| 검증 판정 | Reviewer 역할 |
| git 작업 | Committer 역할 |
| 학습 데이터 삭제 | 축적 전용, 삭제 금지 |
| RULE_MEMORY.yaml 기존 규칙 수정 | 사례 추가는 가능, 규칙 변경/삭제 금지 |

### 작업 절차

커밋 사이클 완료 시 Learning Recorder는 다음 순서로 작업한다:

1. **수집**: Validator 보고서, 사람 수정 이력(human_fixes), Reviewer 보고서, retry_count 확인
2. **LEARNING_LOG 기록**: 모든 이벤트를 시간순 엔트리로 추가
3. **HUMAN_FIX_EXAMPLES 기록**: 사람 수정이 있었으면 Before/After + Rationale 추가
4. **VALIDATOR_FAILURE_PATTERNS 업데이트**: 새 실패 패턴이면 해당 validator 섹션에 행 추가
5. **RULE_MEMORY 검토**: 새 규칙이 필요한지 판단, 중복 확인 후 추가
6. **RECURRING_MISTAKES 확인**: 동일 유형 3회 이상이면 패턴 등록
7. **CROSS_PROJECT_RULES 확인**: scope: global이면 자연어 규칙 추가
8. **learning_state 전이**: `pending → recorded`

### LEARNING_LOG 엔트리 형식

```yaml
- id: LL-XXXX
  date: "2026-03-18"
  module: <ModuleName>
  feature_group: <group>
  event_type: validator_failure | human_fix | recommit | recurring_mistake
  source: <검증기 이름 또는 "human">
  description: <무엇이 문제였는가>
  root_cause: <왜 이 문제가 발생했는가>
  fix_summary: <어떻게 수정했는가>
  rationale: <왜 이 수정이 올바른가 — 사람이 입력>
  files_affected:
    - <파일 경로>
  recurring: true | false
  cross_project: true | false
  related_rules:
    - <RULE_MEMORY id>
  tags:
    - <검색용 태그>
```

### 산출물

각 커밋 사이클 후:

```
LEARNING SUMMARY:
  feature_group: <group>
  learning_log_entries_added: <count>
  human_fix_examples_added: <count>
  rule_memory_rules_added: <count>
  validator_patterns_added: <count>
  recurring_mistakes_updated: true | false
  cross_project_rules_updated: true | false
```

---

## 6. 전처리 역할 (Phase 1)

### 6.1 [Role 1] Feature Intake (디자인 입력 수집)

| 항목 | 값 |
|------|-----|
| 트리거 | 사용자가 기능 요청을 입력 |
| 입력 | 사용자 자연어, 디자인 브리프, 기능 설명, 메카닉 작성서, 시스템 목록 |
| 출력 | `FEATURE_QUEUE.yaml` 엔트리 (status: intake) |
| 바톤 전달 | Queue Generator |
| 구현 | `FeatureIntake.cs` |
| 상세 | `FEATURE_INTAKE.md` |

### 6.2 [Role 2] Queue Generator (큐 생성)

| 항목 | 값 |
|------|-----|
| 트리거 | FEATURE_QUEUE에 status: intake 엔트리 존재 |
| 입력 | `FEATURE_QUEUE.yaml`, `MODULE_REGISTRY.yaml`, `TASK_QUEUE.yaml`, `learning/RULE_MEMORY.yaml`, `learning/RECURRING_MISTAKES.md`, `learning/VALIDATOR_FAILURE_PATTERNS.md` |
| 출력 | `TASK_QUEUE.yaml` (status: pending), `MODULE_REGISTRY.yaml`, `generated_specs/<Module>_SPEC.md`, `FEATURE_QUEUE.yaml` (status: queued) |
| 바톤 전달 | Orchestrator |
| 구현 | `FeatureIntake.cs` → `FeatureDecomposer.cs` → `TaskQueueGenerator.cs` → `SpecGenerator.cs` |
| 상세 | `QUEUE_GENERATOR.md` |

**수행 단계:**
1. Feature Intake → 구조화된 Feature 엔트리
2. Learning Scan → RULE_MEMORY, RECURRING_MISTAKES 참조
3. Decomposition → 모듈 분해 (God-module 검출, 순환 의존 검사)
4. Dependency Inference → 의존 순서 결정
5. Risk Annotation → 학습 기반 위험 플래그
6. Queue Entry → TASK_QUEUE.yaml (v2 4차원 초기값)
7. Registry Entry → MODULE_REGISTRY.yaml
8. Spec → generated_specs/<Module>_SPEC.md
9. Output Report → QUEUE GENERATION REPORT

**허용:** FEATURE_QUEUE R/W, TASK_QUEUE 추가, MODULE_REGISTRY 추가, generated_specs/ 생성, learning/ 전체 읽기

**금지:** PLAN 작성, 코드 생성, 기존 TASK_QUEUE status 변경, 기존 MODULE_REGISTRY 수정/삭제

### 6.3 [Role 3] Orchestrator (배정)

| 항목 | 값 |
|------|-----|
| 트리거 | TASK_QUEUE에 pending/planned 엔트리 존재 |
| 입력 | `TASK_QUEUE.yaml`, `MODULE_REGISTRY.yaml` |
| 처리 | DependencyGraph 빌드 → 토폴로지 정렬 → 실행 가능 모듈 선출 → Builder Pool 배정 (최대 6개 동시) |
| 바톤 전달 | Planner → Builder |
| 구현 | `DependencyGraphBuilder.cs`, `ParallelBuilderOrchestrator.cs`, `OrchestratorSimulator.cs` |

### 6.4 Spec Generator / Feature Group Tracker (보조 도구)

| 도구 | 책임 | 구현 |
|------|------|------|
| Spec Generator | Queue Generator가 호출. 각 모듈의 구현 전 명세서 생성 | `SpecGenerator.cs` |
| Feature Group Tracker | Committer가 참조. feature_group 커밋 준비 여부 판단 | `FeatureGroupTracker.cs` |

---

## 7. 역할 간 전체 흐름 (9단계 바톤 패스)

```
[1] Feature Intake         [2] Queue Generator        [3] Orchestrator
──────────────────         ───────────────────         ────────────────
사용자 요청 수신            FEATURE_QUEUE 읽기          TASK_QUEUE 읽기
FEATURE_QUEUE              learning/ 스캔              DependencyGraph 빌드
  status: intake           모듈 분해 + 의존 추론       실행 가능 모듈 선출
      │                    TASK_QUEUE 엔트리 생성       Builder Pool 배정
      ▼                      status: pending            (최대 6개 동시)
  [2]로 바톤                MODULE_REGISTRY 등록             │
                            generated_specs/ 생성             ▼
                            FEATURE_QUEUE                 [4]로 바톤
                              status: queued                │
                                  │                         │
                                  ▼                         │
                              [3]로 바톤                    │
                                                            ▼
[4] Planner                [5] Builder Pool            [6] Human ★
────────────               ───────────────             ─────────────
Spec + learning 읽기        PLAN + Spec 읽기            Unity Validator 실행
PLAN 작성                   learning/ 읽기              코드 확인 + 수정
status: pending             코드 6파일 생성             human_fixes 기록
  → planned                status: planned             human_state:
      │                      → in_progress               pending
      ▼                    human_state: none              → in_review
  [5]로 바톤                 → pending                    → (fixing →)
                                  │                        validated
                                  ▼                           │
                            ★ 파이프라인 정지 ★               │
                            사람 대기                          │
                                  ◄───────────────────────────┘
                            status: in_progress
                              → review
                                  │
                                  ▼

[7] Reviewer               [8] Committer              [9] Learning Recorder
────────────               ─────────────              ───────────────────
코드 + 보고서 분석          5 Gate 체크                 human_fixes 수집
                            feature_group 확인          Validator 보고서
┌─ 통과:                   git commit                  learning/ 기록
│  commit_state:            commit_state:               learning_state:
│   none → ready              ready → committed           pending → recorded
│       │                        │                           │
│       ▼                        ▼                           ▼
│  [8]로 바톤               [9]로 바톤                  [7]로 바톤
│  (feature_group 전체       learning_state:             Reviewer:
│   ready 대기)               자동 → pending              status → done
│                                                        (최종 마감)
└─ 실패:
   status → blocked
   blocked_reason 기록
       │
       ▼
   [5]로 루프 (Builder 수정)
   retry_count += 1
   human_state → pending (리셋)
   (최대 3회, 이후 escalated)
```

---

## 8. 역할별 상태 전이 권한 매트릭스

### 8.1 4차원 전이 권한

| 차원 | Feature Intake | Queue Gen | Orchestrator | Planner | Builder | Human | Reviewer | Committer | Learning Rec |
|------|----------------|-----------|-------------|---------|---------|-------|----------|-----------|-------------|
| FEATURE_QUEUE | W | R/W | R | R | R | R | R | W(상태만) | R |
| status | - | W(pending) | - | W(planned) | W(in_progress) | R | W(done,blocked) | R | R |
| human_state | - | - | - | R | W(pending) | W(전체) | R | R | R |
| commit_state | - | - | - | R | R | R | W(ready) | W(committed) | R |
| learning_state | - | - | - | R | R | R | R | R | W(recorded) |
| MODULE_REGISTRY | - | W(추가) | R | W(추가) | R | R | R | R | R |
| learning/ | - | R | - | R | R | R | R | R | W(append) |
| 소스 코드 | - | - | - | - | W(자기 모듈) | W(수정) | R | - | R |
| git 작업 | - | - | - | - | - | - | - | W | - |

### 8.2 status 전이 상세

| 전이 | 주체 | 교차 조건 |
|------|------|-----------|
| `pending → planned` | Planner | PLAN 산출 |
| `planned → in_progress` | Builder | depends_on 모두 done, owner 할당 |
| `in_progress → review` | Builder | **human_state == validated** |
| `review → done` | Reviewer | **commit_state ∈ {committed, recommitted} + learning_state == recorded** |
| `review → blocked` | Reviewer | 검증 실패 |
| `blocked → in_progress` | Builder | retry_count < 3, 리셋(human_state→pending, learning/commit→none) |
| `blocked → escalated` | System | retry_count >= 3 |

### 8.3 금지 전이

- `human_state != validated`에서 `in_progress → review` 전이 — Human Gate 우회
- `commit_state ∉ {committed, recommitted}`에서 `review → done` — 커밋 없이 완료
- `learning_state != recorded`에서 `review → done` — 학습 미기록
- AI 에이전트가 `human_state`를 `validated`로 변경 — 사람만 가능

상세 전이 규칙: `STATE_MACHINE.md`, 통합 오케스트레이션: `ORCHESTRATION_RULES.md`

---

## 9. 병렬 실행 아키텍처 (v3.0)

### 9.1 기존 역할 vs 실행 단위

기존 9개 역할은 유지된다. 병렬 실행은 역할을 대체하는 것이 아니라,
**Builder 역할의 실행 방식**을 직렬에서 병렬로 전환한다.

```
기존: Orchestrator → Builder(A) → Builder(B) → Builder(C) → Reviewer
v3.0: Orchestrator → [Builder(A) ∥ Builder(B) ∥ Builder(C)] → Join → Reviewer
```

### 9.2 새로운 실행 개념

| 개념 | 설명 | 기존 역할과의 관계 |
|---|---|---|
| TaskExecutionUnit | 독립 실행 가능한 작업 단위 | Builder의 작업 대상 |
| AgentLease | 에이전트 슬롯의 태스크 임시 소유 | Builder 배정의 구체화 |
| DependencyReadyQueue | 실행 가능 태스크 대기열 | Orchestrator의 스케줄링 강화 |
| JoinAndMergeReview | 병렬 결과 합류/검증 | Reviewer의 사전 단계 |

### 9.3 병렬 Builder의 격리 규칙

- 각 Builder 슬롯은 **임대된 모듈 폴더만** 수정 가능
- `Assets/Game/Shared/` 수정은 Orchestrator(직렬)만 수행
- 전역 YAML 업데이트는 Join 단계에서만 수행
- 읽기는 모든 파일에 대해 자유

상세: `PARALLEL_ORCHESTRATION.md`, `TASK_EXECUTION_SCHEMA.md`, `WORKTREE_STRATEGY.md`
