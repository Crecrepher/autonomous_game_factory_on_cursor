# Validator Failure Patterns — Autonomous Game Factory v2

이 문서는 12개 Unity Validator가 발생시키는 실패 패턴을 구조화하여 정리한다.
AI 에이전트는 코드 생성 전에 이 문서를 참조하여 동일한 실패를 예방한다.

**삭제 금지. 패턴 추가만 허용.**

---

## 사용법

| 역할 | 접근 |
|------|------|
| Builder | 코드 생성 전 해당 모듈 유형의 패턴 확인 |
| Reviewer | 검증 실패 시 이 문서에서 근본 원인과 해결책 조회 |
| Learning Recorder | 새 실패 패턴 발견 시 append |

---

## 1. CompileErrorValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-CE-001 | 스크립트 컴파일 에러 | 문법 오류, 누락된 using, 타입 불일치 | Builder가 생성 후 자체 문법 검사 수행 |
| VF-CE-002 | 참조 누락 | 다른 모듈의 타입을 참조했으나 Assembly Definition 설정 누락 | MODULE_REGISTRY dependencies 먼저 확인 |

---

## 2. ArchitectureRuleValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-AR-001 | Runtime이 MonoBehaviour 상속 | AI가 Unity 라이프사이클이 필요하다고 판단하여 상속 추가 | Runtime은 절대 MonoBehaviour 금지. Bootstrap에서 호출. |
| VF-AR-002 | Config가 ScriptableObject 미상속 | 일반 class로 생성 | Config 파일은 반드시 ScriptableObject 상속 + CreateAssetMenu |

---

## 3. ArchitecturePatternValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-AP-001 | MonoBehaviour 싱글턴 | static Instance 패턴 사용 | DI 또는 명시적 SerializeField 참조 사용 |
| VF-AP-002 | namespace 누락 | 파일 생성 시 namespace 블록 미포함 | 모든 파일에 `namespace Game { }` 필수 |
| VF-AP-003 | public 필드 직접 노출 | 프로퍼티 대신 public 필드 사용 | get-only 프로퍼티 또는 `=>` 표현식 사용 |
| VF-AP-004 | Bootstrap 외 클래스에서 Awake 사용 | 초기화를 Awake에서 수행 | Init() 패턴 사용, 상위 객체가 초기화 제어 |
| VF-AP-005 | System.Reflection 사용 | 타입 정보 동적 조회 | 인터페이스, 제네릭, 또는 명시적 참조 사용 |

---

## 4. CodingStyleValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-CS-001 | foreach 사용 | 컬렉션 순회 시 습관적 foreach | for (int i = 0; ...) 패턴으로 대체 |
| VF-CS-002 | System.Linq using | LINQ 메서드 편의성 | 수동 루프 + 배열 인덱싱 |
| VF-CS-003 | LINQ 메서드 호출 | Select, Where, ToList 등 | for문 + 조건문으로 필터링 |
| VF-CS-004 | Invoke/InvokeRepeating | 문자열 기반 메서드 호출 | 타이머 변수 + Update 체크 또는 상태 머신 |
| VF-CS-005 | 코루틴 (StartCoroutine) | 비동기 로직 구현 | 델타타임 누적 타이머 또는 상태 기반 루프 |
| VF-CS-006 | 람다 표현식 | 이벤트 구독 시 인라인 함수 | named method로 캐싱 |
| VF-CS-007 | nullable `?` 필드 | C# nullable 문법 사용 | null 체크를 명시적 if문으로 |

---

## 5. PerformanceValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-PF-001 | 런타임 GetComponent | 동적 컴포넌트 조회 | SerializeField 또는 초기화 시 캐싱 |
| VF-PF-002 | GameObject.Find | 이름으로 오브젝트 검색 | 참조 캐싱 또는 이벤트 기반 통신 |
| VF-PF-003 | FindObjectOfType | 씬 전체 검색 | 레지스트리 패턴 또는 명시적 참조 |
| VF-PF-004 | Update에서 magnitude/distance | 매 프레임 sqrt 연산 | sqrMagnitude + 제곱 임계값 |
| VF-PF-005 | Instantiate 무분별 사용 | 오브젝트 생성 | 오브젝트 풀링 |
| VF-PF-006 | Destroy 무분별 사용 | 오브젝트 파괴 | 풀로 반환 |

---

