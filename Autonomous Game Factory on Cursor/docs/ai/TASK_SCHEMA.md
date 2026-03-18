# Task Schema — TASK_QUEUE.yaml 필드 명세

이 문서는 `docs/ai/TASK_QUEUE.yaml`의 각 태스크 엔트리 필드를 정의한다.
모든 에이전트는 태스크를 생성/수정할 때 이 스키마를 준수해야 한다.

---

## 1. 스키마 버전

현재: **v2.3** (Architecture Diff Analyzer)

v1 → v2 변경:
- `human_state`, `learning_state`, `commit_state` 3개 차원 추가
- `human_fixes`, `retry_count`, `blocked_reason`, `feature_group` 메타데이터 추가
- `escalated` 상태 추가
- 기존 필드는 모두 유지 (하위 호환)

v2.1 → v2.2 변경:
- `ai_post_validation_fixes` — AI가 Validator 실패 후 자동 수정한 내역 분리
- `post_validation_fix_count` — AI 자동 수정 건수
- `post_validation_fix_actor` — 수정 주체 (human/ai/none)
- `learning_note_required` — 학습 기록 필수 여부 (fix가 있으면 true)

v2.2 → v2.3 변경:
- `arch_diff_risk` — Architecture Diff Analyzer 위험 등급 (low/medium/high/critical/not_analyzed)
- `arch_diff_blocked` — critical 위험 시 파이프라인 차단 여부
- `arch_diff_report_path` — Diff 리포트 파일 경로

---

## 2. 필드 정의

### 2.1 필수 필드 (Required)

| 필드 | 타입 | 설명 | 예시 |
|------|------|------|------|
| `name` | string | 모듈 이름 (PascalCase) | `Economy` |
| `status` | enum | 빌드 진행 상태 | `pending` |
| `priority` | enum | 우선순위 | `high` |
| `owner` | string\|null | 현재 작업 에이전트 ID | `null` |
| `role` | enum\|null | 현재 담당 역할 | `builder` |
| `depends_on` | string[] | 선행 의존 모듈 목록 | `[Economy, Warriors]` |
| `module_path` | string | 모듈 코드 경로 | `Assets/Game/Modules/Economy` |
| `description` | string | 모듈 한 줄 설명 | `코인 경제 시스템` |

### 2.2 v2 확장 필드 (신규 태스크에 필수)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `human_state` | enum | `none` | 사람 검증 상태 |
| `learning_state` | enum | `none` | 학습 기록 상태 |
| `commit_state` | enum | `none` | 커밋 상태 |
| `feature_group` | string | (필수) | 소속 feature_group |

### 2.3 v2.1 통합 전략 필드 (Integration Strategy — 신규 태스크에 권장)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `integration_strategy` | enum | `create_new` | 모듈 통합 전략 |
| `existing_module_candidates` | string[] | `[]` | Module Discovery로 발견된 유사 모듈 목록 |
| `compatibility_review` | enum | `not_required` | 호환성 검토 상태 |
| `impact_analysis` | enum | `not_required` | 영향 분석 상태 |
| `migration_required` | boolean | `false` | 마이그레이션 필요 여부 |

### 2.4 v2.2 Fix Classification 필드 (Pipeline Hardening — 신규 태스크에 권장)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `ai_post_validation_fixes` | object[] | `[]` | AI가 Validator 실패 후 자동 수정한 내역 배열 |
| `post_validation_fix_count` | int | `0` | AI 자동 수정 건수 |
| `post_validation_fix_actor` | enum | `none` | 가장 최근 수정 주체 (none/ai/human/both) |
| `learning_note_required` | boolean | `false` | 학습 기록 필수 여부 (fix가 1건 이상이면 true) |

### 2.5 v2.3 Architecture Diff Analysis 필드 (신규 태스크에 권장)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `arch_diff_risk` | enum | `not_analyzed` | Diff Analyzer 위험 등급 |
| `arch_diff_blocked` | boolean | `false` | critical 위험 시 파이프라인 차단 여부 |
| `arch_diff_report_path` | string\|null | `null` | Diff 리포트 파일 경로 (예: `docs/ai/diff_reports/Economy_DIFF.md`) |

