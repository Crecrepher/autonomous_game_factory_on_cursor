# Enemies 모듈 PLAN

> Planner 에이전트 산출물 — TASK_QUEUE.yaml `Enemies` 태스크

---

## PLAN

```yaml
PLAN:
  module: Enemies
  target_files:
    - Assets/Game/Modules/Enemies/IEnemies.cs
    - Assets/Game/Modules/Enemies/EnemiesConfig.cs
    - Assets/Game/Modules/Enemies/EnemiesRuntime.cs
    - Assets/Game/Modules/Enemies/EnemiesFactory.cs
    - Assets/Game/Modules/Enemies/EnemiesBootstrap.cs
    - Assets/Game/Modules/Enemies/Tests/Editor/EnemiesTests.cs
  dependencies: [UnityEngine, System]
  core_access: false
  risk: medium
  notes: >
    적 스폰, 웨이브 패턴, 일반/큰 적 AI, HP 관리.
    Pickups 모듈에서 드롭 코인량 참조, GameManager에서 웨이브 제어.
```

---

## 1. 모듈 목적

Enemies 모듈은 **적 캐릭터 스폰·웨이브 관리·HP·사망 이벤트**를 담당한다.

- 스폰: 웨이브 패턴에 따라 적 생성
- HP 관리: 일반 적 / 큰 적 HP, 피격 처리
- 사망 이벤트: 처치 시 코인 드롭량 정보 전달 (Pickups/Economy와 연동)
- 웨이브 관리: 등장 패턴, 방향(우측/정면), 리스폰 주기

---

## 2. 인터페이스 설계 (`IEnemies.cs`)

```csharp
namespace Game
{
    public interface IEnemies
    {
        void Init();
        void Tick(float deltaTime);

        int AliveCount { get; }
        int TotalSpawned { get; }
        int TotalKilled { get; }

        void StartSpawning();
        void StopSpawning();
        void SetSpawnRate(float enemiesPerSecond);

        event System.Action<int> OnEnemyKilled;
        event System.Action<int> OnWaveStarted;
    }
}
```

### 설계 근거

- `OnEnemyKilled(int coinDrop)`: 적 처치 시 드롭 코인량 전달 → Pickups/Economy에서 구독
- `OnWaveStarted(int waveIndex)`: 웨이브 시작 이벤트 → GameManager에서 구독
- `SetSpawnRate`: DynamicConfig에서 리스폰 속도 조절

---

## 3. Config 설계 (`EnemiesConfig.cs`)

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `_normalEnemyHp` | `int` | 4 | 일반 적 HP |
| `_normalEnemyCoinDrop` | `int` | 1 | 일반 적 처치 드롭 코인 |
| `_bigEnemyHp` | `int` | 20 | 큰 적 HP |
| `_bigEnemyCoinDrop` | `int` | 30 | 큰 적 처치 드롭 코인 |
| `_spawnInterval` | `float` | 2f | 스폰 간격 (초) |
| `_maxAliveEnemies` | `int` | 20 | 동시 생존 최대 적 수 |

---

## 4. Runtime 설계 (`EnemiesRuntime.cs`)

### 상태

- `_spawnTimer` (float): 스폰 타이머
- `_isSpawning` (bool): 스폰 활성화 여부
- `_aliveCount` (int): 현재 생존 적 수
- `_totalSpawned` (int): 총 스폰 수
- `_totalKilled` (int): 총 처치 수
- `_inverseSpawnInterval` (float): 스폰 간격 역수 캐싱

### 핵심 로직

1. **Init()**: Config 값으로 초기화
2. **Tick(float)**: 스폰 타이머 갱신, 조건 충족 시 스폰
3. **StartSpawning/StopSpawning**: 스폰 제어
4. 적 사망 처리 → OnEnemyKilled 이벤트 발행

---

## 5. 테스트 설계

| # | 테스트명 | 검증 내용 |
|---|---------|-----------|
| 1 | `CreateRuntime_WithConfig_ReturnsNonNull` | Factory 생성 |
| 2 | `Init_ThenTick_DoesNotThrow` | 기본 동작 |
| 3 | `Init_SetsDefaultValues` | 초기값 확인 |
| 4 | `StartSpawning_EnablesSpawning` | 스폰 활성화 |
| 5 | `StopSpawning_DisablesSpawning` | 스폰 비활성화 |

---

## 6. 위험 요소

| 위험 | 수준 | 대응 |
|------|------|------|
| 웨이브 패턴 복잡도 | medium | 현재는 단순 타이머 기반, 향후 웨이브 데이터 SO 확장 |
| 적 오브젝트 풀링 | medium | Runtime에서 논리적 관리, 실제 풀링은 Bootstrap/View 계층 |
| GameManager 연동 | low | 이벤트 기반으로 느슨한 결합 |
