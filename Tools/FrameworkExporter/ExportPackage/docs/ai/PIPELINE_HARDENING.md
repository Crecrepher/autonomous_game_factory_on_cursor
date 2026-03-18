# Pipeline Hardening — Autonomous Game Factory v2.2

InventorySystem 파이프라인 실행에서 발견된 약점을 보강하는 명세.

---

## 1. 발견된 문제와 개선

| # | 문제 | 개선 | 영향 범위 |
|---|------|------|-----------|
| 1 | Human fix와 AI auto-fix가 구분 없이 기록됨 | Fix Classification System | TASK_SCHEMA, Builder, Learning Recorder |
| 2 | Learning Recorder가 작은 교훈을 건너뜀 | Learning Event Classification | Learning Recorder, LEARNING_LOG |
| 3 | 관련 모듈 간 Config 중복 위험 | Config Source-of-Truth Protection | Validator, CODING_RULES |
| 4 | 파이프라인당 여러 개의 meta commit 생성 | Committer Meta Commit Consolidation | Committer |

---

## 2. IMPROVEMENT 1 — Fix Classification System

### 2.1 문제

InventorySystem 파이프라인에서 AI가 `EInventoryResult.cs → EInventorySystemResult.cs` 이름 변경을 수행했으나, 이 수정이 `human_fixes`에도 기록될 수 없고 아무 데도 기록되지 않았다. 학습 시스템이 이 사건을 놓쳤다.

### 2.2 해결

두 가지 fix 배열을 분리한다:

| 배열 | 기록 주체 | 기록 시점 |
|------|-----------|-----------|
| `human_fixes[]` | 사람 | Human Validation 단계에서 사람이 코드를 직접 수정할 때 |
| `ai_post_validation_fixes[]` | AI (Builder) | Validator 실패 후 AI가 코드를 자동 수정할 때 |

### 2.3 동기화 필드

| 필드 | 자동 계산 |
|------|-----------|
| `post_validation_fix_count` | `ai_post_validation_fixes.length` |
| `post_validation_fix_actor` | human_fixes만 → `human`, ai만 → `ai`, 둘 다 → `both`, 없으면 → `none` |
| `learning_note_required` | fix가 1건 이상이면 `true` |

### 2.4 Builder 행동 규칙

```
Validator 실패 → AI가 코드 수정 시:
1. ai_post_validation_fixes[]에 엔트리 추가
2. post_validation_fix_count += 1
3. post_validation_fix_actor 업데이트
4. learning_note_required = true
5. related_rule 필드에 기존 RULE_MEMORY 규칙 ID 기록 (있으면)
```

### 2.5 예시

```yaml
ai_post_validation_fixes:
  - validator: ModuleBoundaryValidator
    reason: "Unrecognized file in module root: EInventoryResult.cs"
    change: "EInventoryResult.cs → EInventorySystemResult.cs (파일명 + 타입명 변경)"
    related_rule: RM-0007
    timestamp: "2026-03-18T20:15:00"
post_validation_fix_count: 1
post_validation_fix_actor: ai
learning_note_required: true
```

---

## 3. IMPROVEMENT 2 — Learning Recorder Enhancement

### 3.1 문제

InventorySystem에서 AI가 네이밍 규칙 위반을 자동 수정했지만 `human_fixes`가 비어 있어서 Learning Recorder가 "기록할 것 없음"으로 판단하고 건너뛰었다. 하지만 이건 RM-0007 규칙이 다시 한번 강화되어야 할 소중한 데이터였다.

### 3.2 해결

Learning Recorder가 수집하는 데이터 소스를 확장한다:

| 기존 소스 | 추가 소스 (v2.2) |
|-----------|-----------------|
| `human_fixes[]` | `ai_post_validation_fixes[]` |
| Validator 보고서 | `post_validation_fix_actor` |
| Reviewer 보고서 | `learning_note_required` |

### 3.3 Learning Event Classification

모든 fix (human + ai)에 대해 이벤트를 분류한다:

| 분류 | 조건 | 기록 위치 |
|------|------|-----------|
| `new_rule` | RULE_MEMORY에 관련 규칙 없음 | RULE_MEMORY.yaml + LEARNING_LOG.md |
| `existing_rule_reinforced` | related_rule이 기존 규칙 ID를 참조 | LEARNING_LOG.md (강화 카운트 증가) |
| `failure_pattern` | 동일 Validator가 3회 이상 실패 | RECURRING_MISTAKES.md |
| `architecture_adjustment` | Config/아키텍처 수준 변경 | LEARNING_LOG.md + RULE_MEMORY.yaml |

### 3.4 learning_state 확장

| 값 | 의미 |
|----|------|
| `recorded` | 새 규칙이나 실패 패턴이 기록됨 |
| `recorded_existing_rule_reference` | 기존 규칙 강화로 기록됨 (v2.2) |

### 3.5 Learning Recorder 강화 절차

```
1. human_fixes[] + ai_post_validation_fixes[] 수집
2. learning_note_required 확인
   → false이고 fix가 0건이면: learning_state → recorded (빈 기록)
   → true이면: 반드시 기록 진행
3. 각 fix에 대해:
   a. related_rule이 있으면 → existing_rule_reinforced 이벤트
   b. related_rule이 없으면 → new_rule 이벤트 후보
   c. 동일 Validator 실패 3회+ → failure_pattern
4. LEARNING_LOG.md에 이벤트 기록
5. learning_state 전이:
   → 새 규칙 있으면: recorded
   → 기존 규칙 강화만: recorded_existing_rule_reference
```

