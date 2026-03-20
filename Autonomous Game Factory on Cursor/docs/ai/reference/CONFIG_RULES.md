# Config Rules — Autonomous Game Factory v2.2

Config ScriptableObject의 Source-of-Truth 보호 규칙.

---

## 1. 목적

| 문제 | 해결 |
|------|------|
| 관련 모듈 간 Config 필드 중복 정의 | Source-of-Truth 단일화 규칙 |
| 중복 Config로 인한 런타임 불일치 | 자동 감지 Validator |
| 어떤 Config가 권위 있는지 불명확 | 원본 모듈 우선 규칙 |

---

## 2. Source-of-Truth 규칙

### 2.1 핵심 원칙

**하나의 공유 행동에 대한 설정값은 정확히 하나의 Config에서만 정의한다.**

### 2.2 적용 범위

| 대상 | 설명 |
|------|------|
| ScriptableObject Config | `<Module>Config.cs` 파일의 SerializeField |
| 의존 관계 모듈 쌍 | MODULE_REGISTRY.yaml의 dependencies로 연결된 모듈 |
| 간접 의존 | A→B→C에서 A와 C의 Config도 검사 대상 |

### 2.3 예시: InventorySystem + ItemStacking

```
문제:
  InventorySystemConfig._maxStackSizePerSlot  ← 스택 크기
  ItemStackingConfig._maxStackSize            ← 스택 크기

  InventorySystem이 ItemStacking에 의존한다.
  스택 크기는 ItemStacking의 고유 책임이다.

해결:
  InventorySystemConfig에서 _maxStackSizePerSlot 제거.
  InventorySystem이 ItemStackingConfig를 주입받아 참조.
```

---

## 3. 충돌 감지 규칙

### 3.1 유사 필드 판정

| 조건 | 유사도 | 예시 |
|------|--------|------|
| 정확히 동일한 필드명 | 확정 충돌 | `_maxStackSize` vs `_maxStackSize` |
| 접두어/접미어만 다름 | 높은 충돌 | `_maxStackSize` vs `_maxStackSizePerSlot` |
| 핵심 키워드 공유 | 의심 충돌 | `_stackLimit` vs `_maxStackSize` |
| 동일 타입 + 유사 목적 | 의심 충돌 | `int _maxSize` vs `int _capacity` |

### 3.2 감지 우선순위

```
1. 직접 의존 모듈 쌍 우선 검사
2. 같은 feature_group 내 모듈 검사
3. 전체 모듈 쌍 검사 (선택)
```

### 3.3 Validator 출력 형식

```
[Architecture] Config conflict: duplicate source-of-truth for '<행동>'
  <Module1>Config.<field1>
  <Module2>Config.<field2>
  Preferred source: <원본 모듈>Config (원본 모듈이 해당 행동의 소유자)
  Action: <의존 모듈>은 <원본 모듈>Config를 주입받아 참조해야 함
```

---

## 4. 해결 방법 우선순위

| 순서 | 방법 | 설명 | 예시 |
|------|------|------|------|
| 1 | 의존 Config 주입 | 의존 모듈의 Config를 생성자/Init으로 전달 | `InventorySystem(invConfig, stackConfig)` |
| 2 | 통합 Config | 공통 값을 상위 Config에서 관리 | `GameConfig.stackSize` |
| 3 | 어댑터 변환 | Config 값을 변환하여 전달 | `stackConfig.MaxSize → slot.maxPerSlot` |

### 4.1 금지 사항

| 금지 | 이유 |
|------|------|
| 같은 값을 두 Config에 복제 | 런타임 불일치 위험 |
| 하드코딩으로 우회 | 매직넘버 금지 규칙 위반 |
| static 전역 변수로 공유 | 아키텍처 규칙 위반 |

---

## 5. 원본 모듈 결정 규칙

**해당 행동을 가장 먼저 정의한 모듈이 원본이다.**

| 판정 기준 | 설명 |
|-----------|------|
| 모듈 생성 순서 | 먼저 생성된 모듈이 원본 |
| 책임 소유 | 해당 행동이 모듈의 핵심 책임이면 원본 |
| 의존 방향 | A→B이면 B가 원본 (B가 먼저 존재) |

---

## 6. Queue Generator 통합

Queue Generator가 새 모듈을 분해할 때:

```
1. 새 모듈의 Config 필드 후보를 추출
2. 의존 모듈의 Config 필드와 비교
3. 유사 필드 발견 시:
   → SPEC에 "이 값은 <원본 모듈>Config에서 참조" 명시
   → PLAN에 Config 주입 설계 포함
   → acceptance_criteria에 "중복 Config 필드 없음" 추가
```

---

## 7. 구현 참조

| 구성 요소 | 파일 |
|-----------|------|
| Config 충돌 감지 | `Assets/Editor/AI/Validators/ConfigConflictValidator.cs` |
| Config 스캔 | SerializeField 필드 추출 |
| 의존 관계 | `DependencyGraphBuilder.cs` |

---

## 8. 참조 문서

| 문서 | 관계 |
|------|------|
| `PIPELINE_HARDENING.md` | 개선 개요 |
| `CODING_RULES.md` | Config ScriptableObject 규칙 |
| `MODULE_TEMPLATES.md` | Config 파일 템플릿 |
| `ORCHESTRATION_RULES.md` | Validator 통합 |
