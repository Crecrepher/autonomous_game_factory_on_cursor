# PROJECT CONSTITUTION — 불변 규칙

이 문서는 프로젝트의 변경 불가능한 핵심 규칙이다.
모든 AI 에이전트는 예외 없이 이 규칙을 따른다.

---

## 1. 프로젝트 정의

- **목적**: Human-in-the-Loop Learning Pipeline 기반 Unity AI 개발 팩토리
- **타겟**: 모든 장르의 Playable Ad (2D/3D, 모바일)
- **엔진**: Unity (C#), 네임스페이스 `Game`
- **핵심 사이클**: AI 생성 → 사람 검증 → 수정 → 학습 축적 → 다음 생성 품질 향상

---

## 2. 9단계 파이프라인 (변경 불가)

```
[1] Feature Intake       → FEATURE_QUEUE.yaml
[2] Queue Generator      → TASK_QUEUE + REGISTRY + Specs
[2.9] Architecture Diff  → 위험 분석 (critical → 차단)
[3] Orchestrator         → 의존성 그래프, Builder 배정
[4] Planner              → PLAN 작성
[5] Builder              → 모듈 코드 생성 (6파일)
[6] ★ Human Validator ★ → 사람이 검증 + 수정 (파이프라인 정지)
[7] Reviewer             → AI 재검증
[8] Committer            → feature_group 단위 커밋 (7 Gate)
[9] Learning Recorder    → 학습 축적
```

**Human Validator(6단계)는 필수 관문이다. 우회 절대 금지.**

---

## 3. 모듈 구조 (6파일, 예외 없음)

```
Assets/Game/Modules/<Module>/
├── I<Module>.cs              # 인터페이스 (공개 계약)
├── <Module>Config.cs         # ScriptableObject (설정, 로직 없음)
├── <Module>Runtime.cs        # 순수 C# (비즈니스 로직, MB 상속 금지)
├── <Module>Factory.cs        # static class (생성/DI)
├── <Module>Bootstrap.cs      # MonoBehaviour (씬 진입점, 얇게)
└── Tests/Editor/<Module>Tests.cs  # NUnit 테스트 (최소 2개)
```

---

## 4. 코딩 규칙 (위반 시 빌드 차단)

### 절대 금지
- foreach, 코루틴, 람다, LINQ, Invoke
- 매직넘버 (→ const UPPER_SNAKE_CASE)
- GetComponent 런타임 사용
- null conditional (`?.`, `??`)
- 이벤트 버스, 리플렉션
- Runtime에서 MonoBehaviour 상속
- 싱글턴 패턴 (MonoBehaviour)
- public 필드 직접 노출 (이벤트 제외)

### 필수 패턴
- private 필드: `_camelCase` (private 키워드 생략)
- public 노출: 프로퍼티 또는 `=>`
- 상수: `const UPPER_SNAKE_CASE`
- Enum: `E` 접두사
- Animator: `Animator.StringToHash` 캐싱
- 필드 순서: const → static → 이벤트 → 프로퍼티 → public → [SerializeField] → private
- for문만 사용, 배열 우선 (길이 고정 시)
- Update 내 나눗셈 → 역수 캐싱 후 곱셈
- sqrMagnitude 사용 (magnitude/distance 대신)

---

## 5. 4차원 상태 모델

| 차원 | 필드 | 추적 대상 |
|------|------|-----------|
| 빌드 | `status` | pending → planned → in_progress → review → done |
| 사람 | `human_state` | none → pending → in_review → validated |
| 학습 | `learning_state` | none → pending → recorded |
| 커밋 | `commit_state` | none → ready → committed |

**교차 조건**:
- `in_progress → review`: human_state == validated 필수
- `review → done`: commit_state ∈ {committed, recommitted} + learning_state == recorded 필수
- AI가 human_state를 validated로 변경하는 것은 금지

---

## 6. 7 Gate 커밋 체크 (전부 통과해야 커밋)

1. **Validation Report Gate**: AIValidationReport.json Passed == true
2. **Reviewer Gate**: commit_state == ready
3. **Human Gate**: human_state == validated
4. **Learning Gate**: human_fixes > 0 → learning_state == recorded
5. **Completeness Gate**: feature_group 전체 ready
6. **Scope Gate**: 관련 파일만 스테이징
7. **Architecture Diff Gate**: arch_diff_blocked != true

---

## 7. 수정 금지 영역

| 경로 | 이유 |
|------|------|
| `Assets/Editor/AI/` | 검증 시스템 인프라 |
| `Assets/Game/Core/` | editable: false (명시적 허가 없이 불가) |
| `Assets/Game/Modules/Template/` | 참조용 원본 |
| `.cursor/rules/` | 사용자만 수정 |

---

## 8. 의존성 규칙

- 모듈 간 참조는 인터페이스(`I<Module>`)만 허용
- 순환 의존 절대 금지
- MODULE_REGISTRY.yaml에 선언된 의존성만 사용
- depends_on과 dependencies는 일치해야 함
- 모듈 전략 우선순위: reuse > extend > adapt > replace > create_new

---

## 9. 글로벌 안전 규칙

- 기존 파일을 무단으로 덮어쓰지 않는다
- done/pass/complete를 검증 없이 주장하지 않는다
- 신규 아키텍처를 발명하지 않는다
- force push 절대 금지
- 파일이 수정되지 않은 응답은 완료가 아니다
- 분석만 출력한 응답은 실행이 아니다
