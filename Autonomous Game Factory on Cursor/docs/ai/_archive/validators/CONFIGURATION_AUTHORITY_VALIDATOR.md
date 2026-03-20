# ConfigurationAuthorityValidator

> Version: 1.0  
> Category: **Warning** (기본), **Error/Blocking** (런타임 안전 위협 시)  
> Registered in: `ValidationRunner.cs`  
> Source: `Assets/Editor/AI/Validators/ConfigurationAuthorityValidator.cs`

## 목적

공유 동작에 대한 **단일 설정 권위(Single Source-of-Truth)**를 보장한다.  
서로 다른 모듈의 Config가 동일한 런타임 동작을 제어하는 필드를 중복 정의하는 것을 탐지한다.

## 기존 ConfigConflictValidator와의 차이

| 항목 | ConfigConflictValidator | ConfigurationAuthorityValidator |
|---|---|---|
| 범위 | Config 파일의 필드명 유사도 | Config + Factory + Bootstrap 전체 |
| 탐지 방식 | 문자열 유사도 | 개념 기반 키워드 매칭 + 정확 일치 |
| Factory/Bootstrap | 검사 안함 | 주입 충돌 탐지 |
| Blocking | 없음 (Warning only) | Factory/Bootstrap 충돌 시 Error |

## 검증 대상

### 1. Config 필드 추출

각 모듈에서 다음 소스를 스캔한다:

| 파일 | 소스 타입 | 추출 대상 |
|---|---|---|
| `<Module>Config.cs` | Config_Serialized | `[SerializeField]` 필드 |
| `<Module>Config.cs` | Config_Public | public 필드 |
| `<Module>Factory.cs` | Factory_Inject | `config.FieldName` 패턴 |
| `<Module>Bootstrap.cs` | Bootstrap_Inject | `config.FieldName` 패턴 |

### 2. 의존 관계 내 충돌 (Cross-Module)

의존 관계가 있는 모듈 쌍에서:

| 조건 | 판정 |
|---|---|
| 정확히 동일한 정규화된 필드명 | **Warning** (Warning→Error if Factory/Bootstrap 관여) |
| 동일한 런타임 개념 키워드 공유 | **Warning** |

### 3. 무관계 모듈 간 충돌

의존 관계가 없는 모듈에서도:

| 조건 | 판정 |
|---|---|
| 정확히 동일한 정규화된 필드명 | **Warning** |

### 4. Blocking 조건

| 조건 | 판정 |
|---|---|
| 충돌 쌍 중 Factory_Inject 또는 Bootstrap_Inject 관여 | **Error** (Blocking) |
| 그 외 Config 간 충돌 | **Warning** |

## 런타임 개념 키워드

```
capacity, max, limit, size, count,
speed, duration, cooldown, interval, delay,
cost, price, rate, damage, range,
health, hp, stack, slot, tier, level
```

필드명에서 개념 키워드를 추출한 뒤, 70%+ 개념 겹침이 있으면 충돌로 판정한다.

## 필드명 정규화

```
_maxStackSize → max_stack_size
MaxCapacity → max_capacity
_cooldownDuration → cooldown_duration
```

Leading underscore 제거 → CamelCase를 snake_case로 분리 → 소문자 변환

## 실패 케이스 예시

### Case 1: 중복 소유권 (Config vs Config)
```
InventorySystemConfig._maxStackSize
ItemStackingConfig._maxStackSize
→ Warning: "Exact duplicate config field name across dependent modules"
→ Recommended source-of-truth: ItemStackingConfig
```

### Case 2: 개념 충돌 (capacity 개념)
```
InventorySystemConfig._slotCapacity (concepts: [capacity, slot])
ItemStackingConfig._stackCapacity (concepts: [capacity, stack])
→ Warning: "Config fields controlling similar concept"
```

### Case 3: Factory 주입 충돌 (Blocking)
```
InventorySystemConfig._maxSize (Config_Serialized)
ItemStackingFactory → config.maxSize (Factory_Inject)
→ Error: "Configuration authority conflict... Factory_Inject involved"
→ Blocking: Factory/Bootstrap 관여로 런타임 안전 위협
```

### Case 4: 무관계 모듈 간 동일 필드
```
EconomyConfig._maxBalance
FortressConfig._maxBalance
→ Warning: "Identical config field name in unrelated modules"
```

## 권위 결정 로직

```
1. Config_Serialized/Config_Public 소스가 있는 쪽 우선
2. 동일하면 먼저 등록된 모듈(알파벳순) 우선
3. 의존 관계가 있으면 피의존(하위) 모듈 우선
```

## 아키텍처 수정 권장사항

충돌 발견 시 다음 순서로 해결:

1. **통합**: 하나의 Config로 필드를 통합
2. **참조**: 의존하는 쪽에서 다른 모듈의 Config를 참조
3. **위임**: Runtime에서 Config를 주입받아 사용 (Factory 패턴)
4. **분리**: 완전히 다른 개념이면 필드명을 명확히 구분

## 통합 위치

- `ValidationRunner.cs`에서 `ModuleReuseIntegrityValidator` 이후 실행
- 결과는 `AIValidationReport.json`에 기록
- Factory/Bootstrap 충돌은 Error로 Console에 출력

## 재테스트 시나리오

1. 의존 관계 있는 두 모듈 Config에 동일 필드명 추가 → Warning 확인
2. Factory에서 다른 모듈 Config와 동일 필드 참조 → Error 확인
3. 무관계 모듈에 동일 필드명 추가 → Warning 확인
4. 개념 키워드(max, capacity) 공유 필드 추가 → Warning 확인
