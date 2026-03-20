# AI Game Dev Framework 개요

Cursor + Unity 환경에서 AI 에이전트가 안전하고 체계적으로 게임 코드를 생성·검증·개선하는 프레임워크.

---

## 핵심 구성 요소

### 1. Cursor 규칙 (`.cursor/rules/`)

AI 에이전트의 행동 규칙. 문서 읽기 → 계획 → 구현 → 검증 → 요약 워크플로를 강제한다.

### 2. Editor 검증 도구 (`Assets/Editor/AI/`)

| 파일 | 역할 |
|------|------|
| `IModuleValidator.cs` | 검증기 인터페이스 |
| `ValidationReport.cs` | 검증 리포트 모델 |
| `ValidationRunner.cs` | 검증 실행기 |
| `ValidateGeneratedModulesMenu.cs` | Unity 메뉴 (`Tools/AI/...`) |

#### 기본 검증기 (`Validators/`)

| 검증기 | 검사 내용 |
|--------|-----------|
| `ForbiddenFolderValidator` | Core 폴더 무단 변경 감지 |
| `ModuleStructureValidator` | 모듈 필수 파일 존재 여부 |
| `ArchitectureRuleValidator` | Runtime/Config 상속 규칙 |
| `CompileErrorValidator` | 컴파일 에러 감지 |

### 3. 모듈 템플릿 (`Assets/Game/Modules/Template/`)

| 파일 | 패턴 |
|------|------|
| `ITemplate.cs` | 모듈 공개 인터페이스 |
| `TemplateConfig.cs` | ScriptableObject 설정 |
| `TemplateRuntime.cs` | 순수 C# 런타임 로직 |
| `TemplateFactory.cs` | Config → Runtime 팩토리 |
| `TemplateBootstrap.cs` | MonoBehaviour 씬 진입점 |
| `Tests/Editor/TemplateTests.cs` | EditMode 테스트 |

### 4. AI 문서 (`Docs/ai/`)

| 파일 | 용도 |
|------|------|
| `PROJECT_OVERVIEW.example.md` | 프로젝트 개요 예시 |
| `CODING_RULES.example.md` | 코딩 규칙 예시 |
| `MODULE_REGISTRY.example.yaml` | 모듈 레지스트리 예시 |
| `AI_DEVELOPMENT_LOOP.example.md` | AI 개발 루프 예시 |

---

## 설계 원칙

자세한 내용은 [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) 참조.