### 3.6 예시: InventorySystem에서 누락됐던 학습

```yaml
learning_event:
  type: existing_rule_reinforced
  rule: RM-0007
  context: "ModuleBoundaryValidator — enum 파일명은 E<ModuleName>* 패턴을 따라야 함"
  module: InventorySystem
  fix_actor: ai
  timestamp: "2026-03-18T20:15:00"
```

---

## 4. IMPROVEMENT 3 — Config Source-of-Truth Protection

### 4.1 문제

InventorySystem이 `InventorySystemConfig.maxStackSizePerSlot`을 정의했는데, ItemStacking이 이미 `ItemStackingConfig.maxStackSize`를 정의하고 있었다. 두 Config가 같은 행동(스택 크기)의 설정값을 이중 정의하면서 불일치 위험이 생겼다.

### 4.2 해결

Config Source-of-Truth 규칙을 도입한다:

**하나의 공유 행동에 대한 설정값은 정확히 하나의 Config에서만 정의한다.**

### 4.3 감지 로직

```
1. MODULE_REGISTRY.yaml에서 모든 모듈의 dependencies 스캔
2. 의존 관계에 있는 모듈 쌍의 Config 필드 비교
3. 이름/타입이 유사한 필드가 양쪽 Config에 존재하면 경고
```

### 4.4 유사 필드 판정 기준

| 조건 | 예시 |
|------|------|
| 동일 필드명 | `maxStackSize` vs `maxStackSize` |
| 접미어만 다른 필드명 | `maxStackSize` vs `maxStackSizePerSlot` |
| 동일 의미의 약어 | `maxStack` vs `stackLimit` |

### 4.5 Validator 출력

```
[Architecture] Config conflict: duplicate source-of-truth for 'stack size'
  InventorySystemConfig._maxStackSizePerSlot
  ItemStackingConfig._maxStackSize
  Preferred source: ItemStackingConfig (원본 모듈)
  Action: InventorySystem should reference ItemStackingConfig instead
```

### 4.6 해결 방법 우선순위

1. **의존 모듈의 Config를 참조** (InventorySystem이 ItemStackingConfig를 주입받음)
2. **상위 Config에서 통합 관리** (공통 Config ScriptableObject)
3. **어댑터 패턴** (Config 값을 변환하여 전달)

상세: `docs/ai/CONFIG_RULES.md`

---

## 5. IMPROVEMENT 4 — Committer Meta Commit Consolidation

### 5.1 문제

InventorySystem 파이프라인에서 다음과 같이 3개 커밋이 생겼다:
```
c5d95fdd9 — feat(inventory-system): add InventorySystem module
d96612096 — chore(ai-meta): update pipeline state for inventory-system
3adc03a3c — chore(ai-meta): finalize inventory-state pipeline state
```

2번째와 3번째 커밋은 동일한 YAML 파일들의 중간/최종 상태인데, 히스토리에 불필요한 노이즈를 생성했다.

### 5.2 해결

**파이프라인 전체에서 메타 커밋은 정확히 1개만 생성한다.**

### 5.3 규칙

| 규칙 | 설명 |
|------|------|
| 메타 변경 축적 | 파이프라인 진행 중 YAML 변경은 메모리에 축적만 한다 |
| 단일 메타 커밋 | 파이프라인 완전 종료 후 1개의 `chore(ai-meta)` 커밋으로 통합 |
| 중간 커밋 금지 | `feat` 커밋과 `chore(ai-meta)` 사이에 추가 meta 커밋 금지 |
| 예외 | 파이프라인이 blocked/escalated로 중단되면 현재 상태를 1개 커밋으로 저장 |

### 5.4 커밋 순서 (최종)

```
1. feat(<group>): add <modules>           ← 기능 코드
2. chore(ai-meta): finalize <group>       ← YAML 메타 (1개만)
3. chore(ai-learning): record lessons     ← 학습 데이터 (있으면)
```

### 5.5 커밋 메시지 형식 (통합)

```
chore(ai-meta): finalize pipeline state for <feature-group>

- feature_group: <group>
- pipeline_result: done | blocked
- updated_files:
  - docs/ai/TASK_QUEUE.yaml
  - docs/ai/MODULE_REGISTRY.yaml
  - docs/ai/FEATURE_QUEUE.yaml
- modules_finalized: [<list>]
- generated by: Autonomous Game Factory v2.2
```

---

## 6. 영향받는 파이프라인 단계

| 단계 | 변경 |
|------|------|
| [5] Builder | `ai_post_validation_fixes` 기록, `learning_note_required` 자동 설정 |
| [6] Human Validator | `human_fixes`만 기록 (변경 없음, 분리만 명확화) |
| [7] Reviewer | `post_validation_fix_actor` 확인, `learning_note_required` 검증 |
| [8] Committer | 메타 커밋 1개로 통합, Learning Gate 강화 |
| [9] Learning Recorder | `ai_post_validation_fixes` 수집, 이벤트 분류, 기존 규칙 강화 기록 |
| Validator | ConfigConflictValidator 추가 (13 → 14개) |

---

## 7. 참조 문서

| 문서 | 관계 |
|------|------|
| `TASK_SCHEMA.md` | v2.2 필드 정의 |
| `LEARNING_SYSTEM.md` | Learning Recorder 강화 명세 |
| `CONFIG_RULES.md` | Config Source-of-Truth 규칙 |
| `ORCHESTRATION_RULES.md` | 역할별 I/O 업데이트 |
| `COMMIT_RULES.md` | 메타 커밋 통합 규칙 |
