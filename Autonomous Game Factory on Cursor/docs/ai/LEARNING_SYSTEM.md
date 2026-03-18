# Learning System — Autonomous Game Factory v2.2

Learning Recorder 강화 명세. 작은 교훈도 놓치지 않고 기록하는 시스템.

---

## 1. 목적

| 문제 | 해결 |
|------|------|
| AI auto-fix가 기록되지 않아 교훈 누락 | ai_post_validation_fixes를 학습 소스로 추가 |
| 기존 규칙 강화 이벤트가 무시됨 | existing_rule_reinforced 분류 도입 |
| learning_state 전이가 이진적 | recorded_existing_rule_reference 상태 추가 |

---

## 2. Learning Event Classification

모든 fix (human + ai)에 대해 이벤트를 분류한다.

### 2.1 이벤트 타입

| 타입 | 설명 | 트리거 |
|------|------|--------|
| `new_rule` | RULE_MEMORY에 없는 새 규칙 발견 | related_rule == null이고 패턴이 새로움 |
| `existing_rule_reinforced` | 기존 규칙이 다시 위반됨 → 강화 필요 | related_rule != null |
| `failure_pattern` | 동일 Validator가 여러 모듈에서 반복 실패 | 동일 validator 3회+ |
| `architecture_adjustment` | Config/구조 수준의 설계 변경 | Config 충돌 해결, 모듈 구조 변경 |

### 2.2 이벤트 스키마

```yaml
learning_event:
  type: "<new_rule | existing_rule_reinforced | failure_pattern | architecture_adjustment>"
  rule: "<규칙 ID | null>"
  context: "<이벤트 설명>"
  module: "<모듈명>"
  fix_actor: "<ai | human>"
  validator: "<Validator 이름 | null>"
  timestamp: "<ISO 8601>"
```

---

## 3. Learning Recorder 강화 절차

### 3.1 데이터 수집 (확장)

```
기존 소스:
  - human_fixes[]
  - AIValidationReport.json
  - reviews/<Module>_REVIEW.md

추가 소스 (v2.2):
  - ai_post_validation_fixes[]
  - post_validation_fix_actor
  - learning_note_required
```

### 3.2 실행 절차

```
1. TASK_QUEUE에서 대상 태스크의 모든 fix 데이터 수집
   - human_fixes[]
   - ai_post_validation_fixes[]

2. learning_note_required 확인
   IF learning_note_required == false AND fix 총합 == 0:
     → learning_state: pending → recorded (빈 기록, 로그만)
     → 종료

   IF learning_note_required == true:
     → 반드시 이벤트 분류 + 기록 진행

3. 각 fix에 대해 이벤트 분류:
   a. related_rule이 존재하면:
      → type: existing_rule_reinforced
      → RULE_MEMORY.yaml에서 해당 규칙의 reinforcement_count += 1
      → LEARNING_LOG.md에 이벤트 기록

   b. related_rule이 없으면:
      → RULE_MEMORY.yaml에서 유사 규칙 검색
      → 유사 규칙 있으면: type: existing_rule_reinforced
      → 유사 규칙 없으면: type: new_rule
        → RULE_MEMORY.yaml에 새 규칙 추가
        → LEARNING_LOG.md에 이벤트 기록

   c. 동일 validator가 3회+ 실패한 이력:
      → type: failure_pattern 추가
      → RECURRING_MISTAKES.md에 패턴 등록

4. LEARNING_LOG.md에 시간순 엔트리 추가 (이벤트 목록 포함)

5. learning_state 전이:
   IF new_rule 또는 failure_pattern 이벤트 존재:
     → learning_state: pending → recorded
   ELSE IF existing_rule_reinforced만 존재:
     → learning_state: pending → recorded_existing_rule_reference
   ELSE:
     → learning_state: pending → recorded
```

---

## 4. LEARNING_LOG.md 엔트리 형식 (v2.2)

```markdown
## <Module> — <timestamp>

### Fix Summary
- human_fixes: <count>
- ai_post_validation_fixes: <count>
- fix_actor: <none | ai | human | both>

### Learning Events
- [existing_rule_reinforced] RM-0007: enum 파일명 E<ModuleName>* 패턴 (ai fix)
- [new_rule] RM-0015: Config 중복 정의 방지 (architecture_adjustment)

### Context
- Module: <ModuleName>
- Feature Group: <feature_group>
- Pipeline Result: done
```

---

## 5. RULE_MEMORY.yaml 확장 (v2.2)

기존 RULE_MEMORY 엔트리에 reinforcement 추적을 추가한다:

```yaml
- id: RM-0007
  rule: "모듈 루트 파일은 I<Module>, <Module>Config, <Module>Runtime, <Module>Factory, <Module>Bootstrap, E<Module>* 패턴만 허용"
  severity: error
  scope: project
  reinforcement_count: 2      # v2.2 — 이 규칙이 강화된 횟수
  last_reinforced: "2026-03-18T20:15:00"  # v2.2 — 마지막 강화 시각
  reinforced_by:               # v2.2 — 강화 이력
    - module: InventorySystem
      fix_actor: ai
      timestamp: "2026-03-18T20:15:00"
```

---

## 6. Learning Gate 강화

### 6.1 기존 규칙

```
human_fixes > 0 → learning_state == recorded 없이 done 불가
```

### 6.2 강화 규칙 (v2.2)

```
learning_note_required == true → learning_state ∈ {recorded, recorded_existing_rule_reference} 없이 done 불가
```

이 규칙은 `human_fixes`뿐 아니라 `ai_post_validation_fixes`도 포함한다.

---

## 7. 참조 문서

| 문서 | 관계 |
|------|------|
| `PIPELINE_HARDENING.md` | 개선 개요 |
| `TASK_SCHEMA.md` | v2.2 필드 정의 |
| `ORCHESTRATION_RULES.md` | Learning Recorder I/O |
| `learning/RULE_MEMORY.yaml` | 규칙 저장소 |
| `learning/LEARNING_LOG.md` | 시간순 학습 기록 |
