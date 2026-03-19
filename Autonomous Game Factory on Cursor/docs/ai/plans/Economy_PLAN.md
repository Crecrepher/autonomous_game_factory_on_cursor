# Economy 모듈 PLAN

> **범용 모듈** — dimension: both, genre_tags: [universal, idle, arcade, rpg, tower-defense, simulation]
> 아래 PLAN은 최초 구현 시의 컨텍스트(타워디펜스)를 포함하지만, Economy 모듈 자체는 모든 장르에서 재사용 가능하다.

> Planner 에이전트 산출물 — TASK_QUEUE.yaml `Economy` 태스크

---

## PLAN

```yaml
PLAN:
  module: Economy
  target_files:
    - Assets/Game/Modules/Economy/IEconomy.cs
    - Assets/Game/Modules/Economy/EconomyConfig.cs
    - Assets/Game/Modules/Economy/EconomyRuntime.cs
    - Assets/Game/Modules/Economy/EconomyFactory.cs
    - Assets/Game/Modules/Economy/EconomyBootstrap.cs
    - Assets/Game/Modules/Economy/Tests/Editor/EconomyTests.cs
  dependencies: [UnityEngine, System]
  core_access: false
  risk: medium
  notes: >
    코인 경제 시스템 — 재화 정의, 잔고 관리, 획득/소비 이벤트.
    다수의 후속 모듈(Warriors, DefenseTowers, Fortress, Pickups, HireNodes, Blacksmith, UI, GameManager)이
    Economy에 의존하므로 인터페이스 설계가 핵심이다.
```

---

## 1. 모듈 목적

Economy 모듈은 게임 내 **코인 재화 시스템**의 단일 진실 원천(Single Source of Truth)이다.

- 잔고(Balance) 관리: 초기 잔고, 최대 잔고, 현재 잔고 상태 유지
- 재화 획득(Add): 적 처치 코인 드롭, 기타 보상
- 재화 소비(TrySpend): 고용, 건설, 업그레이드, 대장간 등에서 비용 차감
- 잔고 변동 이벤트: 다른 모듈(UI, Guide 등)이 잔고 변경에 반응할 수 있도록 이벤트 제공
- 잔고 조회: 현재 잔고, 특정 금액 지불 가능 여부 확인

---

## 2. 게임 기획 기반 요구사항

PROJECT_OVERVIEW에서 추출한 Economy 관련 핵심 수치 및 규칙:

| 항목 | 값 | 비고 |
|------|------|------|
| 시작 코인 | 6원 | Config에서 설정 |
| 일반 적 처치 드롭 | 1원 | Enemies/Pickups 모듈에서 Add 호출 |
| 큰 적 처치 드롭 | 30원 | 동일 |
| 전사 고용 비용 | 2→4→6→8→10원 (해금마다 +2) | Warriors/HireNodes에서 TrySpend 호출 |
| 기지 1차 업그레이드 | 10원 | Fortress에서 TrySpend 호출 |
| 기지 2차 업그레이드 | 80원 | 동일 |
| 수비탑 1·2차 | 5원 | DefenseTowers에서 TrySpend 호출 |
| 수비탑 3·4·5·6차 | 30원 | 동일 |
| 대장간 | 50원/회 | Blacksmith에서 TrySpend 호출 |

> Economy 모듈 자체는 위 비용값을 알 필요 없다. 각 소비 모듈이 자기 Config에서 비용을 정의하고, Economy의 `TrySpend(amount)`를 호출한다. Economy는 **잔고 관리와 이벤트 발행**에만 집중한다.

---

## 3. 인터페이스 설계 (`IEconomy.cs`)

```csharp
namespace Game
{
    public interface IEconomy
    {
        void Init();
        void Tick(float deltaTime);

        int Balance { get; }
        bool CanAfford(int amount);
        bool TrySpend(int amount);
        void Add(int amount);

        event System.Action<int> OnBalanceChanged;
        event System.Action<int> OnCoinAdded;
        event System.Action<int> OnCoinSpent;
    }
}
```