**arch_diff_risk enum 값:**

| 값 | 설명 |
|-----|------|
| `not_analyzed` | Diff 분석 미수행 (초기값) |
| `low` | 위험 없음, 진행 허용 |
| `medium` | 경고 전달, 진행 허용 |
| `high` | Human 승인 권장, 진행 허용 |
| `critical` | **파이프라인 차단** — arch_diff_blocked=true |

### 2.6 v2.3 Automation 필드 (Game Factory Capabilities)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `regression_check` | enum | `not_checked` | Regression Guardian 검사 결과 (not_checked/passed/issues_found/blocked) |
| `self_heal_applied` | boolean | `false` | Self-Healer가 메타데이터 수정을 적용했는지 여부 |
| `decomposition_source` | enum | `manual` | 모듈 분해 출처 (manual/intelligent_decomposer) |
| `knowledge_patterns_checked` | boolean | `false` | Architecture Knowledge Memory 패턴 체크 여부 |

### 2.7 선택 필드 (Optional)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `retry_count` | int | `0` | blocked → 재시도 횟수 (3 초과 시 escalated) |
| `blocked_reason` | string\|null | `null` | 가장 최근 blocked 사유 |
| `human_fixes` | object[] | `[]` | **사람만** 수정한 내역 배열 (AI 수정과 분리) |
| `review_report` | string\|null | `null` | 최근 Reviewer 보고서 경로 |
| `commit_hash` | string\|null | `null` | 최근 커밋 해시 |
| `created_at` | string\|null | `null` | 태스크 생성 일시 |
| `updated_at` | string\|null | `null` | 최종 업데이트 일시 |

---

## 3. Enum 값 정의

### 3.1 status

| 값 | 설명 | 이전 상태 | 다음 상태 |
|----|------|-----------|-----------|
| `pending` | 초기 상태 | — | `planned` |
| `planned` | PLAN 작성 완료 | `pending` | `in_progress` |
| `in_progress` | Builder 구현 중 | `planned`, `blocked` | `review` |
| `review` | Reviewer 검증 중 | `in_progress` | `done`, `blocked` |
| `done` | 완료 (커밋 + 학습 완료) | `review` | `blocked` (사후 이슈) |
| `blocked` | 검증 실패/의존 미충족 | `review`, `done` | `in_progress`, `escalated` |
| `escalated` | 사람 직접 해결 필요 | `blocked` | `planned` (수동 복원) |

### 3.2 human_state

| 값 | 설명 |
|----|------|
| `none` | 사람 검증 단계 아님 |
| `pending` | 사람 검증 대기 중 |
| `in_review` | 사람이 검증 중 |
| `fixing` | 사람이 코드 수정 중 |
| `validated` | 사람 검증 완료 |

### 3.3 learning_state

| 값 | 설명 |
|----|------|
| `none` | 학습 기록 단계 아님 |
| `pending` | 학습 기록 대기 중 |
| `recorded` | 학습 기록 완료 (새 규칙 또는 실패 패턴) |
| `recorded_existing_rule_reference` | 기존 규칙 강화로 기록 완료 (v2.2) |

### 3.3.1 post_validation_fix_actor

| 값 | 설명 |
|----|------|
| `none` | 수정 없음 |
| `ai` | AI만 수정 |
| `human` | 사람만 수정 |
| `both` | AI + 사람 모두 수정 |

### 3.4 commit_state

| 값 | 설명 |
|----|------|
| `none` | 커밋 단계 아님 |
| `ready` | 커밋 가능 (Reviewer 통과) |
| `committed` | 커밋 완료 |
| `recommit_ready` | 재커밋 필요 |
| `recommitted` | 재커밋 완료 |

### 3.5 integration_strategy

| 값 | 설명 |
|----|------|
| `reuse` | 기존 모듈을 그대로 사용. 신규 모듈 생성하지 않음 |
| `extend` | 기존 모듈에 새 기능/API 추가 |
| `adapt` | 기존 모듈을 래핑하여 새 인터페이스 제공 |
| `replace` | 기존 모듈을 새 모듈로 대체 (Impact Analysis + Migration 필수) |
| `create_new` | 유사 모듈 없음, 처음부터 생성 |

