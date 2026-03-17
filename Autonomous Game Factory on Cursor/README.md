# AI Game Dev Framework

Cursor IDE + Unity 환경에서 AI 에이전트가 안전하고 체계적으로 게임 코드를 생성·검증·개선할 수 있도록 설계된 재사용 가능한 개발 프레임워크입니다.

---

## 이 저장소는 무엇인가

이 저장소는 **프레임워크 소스 저장소**입니다.  
실제 게임 프로젝트가 아닙니다. Unity로 직접 열어서 게임을 만드는 곳이 아닙니다.

여기에는 다음이 포함됩니다:

- AI 에이전트 행동 규칙 (Cursor Rules)
- Unity Editor 모듈 검증 도구 (Validators)
- 표준 모듈 템플릿 (Interface, Config, Runtime, Factory, Bootstrap, Tests)
- 프로젝트에 적용하는 방법을 설명하는 가이드 문서
- 실제 프로젝트에서 AI 문서를 작성할 때 참고할 예시 파일

## 이 저장소에 들어가야 하는 것

| 분류 | 위치 | 설명 |
|------|------|------|
| Cursor 규칙 | `.cursor/rules/` | AI 에이전트의 자율 개발 워크플로 |
| 검증 도구 소스 | `framework/editor/AI/` | 모듈 구조·아키텍처·컴파일 검증 C# 코드 |
| 모듈 템플릿 소스 | `framework/templates/` | 새 모듈 생성 시 복사할 표준 패턴 |
| 프레임워크 문서 | `docs/framework/` | 설계 원칙, 아키텍처 개요 |
| 적용 가이드 | `docs/guides/` | 설치, 프로젝트 적용, 규칙 진화 방법 |
| 예시 문서 | `docs/examples/` | 프로젝트별 AI 문서 작성 참고용 샘플 |
| 설치 스크립트 | `scripts/install/` | 프로젝트에 파일 복사하는 방법 안내 |

## 이 저장소에 들어가면 안 되는 것

- 실제 게임 모듈 코드 (Combat, Economy, Player 등)
- Unity 프로젝트 파일 (ProjectSettings, Packages, Library, .meta 등)
- 특정 게임에 종속된 ScriptableObject, 프리팹, 씬
- 특정 프로젝트의 코딩 규칙이나 모듈 레지스트리 (이것들은 `docs/examples/`에 예시로만 존재)

## 실제 Unity 프로젝트에 적용하는 방법

1. 이 저장소를 클론합니다
2. [docs/guides/HOW_TO_APPLY_TO_PROJECT.md](docs/guides/HOW_TO_APPLY_TO_PROJECT.md)를 따라 파일을 대상 프로젝트에 복사합니다
3. `docs/examples/`의 예시를 참고하여 프로젝트에 맞는 AI 문서를 작성합니다
4. Cursor에서 프로젝트를 열면 AI 에이전트가 규칙을 따라 동작합니다

## 저장소 구조

```
/
├── .cursor/rules/                    ← AI 에이전트 행동 규칙
│   └── autonomous-developer.mdc
│
├── framework/                        ← 재사용 가능한 프레임워크 소스
│   ├── editor/AI/                    ← Unity Editor 검증 도구
│   │   ├── IModuleValidator.cs
│   │   ├── ValidationReport.cs
│   │   ├── ValidationRunner.cs
│   │   ├── ValidateGeneratedModulesMenu.cs
│   │   └── Validators/
│   └── templates/ModuleTemplate/     ← 표준 모듈 템플릿
│       ├── ITemplate.cs
│       ├── TemplateConfig.cs
│       ├── TemplateRuntime.cs
│       ├── TemplateFactory.cs
│       ├── TemplateBootstrap.cs
│       └── Tests/
│
├── docs/
│   ├── framework/                    ← 프레임워크 설계 문서
│   │   ├── FRAMEWORK_OVERVIEW.md
│   │   └── DESIGN_PRINCIPLES.md
│   ├── guides/                       ← 적용·운영 가이드
│   │   ├── INSTALL.md
│   │   ├── HOW_TO_APPLY_TO_PROJECT.md
│   │   └── HOW_TO_EVOLVE_RULES.md
│   └── examples/                     ← 프로젝트별 AI 문서 예시
│       ├── PROJECT_OVERVIEW.example.md
│       ├── CODING_RULES.example.md
│       ├── MODULE_REGISTRY.example.yaml
│       └── AI_DEVELOPMENT_LOOP.example.md
│
└── scripts/install/                  ← 설치 방법 안내
    └── copy-framework-to-project.md
```

## 문서 링크

| 문서 | 내용 |
|------|------|
| [INSTALL.md](docs/guides/INSTALL.md) | 이 저장소 소개 및 환경 요구사항 |
| [HOW_TO_APPLY_TO_PROJECT.md](docs/guides/HOW_TO_APPLY_TO_PROJECT.md) | Unity 프로젝트에 주입하는 단계별 가이드 |
| [HOW_TO_EVOLVE_RULES.md](docs/guides/HOW_TO_EVOLVE_RULES.md) | 규칙·검증기 진화 방법 |
| [FRAMEWORK_OVERVIEW.md](docs/framework/FRAMEWORK_OVERVIEW.md) | 프레임워크 구조·구성 요소 |
| [DESIGN_PRINCIPLES.md](docs/framework/DESIGN_PRINCIPLES.md) | 설계 결정과 그 이유 |
