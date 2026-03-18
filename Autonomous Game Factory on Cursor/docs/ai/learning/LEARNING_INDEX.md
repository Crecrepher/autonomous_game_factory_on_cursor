# Learning Index — Autonomous Game Factory v2

이 문서는 Learning Memory 시스템의 진입점이다.
모든 AI 에이전트는 작업 시작 전 이 문서를 먼저 읽는다.

---

## Learning Memory 구조

```
docs/ai/learning/
├── LEARNING_INDEX.md              ← 이 파일 (진입점)
├── RULE_MEMORY.yaml               ← 핵심 규칙 저장소 (기계 판독용 YAML)
├── LEARNING_LOG.md                ← 시간순 학습 이벤트 로그
├── VALIDATOR_FAILURE_PATTERNS.md  ← 12개 Validator별 실패 패턴 사전
├── CODING_PATTERNS.md             ← 검증된 코딩 패턴 (BAD/GOOD 예시)
├── HUMAN_FIX_EXAMPLES.md          ← Before/After 사람 수정 사례
├── CROSS_PROJECT_RULES.md         ← 프로젝트 간 재사용 가능 규칙
└── RECURRING_MISTAKES.md          ← 3회 이상 반복된 AI 실수 패턴
```

---

## 역할별 필수 읽기

| 역할 | 작업 시작 전 필수 읽기 | 참조용 |
|------|----------------------|--------|
| **Planner** | RULE_MEMORY.yaml, RECURRING_MISTAKES.md | LEARNING_LOG.md |
| **Builder** | RULE_MEMORY.yaml, CODING_PATTERNS.md, HUMAN_FIX_EXAMPLES.md | VALIDATOR_FAILURE_PATTERNS.md |
| **Reviewer** | RULE_MEMORY.yaml, VALIDATOR_FAILURE_PATTERNS.md | HUMAN_FIX_EXAMPLES.md |
| **Committer** | (읽기 전용) LEARNING_LOG.md | - |
| **Learning Recorder** | 전체 (쓰기 권한) | - |

---

## 에이전트 Quick-Start 가이드

### Planner가 PLAN 작성 시

1. `RULE_MEMORY.yaml`에서 대상 모듈 유형과 관련된 규칙 필터링 (`module_pattern`, `tags`)
2. `RECURRING_MISTAKES.md`에서 과거 반복 실수 확인
3. PLAN에 "이 모듈 생성 시 주의할 규칙" 섹션 추가

### Builder가 코드 생성 시

1. `CODING_PATTERNS.md`에서 해당 파일 유형의 표준 패턴 확인 (Runtime, Config, Factory 등)
2. `RULE_MEMORY.yaml`에서 `module_pattern`이 매칭되는 규칙 확인
3. `HUMAN_FIX_EXAMPLES.md`에서 유사 모듈의 과거 수정 사례 참조
4. 코드 생성 후 `VALIDATOR_FAILURE_PATTERNS.md`에서 자가 체크

### Reviewer가 검증 시

1. `VALIDATOR_FAILURE_PATTERNS.md`에서 해당 validator의 알려진 패턴 대조
2. `RULE_MEMORY.yaml`의 규칙 위반 여부 확인
3. 검증 실패 시 REVIEW RESULT에 관련 패턴 ID 포함

### Learning Recorder가 기록 시

1. Validator 실패 → `LEARNING_LOG.md`에 엔트리 추가
2. 사람 수정 → `HUMAN_FIX_EXAMPLES.md`에 Before/After 추가
3. 새 패턴 발견 → `RULE_MEMORY.yaml`에 규칙 추가
4. 3회 이상 반복 → `RECURRING_MISTAKES.md`에 패턴 추가
5. 프로젝트 무관 교훈 → `CROSS_PROJECT_RULES.md`에 추가
6. Validator 실패 패턴 → `VALIDATOR_FAILURE_PATTERNS.md`에 행 추가

---

## ID 체계

| 파일 | ID 형식 | 예시 |
|------|---------|------|
| RULE_MEMORY.yaml | RM-XXXX | RM-0001 |
| LEARNING_LOG.md | LL-XXXX | LL-0001 |
| VALIDATOR_FAILURE_PATTERNS.md | VF-XX-XXX | VF-CS-001 |
| CODING_PATTERNS.md | CP-XXX | CP-001 |
| HUMAN_FIX_EXAMPLES.md | HF-XXXX | HF-0001 |
| RECURRING_MISTAKES.md | REC-XXX | REC-001 |
| CROSS_PROJECT_RULES.md | CPR-XXX | CPR-001 |

---

## 상호 참조 규칙

- LEARNING_LOG 엔트리는 관련 RULE_MEMORY 규칙을 `related_rules`로 참조한다.
- HUMAN_FIX_EXAMPLES는 관련 RULE_MEMORY와 VALIDATOR_FAILURE_PATTERNS를 참조한다.
- RECURRING_MISTAKES는 근거가 되는 LEARNING_LOG 엔트리들을 참조한다.
- CROSS_PROJECT_RULES는 RULE_MEMORY에서 `scope: global`인 규칙과 대응한다.

---

## 축적 원칙

1. **삭제 금지**: 모든 학습 데이터는 append-only다.
2. **중복 병합**: 동일 규칙은 새 항목을 만들지 않고 기존 항목에 사례를 추가한다.
3. **근거 필수**: rationale 없는 기록은 존재해서는 안 된다.
4. **scope 구분**: project 전용인지 global 재사용 가능한지 반드시 명시한다.
5. **교차 참조**: 관련 문서와 ID로 반드시 연결한다.

---

## 프로젝트 간 이전

새 프로젝트 시작 시:

1. `docs/ai/learning/` 폴더 전체를 복사한다.
2. `scope: project`인 항목은 제거하거나 비활성화한다.
3. `scope: global` 항목은 그대로 유지 — 이것이 축적된 지식이다.
4. LEARNING_LOG.md는 새 프로젝트용으로 비운다 (과거 로그는 아카이브).
5. RULE_MEMORY.yaml의 global 규칙은 첫 생성부터 적용된다.
