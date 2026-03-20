# GDD Execution Entrypoint — AGF v2.3

이 문서는 Game Design Document(GDD/기획서)가 입력되었을 때 AI가 반드시 따라야 하는 유일한 실행 경로를 정의한다.

---

## 1. 유일한 진입점

GDD가 첨부·붙여넣기·참조되면, AI는 반드시 아래 7단계 파이프라인을 **순서대로** 실행해야 한다.
분석만 하고 멈추거나, 일부 단계를 건너뛰는 것은 **유효하지 않은 실행**이다.

---

## 2. 필수 파이프라인 실행 순서

| 단계 | 이름 | 필수 산출물 | 완료 조건 |
|------|------|-----------|----------|
| 1 | **Feature Intake** | FEATURE_QUEUE.yaml 엔트리 추가/업데이트 | feature_group 결정, status: intake → queued |
| 2 | **TASK_QUEUE.yaml Update** | TASK_QUEUE.yaml 엔트리 추가 | 모든 모듈이 pending 엔트리로 등록 |
| 3 | **Architecture Diff** | 기존 모듈과 충돌 분석 완료 | arch_diff_risk 설정, critical이면 차단 |
| 4 | **Planning** | 기존 모듈 경계 존중한 계획 | 각 모듈의 책임·의존성·전략 결정 |
| 5 | **Builder Execution** | 모듈 코드 생성/수정 (6파일 구조) | 파일이 디스크에 실제로 존재 |
| 6 | **MODULE_REGISTRY.yaml Update** | 신규 모듈 등록 | name, path, dependencies, dimension, genre_tags 포함 |
| 7 | **Validation-Ready Preparation** | CODING_RULES.md 준수 확인 | status: in_progress, human_state: pending |

---

## 3. 단계별 세부 규칙

### 3.1 Feature Intake (단계 1)

- GDD를 분석하여 feature name, feature_group을 결정한다.
- `docs/ai/FEATURE_QUEUE.yaml`에 엔트리를 추가한다.
- 기존 엔트리와 중복 여부를 확인한다.
- GDD가 없으면 사용자에게 요청한다 — 추측으로 진행하지 않는다.

### 3.2 TASK_QUEUE.yaml Update (단계 2)

- Feature를 모듈 단위로 분해한다.
- 각 모듈에 대해 `docs/ai/TASK_QUEUE.yaml`에 엔트리를 추가한다.
- 초기 상태: `status: pending`, `human_state: none`, `commit_state: none`.
- `integration_strategy`를 결정한다: `reuse > extend > adapt > create_new`.
- `depends_on`은 MODULE_REGISTRY.yaml의 `dependencies`와 일치해야 한다.

### 3.3 Architecture Diff (단계 3)

- 신규/변경 모듈이 기존 아키텍처를 위반하는지 분석한다.
- 검사 범위: 모듈 책임 겹침, 인터페이스 호환성, 의존성 순환, GC 위험 패턴.
- `arch_diff_risk`: low / medium / high / critical.
- critical이면 `arch_diff_blocked: true` → 사용자 승인 없이 다음 단계 진입 불가.

### 3.4 Planning (단계 4)

- 기존 모듈 경계를 존중한다 — 기존 모듈의 핵심 책임을 변경하지 않는다.
- `editable: false` 모듈은 수정하지 않는다.
- 각 모듈에 대해 `docs/ai/plans/<Module>_PLAN.md`를 작성한다 (선택).
- Architecture Diff에서 medium 이상 항목을 PLAN에 경고로 포함한다.

### 3.5 Builder Execution (단계 5)

- 모든 신규 모듈은 6파일 구조를 따른다:

```
Assets/Game/Modules/<Module>/
├── I<Module>.cs
├── <Module>Config.cs
├── <Module>Runtime.cs
├── <Module>Factory.cs
├── <Module>Bootstrap.cs
└── Tests/Editor/<Module>Tests.cs
```