### 3.6 compatibility_review

| 값 | 설명 |
|----|------|
| `not_required` | 호환성 검토 불필요 (create_new, reuse) |
| `pending` | 호환성 검토 대기 |
| `passed` | 호환성 검토 통과 |
| `failed` | 호환성 검토 실패 → 전략 재결정 필요 |

### 3.7 impact_analysis

| 값 | 설명 |
|----|------|
| `not_required` | 영향 분석 불필요 (create_new, reuse, adapt) |
| `pending` | 영향 분석 대기 |
| `completed` | 영향 분석 완료 |

### 3.8 priority

| 값 | 설명 |
|----|------|
| `high` | 다른 모듈의 의존 대상, 최우선 |
| `medium` | 일반 기능 모듈 |
| `low` | 최종 단계, 의존이 많은 통합 모듈 |

### 3.6 role

| 값 | 설명 |
|----|------|
| `planner` | Planner 에이전트 |
| `builder` | Builder 에이전트 |
| `reviewer` | Reviewer 에이전트 |
| `committer` | Committer 에이전트 |
| `learning_recorder` | Learning Recorder 에이전트 |
| `null` | 미할당 |

---

## 4. Fix 엔트리 구조

### 4.1 human_fixes — 사람이 수정한 내역

사람이 코드를 수정할 때 각 수정 건을 기록한다. **AI 자동 수정은 여기에 기록하지 않는다.**

```yaml
human_fixes:
  - file: "<파일명>"
    change: "<변경 요약>"
    rationale: "<수정 이유>"
    timestamp: "<ISO 8601>"
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `file` | string | 수정한 파일 경로 (모듈 내 상대 경로 가능) |
| `change` | string | 무엇을 바꿨는가 |
| `rationale` | string | 왜 수정했는가 (AI 에이전트와 Learning Recorder가 읽음) |
| `timestamp` | string | 수정 시각 (ISO 8601) |

### 4.2 ai_post_validation_fixes — AI가 Validator 실패 후 수정한 내역

AI가 Validator 실패를 받고 코드를 자동 수정했을 때 기록한다. **사람 수정과 분리하여 학습 정확도를 높인다.**

```yaml
ai_post_validation_fixes:
  - validator: "<실패한 Validator 이름>"
    reason: "<실패 사유>"
    change: "<변경 요약>"
    related_rule: "<기존 규칙 ID | null>"
    timestamp: "<ISO 8601>"
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `validator` | string | 실패를 보고한 Validator 이름 (e.g. ModuleBoundaryValidator) |
| `reason` | string | Validator가 보고한 에러 사유 |
| `change` | string | AI가 수행한 수정 내용 |
| `related_rule` | string\|null | 기존 RULE_MEMORY의 규칙 ID (있으면 reinforced로 분류) |
| `timestamp` | string | 수정 시각 (ISO 8601) |

### 4.3 Fix Classification 규칙

| 상황 | 기록 위치 | post_validation_fix_actor |
|------|-----------|--------------------------|
| AI가 Validator 실패 후 코드 수정 | `ai_post_validation_fixes` | `ai` |
| 사람이 코드 직접 수정 | `human_fixes` | `human` |
| AI + 사람 모두 수정 | 각각 해당 배열 | `both` |
| 수정 없음 | 둘 다 비어있음 | `none` |

### 4.4 learning_note_required 자동 결정

```
IF human_fixes.length > 0 OR ai_post_validation_fixes.length > 0:
  learning_note_required = true
ELSE:
  learning_note_required = false
```

**learning_note_required == true이면 Learning Recorder는 건너뛰지 않고 반드시 기록해야 한다.**

---

## 5. 전체 엔트리 예시

### 5.1 예시 1: 신규 모듈 — 첫 생성에서 완료까지

