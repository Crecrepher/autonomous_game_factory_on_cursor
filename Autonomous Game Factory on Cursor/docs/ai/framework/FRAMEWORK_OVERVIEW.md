# AI Game Dev Framework 개요

## 프레임워크란?

AI Game Dev Framework는 **Cursor IDE + Unity** 환경에서 AI 에이전트가 **안전하고 체계적으로** 게임 코드를 생성·검증·개선할 수 있도록 설계된 개발 프레임워크입니다.

이 프레임워크는 특정 게임에 종속되지 않으며, 다양한 Unity 프로젝트에 주입하여 사용할 수 있습니다.

---

## 핵심 구성 요소

### 1. Cursor 규칙 (`.cursor/rules/`)

AI 에이전트의 행동을 제어하는 규칙 파일입니다.

- **autonomous-developer.mdc**: 자율 개발 워크플로 정의
  - 문서 먼저 읽기 → 계획 → 구현 → 검증 → 요약
  - Core 폴더 수정 금지
  - 모듈 매핑 필수
  - 테스트 생성 필수

### 2. Editor 검증 도구 (`framework/editor/AI/`)

Unity Editor에서 AI가 생성한 코드를 자동으로 검증하는 도구입니다.

| 파일 | 역할 |
|------|------|
| `IModuleValidator.cs` | 검증기 인터페이스 |
| `ValidationReport.cs` | 검증 결과 리포트 모델 |
| `ValidationRunner.cs` | 검증 실행기 (컴파일 트리거 포함) |
| `ValidateGeneratedModulesMenu.cs` | Unity 메뉴 진입점 (`Tools/AI/...`) |

#### 기본 제공 검증기 (`Validators/`)

| 검증기 | 검사 내용 |
|--------|-----------|
| `ForbiddenFolderValidator` | Core 폴더에 무단으로 추가된 파일 감지 |
| `ModuleStructureValidator` | MODULE_REGISTRY에 등록된 모듈의 필수 파일 존재 여부 (I*.cs, *Config.cs, *Runtime.cs, *Factory.cs, Tests/) |
| `ArchitectureRuleValidator` | Runtime 파일이 MonoBehaviour를 상속하지 않는지, Config 파일이 ScriptableObject를 상속하는지 |
| `CompileErrorValidator` | 컴파일 에러 수집 |

### 3. 모듈 템플릿 (`framework/templates/ModuleTemplate/`)

새 모듈을 만들 때 복사해서 사용하는 표준 템플릿입니다.

| 파일 | 패턴 |
|------|------|
| `ITemplate.cs` | 모듈 공개 인터페이스 (계약) |
| `TemplateConfig.cs` | ScriptableObject 설정 데이터 |
| `TemplateRuntime.cs` | 순수 C# 런타임 로직 (MonoBehaviour 없음) |
| `TemplateFactory.cs` | Config → Runtime 생성 팩토리 |
| `TemplateBootstrap.cs` | 얇은 MonoBehaviour (씬 진입점) |
| `Tests/TemplateTests.cs` | NUnit 테스트 |

### 4. 예시 문서 (`docs/examples/`)

실제 프로젝트에서 AI 문서를 작성할 때 참고할 수 있는 예시입니다.

| 예시 | 용도 |
|------|------|
| `PROJECT_OVERVIEW.example.md` | 프로젝트 개요 작성법 |
| `CODING_RULES.example.md` | 코딩 규칙 정의법 |
| `MODULE_REGISTRY.example.yaml` | 모듈 레지스트리 작성법 |
| `AI_DEVELOPMENT_LOOP.example.md` | AI 개발 루프 설계법 |

---

## 아키텍처 다이어그램

> 아래 다이어그램은 프레임워크를 **대상 Unity 프로젝트에 주입한 후**의 런타임 구조입니다.
> 경로(Docs/ai/*, Assets/Editor/AI/ 등)는 대상 프로젝트 기준입니다.

```
┌──────────────────────────────────────────────────────────────┐
│  Cursor IDE  (대상 Unity 프로젝트)                            │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  .cursor/rules/autonomous-developer.mdc               │  │
│  │  → AI 에이전트 행동 규칙                                │  │
│  └──────────────────────┬─────────────────────────────────┘  │
│                         │ 규칙 준수                           │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  AI 에이전트 (Cursor)                                   │  │
│  │                                                        │  │
│  │  1. Docs/ai/* 읽기 (PROJECT_OVERVIEW, CODING_RULES,   │  │
│  │     MODULE_REGISTRY, AI_DEVELOPMENT_LOOP)              │  │
│  │  2. PLAN 작성                                          │  │
│  │  3. 모듈 코드 생성 (Template 기반)                      │  │
│  │  4. 검증 요청                                          │  │
│  └──────────────────────┬─────────────────────────────────┘  │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Unity Editor                                          │  │
│  │  Tools/AI/Validate Generated Modules                   │  │
│  │  → ValidationRunner → Validators → Report              │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

---

## 설계 원칙

자세한 내용은 [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md)를 참조하세요.
