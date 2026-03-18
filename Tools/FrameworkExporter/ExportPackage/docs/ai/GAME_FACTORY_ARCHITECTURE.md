# Game Factory Architecture — Autonomous Game Factory v2.3

이 문서는 AI 기반 게임 개발 팩토리의 전체 아키텍처를 정의한다.

---

## 1. 시스템 개요

```
┌─────────────────────────────────────────────────────────────────┐
│                 Autonomous Game Factory v2.3                    │
│                                                                 │
│  ┌──────────┐  ┌───────────┐  ┌───────────┐  ┌──────────────┐ │
│  │ Feature  │→ │ Queue     │→ │ Arch Diff │→ │ Orchestrator │ │
│  │ Intake   │  │ Generator │  │ Analyzer  │  │ + Planner    │ │
│  └──────────┘  └───────────┘  └───────────┘  └──────────────┘ │
│                     │                              │           │
│           ┌─────────┴──────────┐                   │           │
│           │ Intelligent        │                   ▼           │
│           │ Decomposer (CAP4)  │           ┌──────────────┐   │
│           └────────────────────┘           │ Builder      │   │
│                                            └──────────────┘   │
│                                                    │           │
│  ┌──────────────┐  ┌───────────┐  ┌─────────────┐ │           │
│  │ Regression   │  │ Human     │← │ Validator   │ │           │
│  │ Guardian     │  │ Gate      │  │ Pipeline    │←┘           │
│  │ (CAP2)       │  └───────────┘  └─────────────┘            │
│  └──────────────┘        │                                    │
│                          ▼                                    │
│  ┌──────────────┐  ┌───────────┐  ┌─────────────┐           │
│  │ Self-Healing │  │ Reviewer  │→ │ Committer   │           │
│  │ Engine (CAP3)│  └───────────┘  └─────────────┘           │
│  └──────────────┘                       │                    │
│                                         ▼                    │
│  ┌──────────────┐              ┌─────────────────┐          │
│  │ Architecture │              │ Learning        │          │
│  │ Knowledge    │← ─ ─ ─ ─ ─ ─│ Recorder        │          │
│  │ Memory (CAP1)│              └─────────────────┘          │
│  └──────────────┘                                           │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ Game Factory Control Panel (CAP5) — Unity EditorWindow│   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. 캡빌리티 매핑

| # | 캡빌리티 | 구현 파일 | 역할 |
|---|----------|-----------|------|
| CAP1 | Architecture Knowledge Memory | `ArchitectureKnowledgeMemory.cs` + `architecture_memory/` | 아키텍처 패턴/안티패턴 저장소 |
| CAP2 | Automated Regression Guardian | `RegressionGuardian.cs` + `RegressionGuardianValidator.cs` | 회귀 감지 + 커밋 차단 |
| CAP3 | Pipeline Self-Healing Engine | `PipelineSelfHealer.cs` | 메타데이터 자동 복구 |
| CAP4 | Intelligent Feature Decomposer | `IntelligentDecomposer.cs` | 게임 디자인 → 모듈 분해 |
| CAP5 | Game Factory Control Panel | `GameFactoryControlPanel.cs` | Unity Editor 통합 UI |

---

## 3. 폴더 구조

```
Assets/
├── Editor/
│   ├── AI/
│   │   ├── ArchitectureDiffAnalyzer.cs      # Diff 분석 엔진
│   │   ├── ArchitectureKnowledgeMemory.cs   # CAP1 — 지식 저장소 엔진
│   │   ├── AutonomousPipeline.cs            # 파이프라인 엔트리포인트
│   │   ├── BuilderAgent.cs                  # 빌더 에이전트
│   │   ├── DependencyGraphBuilder.cs        # 의존성 그래프
│   │   ├── DependencyGraphTestRunner.cs     # 그래프 테스트
│   │   ├── FeatureDecomposer.cs             # 기존 분해기
│   │   ├── FeatureGroupTracker.cs           # 그룹 추적
│   │   ├── FeatureIntake.cs                 # 기능 접수
│   │   ├── GitCommitStage.cs                # Git 커밋
│   │   ├── IModuleValidator.cs              # Validator 인터페이스
│   │   ├── ImpactAnalyzer.cs                # 영향 분석
│   │   ├── IntelligentDecomposer.cs         # CAP4 — 스마트 분해기
│   │   ├── IntegrationStrategyEngine.cs     # 통합 전략
│   │   ├── LearningRecorderWriter.cs        # 학습 기록
│   │   ├── ModuleDiscovery.cs               # 모듈 탐색
│   │   ├── OrchestratorSimulator.cs         # 오케스트레이터 시뮬레이터
│   │   ├── ParallelBuilderOrchestrator.cs   # 병렬 빌더
│   │   ├── PipelineSelfHealer.cs            # CAP3 — 자동 복구
│   │   ├── RegressionGuardian.cs            # CAP2 — 회귀 감시
│   │   ├── SpecGenerator.cs                 # Spec 생성
│   │   ├── TaskQueueGenerator.cs            # Queue 생성
│   │   ├── TaskStateTransition.cs           # 상태 전이
│   │   ├── ValidateGeneratedModulesMenu.cs  # 메뉴 진입점
│   │   ├── ValidationReport.cs              # 검증 보고서
│   │   ├── ValidationRunner.cs              # 검증 실행기
│   │   └── Validators/
│   │       ├── ArchitectureDiffValidator.cs
│   │       ├── ArchitecturePatternValidator.cs
│   │       ├── ArchitectureRuleValidator.cs
│   │       ├── CircularDependencyValidator.cs
│   │       ├── CodingStyleValidator.cs
│   │       ├── CompileErrorValidator.cs
│   │       ├── ConfigConflictValidator.cs
│   │       ├── DependencyValidator.cs
│   │       ├── ForbiddenFolderValidator.cs
│   │       ├── IntegrationStrategyValidator.cs
│   │       ├── ModuleBoundaryValidator.cs
│   │       ├── ModuleStructureValidator.cs
│   │       ├── PerformanceValidator.cs
│   │       ├── RegressionGuardianValidator.cs  # CAP2
│   │       ├── PipelineTruthValidator.cs       # 파이프라인 진실성
│   │       ├── ModuleReuseIntegrityValidator.cs # 재사용 무결성
│   │       ├── ConfigurationAuthorityValidator.cs # 설정 권위
│   │       ├── StringAndAnimatorValidator.cs
│   │       └── ValidatorRegistrationValidator.cs
│   └── GameFactory/
│       ├── GameFactoryControlPanel.cs       # CAP5 — 컨트롤 패널 + Prompt OS UI
│       ├── GameFactoryBootstrapWindow.cs    # Bootstrap EditorWindow
│       ├── BootstrapEngine.cs               # Bootstrap 생성 엔진
│       ├── CodebaseIngestionWindow.cs       # Ingestion EditorWindow
│       ├── CodebaseAnalyzer.cs              # 코드베이스 분석 엔진
│       ├── ModuleCandidateInferrer.cs       # 모듈 후보 추론 엔진
│       ├── IngestionReportGenerator.cs      # 리포트/큐/레지스트리 생성
│       ├── IntentRouter.cs                  # Prompt OS — 인텐트 라우터
│       └── PipelineDispatcher.cs            # Prompt OS — 파이프라인 디스패처
├── Game/
│   ├── Core/                                # 읽기 전용
│   ├── Modules/                             # 모듈 코드
│   │   ├── Template/                        # 모듈 템플릿
│   │   ├── InventorySystem/
│   │   ├── ItemStacking/
│   │   └── ...
│   └── Shared/                              # 공유 인터페이스/타입
└── ...