```yaml
- name: InventorySystem
  status: done
  priority: medium
  owner: null
  role: null
  depends_on: [Economy]
  module_path: Assets/Game/Modules/InventorySystem
  feature_group: farming-loop
  description: 아이템 인벤토리 관리 — 슬롯, 스택, 추가/제거

  # v2 상태 차원
  human_state: validated
  learning_state: recorded
  commit_state: committed

  # 메타데이터
  retry_count: 1
  blocked_reason: null
  commit_hash: "a1b2c3d"
  created_at: "2026-03-18T10:00:00"
  updated_at: "2026-03-18T16:45:00"

  # 사람 수정 이력
  human_fixes:
    - file: "InventorySystemRuntime.cs"
      change: "foreach를 for문으로 변경 (L45, L78)"
      rationale: "CODING_RULES에서 foreach 금지, Builder가 규칙 누락"
      timestamp: "2026-03-18T14:30:00"
    - file: "InventorySystemConfig.cs"
      change: "매직넘버 20을 const MAX_SLOT_COUNT로 변경"
      rationale: "매직넘버 금지 규칙 위반"
      timestamp: "2026-03-18T14:35:00"

  # 리뷰 보고서
  review_report: "docs/ai/reviews/InventorySystem_REVIEW.md"
```

### 5.2 예시 2: 사람 검증 대기 중인 모듈

```yaml
- name: CropGrowth
  status: in_progress
  priority: medium
  owner: builder_2
  role: builder
  depends_on: []
  module_path: Assets/Game/Modules/CropGrowth
  feature_group: farming-loop
  description: 작물 성장 시스템 — 성장 단계, 타이머, 수확 조건

  # v2 상태 차원
  human_state: pending
  learning_state: none
  commit_state: none

  # 메타데이터
  retry_count: 0
  blocked_reason: null
  commit_hash: null
  created_at: "2026-03-18T10:00:00"
  updated_at: "2026-03-18T12:30:00"

  human_fixes: []
  review_report: null
```

### 5.3 예시 3: blocked 후 재수정 중인 모듈

```yaml
- name: HarvestSystem
  status: in_progress
  priority: medium
  owner: builder_1
  role: builder
  depends_on: [CropGrowth]
  module_path: Assets/Game/Modules/HarvestSystem
  feature_group: farming-loop
  description: 수확 시스템 — 작물 수확, 보상 산출, 인벤토리 연동

  # v2 상태 차원
  human_state: pending
  learning_state: none
  commit_state: none

  # 메타데이터
  retry_count: 1
  blocked_reason: "ModuleBoundaryValidator: HarvestSystem이 CropGrowth의 Runtime을 직접 참조"
  commit_hash: null
  created_at: "2026-03-18T10:00:00"
  updated_at: "2026-03-18T15:00:00"

  human_fixes:
    - file: "HarvestSystemRuntime.cs"
      change: "CropGrowthRuntime 직접 참조를 ICropGrowth 인터페이스로 변경"
      rationale: "모듈 간 참조는 인터페이스만 허용"
      timestamp: "2026-03-18T14:50:00"

  review_report: "docs/ai/reviews/HarvestSystem_REVIEW.md"
```

### 5.4 예시 4: 기존 v1 모듈 (하위 호환)

기존 엔트리는 새 필드 없이도 유효하다. 암묵적으로 다음과 같이 해석한다:

```yaml
- name: Economy
  status: done
  priority: high
  owner: null
  role: null
  depends_on: []
  module_path: Assets/Game/Modules/Economy
  description: 코인 경제 시스템 — 재화 정의, 잔고 관리, 획득/소비 이벤트
  # human_state 미기재 → done이면 validated로 간주
  # learning_state 미기재 → none (v2 이전)
  # commit_state 미기재 → done이면 committed로 간주
```

---

## 6. 에이전트별 필드 수정 권한

