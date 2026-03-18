# AGF Prompt OS — 프롬프트 운영체제

> Version: 1.1  
> 대상: Autonomous Game Factory v2.3  
> 목적: 짧은 자연어 명령 → 올바른 파이프라인 자동 실행  
> 핵심 변경: bootstrap_from_design 인텐트 강화 — 원클릭 프로젝트 셋업 지원

---

## 개요

Prompt OS는 사용자의 짧은 자연어 명령을 해석하여 AGF 파이프라인의 올바른 흐름으로 자동 라우팅하는 **명령 해석 및 실행 정책 레이어**다.

일반 챗봇이 아니라, **파이프라인 디스패처**다.

---

## 아키텍처

```
사용자 프롬프트
  ↓
[1] Intent Router          ← COMMAND_PROFILES.yaml
  ↓
[2] Auto-Context Resolver  ← AUTO_CONTEXT_RULES.md
  ↓
[3] Execution Policy       ← DEFAULT_EXECUTION_POLICY.yaml
  ↓
[4] Pipeline Dispatcher    ← 기존 파이프라인 시스템으로 디스패치
```

---

## 구성요소

### 1. Intent Router (`IntentRouter.cs`)

사용자 프롬프트를 분석하여 인텐트를 분류한다.

| 인텐트 | 설명 | 대표 트리거 |
|---|---|---|
| `bootstrap_from_design` | 기획서 기반 프로젝트 셋업 | "기획서 첨부했어, 셋팅해줘" |
| `ingest_codebase` | 기존 코드 분석/흡수 | "폴더를 읽어", "코드 흡수해" |
| `integrate_feature` | 새 기능 추가 (풀 파이프라인) | "이 기능 추가해줘", "만들어줘" |
| `reuse_first_integration` | 기존 모듈 우선 활용 | "기존 모듈 활용해서 붙여" |
| `analyze_existing_modules` | 현재 모듈 분석 | "모듈 분석해줘", "의존성 확인" |
| `generate_queue_only` | 큐/레지스트리만 생성 | "큐만 생성해줘" |
| `run_validation_only` | 검증만 실행 | "검증해줘", "체크해줘" |
| `commit_changes` | 커밋 | "커밋해줘" |
| `review_learning` | 학습 기록 확인 | "학습 기록 보여줘" |

**매칭 방식**: 트리거 키워드 점수 기반. 정확 매칭 10점, 부분 매칭 3점, 첨부 보너스 5점.

### 2. Command Profile Registry (`COMMAND_PROFILES.yaml`)

각 인텐트의 트리거 키워드, 파이프라인, 자동 컨텍스트, 안전 수준을 정의한다.

프로파일 추가/수정은 YAML 파일만 편집하면 된다. 코드 변경 불필요.

### 3. Default Execution Policy (`DEFAULT_EXECUTION_POLICY.yaml`)

모든 파이프라인 실행에 자동 주입되는 규칙:

- **모듈 전략**: `reuse > adapt > extend > wrap > create_new`
- **덮어쓰기**: 절대 금지 (blind overwrite)
- **진실 보고**: 검증 안 됐으면 done/pass 주장 금지
- **Human Gate**: 항상 필수
- **GC 금지**: 코루틴, 람다, LINQ, foreach, Invoke
- **매직넘버**: const 필수
- **레거시 코드**: 자동 삭제 금지, 어댑터 우선

### 4. Auto-Context Resolver

인텐트별로 자동으로 읽어야 할 파일 목록. `AUTO_CONTEXT_RULES.md` 참조.

### 5. Pipeline Dispatcher (`PipelineDispatcher.cs`)

인텐트 → 파이프라인 매핑:

| 인텐트 | 파이프라인 | 실행 대상 |
|---|---|---|
| `bootstrap_from_design` | `bootstrap` | `ExecuteBootstrapPipeline` (6단계 자동 실행) |
| `ingest_codebase` | `ingestion` | `CodebaseIngestionWindow` |
| `integrate_feature` | `full_pipeline` | 9단계 파이프라인 |
| `reuse_first_integration` | `reuse_then_pipeline` | ModuleDiscovery → 파이프라인 |
| `analyze_existing_modules` | `analysis_only` | RegressionGuardian + DependencyGraph |
| `generate_queue_only` | `queue_generation` | TASK_QUEUE/FEATURE_QUEUE 생성 |
| `run_validation_only` | `validation_only` | ValidationRunner (19 validators) |
| `commit_changes` | `commit` | 7 Gate 커밋 |
| `review_learning` | `learning_review` | 학습 파일 확인 |

---

## 안전 수준

