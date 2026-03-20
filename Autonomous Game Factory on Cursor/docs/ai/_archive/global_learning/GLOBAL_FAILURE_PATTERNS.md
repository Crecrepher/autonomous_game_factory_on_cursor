# Global Failure Patterns — Cross-Project Intelligence

프로젝트 간 반복되는 실패 패턴 저장소.

---

## 기록 형식

```
### FP-XXX: <패턴 이름>

- **설명**: <what goes wrong>
- **발견 프로젝트**: <project>
- **발생 빈도**: <count>
- **감지 방법**: <validator or manual>
- **예방 방법**: <fix strategy>
```

---

## 패턴 목록

### FP-001: Runtime MonoBehaviour 침투

- **설명**: AI가 Runtime 클래스에 MonoBehaviour를 상속시킴
- **발견 프로젝트**: luna_lumberchopper
- **발생 빈도**: 1
- **감지 방법**: ArchitectureRuleValidator
- **예방 방법**: 템플릿에 Runtime = plain class 명시. Validator가 자동 차단.

### FP-002: Config 필드 중복

- **설명**: 의존 관계 모듈 간 유사한 설정 필드가 양쪽 Config에 존재
- **발견 프로젝트**: luna_lumberchopper
- **발생 빈도**: 1
- **감지 방법**: ConfigConflictValidator
- **예방 방법**: 상위 모듈의 Config를 Source-of-Truth로 지정

### FP-003: 누락된 Registry 엔트리

- **설명**: 코드가 있지만 MODULE_REGISTRY.yaml에 등록되지 않음
- **발견 프로젝트**: luna_lumberchopper
- **발생 빈도**: 0
- **감지 방법**: PipelineSelfHealer
- **예방 방법**: Queue Generator가 자동 등록. SelfHealer가 누락 감지.

---

*IntelligenceFeedbackLoop가 파이프라인 완료 후 자동 업데이트.*