- CODING_RULES.md의 모든 규칙을 준수한다.
- GC 유발 코드 금지: 코루틴, 람다, LINQ, foreach, Invoke.
- 매직넘버 금지 → const UPPER_SNAKE_CASE.
- GetComponent 런타임 사용 금지.
- `?` (null conditional) 사용 금지.
- Runtime은 MonoBehaviour 상속 금지.
- **파일이 실제로 디스크에 생성/수정되어야 한다.**

### 3.6 MODULE_REGISTRY.yaml Update (단계 6)

- 신규 모듈을 `docs/ai/MODULE_REGISTRY.yaml`에 등록한다.
- 필수 필드: name, path, editable, risk, description, dependencies, dimension, genre_tags.
- TASK_QUEUE.yaml의 `depends_on`과 MODULE_REGISTRY의 `dependencies`가 일치해야 한다.
- 중복 엔트리 금지.

### 3.7 Validation-Ready Preparation (단계 7)

- TASK_QUEUE 상태 전이: `status: in_progress`, `human_state: pending`.
- Builder 자체 검증 10가지 체크리스트 통과:
  1. 필수 파일 6개 존재
  2. 네임스페이스 `Game`
  3. Runtime이 MonoBehaviour 미상속
  4. Config가 ScriptableObject 상속
  5. Factory가 static class
  6. 매직넘버 없음
  7. GC 유발 코드 없음
  8. 테스트 최소 1개
  9. 의존성이 MODULE_REGISTRY에 선언
  10. 순환 의존 없음
- 린터 에러 0건.

---

## 4. 금지 사항

| 금지 행위 | 설명 |
|----------|------|
| **단계 건너뛰기** | 7단계를 모두 실행해야 한다. 어떤 단계도 건너뛸 수 없다 |
| **분석만 출력** | 분석/보고서만 출력하고 파일을 수정하지 않으면 실행이 완료되지 않은 것이다 |
| **파일 미수정** | 파일이 수정되지 않으면 작업이 완료된 것이 아니다 |
| **신규 아키텍처 발명** | AGF v2.3에 존재하지 않는 시스템/버전/추상화를 도입하지 않는다 |
| **Human Validator 우회** | human_state == validated 없이 review/commit 진입 불가 |
| **기존 시스템 재설계** | 기존 모듈/파이프라인을 재설계하지 않는다 |
| **YAML 불일치** | TASK_QUEUE와 MODULE_REGISTRY 간 의존성 불일치 금지 |
| **중복 엔트리** | TASK_QUEUE에 동일 모듈이 2번 이상 등록되면 안 된다 |

---

## 5. 유효한 실행의 정의

실행이 유효하려면 반드시:

1. FEATURE_QUEUE.yaml이 업데이트되었다.
2. TASK_QUEUE.yaml이 업데이트되었다.
3. 모듈 코드 파일이 생성 또는 수정되었다.
4. MODULE_REGISTRY.yaml이 업데이트되었다 (신규 모듈이 있을 경우).
5. 모든 코드가 CODING_RULES.md를 준수한다.
6. human_state: pending 상태에 도달했다.

위 조건 중 하나라도 충족되지 않으면 실행은 **미완료**이다.

---

## 6. 참조 문서

- `docs/ai/CODING_RULES.md`
- `docs/ai/MODULE_TEMPLATES.md`
- `docs/ai/MODULE_REGISTRY.yaml`
- `docs/ai/TASK_QUEUE.yaml`
- `docs/ai/FEATURE_QUEUE.yaml`
- `docs/ai/ORCHESTRATION_RULES.md`
- `docs/ai/STATE_MACHINE.md`
- `docs/ai/COMMIT_RULES.md`
- `docs/ai/COMPLETION_CRITERIA.md`
- `docs/ai/PARALLEL_EXECUTION_PROOF.md`

---

## ENFORCEMENT

**All future AI tasks must follow this document.**
**Any response violating this document is invalid.**

GDD가 입력되었을 때 이 문서의 7단계 파이프라인을 실행하지 않는 응답은 유효하지 않다.
파일이 수정되지 않은 응답은 완료된 것이 아니다.
이 규칙은 모든 AI 에이전트(Orchestrator, Builder, Reviewer, Planner 등)에 동일하게 적용된다.