docs/ai/
├── architecture_memory/                      # CAP1
│   ├── ARCHITECTURE_PATTERNS.yaml
│   ├── ANTI_PATTERNS.yaml
│   ├── MODULE_EVOLUTION_LOG.md
│   └── ARCHITECTURE_DECISIONS.md
├── diff_reports/                             # Diff 리포트
├── learning/                                 # 학습 데이터
├── plans/                                    # 모듈 PLAN
├── generated_specs/                          # 모듈 SPEC
├── runs/                                     # 실행 로그
├── ORCHESTRATION_RULES.md
├── TASK_QUEUE.yaml
├── MODULE_REGISTRY.yaml
├── FEATURE_QUEUE.yaml
├── GAME_FACTORY_ARCHITECTURE.md             # 이 문서
├── PIPELINE_AUTOMATION.md                   # 자동화 명세
├── validators/                              # Validator 문서
│   ├── PIPELINE_TRUTH_VALIDATOR.md
│   ├── MODULE_REUSE_INTEGRITY_VALIDATOR.md
│   └── CONFIGURATION_AUTHORITY_VALIDATOR.md
├── bootstrap/                               # Bootstrap 리포트
│   └── BOOTSTRAP_REPORT.md
├── BOOTSTRAP_WORKFLOW.md                    # Bootstrap 워크플로우 명세
├── CODEBASE_INGESTION.md                   # Ingestion 워크플로우 명세
├── PROMPT_OS.md                            # Prompt OS 아키텍처 명세
├── COMMAND_PROFILES.yaml                   # 커맨드 프로파일 레지스트리
├── DEFAULT_EXECUTION_POLICY.yaml           # 기본 실행 정책
├── AUTO_CONTEXT_RULES.md                   # 자동 컨텍스트 규칙
├── ingestion/                               # Ingestion 리포트
│   ├── CODEBASE_INVENTORY.md
│   ├── MODULE_CANDIDATES.md
│   ├── DEPENDENCY_GRAPH.md
│   ├── MODULARIZATION_PLAN.md
│   ├── MIGRATION_PLAN.md
│   ├── CODEBASE_INDEX.yaml
│   └── MODULE_CANDIDATES.yaml
└── ...
```

---

## 4. 데이터 흐름

```
사용자 요청 (자연어)
 │
 ▼
