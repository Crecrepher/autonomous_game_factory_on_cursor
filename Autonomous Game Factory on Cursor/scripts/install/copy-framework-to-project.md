# 프레임워크를 Unity 프로젝트에 복사하기

이 문서는 AI Game Dev Framework 파일을 실제 Unity 프로젝트로 복사하는 구체적인 방법을 안내합니다.

---

## 수동 복사 (권장)

### 1. Cursor 규칙

```bash
# <FRAMEWORK> = 이 프레임워크 저장소의 로컬 경로
# <PROJECT>   = 대상 Unity 프로젝트의 루트 경로

mkdir -p <PROJECT>/.cursor/rules
cp <FRAMEWORK>/.cursor/rules/autonomous-developer.mdc <PROJECT>/.cursor/rules/
```

### 2. Editor 검증 도구

```bash
mkdir -p <PROJECT>/Assets/Editor/AI/Validators
cp <FRAMEWORK>/framework/editor/AI/*.cs <PROJECT>/Assets/Editor/AI/
cp <FRAMEWORK>/framework/editor/AI/Validators/*.cs <PROJECT>/Assets/Editor/AI/Validators/
```

### 3. 모듈 템플릿

```bash
mkdir -p <PROJECT>/Assets/Game/Modules/Template/Tests
cp <FRAMEWORK>/framework/templates/ModuleTemplate/*.cs <PROJECT>/Assets/Game/Modules/Template/
cp <FRAMEWORK>/framework/templates/ModuleTemplate/Tests/*.cs <PROJECT>/Assets/Game/Modules/Template/Tests/
```

### 4. AI 문서 (예시 기반으로 새로 작성)

```bash
mkdir -p <PROJECT>/Docs/ai
# 예시 파일을 복사한 뒤, 프로젝트에 맞게 내용을 수정하세요
cp <FRAMEWORK>/docs/examples/PROJECT_OVERVIEW.example.md <PROJECT>/Docs/ai/PROJECT_OVERVIEW.md
cp <FRAMEWORK>/docs/examples/CODING_RULES.example.md <PROJECT>/Docs/ai/CODING_RULES.md
cp <FRAMEWORK>/docs/examples/MODULE_REGISTRY.example.yaml <PROJECT>/Docs/ai/MODULE_REGISTRY.yaml
cp <FRAMEWORK>/docs/examples/AI_DEVELOPMENT_LOOP.example.md <PROJECT>/Docs/ai/AI_DEVELOPMENT_LOOP.md
```

---

## 복사 후 필수 수정 사항

1. **네임스페이스 변경**: 모든 `.cs` 파일의 `Game` → 프로젝트 네임스페이스로 변경
2. **문서 내용 수정**: 예시 문서의 내용을 실제 프로젝트에 맞게 수정
3. **MODULE_REGISTRY.yaml**: Core, Shared, Template만 남기고 나머지 모듈은 삭제 후 프로젝트 모듈로 교체
4. **autonomous-developer.mdc**: 문서 경로 참조가 실제 프로젝트 구조와 일치하는지 확인

---

## 자동화 스크립트 (향후)

향후 PowerShell 또는 Bash 스크립트로 자동화할 수 있습니다.
현재는 수동 복사를 권장합니다.

```
# 예시: 향후 자동화 스크립트 형태
# ./scripts/install/inject-framework.ps1 -ProjectPath "C:\MyUnityProject"
# ./scripts/install/inject-framework.sh /path/to/my-unity-project
```
