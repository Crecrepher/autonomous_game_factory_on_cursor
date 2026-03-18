# ItemStacking Module Spec

## 개요

아이템 스태킹/언스태킹 시스템. 캐릭터가 아이템을 차곡차곡 쌓고(Stack), 특정 지점에서 내려놓는(Unstack) 메카닉을 관리한다.

## 모듈 정보

- **모듈명**: ItemStacking
- **경로**: Assets/Game/Modules/ItemStacking
- **feature_group**: item-stacking
- **의존성**: [UnityEngine, System]
- **위험도**: low

## 핵심 요구사항

### 데이터 구조
- 고정 크기 배열 기반 스택 (maxSize는 Config에서 설정)
- int 아이템 ID로 관리
- GC 할당 없음

### 공개 API (인터페이스)
- `void Init()` — 초기화
- `void Tick(float deltaTime)` — 프레임 업데이트 (필요 시 애니메이션 타이밍 등)
- `bool Push(int itemId)` — 아이템 푸시. 가득 차면 false
- `int Pop()` — 최상단 아이템 팝. 비어있으면 -1
- `int PopAll(int[] outBuffer)` — 모든 아이템 팝하여 outBuffer에 담고 개수 반환
- `int Peek()` — 최상단 아이템 확인 (제거하지 않음). 비어있으면 -1
- `bool IsFull { get; }` — 스택이 가득 찼는지
- `bool IsEmpty { get; }` — 스택이 비었는지
- `int Count { get; }` — 현재 쌓인 아이템 수
- `int MaxSize { get; }` — 최대 스택 크기
- `event Action<int> OnPushed` — 아이템 추가됨
- `event Action<int> OnPopped` — 아이템 제거됨
- `event Action OnStackCleared` — 스택 전체 비워짐

### Config (ScriptableObject)
- `maxStackSize` (int) — 최대 스택 크기. 기본값 10

### Runtime 규칙
- MonoBehaviour 상속 금지
- foreach, LINQ, 람다, 코루틴, Invoke 사용 금지
- `?` (null conditional) 사용 금지
- 매직넘버 금지, const UPPER_SNAKE_CASE
- 배열 기반, List 사용 금지

### Factory
- static class
- `CreateRuntime(ItemStackingConfig config)` → `IItemStacking` 반환

### Bootstrap
- MonoBehaviour (얇게)
- SerializeField로 Config 참조
- Start()에서 Factory 호출 → Init()

### Tests (최소 2개)
1. Factory로 Runtime 생성 후 null이 아닌지
2. Init → Tick 시 예외 없는지
3. Push/Pop 정상 동작
4. Push 가득 찼을 때 false 반환
5. Pop 비었을 때 -1 반환
6. PopAll 동작 확인
