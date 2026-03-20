# Unity 프로젝트에 프레임워크 적용하기

이 문서는 AI Game Dev Framework를 **실제 Unity 프로젝트**에 주입하는 방법을 설명합니다.

---

## 1. 프레임워크에서 가져올 것들

| 원본 (프레임워크) | 대상 (Unity 프로젝트) | 설명 |
|---|---|---|
| `.cursor/rules/autonomous-developer.mdc` | `.cursor/rules/autonomous-developer.mdc` | AI 에이전트 행동 규칙 |
| `framework/editor/AI/` 전체 | `Assets/Editor/AI/` | 모듈 검증 도구 |
| `framework/templates/ModuleTemplate/` | `Assets/Game/Modules/Template/` | 표준 모듈 템플릿 |

---

## 2. 단계별 적용

### Step 1: Cursor 규칙 복사

```bash
# Unity 프로젝트 루트에서
mkdir -p .cursor/rules
cp <framework-repo>/.cursor/rules/autonomous-developer.mdc .cursor/rules/
```

### Step 2: Editor 검증 도구 복사

```bash
# Assets/Editor/AI/ 폴더로 복사
mkdir -p Assets/Editor/AI/Validators
cp <framework-repo>/framework/editor/AI/*.cs Assets/Editor/AI/
cp <framework-repo>/framework/editor/AI/Validators/*.cs Assets/Editor/AI/Validators/
```

### Step 3: 모듈 템플릿 복사

```bash
# Assets/Game/Modules/Template/ 폴더로 복사
mkdir -p Assets/Game/Modules/Template/Tests
cp <framework-repo>/framework/templates/ModuleTemplate/*.cs Assets/Game/Modules/Template/
cp <framework-repo>/framework/templates/ModuleTemplate/Tests/*.cs Assets/Game/Modules/Template/Tests/
```

### Step 4: AI 문서 작성

`docs/examples/` 폴더의 예시를 참고하여, 프로젝트에 맞는 문서를 작성합니다:

```bash
mkdir -p Docs/ai
```

필수 문서:

| 파일 | 용도 | 예시 참조 |
|------|------|-----------|
| `Docs/ai/PROJECT_OVERVIEW.md` | 프로젝트 목적, 게임플레이, 아키텍처, 폴더 구조 | `PROJECT_OVERVIEW.example.md` |
| `Docs/ai/CODING_RULES.md` | 코딩 규칙, 네이밍, 성능, 구조 | `CODING_RULES.example.md` |
| `Docs/ai/MODULE_REGISTRY.yaml` | 모듈 경로, 의존성, editable, risk | `MODULE_REGISTRY.example.yaml` |
| `Docs/ai/AI_DEVELOPMENT_LOOP.md` | AI 개발 루프 단계 정의 | `AI_DEVELOPMENT_LOOP.example.md` |

### Step 5: 네임스페이스 조정

프레임워크의 기본 네임스페이스는 `Game` 및 `Game.Editor.AI`입니다.
프로젝트에 맞게 변경하세요:

- `Game` → `YourProjectName`
- `Game.Editor.AI` → `YourProjectName.Editor.AI`

### Step 6: 검증 도구 테스트

Unity 에디터에서:
1. `Tools > AI > Update Core Baseline` — Core 폴더 기준선 생성
2. `Tools > AI > Validate Generated Modules` — 검증 실행
3. Console에서 결과 확인

---

## 3. 적용 후 구조 (예시)

```
YourUnityProject/
  .cursor/
    rules/
      autonomous-developer.mdc
  Assets/
    Editor/
      AI/
        IModuleValidator.cs
        ValidationReport.cs
        ValidationRunner.cs
        ValidateGeneratedModulesMenu.cs
        Validators/
          ArchitectureRuleValidator.cs
          CompileErrorValidator.cs
          ForbiddenFolderValidator.cs
          ModuleStructureValidator.cs
    Game/
      Core/          ← 프로젝트별 부트스트랩, 입력, 카메라, 세이브
      Shared/        ← 공용 인터페이스, enum, 상수
      Modules/
        Template/    ← 모듈 템플릿 (새 모듈 생성 시 복사해서 사용)
        <MyModule>/  ← 프로젝트 모듈들
  Docs/
    ai/
      PROJECT_OVERVIEW.md
      CODING_RULES.md
      MODULE_REGISTRY.yaml
      AI_DEVELOPMENT_LOOP.md
```

---

## 4. 주의사항

- `autonomous-developer.mdc`의 경로 참조(`Docs/ai/PROJECT_OVERVIEW.md` 등)가 실제 프로젝트 문서 경로와 일치하는지 확인하세요.
- `ModuleStructureValidator.cs`에 `MODULE_REGISTRY.yaml` 경로(`Docs/ai/MODULE_REGISTRY.yaml`)가 하드코딩되어 있습니다. 프로젝트 구조가 다르면 수정이 필요합니다.
- 검증 도구는 Unity Editor 전용입니다. 빌드에 포함되지 않습니다.
