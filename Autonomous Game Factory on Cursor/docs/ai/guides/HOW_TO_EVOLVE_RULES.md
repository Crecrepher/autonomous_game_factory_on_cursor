# 규칙과 검증을 프로젝트에 맞게 진화시키기

프레임워크를 프로젝트에 적용한 후, AI 개발 루프가 반복되면서 규칙과 검증을 **점진적으로 개선**할 수 있습니다.
이 문서는 그 방법을 설명합니다.

---

## 1. 진화 가능한 요소

> 아래 경로들은 프레임워크를 주입한 **대상 Unity 프로젝트** 기준입니다.

| 요소 | 대상 프로젝트 내 위치 | 진화 방식 |
|------|------|-----------|
| **Cursor 규칙** | `.cursor/rules/autonomous-developer.mdc` | 행동 제약 추가/수정 |
| **코딩 규칙 문서** | `Docs/ai/CODING_RULES.md` | 자주 하는 실수, 프로젝트 특화 규칙 추가 |
| **모듈 레지스트리** | `Docs/ai/MODULE_REGISTRY.yaml` | 새 모듈 등록, 의존성 업데이트 |
| **검증기** | `Assets/Editor/AI/Validators/` | 새로운 검증 규칙 추가 |
| **프로젝트 개요** | `Docs/ai/PROJECT_OVERVIEW.md` | 기능 도메인, 아키텍처 원칙 갱신 |

---

## 2. Cursor 규칙 진화

### 새 규칙 추가 시기

- AI가 반복적으로 같은 실수를 하는 경우
- 새로운 제약이 필요한 경우 (예: "특정 폴더에 파일 생성 금지")
- 워크플로 단계를 추가하거나 세분화해야 할 경우

### 규칙 추가 방법

`.cursor/rules/autonomous-developer.mdc`에 새 섹션을 추가합니다:

```markdown
## N. 새 규칙 제목

- 규칙 설명...
- 위반 시 처리 방식...
```

### 규칙 파일 분리

규칙이 너무 커지면 별도의 `.mdc` 파일로 분리할 수 있습니다:

```
.cursor/rules/
  autonomous-developer.mdc     ← 핵심 워크플로
  naming-conventions.mdc       ← 네이밍 규칙 전용
  performance-rules.mdc        ← 성능 규칙 전용
```

---

## 3. 검증기 진화

### 새 검증기 추가

1. `IModuleValidator` 인터페이스를 구현하는 새 클래스를 대상 프로젝트의 `Assets/Editor/AI/Validators/`에 생성합니다.

2. 대상 프로젝트의 `ValidationRunner.cs` 안 `RunSyncValidators` 메서드에 새 검증기를 등록합니다:

```csharp
static void RunSyncValidators(ValidationReport report)
{
    IModuleValidator[] validators = new IModuleValidator[]
    {
        new ForbiddenFolderValidator(),
        new ModuleStructureValidator(),
        new ArchitectureRuleValidator(),
        new MyNewValidator()          // 새 검증기 추가
    };
    for (int i = 0; i < validators.Length; i++)
        validators[i].Validate(report);
}
```

### 검증기 아이디어 예시

| 검증기 | 검사 내용 |
|--------|-----------|
| `NamingConventionValidator` | private 필드가 `_`로 시작하는지 |
| `DependencyValidator` | MODULE_REGISTRY의 dependencies 선언과 실제 using 문이 일치하는지 |
| `SingletonValidator` | MonoBehaviour 상속 클래스에 싱글턴 패턴이 없는지 |
| `GCAllocationValidator` | foreach, LINQ, 코루틴 사용 여부 |

---

## 4. 문서 진화

### 코딩 규칙 문서

AI가 반복적으로 위반하는 패턴이 있으면, `CODING_RULES.md`에 "자주 하는 실수" 섹션을 추가합니다:

```markdown
## 자주 하는 실수 (AI 주의사항)

- ❌ Update에서 GetComponent 호출
- ❌ List를 사용하지만 크기가 고정인 경우
- ❌ 람다식으로 이벤트 핸들러 등록
```

### 모듈 레지스트리

새 모듈을 만들 때마다 `MODULE_REGISTRY.yaml`에 항목을 추가합니다:

```yaml
  - name: NewModule
    path: Assets/Game/Modules/NewModule
    editable: true
    risk: medium
    description: 모듈 설명
    dependencies: [Core, Shared]
```

---

## 5. 진화 원칙

1. **코드가 스스로 학습하지 않는다** — 개선은 사람이 문서·규칙·검증을 수정하는 방식으로만 이루어집니다.
2. **점진적으로** — 한 번에 많은 규칙을 바꾸지 않고, 한두 개씩 추가하며 효과를 검증합니다.
3. **예측 가능하게** — 모든 규칙은 명시적이고, AI가 읽을 수 있는 형태여야 합니다.
4. **프레임워크 원본과 분리** — 프로젝트에서 진화시킨 규칙/검증기는 프로젝트에만 적용됩니다. 범용적이라면 프레임워크 저장소에 PR로 기여하세요.