## 6. StringAndAnimatorValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-SA-001 | Animator 문자열 파라미터 | SetFloat("name", v) | static readonly int + StringToHash |
| VF-SA-002 | Animator.Play 문자열 | Play("clipName") | static readonly int + StringToHash |
| VF-SA-003 | Update에서 문자열 결합 | 디버그 로그나 UI 텍스트 | StringBuilder 또는 미리 캐싱된 string[] |

---

## 7. ModuleStructureValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-MS-001 | 인터페이스 파일 누락 | I<Module>.cs 미생성 | 템플릿에서 반드시 6개 파일 생성 |
| VF-MS-002 | Config 파일 누락 | <Module>Config.cs 미생성 | 모듈 생성 체크리스트 확인 |
| VF-MS-003 | Runtime 파일 누락 | <Module>Runtime.cs 미생성 | 모듈 생성 체크리스트 확인 |
| VF-MS-004 | Factory 파일 누락 | <Module>Factory.cs 미생성 | 모듈 생성 체크리스트 확인 |
| VF-MS-005 | Tests 폴더 누락 | 테스트 작성 누락 | 모듈 생성 시 Tests/Editor/ 폴더 필수 생성 |
| VF-MS-006 | *Tests.cs 파일 없음 | 테스트 코드 미작성 | 최소 1개 테스트 클래스 필수 |

---

## 8. ModuleBoundaryValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-MB-001 | 모듈 루트에 비규격 파일 | Helper, Util 등 별도 파일 생성 | Runtime 내부에 로직 통합 또는 서브폴더 활용 |
| VF-MB-002 | Editor 스크립트가 런타임 루트에 위치 | 에디터 전용 코드가 모듈 루트에 존재 | Tests/Editor/ 폴더로 이동 |
| VF-MB-003 | 비-UI 모듈에 UI 파일 존재 | UI 관련 코드가 비즈니스 모듈에 혼재 | 별도 UI 모듈로 분리 |
| VF-MB-004 | 모듈 이름과 무관한 gameplay 파일 | GenericManager.cs 등 | 파일명에 모듈 이름 접두사 필수 |

---

## 9. DependencyValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-DV-001 | 미등록 의존성 using | using Game.X인데 registry에 X 미선언 | MODULE_REGISTRY.yaml dependencies에 먼저 추가 |
| VF-DV-002 | TASK_QUEUE 모듈이 registry에 없음 | 태스크만 추가하고 registry 미등록 | 동시에 양쪽 등록 |
| VF-DV-003 | depends_on 불일치 | TASK_QUEUE의 depends_on과 registry의 dependencies가 다름 | 두 파일 동기화 |

---

## 10. CircularDependencyValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-CD-001 | 직접 순환 (A→B→A) | 양방향 참조 | Shared 인터페이스로 의존 역전 |
| VF-CD-002 | 간접 순환 (A→B→C→A) | 3개 이상 모듈 체인 | 의존성 그래프를 DAG로 유지, 정기적으로 시각화 |
| VF-CD-003 | 위상 정렬 불완전 | 숨겨진 순환 또는 누락 모듈 | MODULE_REGISTRY 전수 검사 |

---

## 11. ForbiddenFolderValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-FF-001 | Core에 baseline 미등록 파일 추가 | 사용자 허가 없이 Core 수정 | Core 수정 전 명시적 허가 확인 |
| VF-FF-002 | Baseline에 있지만 실제 파일 삭제됨 | Core 파일 삭제 후 baseline 미갱신 | Core 변경 시 반드시 baseline 업데이트 |

---

## 12. ValidatorRegistrationValidator

| 패턴 ID | 실패 조건 | 일반적 원인 | 예방법 |
|---------|-----------|-------------|--------|
| VF-VR-001 | 새 Validator가 ValidationRunner에 미등록 | IModuleValidator 구현 후 등록 누락 | Validator 추가 시 ValidationRunner.cs에 `new XxxValidator()` 추가 |

---

## 패턴 추가 규칙

1. 새 패턴 발견 시 해당 validator 섹션에 행을 추가한다.
2. 패턴 ID 형식: `VF-<Validator약어>-<순번>` (예: VF-CS-008)
3. 예방법은 Builder가 즉시 적용 가능한 수준으로 구체적으로 작성한다.
4. 동일 패턴이 3회 이상 반복되면 RULE_MEMORY.yaml에도 규칙 추가한다.
