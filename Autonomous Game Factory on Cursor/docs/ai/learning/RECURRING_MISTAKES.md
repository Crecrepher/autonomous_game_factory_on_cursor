# Recurring Mistakes — Autonomous Game Factory v2

동일 유형의 실수가 **3회 이상** 반복된 경우 여기에 패턴으로 등록한다.
이 문서에 등록된 패턴은 Builder가 코드 생성 시 최우선으로 회피해야 할 항목이다.

**삭제 금지. 축적 전용.**

---

## 사용법

| 역할 | 접근 |
|------|------|
| Planner | PLAN에 "회피할 반복 실수" 섹션으로 포함 |
| Builder | 코드 생성 전 반드시 확인 — 이 목록의 패턴을 재현하면 안 됨 |
| Learning Recorder | 동일 실패 3회 확인 시 새 패턴 추가 |

---

## 엔트리 스키마

```
### REC-XXX: <제목>

- **반복 횟수**: N회
- **관련 Validator**: <validator 이름>
- **관련 규칙**: <RULE_MEMORY id>
- **근거 로그**: <LEARNING_LOG ids>
- **범위**: project | global

#### 패턴 설명
> 어떤 실수가 반복되는가

#### 근본 원인
> 왜 AI가 이 실수를 반복하는가

#### 회피 방법
> Builder가 구체적으로 무엇을 해야 하는가
```

---

## 등록된 패턴

> 현재 프로젝트는 초기 단계이므로 아직 3회 이상 반복된 패턴이 없다.
> 아래는 이전 프로젝트 경험과 프레임워크 설계에서 예상되는 고위험 패턴이다.

### REC-001: Runtime에 MonoBehaviour 상속 추가

- **반복 횟수**: 예상 고위험 (타 프로젝트에서 다수 발생)
- **관련 Validator**: ArchitectureRuleValidator
- **관련 규칙**: RM-0001
- **근거 로그**: [LL-0001 발생 시 연결]
- **범위**: global

#### 패턴 설명

> AI가 Update(), Awake() 같은 Unity 라이프사이클이 필요하다고 판단하여
> Runtime 클래스에 `: MonoBehaviour`를 추가하는 패턴.

#### 근본 원인

> AI의 학습 데이터에서 Unity 코드는 대부분 MonoBehaviour를 상속하고 있어서,
> "Unity = MonoBehaviour"라는 편향이 존재한다.

#### 회피 방법

> 1. *Runtime.cs 파일은 절대 MonoBehaviour 상속 금지
> 2. Unity 라이프사이클이 필요하면 Bootstrap에서 Tick(deltaTime) 호출
> 3. 코드 생성 후 자가 검사: "이 파일이 *Runtime.cs인가? MonoBehaviour가 있는가?"

---

### REC-002: foreach와 LINQ 습관적 사용

- **반복 횟수**: 예상 고위험 (GC-free 규칙 위반의 가장 흔한 형태)
- **관련 Validator**: CodingStyleValidator
- **관련 규칙**: RM-0004, RM-0008
- **근거 로그**: [발생 시 연결]
- **범위**: global

#### 패턴 설명

> AI가 컬렉션 처리 시 foreach나 LINQ(Where, Select, ToList)를 기본적으로 사용.
> 특히 "깔끔한 코드" 관점에서 LINQ를 선호하는 경향.

#### 근본 원인

> 일반적인 C# 코딩에서 LINQ와 foreach는 모범 사례로 학습되어 있다.
> 모바일 게임의 GC-free 제약은 AI의 기본 학습과 상충한다.

#### 회피 방법

> 1. using System.Linq 절대 추가하지 않음
> 2. foreach 대신 for (int i = 0; i < length; i++)
> 3. "LINQ 대신 for문" 규칙을 모든 컬렉션 처리에 적용
> 4. 코드 생성 후 자가 검사: "foreach가 있는가? using System.Linq가 있는가?"

---

### REC-003: 모듈 간 미선언 의존성 참조

- **반복 횟수**: 예상 고위험
- **관련 Validator**: DependencyValidator
- **관련 규칙**: RM-0003
- **근거 로그**: [LL-0002 발생 시 연결]
- **범위**: global

#### 패턴 설명

> AI가 다른 모듈의 타입을 using으로 참조하면서
> MODULE_REGISTRY.yaml의 dependencies에 해당 모듈을 추가하지 않는 패턴.

#### 근본 원인

> 일반 개발에서는 using만 추가하면 되지만,
> 이 프레임워크에서는 YAML 레지스트리에도 선언이 필요하다는 것을 AI가 놓침.

#### 회피 방법

> 1. 다른 모듈 참조 전 MODULE_REGISTRY.yaml 확인
> 2. dependencies 배열에 먼저 추가
> 3. 가능하면 직접 참조 대신 Shared 인터페이스 사용
> 4. 코드 생성 후 자가 검사: "using Game.X가 있는가? → dependencies에 X가 있는가?"

---

## 다음 엔트리 ID: REC-004

---

## 등록 기준

1. LEARNING_LOG.md에서 동일 `event_type` + `source` 조합이 **3회 이상** 확인될 때 등록한다.
2. 타 프로젝트 경험에서 명백히 고위험인 패턴은 "예상 고위험"으로 선 등록 가능하다.
3. 실제 3회 발생 확인 시 "예상 고위험" → 실제 횟수로 업데이트한다.
