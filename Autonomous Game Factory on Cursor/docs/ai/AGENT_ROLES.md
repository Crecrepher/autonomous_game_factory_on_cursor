# AI 에이전트 역할 정의

병렬 Cursor 에이전트 환경에서 **세 가지 역할**이 모듈 생성 파이프라인을 분담한다.
각 역할은 독립된 Cursor 세션(또는 동일 세션의 단계)에서 수행될 수 있다.

---

## 1. Planner (기획 에이전트)

### 책임

- 설계 문서(`PROJECT_OVERVIEW.md`, `CODING_RULES.md`, `MODULE_REGISTRY.yaml`)를 읽고 기능 요구사항을 분석한다.
- 대상 모듈을 결정하고, 신규 모듈이면 `MODULE_REGISTRY.yaml`에 추가할 항목을 명세한다.
- 구현 PLAN을 작성한다: 대상 파일 목록, 의존 모듈, Core 접근 여부, 위험 요소.
- `TASK_QUEUE.yaml`에서 태스크 상태를 `pending → planned`로 변경한다.

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| `Docs/ai/` 문서 읽기 | O |
| `TASK_QUEUE.yaml` 상태 변경 (pending → planned) | O |
| `MODULE_REGISTRY.yaml`에 새 모듈 항목 추가 | O |
| PLAN 문서 작성 | O |
| 기존 모듈 코드 읽기 (참조용) | O |

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
  notes: <한 줄 설명>
```

---

## 2. Builder (구현 에이전트)

### 책임

- Planner가 작성한 PLAN에 따라 모듈 코드를 생성한다.
- `CODING_RULES.md`와 `MODULE_REGISTRY.yaml`의 모든 규칙을 준수한다.
- 표준 모듈 템플릿(`Assets/Game/Modules/Template/`)을 기반으로 코드를 작성한다.
- 최소 1개의 테스트를 포함한다.
- `TASK_QUEUE.yaml`에서 태스크 상태를 `planned → in_progress → review`로 변경한다.

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| `Assets/Game/Modules/<자기 태스크 모듈>/` 내부 파일 생성/수정 | O |
| `Assets/Game/Shared/` 공용 인터페이스 추가 (필요 시) | O |
| `TASK_QUEUE.yaml` 상태 변경 (planned → in_progress → review) | O |
| `MODULE_REGISTRY.yaml`에 Planner가 명세한 항목 반영 | O |
| `Docs/ai/` 문서 읽기 | O |

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

### 코드 작성 체크리스트

Builder는 코드 작성 완료 후 review로 넘기기 전에 다음을 자체 점검한다:

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

---

## 3. Reviewer (검증 에이전트)

### 책임

- Builder가 완료한 코드에 대해 검증을 수행한다.
- Unity 검증 도구 (`Tools/AI/Validate Generated Modules`) 실행 결과를 분석한다.
- 아키텍처 규칙, 코딩 규칙, 모듈 구조 준수 여부를 확인한다.
- 검증 통과 시 `TASK_QUEUE.yaml` 상태를 `review → done`으로 변경한다.
- 검증 실패 시 원인을 정리하고 `review → blocked`로 변경, Builder에게 수정 사항을 전달한다.

### 허용

| 항목 | 가능 여부 |
|------|-----------|
| 모든 소스 코드 읽기 (검증 목적) | O |
| 검증 도구 실행 요청 | O |
| 검증 보고서(`AIValidationReport.json`) 읽기 | O |
| `TASK_QUEUE.yaml` 상태 변경 (review → done 또는 blocked) | O |
| `Docs/ai/` 문서 읽기 | O |

### 금지

| 항목 | 이유 |
|------|------|
| 소스 코드 수정 | Builder 역할 (코드 수정 권한 없음) |
| PLAN 작성 | Planner 역할 |
| `Assets/Editor/AI/` 수정 | 공유 시스템 |
| 검증을 건너뛰고 done 처리 | 안전 장치 |
| TASK_QUEUE에서 pending/planned 태스크 상태 변경 | 권한 밖 |

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

### 산출물

```
REVIEW RESULT:
  module: <ModuleName>
  status: done | blocked
  validation_passed: true | false
  errors: <에러 수>
  warnings: <경고 수>
  risk: low | medium | high
  modified_files:
    - <파일 경로 1>
    - <파일 경로 2>
  issues: (blocked인 경우)
    - <위반 규칙>: <파일>: <설명>
  action_required: (blocked인 경우)
    - <Builder가 수정해야 할 사항>
```

---

## 4. 역할 간 흐름 요약

```
  Planner                 Builder                  Reviewer
  ───────                 ───────                  ────────
  문서 읽기
  PLAN 작성
  pending → planned
                          PLAN 수신
                          코드 생성
                          planned → in_progress
                          자체 점검
                          in_progress → review
                                                   코드 읽기
                                                   검증 실행
                                                   결과 분석
                                                   ┌─ 통과 → done
                                                   └─ 실패 → blocked
                                                              │
                          ◄── 수정 사항 전달 ──────────────────┘
                          코드 수정
                          blocked → review (재검증)
```

---

## 5. 병렬 운영 규칙

- **서로 다른 모듈**을 작업하는 에이전트들은 동시에 작업할 수 있다.
- **같은 모듈**에 두 명 이상의 Builder가 동시에 할당될 수 없다.
- 모든 에이전트는 작업 시작 전 `TASK_QUEUE.yaml`를 읽어 충돌 여부를 확인한다.
- owner 필드가 null이 아닌 태스크는 다른 에이전트가 가져갈 수 없다.
- 의존 모듈이 done이 아닌 태스크는 planned 이후로 진행할 수 없다.