### 설계 근거

- `CanAfford(int)`: TrySpend 전 UI에서 버튼 활성/비활성 판단에 사용. TrySpend와 분리하여 SRP 준수.
- `OnBalanceChanged`: 잔고 변동 시 최종 잔고 전달. UI 코인 표시 갱신용.
- `OnCoinAdded` / `OnCoinSpent`: 획득/소비 금액(delta) 전달. 연출(코인 획득 이펙트, 소비 애니메이션) 등에 활용.
- `Tick`: 현재 Economy에는 매 프레임 로직이 없지만, 향후 시간 기반 수입(idle income) 등 확장 시 사용. 빈 구현으로 유지.

---

## 4. Config 설계 (`EconomyConfig.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `_startingBalance` | `int` | 6 | 게임 시작 시 초기 코인 |
| `_maxBalance` | `int` | 99999 | 최대 보유 가능 코인 (오버플로우 방지) |

- ScriptableObject로 에디터에서 밸런스 튜닝 가능
- 다이나믹 대응 모듈(DynamicConfig)과 연동 시 런타임에 Config 값을 오버라이드할 수 있도록 프로퍼티 노출

---

## 5. Runtime 설계 (`EconomyRuntime.cs`)

### 상태

- `_balance` (int): 현재 잔고
- `_config` (EconomyConfig): 주입받은 설정 참조

### 핵심 로직

1. **Init()**: `_balance = _config.StartingBalance` 초기화
2. **Add(int amount)**: 잔고 증가 → 최대치 클램프 → `OnCoinAdded` + `OnBalanceChanged` 발행
3. **TrySpend(int amount)**: 잔고 부족 시 false 반환 → 성공 시 차감 → `OnCoinSpent` + `OnBalanceChanged` 발행
4. **CanAfford(int amount)**: `_balance >= amount` 단순 비교
5. **Tick(float deltaTime)**: 빈 구현 (향후 확장용)

### 제약 준수 체크리스트

