# DynamicConfig 모듈 PLAN

> Planner 에이전트 산출물 — TASK_QUEUE.yaml `DynamicConfig` 태스크

---

## PLAN

```yaml
PLAN:
  module: DynamicConfig
  target_files:
    - Assets/Game/Modules/DynamicConfig/IDynamicConfig.cs
    - Assets/Game/Modules/DynamicConfig/DynamicConfigConfig.cs
    - Assets/Game/Modules/DynamicConfig/DynamicConfigRuntime.cs
    - Assets/Game/Modules/DynamicConfig/DynamicConfigFactory.cs
    - Assets/Game/Modules/DynamicConfig/DynamicConfigBootstrap.cs
    - Assets/Game/Modules/DynamicConfig/Tests/Editor/DynamicConfigTests.cs
  dependencies: [UnityEngine, System]
  core_access: false
  risk: low
  notes: >
    다이나믹 대응 설정값 관리. 런타임 밸런스 조절.
    각 모듈의 setter를 통해 속도/비용/HP 등을 동적으로 변경.
```

---

## 1. 모듈 목적

DynamicConfig 모듈은 **런타임 밸런스 조절 시스템**을 담당한다.

- 키-값 기반 설정 관리
- float/int 값 저장 및 조회
- 값 변경 이벤트 발행

---

## 2. 인터페이스 설계 (`IDynamicConfig.cs`)

```csharp
namespace Game
{
    public interface IDynamicConfig
    {
        void Init();
        void Tick(float deltaTime);

        float GetFloat(int key, float defaultValue);
        int GetInt(int key, int defaultValue);
        void SetFloat(int key, float value);
        void SetInt(int key, int value);
        bool HasKey(int key);

        event System.Action<int> OnValueChanged;
    }
}
```

---

## 3. Config 설계 (`DynamicConfigConfig.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `_maxEntries` | `int` | 64 | 최대 설정 항목 수 |

---

## 4. 테스트 설계

| # | 테스트명 | 검증 내용 |
|---|---------|-----------|
| 1 | `CreateRuntime_WithConfig_ReturnsNonNull` | Factory 생성 |
| 2 | `Init_ThenTick_DoesNotThrow` | 기본 동작 |
| 3 | `SetFloat_GetFloat_ReturnsValue` | float 저장/조회 |
| 4 | `SetInt_GetInt_ReturnsValue` | int 저장/조회 |
| 5 | `OnValueChanged_FiresOnSet` | 이벤트 발행 |
