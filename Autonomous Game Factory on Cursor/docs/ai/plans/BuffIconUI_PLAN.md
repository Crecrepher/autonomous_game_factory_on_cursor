# BuffIconUI 모듈 PLAN

> Planner 에이전트 산출물 — TASK_QUEUE.yaml `BuffIconUI` 태스크

---

## PLAN

```yaml
PLAN:
  module: BuffIconUI
  target_files:
    - Assets/Game/Modules/BuffIconUI/IBuffIconUI.cs
    - Assets/Game/Modules/BuffIconUI/BuffIconUIConfig.cs
    - Assets/Game/Modules/BuffIconUI/BuffIconUIRuntime.cs
    - Assets/Game/Modules/BuffIconUI/BuffIconUIFactory.cs
    - Assets/Game/Modules/BuffIconUI/BuffIconUIBootstrap.cs
    - Assets/Game/Modules/BuffIconUI/Tests/Editor/BuffIconUITests.cs
  dependencies: [UnityEngine, System, StatusEffect]
  core_access: false
  risk: low
  notes: >
    상태이상 아이콘 UI 표시. StatusEffect 모듈(done)에 의존.
```

---

## 1. 모듈 목적

BuffIconUI 모듈은 **상태이상 아이콘 UI 표시 시스템**을 담당한다.

- 버프/디버프 아이콘 슬롯 관리
- 상태이상 추가/제거 시 UI 갱신 이벤트
- 남은 시간 표시 데이터 관리

---

## 2. 인터페이스 설계 (`IBuffIconUI.cs`)

```csharp
namespace Game
{
    public interface IBuffIconUI
    {
        void Init();
        void Tick(float deltaTime);

        int ActiveIconCount { get; }
        void AddIcon(int effectId, float duration);
        void RemoveIcon(int effectId);
        void ClearAll();

        event System.Action<int> OnIconAdded;
        event System.Action<int> OnIconRemoved;
    }
}
```

---

## 3. Config 설계 (`BuffIconUIConfig.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `_maxIcons` | `int` | 8 | 최대 동시 표시 아이콘 수 |

---

## 4. 테스트 설계

| # | 테스트명 | 검증 내용 |
|---|---------|-----------|
| 1 | `CreateRuntime_WithConfig_ReturnsNonNull` | Factory 생성 |
| 2 | `Init_ThenTick_DoesNotThrow` | 기본 동작 |
| 3 | `AddIcon_IncreasesCount` | 아이콘 추가 |
| 4 | `RemoveIcon_DecreasesCount` | 아이콘 제거 |
| 5 | `ClearAll_ResetsCount` | 전체 초기화 |
