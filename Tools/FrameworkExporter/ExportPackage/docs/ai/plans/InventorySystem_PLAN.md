# InventorySystem — PLAN

## 대상 모듈
- **InventorySystem** (Assets/Game/Modules/InventorySystem/)

## 의존성
- ItemStacking (IItemStacking, ItemStackingConfig, ItemStackingFactory)

## 회피할 반복 실수 (RULE_MEMORY / RECURRING_MISTAKES)
- RM-0001: Runtime에 MonoBehaviour 상속 금지
- RM-0002: Config는 ScriptableObject 상속 필수
- RM-0003: 의존 모듈은 MODULE_REGISTRY.yaml dependencies에 선언 필수
- RM-0004: foreach 금지, for문만
- RM-0008: LINQ 금지
- RM-0007: 모듈 루트에는 규정된 파일만 존재
- RM-0015: 테스트 최소 1개 필수
- REC-001: Runtime에 MonoBehaviour 상속 추가 금지
- REC-002: foreach/LINQ 습관적 사용 금지
- REC-003: 모듈 간 미선언 의존성 참조 금지

## 구현 단계

### 1. EInventorySystemResult 열거형
- 파일: `EInventorySystemResult.cs` (모듈 루트, E로 시작하는 enum 허용)
- Success, Partial, Failed

### 2. IInventorySystem 인터페이스
- 파일: `IInventorySystem.cs`
- Add, Remove, GetCount, Has, TotalItemCount, UsedSlotCount, MaxSlots, IsFull
- GetSlotItemId, GetSlotCount
- 이벤트: OnItemAdded, OnItemRemoved, OnInventoryFull

### 3. InventorySystemConfig
- 파일: `InventorySystemConfig.cs`
- ScriptableObject 상속
- _maxSlots, _maxStackSizePerSlot
- 내부에서 ItemStackingConfig를 슬롯 수만큼 생성할 수 있도록 maxStackSizePerSlot 제공

### 4. InventorySystemRuntime
- 파일: `InventorySystemRuntime.cs`
- 순수 C# (MonoBehaviour 금지)
- 슬롯 배열: int[] _slotItemIds + IItemStacking[] _slots
- Add: 같은 itemId 슬롯 찾기 → 빈 공간에 Push → 가득 차면 새 슬롯 할당 → 슬롯 없으면 Partial/Failed
- Remove: 같은 itemId 슬롯에서 Pop → 빈 슬롯 정리
- GetCount: 모든 슬롯 순회하며 같은 itemId의 Count 합산
- Has: GetCount >= amount

### 5. InventorySystemFactory
- 파일: `InventorySystemFactory.cs`
- static class
- CreateRuntime(InventorySystemConfig) → IInventorySystem

### 6. InventorySystemBootstrap
- 파일: `InventorySystemBootstrap.cs`
- MonoBehaviour, 얇게
- SerializeField로 Config
- Start에서 Factory → Init

### 7. InventorySystemTests
- 파일: `Tests/Editor/InventorySystemTests.cs`
- 최소 8개 테스트 케이스

## 파일 체크리스트
```
Assets/Game/Modules/InventorySystem/
├── EInventorySystemResult.cs
├── IInventorySystem.cs
├── InventorySystemConfig.cs
├── InventorySystemRuntime.cs
├── InventorySystemFactory.cs
├── InventorySystemBootstrap.cs
└── Tests/Editor/
    └── InventorySystemTests.cs
```
