# Global Learning System — CPIL

프로젝트 간 공유되는 학습 시스템.

---

## 1. 저장소 구조

```
docs/ai/global_learning/
├── GLOBAL_LEARNING_LOG.md       # 시간순 학습 이벤트
├── GLOBAL_RULE_MEMORY.yaml      # 검증된 글로벌 규칙
├── GLOBAL_FAILURE_PATTERNS.md   # 반복되는 실패 패턴
└── GLOBAL_CODING_PATTERNS.md    # 검증된 코딩 패턴
```

---

## 2. 학습 흐름

```
프로젝트 파이프라인 실행
  ↓
Validator 결과 수집
  ↓
Human Fix 이력 수집
  ↓
학습 이벤트 분류
  ├── cross-project → GLOBAL_LEARNING_LOG.md
  └── project-specific → learning/LEARNING_LOG.md (프로젝트 내)
  ↓
규칙 강화
  └── reinforcement_count++ → GLOBAL_RULE_MEMORY.yaml
  ↓
실패 패턴 기록
  └── GLOBAL_FAILURE_PATTERNS.md
```

---

## 3. 글로벌 규칙 (GR-XXX)

현재 등록된 규칙:

| ID | 이름 | 심각도 | 카테고리 |
|----|------|--------|----------|
| GR-001 | RuntimePureCSharp | critical | architecture |
| GR-002 | NoGCInGameLoop | critical | performance |
| GR-003 | SixFileModule | high | structure |
| GR-004 | InterfaceOnlyCommunication | high | architecture |
| GR-005 | NoCyclicDependency | critical | dependency |
| GR-006 | ConfigSourceOfTruth | high | data |

---

## 4. 실패 패턴 (FP-XXX)

| ID | 패턴 | 감지 |
|----|------|------|
| FP-001 | Runtime MonoBehaviour 침투 | ArchitectureRuleValidator |
| FP-002 | Config 필드 중복 | ConfigConflictValidator |
| FP-003 | 누락된 Registry 엔트리 | PipelineSelfHealer |

---

## 5. 코딩 패턴

| 패턴 | 설명 |
|------|------|
| 필드 선언 순서 | const → static → event → property → serialized → private |
| GC-free 루프 | for문만 사용 |
| Animator 해시 | static readonly int으로 캐싱 |
| Update 최적화 | 역수 캐싱으로 나눗셈 제거 |
| 팩토리 패턴 | static class, Create 메서드 |

---

## 6. API

```csharp
// 글로벌 규칙 로드
GlobalRule[] rules = CrossProjectLearning.LoadGlobalRules();

// 학습 이벤트 기록
CrossProjectLearning.AppendLearningEvent(
    "luna_lumberchopper", "Validator Failure",
    "Runtime에서 foreach 사용",
    "GC 할당 위험",
    "for문으로 교체",
    "GR-002",
    "cross-project");

// 피드백 루프 실행
IntelligenceFeedbackLoop.FeedbackReport report =
    IntelligenceFeedbackLoop.RunPostPipelineFeedback("luna_lumberchopper");
```

---

## 7. Pattern Recognition Engine

8개 내장 아키텍처 템플릿:

| 패턴 | 카테고리 | 복잡도 |
|------|----------|--------|
| InventoryPattern | Inventory | medium |
| CurrencyPattern | Economy | low |
| BuffSystemPattern | Combat | medium |
| CropGrowthPattern | Farming | medium |
| SkillTreePattern | Progression | high |
| CombatCorePattern | Combat | high |
| QuestPattern | Quest | medium |
| SaveLoadPattern | Save | medium |

---

## 8. 신규 프로젝트 부트스트랩 예시

**입력:**
```
아늑한 농사 게임. 작물을 심고 키우고 수확.
상점에서 판매하여 골드를 벌고, 새 씨앗 구매.
인벤토리, 레벨업, 퀘스트, 저장 기능.
```

**출력:**
```
Detected Patterns:
  1. CropGrowthPattern (Farming, score: 0.45)
  2. CurrencyPattern (Economy, score: 0.38)
  3. InventoryPattern (Inventory, score: 0.35)
  4. QuestPattern (Quest, score: 0.30)
  5. SaveLoadPattern (Save, score: 0.28)

Proposed Modules:
  Farming:
    [NEW] CropGrowth, FarmPlot, HarvestReward, SeedInventory
  Economy:
    [NEW] CurrencyWallet, ShopSystem, PriceCalculator
  Inventory:
    [REUSE] InventorySystem, ItemStacking
    [NEW] EquipmentSlot
  Quest:
    [NEW] QuestTracker, ObjectiveEvaluator, RewardDistributor
  Save:
    [NEW] SaveSystem, DataSerializer

Total: 16 modules (2 reusable, 14 new)
```
