# Module Discovery — Autonomous Game Factory v2

기존 모듈 탐색 및 유사도 분석 명세.
Queue Generator가 신규 모듈을 만들기 전에 기존 코드베이스에서 재사용 가능한 후보를 찾는다.

---

## 1. 목적

| 문제 | 해결 |
|------|------|
| 동일 책임의 모듈을 중복 생성 | 기존 모듈을 먼저 탐색 |
| 기존 모듈을 몰라서 처음부터 구현 | 후보 목록과 유사도 점수 제공 |
| 의존성 그래프 파편화 | 재사용으로 의존성 단순화 |

---

## 2. 실행 시점

```
Queue Generator 내부:
  [A] Feature Intake
  [B] Learning Scan
  [B.5] ★ Module Discovery ★  ← 여기
  [C] Decomposition (Discovery 결과 반영)
  [D] Dependency Inference
  ...
```

Discovery는 Decomposition **직전**에 실행된다.
Decomposition은 Discovery 결과를 받아서 신규/재사용/확장 결정을 한다.

---

## 3. Discovery 절차

```
1. MODULE_REGISTRY.yaml 전체 로드
2. Assets/Game/Modules/ 디렉토리 스캔
3. 요청된 기능 키워드 추출
4. 각 기존 모듈에 대해 유사도 분석:
   a. 이름 유사도 (name matching)
   b. 인터페이스 유사도 (I<Module>.cs API 비교)
   c. 책임 유사도 (description 비교)
   d. 의존성 그래프 유사도 (dependencies 패턴)
5. similarity_score >= DISCOVERY_THRESHOLD 인 모듈을 후보로 선정
6. 후보 목록을 DiscoveryResult로 반환
```

---

## 4. 유사도 분석 기준

### 4.1 이름 유사도 (Name Matching)

| 방법 | 가중치 | 예시 |
|------|--------|------|
| 정확 일치 | 1.0 | 요청: "ItemStacking" → 기존: "ItemStacking" |
| 부분 일치 (키워드) | 0.7 | 요청: "StackSystem" → 기존: "ItemStacking" ("Stack" 공유) |
| 접두어/접미어 일치 | 0.5 | 요청: "InventoryStack" → 기존: "ItemStacking" |
| 약어 일치 | 0.3 | 요청: "InvSys" → 기존: "InventorySystem" |

### 4.2 인터페이스 유사도 (API Matching)

기존 모듈의 `I<Module>.cs` 파일에서 메서드 시그니처를 추출하여 비교.

| 비교 항목 | 가중치 |
|-----------|--------|
| 동일 메서드명 존재 | 0.8 |
| 유사 메서드명 (Add/Remove vs Push/Pop) | 0.5 |
| 동일 프로퍼티 패턴 (IsFull, IsEmpty, Count) | 0.6 |
| 동일 이벤트 패턴 (OnAdded, OnRemoved) | 0.4 |

### 4.3 책임 유사도 (Responsibility Matching)

MODULE_REGISTRY.yaml의 `description` 필드와 요청된 기능 설명을 비교.

| 비교 방법 | 기준 |
|-----------|------|
| 핵심 동사 일치 | "관리", "추가", "제거", "조회" |
| 도메인 키워드 일치 | "아이템", "인벤토리", "스택", "슬롯" |
| 목적 일치 | "저장", "추적", "보유" |

### 4.4 의존성 그래프 유사도 (Dependency Pattern)

| 비교 항목 | 의미 |
|-----------|------|
| 같은 모듈에 의존 | 비슷한 도메인일 가능성 |
| 같은 모듈이 의존 | 비슷한 역할일 가능성 |
| 의존 패턴 유사 | 대체 가능성 |

---

## 5. Discovery 결과 스키마

```yaml
discovery_result:
  query: "<요청된 기능 설명>"
  timestamp: "<ISO 8601>"
  candidate_count: <int>
  candidates:
    - module: "<모듈명>"
      similarity_score: <0.0 ~ 1.0>
      reason_for_similarity: "<유사한 이유>"
      potential_reuse_level: "full_reuse | partial_reuse | extend | reference_only"
      matching_criteria:
        name: <score>
        interface: <score>
        responsibility: <score>
        dependency: <score>
```

### potential_reuse_level 정의

