# AI Game Dev Framework

Cursor + Unity 환경에서 AI가 안전하고 체계적으로 게임 코드를 생성·검증·개선하는 프레임워크.

---

## 이 저장소는 무엇인가

**Unity 프로젝트에 바로 주입 가능한 AI 개발 프레임워크** 소스 저장소.

폴더 구조가 Unity 프로젝트와 동일하기 때문에, 그대로 복사하면 바로 동작한다.

## 사용법

```bash
cp -r ai-dev-framework/* MyGameProject/
```

그리고 `Docs/ai/` 안의 `.example` 파일들에서 `.example`을 제거하고 프로젝트에 맞게 수정하면 끝.

## 저장소 구조

```
/
├── .cursor/
│   └── rules/
│       └── autonomous-developer.mdc      ← AI 에이전트 행동 규칙
│
├── Assets/
│   ├── Editor/
│   │   └── AI/
│   │       ├── IModuleValidator.cs       ← 검증기 인터페이스
│   │       ├── ValidationReport.cs       ← 검증 리포트 모델
│   │       ├── ValidationRunner.cs       ← 검증 실행기
│   │       ├── ValidateGeneratedModulesMenu.cs  ← Unity 메뉴
│   │       └── Validators/
│   │           ├── ArchitectureRuleValidator.cs
│   │           ├── CompileErrorValidator.cs
│   │           ├── ForbiddenFolderValidator.cs
│   │           └── ModuleStructureValidator.cs
│   └── Game/
│       └── Modules/
│           └── Template/                 ← 표준 모듈 템플릿
│               ├── ITemplate.cs
│               ├── TemplateConfig.cs
│               ├── TemplateRuntime.cs
│               ├── TemplateFactory.cs
│               ├── TemplateBootstrap.cs
│               └── Tests/Editor/
│                   └── TemplateTests.cs
│
├── Docs/
│   └── ai/
│       ├── PROJECT_OVERVIEW.example.md   ← 프로젝트 개요 예시
│       ├── CODING_RULES.example.md       ← 코딩 규칙 예시
│       ├── MODULE_REGISTRY.example.yaml  ← 모듈 레지스트리 예시
│       ├── AI_DEVELOPMENT_LOOP.example.md ← AI 개발 루프 예시
│       ├── FRAMEWORK_OVERVIEW.md         ← 프레임워크 구조 설명
│       ├── DESIGN_PRINCIPLES.md          ← 설계 원칙
│       ├── INSTALL.md                    ← 설치 가이드
│       ├── HOW_TO_APPLY_TO_PROJECT.md    ← 프로젝트 적용 방법
│       └── HOW_TO_EVOLVE_RULES.md        ← 규칙 진화 방법
│
├── .gitignore
└── README.md
```

## 왜 이 구조인가

| 이유 | 설명 |
|------|------|
| **그대로 복사 = 바로 동작** | 경로 변환 없이 `cp -r`로 끝 |
| **경로 혼동 없음** | Rule, Validator, Template이 참조하는 경로가 프레임워크와 프로젝트에서 동일 |
| **Validator 수정 불필요** | `Assets/Editor/AI`가 그대로이니 코드 수정 없음 |
| **Cursor Rule 안정** | `.cursor/rules/`가 루트 기준이라 경로가 깨질 일 없음 |

## 프로젝트에 적용한 후 할 일

1. `Docs/ai/*.example.*` 파일에서 `.example`을 제거
2. 내용을 프로젝트에 맞게 수정
3. 네임스페이스 `Game` → 프로젝트명으로 변경
4. Unity에서 `Tools > AI > Validate Generated Modules` 실행하여 검증

## 이 저장소에 들어가면 안 되는 것

- Library/, Temp/, Logs/, Obj/, Build/ (Unity 생성 폴더)
- 특정 게임의 실제 모듈 코드
- ProjectSettings/, Packages/ (Unity 프로젝트 설정)

## 문서

| 문서 | 내용 |
|------|------|
| [INSTALL.md](Docs/ai/INSTALL.md) | 설치 가이드 |
| [HOW_TO_APPLY_TO_PROJECT.md](Docs/ai/HOW_TO_APPLY_TO_PROJECT.md) | 프로젝트 적용 방법 |
| [HOW_TO_EVOLVE_RULES.md](Docs/ai/HOW_TO_EVOLVE_RULES.md) | 규칙 진화 방법 |
| [FRAMEWORK_OVERVIEW.md](Docs/ai/FRAMEWORK_OVERVIEW.md) | 프레임워크 구조 |
| [DESIGN_PRINCIPLES.md](Docs/ai/DESIGN_PRINCIPLES.md) | 설계 원칙 |