| 수준 | 파일 변경 | 확인 필요 | 설명 |
|---|---|---|---|
| `safe` | 불가 | 불필요 | 분석/읽기 전용 |
| `guarded` | 가능 | 불필요 | 기존 파일 덮어쓰기 보호 |
| `dangerous` | 가능 | 필수 | 커밋/삭제 등 되돌리기 어려운 작업 |

---

## 사용 방법

### Unity Editor에서

1. `Tools > AI > Game Factory Control Panel` 열기
2. "Prompt OS — Command Center" 섹션에서 명령 입력
3. "Route & Dispatch" 클릭 → 파이프라인 자동 실행
4. "Route Only (Dry Run)" 클릭 → 라우팅 결과만 확인

### Cursor AI 대화에서

짧은 자연어 명령을 입력하면 `.cursor/rules/prompt-os.mdc` 룰이 자동으로:
1. 인텐트를 추론
2. 올바른 파이프라인을 선택
3. 안전 규칙을 주입
4. 컨텍스트를 로드

---

## 예시 매핑

```
"기획서 첨부했어, 셋팅해줘"
  → bootstrap_from_design → bootstrap pipeline
  → Auto-context: FEATURE_QUEUE, TASK_QUEUE, MODULE_REGISTRY, CODING_RULES, BOOTSTRAP_WORKFLOW
  → Safety: guarded
  → 자동 실행:
    [1] 레포 검사 (기존 모듈/큐/레지스트리 상태)
    [2] 기존 모듈 탐색 (reuse > adapt > create_new)
    [3] 기획서 분석 → 시스템/모듈 분해
    [4] Dry Run 미리보기
    [5] Apply → 폴더/씬/프리팹/큐/레지스트리 생성
    [6] Bootstrap Report (truthful)

"이 기획서로 시작해" (+ 기획서 텍스트)
  → bootstrap_from_design → bootstrap pipeline (동일 흐름)

"원클릭 셋팅 만들어줘" (+ 기획서 텍스트)
  → bootstrap_from_design → bootstrap pipeline (동일 흐름)

"폴더를 읽어"
  → ingest_codebase → ingestion pipeline
  → Auto-context: MODULE_REGISTRY, TASK_QUEUE, CODING_RULES, CODEBASE_INGESTION
  → Safety: safe
  → 자동 실행:
    [1] 스캔 대상 폴더 자동 감지
    [2] CodebaseAnalyzer로 코드 분석
    [3] ModuleCandidateInferrer로 모듈 후보 추론 (7가지 전략)
    [4] 기존 AGF 모듈과 비교
    [5] 7종 리포트 생성 (docs/ai/ingestion/)
    [6] TASK_QUEUE/MODULE_REGISTRY 드래프트 (pending)

"기존 코드 읽어" / "이 폴더 흡수해" / "코드 흡수"
  → ingest_codebase → ingestion pipeline (동일 흐름)

"이 기능 붙여"
  → integrate_feature → full_pipeline
  → Auto-context: PROJECT_OVERVIEW, CODING_RULES, MODULE_REGISTRY, ...
  → Safety: guarded
  → 실행: 9단계 파이프라인

"기존 모듈 활용해서 붙여"
  → reuse_first_integration → reuse_then_pipeline
  → Auto-context: MODULE_REGISTRY, TASK_QUEUE, CODING_RULES
  → Safety: guarded
  → 실행: ModuleDiscovery → 파이프라인

"검증해줘"
  → run_validation_only → validation_only
  → Safety: safe
  → 실행: ValidationRunner (19 validators)
```

---

## 관련 파일

| 파일 | 역할 |
|---|---|
| `docs/ai/COMMAND_PROFILES.yaml` | 커맨드 프로파일 정의 |
| `docs/ai/DEFAULT_EXECUTION_POLICY.yaml` | 기본 실행 정책 |
| `docs/ai/AUTO_CONTEXT_RULES.md` | 자동 컨텍스트 규칙 |
| `Assets/Editor/GameFactory/IntentRouter.cs` | 인텐트 라우터 |
| `Assets/Editor/GameFactory/PipelineDispatcher.cs` | 파이프라인 디스패처 |
| `.cursor/rules/prompt-os.mdc` | Cursor AI 룰 |

---

## 확장

새 인텐트 추가 절차:
1. `COMMAND_PROFILES.yaml`에 새 프로파일 추가
2. `IntentRouter.cs`의 `EIntent` enum에 새 값 추가
3. `PipelineDispatcher.cs`의 switch에 새 dispatch 메서드 추가
4. `IntentRouter.InvalidateCache()` 호출로 캐시 갱신