| 필드 | Planner | Builder | Reviewer | Committer | Learning Recorder | Human |
|------|---------|---------|----------|-----------|-------------------|-------|
| `status` | W | W | W | — | — | W (escalated 복원) |
| `owner` | W | W | W | W | — | — |
| `role` | W | W | W | W | W | — |
| `human_state` | — | W (none→pending) | — | — | — | W |
| `learning_state` | — | — | — | — | W | — |
| `commit_state` | — | — | W (none→ready) | W | — | — |
| `retry_count` | — | W (+1) | — | — | — | W (리셋) |
| `blocked_reason` | — | — | W | — | — | — |
| `human_fixes` | — | — | — | — | — | W |
| `ai_post_validation_fixes` | — | W | — | — | — | — |
| `post_validation_fix_count` | — | W | — | — | — | — |
| `post_validation_fix_actor` | — | W | — | — | — | W |
| `learning_note_required` | — | W | — | — | W | — |
| `commit_hash` | — | — | — | W | — | — |
| `review_report` | — | — | W | — | — | — |
| `feature_group` | W | — | — | — | — | — |
| `priority` | W | — | — | — | — | W |
| `depends_on` | W | — | — | — | — | — |

W = 쓰기 가능, — = 읽기만

---

## 7. 검증 규칙 (Schema Validation)

TASK_QUEUE.yaml을 읽을 때 다음 규칙을 검증한다:

| # | 규칙 | 검증 시점 |
|---|------|-----------|
| 1 | `name`은 모듈 목록에서 유일해야 한다 | 큐 로드 시 |
| 2 | `module_path`는 `Assets/Game/Modules/`로 시작해야 한다 | 큐 로드 시 |
| 3 | `depends_on`의 모듈이 MODULE_REGISTRY.yaml에 존재해야 한다 | 그래프 빌드 시 |
| 4 | `status`는 정의된 enum 값만 허용 | 전이 시 |
| 5 | `human_state == validated` 없이 `status`를 `review`로 전이 불가 | 전이 시 |
| 6 | `commit_state ∈ {committed, recommitted}` + `learning_state == recorded` 없이 `done` 불가 | 전이 시 |
| 7 | `retry_count >= 3`이면 `escalated`만 가능 | 전이 시 |
| 8 | `owner != null`인 태스크는 다른 에이전트가 가져갈 수 없다 | 할당 시 |
| 9 | 신규 태스크는 `feature_group`이 필수다 | 생성 시 |
| 10 | `integration_strategy == replace`이면 `impact_analysis == completed` + `migration_required == true` 필수 | planned → in_progress |
| 11 | `integration_strategy == extend`이면 `compatibility_review == passed` 필수 (review 전이 전) | in_progress → review |
| 12 | `integration_strategy == reuse`이면 신규 모듈 코드 생성 차단 | Queue Generator 단계 |
| 13 | `learning_note_required == true`이면 `learning_state`가 `recorded` 또는 `recorded_existing_rule_reference`여야 done 가능 | review → done |
| 14 | `ai_post_validation_fixes`에 기록 시 `post_validation_fix_count`와 `post_validation_fix_actor` 동기화 필수 | Builder 수정 시 |

---

## 8. TASK_QUEUE.yaml 파일 구조

```yaml
# docs/ai/TASK_QUEUE.yaml
version: 2

modules:
  - name: <string>
    status: <enum>
    priority: <enum>
    owner: <string|null>
    role: <enum|null>
    depends_on: [<string>, ...]
    module_path: <string>
    feature_group: <string>
    description: <string>

    human_state: <enum>          # v2
    learning_state: <enum>       # v2
    commit_state: <enum>         # v2

    # v2.1 — Integration Strategy
    integration_strategy: <enum> # v2.1, default: create_new
    existing_module_candidates: [<string>] # v2.1, default: []
    compatibility_review: <enum> # v2.1, default: not_required
    impact_analysis: <enum>      # v2.1, default: not_required
    migration_required: <bool>   # v2.1, default: false

    # v2.2 — Fix Classification
    ai_post_validation_fixes: [...] # v2.2, default: []
    post_validation_fix_count: <int> # v2.2, default: 0
    post_validation_fix_actor: <enum> # v2.2, default: none
    learning_note_required: <bool> # v2.2, default: false

    retry_count: <int>           # v2, optional
    blocked_reason: <string|null> # v2, optional
    human_fixes: [...]           # v2, optional (사람만)
    review_report: <string|null> # v2, optional
    commit_hash: <string|null>   # v2, optional
    created_at: <string|null>    # v2, optional
    updated_at: <string|null>    # v2, optional
```