[Prompt OS — Intent Router] → 인텐트 분류 (COMMAND_PROFILES.yaml)
 │  ├── bootstrap_from_design → Bootstrap 워크플로우
 │  ├── ingest_codebase       → Ingestion 워크플로우
 │  ├── integrate_feature     → 풀 파이프라인 (아래)
 │  ├── reuse_first_integration → Reuse-First 통합
 │  ├── run_validation_only   → Validator만 실행
 │  └── ...
 │
 ▼ (integrate_feature 인텐트일 때)
[Feature Intake] → FEATURE_QUEUE.yaml
 │
 ▼
[Intelligent Decomposer] → 시스템/서브시스템/모듈 분해
 │
 ▼
[Queue Generator] → TASK_QUEUE.yaml + MODULE_REGISTRY.yaml
 │  ├── Module Discovery
 │  └── Reuse Decision
 │
 ▼
[Architecture Diff Analyzer] → diff_reports/<Module>_DIFF.md
 │  critical → BLOCKED (파이프라인 중단)
 │
 ▼
[Orchestrator] → 의존성 그래프 → 실행 순서 결정
 │
 ▼
[Planner] → plans/<Module>_PLAN.md (diff 경고 포함)
 │
 ▼
[Builder] → Assets/Game/Modules/<Module>/ (6파일)
 │
 ▼
[Validator Pipeline] → AIValidationReport.json
 │  ├── 19 Validators (구조/아키텍처/성능/회귀/진실성/재사용/설정권위...)
 │  └── Regression Guardian (CAP2)
 │
 ▼
★ [HUMAN GATE] ★ → 사람이 검증 + 수정
 │
 ▼
[Reviewer] → commit_state: ready
 │
 ▼
[Committer] → git commit (7 Gate 체크)
 │
 ▼
[Learning Recorder] → learning/ 업데이트
 │
 ▼
[Architecture Knowledge Memory] → architecture_memory/ 업데이트
```

---

## 5. Validator 파이프라인 (16 Validators)

| # | Validator | 검사 대상 |
|---|-----------|-----------|
| 1 | CompileError | 컴파일 에러 |
| 2 | ValidatorRegistration | Validator 등록 여부 |
| 3 | ForbiddenFolder | Core 보호 |
| 4 | ModuleStructure | 6파일 구조 |
| 5 | ModuleBoundary | 모듈 경계 |
| 6 | ArchitectureRule | Runtime/Config 규칙 |
| 7 | CodingStyle | GC/foreach/LINQ 금지 |
| 8 | Performance | GetComponent/Find 금지 |
| 9 | ArchitecturePattern | 싱글턴/Awake 금지 |
| 10 | StringAndAnimator | 문자열/애니메이터 해시 |
| 11 | Dependency | 의존성 일관성 |
| 12 | CircularDependency | 순환 의존 |
| 13 | IntegrationStrategy | 통합 전략 검증 |
| 14 | ConfigConflict | Config 충돌 |
| 15 | ArchitectureDiff | Diff 분석 검증 |
| 16 | RegressionGuardian | 회귀 감지 (CAP2) |
| 17 | PipelineTruth | 파이프라인 진실성 검증 (Blocking) |
| 18 | ModuleReuseIntegrity | 재사용 결정 무결성 검증 (Blocking/Warning) |
| 19 | ConfigurationAuthority | 설정 권위 검증 (Warning/Blocking) |

---

## 6. 커밋 게이트 (7 Gates)

| # | Gate | 조건 |
|---|------|------|
| 1 | Validation Report | Passed == true |
| 2 | Reviewer | commit_state == ready |
| 3 | Human | human_state == validated |
| 4 | Learning | learning_state == recorded (fix 시) |
| 5 | Completeness | feature_group 전체 ready |
| 6 | Scope | 관련 파일만 스테이징 |
| 7 | Architecture Diff | arch_diff_blocked != true |
