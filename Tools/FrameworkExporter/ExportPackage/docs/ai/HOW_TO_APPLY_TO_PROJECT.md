# Unity 프로젝트에 프레임워크 적용하기

## 1. 복사

프레임워크 구조가 Unity 프로젝트와 동일하므로 그대로 복사한다.

```bash
cp -r ai-dev-framework/* MyGameProject/
```

복사되는 것:

| 경로 | 내용 |
|------|------|
| `.cursor/rules/` | AI 에이전트 행동 규칙 |
| `Assets/Editor/AI/` | 모듈 검증 도구 |
| `Assets/Game/Modules/Template/` | 표준 모듈 템플릿 |
| `Docs/ai/*.example.*` | AI 문서 예시 |

---

## 2. 예시 파일 활성화

`Docs/ai/` 안의 `.example` 파일에서 `.example`을 제거하고 프로젝트에 맞게 수정한다.

| 변경 전 | 변경 후 |
|---------|---------|
| `PROJECT_OVERVIEW.example.md` | `PROJECT_OVERVIEW.md` |
| `CODING_RULES.example.md` | `CODING_RULES.md` |
| `MODULE_REGISTRY.example.yaml` | `MODULE_REGISTRY.yaml` |
| `AI_DEVELOPMENT_LOOP.example.md` | `AI_DEVELOPMENT_LOOP.md` |

---

## 3. 네임스페이스 조정

프레임워크의 기본 네임스페이스는 `Game` 및 `Game.Editor.AI`.
프로젝트에 맞게 변경한다:

- `Game` → `YourProjectName`
- `Game.Editor.AI` → `YourProjectName.Editor.AI`

---

## 4. 검증 도구 테스트

Unity 에디터에서:
1. `Tools > AI > Update Core Baseline` — Core 폴더 기준선 생성
2. `Tools > AI > Validate Generated Modules` — 검증 실행
3. Console에서 결과 확인

---

## 5. 적용 후 구조

```
MyGameProject/
  .cursor/rules/autonomous-developer.mdc
  Assets/
    Editor/AI/            ← 검증 도구 (그대로)
    Game/
      Core/               ← 프로젝트별 부트스트랩, 입력, 카메라, 세이브
      Shared/             ← 공용 인터페이스, enum, 상수
      Modules/
        Template/         ← 모듈 템플릿
        <MyModule>/       ← 프로젝트 모듈들
  Docs/ai/
    PROJECT_OVERVIEW.md   ← .example 제거 후 수정
    CODING_RULES.md
    MODULE_REGISTRY.yaml
    AI_DEVELOPMENT_LOOP.md
```

---

## 6. 주의사항

- `ModuleStructureValidator.cs`에 `Docs/ai/MODULE_REGISTRY.yaml` 경로가 하드코딩되어 있다. 구조가 같으므로 수정 불필요.
- `autonomous-developer.mdc`의 경로 참조도 동일 구조이므로 수정 불필요.
- 검증 도구는 Unity Editor 전용. 빌드에 포함되지 않는다.