| 레벨 | 설명 | 예시 |
|------|------|------|
| `full_reuse` | 기존 모듈을 그대로 사용 가능 | 이미 ItemStacking이 있는데 스택 기능 요청 |
| `partial_reuse` | 일부 기능만 재사용 가능 | ItemStacking의 Push/Pop을 인벤토리 슬롯에 활용 |
| `extend` | 기존 모듈을 확장해야 함 | InventorySystem에 검색 기능 추가 |
| `reference_only` | 구조만 참조, 직접 재사용 불가 | Economy의 패턴을 참고하여 별도 모듈 생성 |

---

## 6. DISCOVERY_THRESHOLD

| 설정 | 값 | 의미 |
|------|-----|------|
| DISCOVERY_THRESHOLD | 0.4 | 이 이상이면 후보에 포함 |
| HIGH_SIMILARITY | 0.7 | 이 이상이면 reuse/extend 우선 검토 필수 |
| EXACT_MATCH | 0.9 | 이 이상이면 중복 생성 차단 경고 |

---

## 7. Discovery 예시

### 예시 1: "아이템 인벤토리 시스템" 요청

```yaml
discovery_result:
  query: "아이템 인벤토리 시스템 — 슬롯, 스택, 추가/제거"
  timestamp: "2026-03-18T20:00:00"
  candidate_count: 2
  candidates:
    - module: ItemStacking
      similarity_score: 0.72
      reason_for_similarity: "Push/Pop/IsFull/IsEmpty 등 스택 API가 인벤토리 슬롯 스택과 동일한 패턴"
      potential_reuse_level: partial_reuse
      matching_criteria:
        name: 0.5
        interface: 0.85
        responsibility: 0.65
        dependency: 0.3

    - module: InventorySystem
      similarity_score: 0.95
      reason_for_similarity: "슬롯 기반 인벤토리, Add/Remove/Query API가 정확히 일치"
      potential_reuse_level: full_reuse
      matching_criteria:
        name: 1.0
        interface: 0.95
        responsibility: 0.95
        dependency: 0.7
```

### 예시 2: "장비 장착 시스템" 요청

```yaml
discovery_result:
  query: "장비 장착 — 슬롯별 장비 착용, 해제, 스탯 반영"
  timestamp: "2026-03-18T21:00:00"
  candidate_count: 2
  candidates:
    - module: InventorySystem
      similarity_score: 0.55
      reason_for_similarity: "슬롯 기반 아이템 관리 패턴 유사하나, 장착/해제/스탯은 별도 책임"
      potential_reuse_level: extend
      matching_criteria:
        name: 0.3
        interface: 0.5
        responsibility: 0.55
        dependency: 0.5

    - module: ItemStacking
      similarity_score: 0.42
      reason_for_similarity: "단일 슬롯 스택 패턴 참조 가능"
      potential_reuse_level: reference_only
      matching_criteria:
        name: 0.2
        interface: 0.45
        responsibility: 0.4
        dependency: 0.3
```

---

## 8. Queue Generator 통합 방식

Discovery 결과는 Queue Generator의 Decomposition 단계에서 다음과 같이 사용된다:

```
IF candidate.similarity_score >= EXACT_MATCH (0.9):
  → 중복 생성 차단. 기존 모듈을 의존성으로 연결.
  → integration_strategy: reuse

IF candidate.similarity_score >= HIGH_SIMILARITY (0.7):
  → 재사용/확장 우선 검토 필수.
  → Reuse Decision Engine(INTEGRATION_STRATEGY.md)에 위임.

IF candidate.similarity_score >= DISCOVERY_THRESHOLD (0.4):
  → 후보로 기록. Planner가 PLAN에서 참고.
  → TASK_QUEUE에 existing_module_candidates[] 기록.

IF candidate_count == 0:
  → 신규 모듈 생성 진행.
  → integration_strategy: create_new
```

---

## 9. 구현 참조

| 구성 요소 | 파일 |
|-----------|------|
| Discovery 실행 | `Assets/Editor/AI/ModuleDiscovery.cs` |
| Registry 로드 | `Assets/Editor/AI/DependencyGraphBuilder.cs` |
| 인터페이스 파싱 | `Assets/Editor/AI/ModuleDiscovery.cs` (InterfaceScanner) |
| Reuse Decision | `INTEGRATION_STRATEGY.md` |
| Impact Analysis | `MIGRATION_RULES.md` |

---

## 10. 참조 문서

| 문서 | 관계 |
|------|------|
| `INTEGRATION_STRATEGY.md` | Discovery 후속 — 전략 결정 |
| `MIGRATION_RULES.md` | extend/replace 시 마이그레이션 |
| `QUEUE_GENERATOR.md` | Discovery가 통합되는 상위 프로세스 |
| `ORCHESTRATION_RULES.md` | 전체 파이프라인 |
