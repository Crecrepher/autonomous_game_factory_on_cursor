# 규칙과 검증을 프로젝트에 맞게 진화시키기

프레임워크를 프로젝트에 적용한 후, 규칙과 검증을 점진적으로 개선할 수 있다.

---

## 1. 진화 가능한 요소

| 요소 | 위치 | 진화 방식 |
|------|------|-----------|
| Cursor 규칙 | `.cursor/rules/autonomous-developer.mdc` | 행동 제약 추가/수정 |
| 코딩 규칙 | `Docs/ai/CODING_RULES.md` | 자주 하는 실수, 프로젝트 특화 규칙 추가 |
| 모듈 레지스트리 | `Docs/ai/MODULE_REGISTRY.yaml` | 새 모듈 등록, 의존성 업데이트 |
| 검증기 | `Assets/Editor/AI/Validators/` | 새로운 검증 규칙 추가 |
| 프로젝트 개요 | `Docs/ai/PROJECT_OVERVIEW.md` | 기능 도메인, 아키텍처 원칙 갱신 |

---

## 2. 새 검증기 추가

1. `IModuleValidator`를 구현하는 클래스를 `Assets/Editor/AI/Validators/`에 생성
2. `ValidationRunner.cs`의 `RunAllValidators`에 등록:

```csharp
IModuleValidator[] validators = new IModuleValidator[]
{
    new CompileErrorValidator(),
    new ForbiddenFolderValidator(),
    new ModuleStructureValidator(),
    new ArchitectureRuleValidator(),
    new MyNewValidator()          // 추가
};
```

---

## 3. 진화 원칙

1. **코드가 스스로 학습하지 않는다** — 사람이 문서·규칙·검증을 수정
2. **점진적으로** — 한두 개씩 추가하며 효과 검증
3. **예측 가능하게** — 모든 규칙은 명시적, AI가 읽을 수 있는 형태
