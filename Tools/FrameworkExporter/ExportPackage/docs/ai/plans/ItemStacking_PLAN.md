# ItemStacking Module — Implementation Plan

## 의존성 그래프
- ItemStacking → 없음 (독립 모듈)
- 빌드 순서: 즉시 실행 가능

## 구현 계획

### 1. IItemStacking.cs (인터페이스)
- Init(), Tick(float deltaTime)
- Push(int itemId) → bool
- Pop() → int (-1 if empty)
- PopAll(int[] outBuffer) → int (count)
- Peek() → int (-1 if empty)
- IsFull, IsEmpty, Count, MaxSize 프로퍼티
- OnPushed, OnPopped, OnStackCleared 이벤트

### 2. ItemStackingConfig.cs (ScriptableObject)
- _maxStackSize (int, default 10)
- 프로퍼티로 노출

### 3. ItemStackingRuntime.cs (순수 C#)
- IItemStacking 구현
- 고정 크기 int[] 배열로 스택 구현
- _count로 현재 크기 추적
- Push: _count < maxSize 검사 → 배열에 추가 → 이벤트 발화
- Pop: _count > 0 검사 → 배열에서 제거 → 이벤트 발화
- PopAll: for문으로 outBuffer에 복사 → _count = 0 → OnStackCleared 발화
- Peek: _count > 0이면 _stack[_count - 1], 아니면 -1

### 4. ItemStackingFactory.cs (static class)
- CreateRuntime(ItemStackingConfig) → IItemStacking

### 5. ItemStackingBootstrap.cs (MonoBehaviour)
- [SerializeField] ItemStackingConfig
- Start()에서 Factory 호출 → Init()

### 6. Tests/Editor/ItemStackingTests.cs
- 최소 6개 테스트:
  1. CreateRuntime 비null 확인
  2. Init → Tick 예외 없음
  3. Push → Count 증가 확인
  4. Pop → 올바른 아이템 반환 확인
  5. Full일 때 Push false 확인
  6. Empty일 때 Pop -1 확인

## 회피할 반복 실수
- Runtime에 MonoBehaviour 상속 금지 (REC-001)
- foreach/LINQ 사용 금지 (REC-002)
- MODULE_REGISTRY 의존성 미선언 금지 (REC-003)
- `?` 연산자 사용 금지
- 매직넘버 금지
