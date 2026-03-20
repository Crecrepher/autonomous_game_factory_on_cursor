# EXECUTION ENTRYPOINT

**이 파일이 유일한 실행 진입점이다. 항상 이 파일부터 읽는다. 예외 없음.**

---

## 1. 읽기 순서

```
① 이 파일 (EXECUTION_ENTRYPOINT.md)
② PROJECT_CONSTITUTION.md
③ CHECKLIST.md
④ 작업에 필요한 runtime 문서 최대 2개 (아래 표)
⑤ TASK_QUEUE.yaml / MODULE_REGISTRY.yaml (운영 데이터)
```

| 작업 유형 | ④에서 읽을 문서 |
|-----------|----------------|
| 모듈 생성 | `MODULE_TEMPLATES.md`, `CODING_RULES.md` |
| 파이프라인 실행 | `ORCHESTRATION_RULES.md` |
| 커밋 | `COMMIT_RULES.md` |
| 큐/분해 | `reference/QUEUE_GENERATOR.md` |
| 기존 코드 분석 | `reference/CODEBASE_INGESTION.md` |
| 모듈 재사용 | `reference/INTEGRATION_STRATEGY.md` |
| 학습 기록 | `learning/LEARNING_INDEX.md` |

**총 참조 문서 5개 초과 금지.**

---

## 2. 금지 규칙

- `docs/ai/` 전체를 임의로 스캔하거나 광범위하게 읽지 않는다
- `reference/` 문서는 위 표에 명시된 경우에만 읽는다
- `_archive/` 폴더는 절대 읽지 않는다
- 이 파일을 AI가 수정하지 않는다
- 이 파일은 모든 `.cursor/rules`보다 우선한다

---

## 3. 실행 루프

모든 사용자 입력은 TASK다. 아래 루프를 따른다.

```
STEP 1 — TASK 분석
  사용자 입력에서 인텐트를 판별한다 (§6 라우팅 테이블)
  불명확하면 추론하지 말고 사용자에게 확인한다

STEP 2 — 모듈 선택
  TASK_QUEUE.yaml + MODULE_REGISTRY.yaml 읽기
  관련 모듈 3~5개만 선택 (초과 금지)
  의존성 확인 → 병렬 가능 여부 판단 (§5)

STEP 3 — 사전 검증
  CHECKLIST.md의 해당 섹션으로 선행 조건 확인
  미충족 항목이 있으면 먼저 해결

STEP 4 — 구현
  선택한 모듈에 대해 작업 수행
  PROJECT_CONSTITUTION.md 규칙 준수

STEP 5 — 사후 검증
  CHECKLIST.md로 완료 조건 전수 확인
  하나라도 미충족이면 STEP 6으로

STEP 6 — 수정
  미충족 항목 수정 → STEP 5 반복 (최대 3회)
  3회 초과 시 사용자에게 보고하고 중단
```

---

## 4. 폴더 구조

```
docs/ai/
├── *.md, *.yaml      ← RUNTIME (이 루트만 읽는다)
├── reference/         ← 필요 시에만 (표에 명시된 경우만)
├── learning/          ← Learning Recorder 전용
├── plans/             ← 파이프라인 산출물
├── generated_specs/   ← 파이프라인 산출물
└── _archive/          ← 읽지 않음
```

---

## 5. 병렬 규칙

**현실: Cursor는 단일 에이전트다. 멀티 에이전트를 시뮬레이션하지 않는다.**
병렬은 "사용자가 여러 Cursor 창을 열어 동시에 작업할 때"만 해당한다.

### 병렬 가능 (모든 조건 충족 시)

- 태스크 간 `depends_on` 없음
- 수정 대상 파일이 겹치지 않음
- Shared/, Core/, 인터페이스(`I<Module>.cs`) 수정 없음
- YAML 운영 파일(TASK_QUEUE, MODULE_REGISTRY, FEATURE_QUEUE) 동시 쓰기 없음
- **최대 3개 동시**

### 병렬 금지 (하나라도 해당 시 → 직렬)

- 동일 모듈 수정
- Core, Editor/AI, .cursor/rules 수정
- Human Gate 대기 중인 모듈
- 의존 모듈이 아직 done이 아님

### 병렬 불가 시

직렬로 전환하고, 결과 보고에 사유를 명시한다.
`parallel: no — <사유>`

---

## 6. 인텐트 라우팅

| 키워드 | 실행 |
|--------|------|
| "만들어줘", "추가해줘", "구현해줘" | 9단계 파이프라인 |
| "기획서", "셋팅해줘", "부트스트랩" | Bootstrap |
| "폴더를 읽어", "코드 분석" | Ingestion |
| "커밋해줘", "commit" | 7 Gate 커밋 |
| "검증만", "체크해줘" | Validation Only |
| "재사용", "기존 거 활용" | Reuse-First |
| 불명확 | **추론 금지 → 사용자에게 확인** |

---

## 7. 결과 보고 형식

모든 작업 완료 시 아래 형식으로 보고한다.

```
RESULT:
  task: <수행한 작업 한 줄 요약>
  modules: [<선택한 모듈 목록>]
  parallel: <yes/no + 사유>
  status: <done | blocked | in_progress>
  files_modified: <수정된 파일 수>
  checklist: <PASS | FAIL (미충족 항목)>
  next: <다음 필요 행동 또는 "없음">
```