- [x] MonoBehaviour 상속 없음 (순수 C#)
- [x] 코루틴/람다/LINQ/foreach/Invoke 사용 안 함
- [x] 매직넘버 없음 — const `MIN_BALANCE = 0`
- [x] `?` 연산자 사용 안 함
- [x] 필드 순서 규칙 준수
- [x] 이벤트는 `System.Action<int>` 사용

---

## 6. Factory 설계 (`EconomyFactory.cs`)

```csharp
namespace Game
{
    public static class EconomyFactory
    {
        public static IEconomy CreateRuntime(EconomyConfig config)
        {
            return new EconomyRuntime(config);
        }
    }
}
```

- static class, Config → IEconomy 반환
- 현재 외부 의존성 없음. 향후 다른 모듈 의존 시 파라미터 추가 가능

---

## 7. Bootstrap 설계 (`EconomyBootstrap.cs`)

- MonoBehaviour 상속 (유일하게 허용)
- `[SerializeField] EconomyConfig _config` 참조
- `Start()`에서 Factory → Init 호출
- 생성된 `IEconomy` 인스턴스를 다른 모듈이 참조할 수 있도록 public 프로퍼티로 노출
- 싱글턴 패턴 사용 금지 — 상위 GameManager나 씬 구성에서 참조 연결

---

## 8. 테스트 설계 (`EconomyTests.cs`)

최소 필수 테스트:

| # | 테스트명 | 검증 내용 |
|---|---------|-----------|
| 1 | `CreateRuntime_WithConfig_ReturnsNonNull` | Factory로 생성 시 null이 아닌지 |
| 2 | `Init_ThenTick_DoesNotThrow` | Init + Tick 호출 시 예외 없는지 |
| 3 | `Init_SetsStartingBalance` | Init 후 Balance == Config.StartingBalance |
| 4 | `Add_IncreasesBalance` | Add(10) 호출 시 잔고 10 증가 |
| 5 | `Add_ClampsToMaxBalance` | MaxBalance 초과 시 클램프 |
| 6 | `TrySpend_WithSufficientBalance_ReturnsTrue` | 잔고 충분 시 true + 차감 |
| 7 | `TrySpend_WithInsufficientBalance_ReturnsFalse` | 잔고 부족 시 false + 잔고 유지 |
| 8 | `CanAfford_ReturnsCorrectResult` | 잔고 비교 정확성 |
| 9 | `OnBalanceChanged_FiresOnAddAndSpend` | 이벤트 발행 검증 |

---

## 9. MODULE_REGISTRY 등록 항목

```yaml
- name: Economy
  path: Assets/Game/Modules/Economy
  editable: true
  risk: medium
  description: >
    코인 경제 시스템. 재화 정의, 잔고 관리, 획득/소비 이벤트.
    다수 모듈(Warriors, DefenseTowers, Fortress, Pickups, HireNodes, Blacksmith, UI, GameManager)의 의존 대상.
  dependencies: [UnityEngine, System]
  config_hint: starting balance, max balance
```

---

## 10. 의존성 분석

### Economy가 의존하는 것

- `UnityEngine` (ScriptableObject, MonoBehaviour for Bootstrap)
- `System` (System.Action 이벤트)
- **다른 게임 모듈에 대한 의존 없음** → 독립 모듈

### Economy에 의존하는 모듈 (후속 영향)

| 모듈 | 사용 방식 |
|------|-----------|
| Warriors | `TrySpend` (고용 비용) |
| DefenseTowers | `TrySpend` (건설 비용) |
| Fortress | `TrySpend` (업그레이드 비용) |
| Pickups | `Add` (코인 줍기) |
| HireNodes | `CanAfford` + `TrySpend` (고용 UI 활성화) |
| Blacksmith | `TrySpend` (대장간 비용) |
| UI | `OnBalanceChanged` 구독 (코인 표시) |
| GameManager | `Balance` 조회 (흐름 제어) |

> 따라서 `IEconomy` 인터페이스의 안정성이 매우 중요하다. 인터페이스 변경은 최소화하고, 확장 시 새 인터페이스(ISP 원칙)를 도입하는 것을 권장한다.

---

## 11. 위험 요소 및 주의사항

| 위험 | 수준 | 대응 |
|------|------|------|
| 인터페이스 변경 시 다수 후속 모듈 영향 | medium | 인터페이스를 최소한으로 설계, ISP 준수 |
| 멀티스레드 안전성 | low | Unity 메인 스레드에서만 호출 전제 |
| 잔고 음수 | low | MIN_BALANCE 클램프로 방지 |
| MaxBalance 오버플로우 | low | Config의 MaxBalance로 클램프 |
| DynamicConfig 연동 | 향후 | 현재 Config 프로퍼티로 충분, 나중에 setter 추가 가능 |

---

## 12. Builder에게 전달할 체크리스트

Builder는 구현 시 아래를 반드시 확인:

- [ ] 네임스페이스 `Game` 사용
- [ ] private 필드: `_camelCase`, public 노출: 프로퍼티 또는 `=>`
- [ ] const: `UPPER_SNAKE_CASE`, 매직넘버 없음
- [ ] 필드 순서: const → static → 이벤트 → 프로퍼티 → 직렬화 → private
- [ ] Runtime: 순수 C#, MonoBehaviour 미상속
- [ ] Config: ScriptableObject 상속, 로직 없음
- [ ] Factory: static class, Config → IEconomy 반환
- [ ] Bootstrap: MonoBehaviour, 얇게 유지
- [ ] Tests: 최소 9개 (위 테스트 목록 참조)
- [ ] GC 유발 코드 없음 (코루틴, 람다, LINQ, foreach 등)
- [ ] for문만 사용 (foreach 금지)
- [ ] `?` 연산자 사용 금지
- [ ] MODULE_REGISTRY.yaml에 Economy 항목 등록
