# InventorySystem Module Spec

## 개요

슬롯 기반 인벤토리 시스템. ItemStacking 모듈을 슬롯 단위 스택으로 재사용하여
아이템 추가/제거/조회 로직을 제공한다.
동일 itemId는 같은 슬롯에 스택되고, 스택이 가득 차면 새 슬롯에 오버플로우된다.

## 모듈 정보

- **모듈명**: InventorySystem
- **경로**: Assets/Game/Modules/InventorySystem
- **feature_group**: inventory-system
- **의존성**: [UnityEngine, System, ItemStacking]
- **위험도**: low

## 핵심 요구사항

### 데이터 구조
- 고정 크기 슬롯 배열 (maxSlots는 Config에서 설정)
- 각 슬롯은 itemId + ItemStackingRuntime 조합
- 빈 슬롯의 itemId = EMPTY_SLOT (-1)
- GC 할당 없음

### 공개 API (인터페이스)

- `void Init()` — 초기화 (슬롯 배열 + 각 슬롯의 ItemStacking Init)
- `EInventorySystemResult Add(int itemId, int amount)` — 아이템 추가. 성공/부분성공/실패 반환
- `EInventorySystemResult Remove(int itemId, int amount)` — 아이템 제거. 성공/부분성공/실패 반환
- `int GetCount(int itemId)` — 특정 아이템의 전체 보유 수량
- `bool Has(int itemId, int amount)` — 특정 아이템을 amount개 이상 보유 중인지
- `int TotalItemCount { get; }` — 전체 아이템 수
- `int UsedSlotCount { get; }` — 사용 중인 슬롯 수
- `int MaxSlots { get; }` — 최대 슬롯 수
- `bool IsFull { get; }` — 모든 슬롯이 사용 중이고 각 스택도 가득 찬 상태
- `int GetSlotItemId(int slotIndex)` — 슬롯의 아이템 ID 조회
- `int GetSlotCount(int slotIndex)` — 슬롯의 현재 수량 조회
- `event Action<int, int> OnItemAdded` — (itemId, addedAmount) 아이템 추가됨
- `event Action<int, int> OnItemRemoved` — (itemId, removedAmount) 아이템 제거됨
- `event Action OnInventoryFull` — 인벤토리가 완전히 가득 참

### 결과 열거형 (EInventorySystemResult)
- `Success` — 요청 수량 전부 처리됨
- `Partial` — 일부만 처리됨 (공간 부족 또는 수량 부족)
- `Failed` — 전혀 처리 불가

### Config (ScriptableObject)
- `maxSlots` (int) — 최대 슬롯 수. 기본값 20
- `maxStackSizePerSlot` (int) — 슬롯당 최대 스택 크기. 기본값 10

### Runtime 규칙
- MonoBehaviour 상속 금지
- foreach, LINQ, 람다, 코루틴, Invoke 사용 금지
- `?` (null conditional) 사용 금지
- 매직넘버 금지, const UPPER_SNAKE_CASE
- 배열 기반, List 사용 금지
- ItemStacking의 IItemStacking 인터페이스로만 접근

### Factory
- static class
- `CreateRuntime(InventorySystemConfig config)` → `IInventorySystem` 반환

### Bootstrap
- MonoBehaviour (얇게)
- SerializeField로 Config 참조
- Start()에서 Factory 호출 → Init()

### Tests (최소 2개)
1. Factory로 Runtime 생성 후 null이 아닌지
2. Add 후 GetCount가 정확한지
3. Remove 후 GetCount가 감소하는지
4. 오버플로우 시 새 슬롯 사용
5. 같은 아이템의 스택이 가득 차면 새 슬롯 오버플로우
6. 인벤토리 가득 찼을 때 Add가 Partial/Failed 반환
7. Has로 보유 확인
8. 빈 인벤토리에서 Remove가 Failed 반환
