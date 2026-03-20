# AGF Prompt OS — Auto-Context Rules

> 인텐트별 자동 컨텍스트 로딩 규칙

---

## 원칙

1. **인텐트가 결정되면** 해당 인텐트의 `auto_context` 파일 목록을 자동으로 읽는다.
2. **모든 인텐트에 공통으로** 읽어야 하는 파일이 있다 (core context).
3. 파일이 존재하지 않으면 경고만 출력하고 진행한다.
4. 컨텍스트 로딩은 파이프라인 실행 전에 완료되어야 한다.

---

## Core Context (항상 로딩)

모든 인텐트에 자동으로 읽히는 파일:

```
docs/ai/PROJECT_OVERVIEW.md
docs/ai/CODING_RULES.md
```

---

## 인텐트별 추가 컨텍스트

### bootstrap_from_design

```
docs/ai/FEATURE_QUEUE.yaml
docs/ai/TASK_QUEUE.yaml
docs/ai/MODULE_REGISTRY.yaml
docs/ai/CODING_RULES.md
docs/ai/MODULE_TEMPLATES.md
```

**추가 검사**: 기존 `Assets/Game/Modules/` 폴더 구조를 스캔하여 충돌 방지.

### ingest_codebase

```
docs/ai/MODULE_REGISTRY.yaml
docs/ai/TASK_QUEUE.yaml
docs/ai/CODING_RULES.md
```

**추가 검사**: 스캔 대상 폴더 (`Assets/Game/`, `Assets/Scripts/`, `Assets/Supercent/`) 존재 여부 확인.

### integrate_feature

```
docs/ai/PROJECT_OVERVIEW.md
docs/ai/CODING_RULES.md
docs/ai/MODULE_REGISTRY.yaml
docs/ai/TASK_QUEUE.yaml
docs/ai/FEATURE_QUEUE.yaml
docs/ai/MODULE_TEMPLATES.md
docs/ai/ORCHESTRATION_RULES.md
```

**추가 검사**: 기존 모듈과의 의존성/충돌 검사.

### reuse_first_integration

```
docs/ai/MODULE_REGISTRY.yaml
docs/ai/TASK_QUEUE.yaml
docs/ai/FEATURE_QUEUE.yaml
docs/ai/CODING_RULES.md
```

**추가 검사**: `Assets/Game/Modules/` 전체 스캔하여 재사용 후보 탐색.

### analyze_existing_modules

```
docs/ai/MODULE_REGISTRY.yaml
docs/ai/TASK_QUEUE.yaml
```

### generate_queue_only

```
docs/ai/MODULE_REGISTRY.yaml
docs/ai/TASK_QUEUE.yaml
docs/ai/FEATURE_QUEUE.yaml
```

### run_validation_only

```
docs/ai/TASK_QUEUE.yaml
docs/ai/MODULE_REGISTRY.yaml
```

### commit_changes

```
docs/ai/TASK_QUEUE.yaml
docs/ai/COMMIT_RULES.md
```

**추가 검사**: `human_state`, `commit_state` 확인. 7 Gate 사전 체크.

### review_learning

```
docs/ai/learning/LEARNING_LOG.md
docs/ai/learning/RECURRING_MISTAKES.md
docs/ai/learning/VALIDATOR_FAILURE_PATTERNS.md
```

---

## 첨부 파일 처리

| 인텐트 | 첨부 필수 | 첨부 유형 |
|---|---|---|
| `bootstrap_from_design` | Yes | 기획서/디자인 문서 |
| `integrate_feature` | No | 기능 설명 (선택) |
| 나머지 | No | 해당 없음 |

첨부 파일이 있으면 `requires_attachment: true` 인텐트에 보너스 점수가 부여된다.

---

## 레포지토리 구조 자동 검사

다음 인텐트는 레포지토리 구조를 자동으로 검사한다:

- `bootstrap_from_design`: 기존 폴더/씬/프리팹 존재 여부
- `ingest_codebase`: 스캔 대상 코드 루트
- `integrate_feature`: 기존 모듈 의존성
- `reuse_first_integration`: 재사용 후보 모듈

---

## 구현 위치

- **Cursor AI 룰**: `.cursor/rules/prompt-os.mdc` 에서 인텐트별 auto_context를 읽도록 지시
- **Unity Editor**: `IntentRouter.cs`의 `RouteResult.AutoContext`와 `PipelineDispatcher.ResolveAutoContext()`
- **프로파일 정의**: `docs/ai/COMMAND_PROFILES.yaml`의 각 프로파일의 `auto_context` 필드
