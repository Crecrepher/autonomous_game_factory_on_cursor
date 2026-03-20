# Global Learning Log — Cross-Project Intelligence

프로젝트 간 공유되는 학습 이벤트 시간순 기록.

---

## 기록 형식

```
### [날짜] [프로젝트] — <학습 유형>

- **교훈**: <what was learned>
- **원인**: <root cause>
- **해결**: <how it was fixed>
- **패턴 ID**: <관련 패턴 ID, 있으면>
- **적용 범위**: project-specific | cross-project
```

---

## 이력

### [2026-03-18] [luna_lumberchopper] — Architecture Pattern

- **교훈**: Runtime 클래스는 순수 C#으로 유지해야 테스트 가능
- **원인**: 초기 설계에서 MonoBehaviour 사용 시도
- **해결**: ArchitectureRuleValidator가 자동 감지 + 차단
- **패턴 ID**: AP-001 (RuntimeIsolation)
- **적용 범위**: cross-project

### [2026-03-18] [luna_lumberchopper] — Config Conflict

- **교훈**: 의존 관계인 두 모듈의 Config에 유사한 필드 금지
- **원인**: InventorySystem과 ItemStacking에 maxStackSize 중복
- **해결**: ConfigConflictValidator 도입
- **패턴 ID**: ANTI-007 (DuplicateConfigValues)
- **적용 범위**: cross-project

---

*파이프라인 완료 후 IntelligenceFeedbackLoop가 자동으로 추가한다.*
