# Guide 모듈 PLAN

> Planner 에이전트 산출물 — TASK_QUEUE.yaml `Guide` 태스크

---

## PLAN

```yaml
PLAN:
  module: Guide
  target_files:
    - Assets/Game/Modules/Guide/IGuide.cs
    - Assets/Game/Modules/Guide/GuideConfig.cs
    - Assets/Game/Modules/Guide/GuideRuntime.cs
    - Assets/Game/Modules/Guide/GuideFactory.cs
    - Assets/Game/Modules/Guide/GuideBootstrap.cs
    - Assets/Game/Modules/Guide/Tests/Editor/GuideTests.cs
  dependencies: [UnityEngine, System]
  core_access: false
  risk: low
  notes: >
    화살표 가이드, 조건부 노출, 유저 유도.
    독립 모듈로 다른 모듈의 이벤트를 구독하여 가이드 표시/숨김.
```

---

## 1. 모듈 목적

Guide 모듈은 **유저 유도 화살표 가이드 시스템**을 담당한다.

- 가이드 등록: 가이드 ID + 대상 위치 등록
- 조건부 노출: 조건 충족 시 가이드 표시, 해제 시 숨김
- 상태 관리: 현재 활성 가이드 추적

---

## 2. 인터페이스 설계 (`IGuide.cs`)

```csharp
namespace Game
{
    public interface IGuide
    {
        void Init();
        void Tick(float deltaTime);

        void ShowGuide(int guideId);
        void HideGuide(int guideId);
        void HideAll();
        bool IsGuideActive(int guideId);

        event System.Action<int> OnGuideShown;
        event System.Action<int> OnGuideHidden;
    }
}
```

---

## 3. Config 설계 (`GuideConfig.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `_maxActiveGuides` | `int` | 3 | 동시 활성 가이드 최대 수 |

---

## 4. 테스트 설계

| # | 테스트명 | 검증 내용 |
|---|---------|-----------|
| 1 | `CreateRuntime_WithConfig_ReturnsNonNull` | Factory 생성 |
| 2 | `Init_ThenTick_DoesNotThrow` | 기본 동작 |
| 3 | `ShowGuide_ActivatesGuide` | 가이드 활성화 |
| 4 | `HideGuide_DeactivatesGuide` | 가이드 비활성화 |
| 5 | `HideAll_ClearsAllGuides` | 전체 비활성화 |
