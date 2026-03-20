# Cross-Project Intelligence Layer (CPIL)

Autonomous Game Factory v2.3의 최상위 레이어.
파이프라인 위에서 프로젝트 간 지식을 축적하고 재사용하는 AI 소프트웨어 엔지니어링 시스템.

---

## 1. 시스템 아키텍처

```
┌─────────────────────────────────────────────────────────┐
│         Cross-Project Intelligence Layer (CPIL)         │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Global       │  │ Pattern      │  │ Project      │ │
│  │ Module       │  │ Recognition  │  │ Bootstrap    │ │
│  │ Library      │  │ Engine       │  │ Generator    │ │
│  │ (COMP1)      │  │ (COMP3)      │  │ (COMP4)      │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                    │
│  │ Cross-Project│  │ Intelligence │                    │
│  │ Learning     │  │ Feedback     │                    │
│  │ Memory       │  │ Loop         │                    │
│  │ (COMP2)      │  │ (COMP5)      │                    │
│  └──────────────┘  └──────────────┘                    │
│                                                         │
│  ── Global Validation Rule ──────────────────────────  │
│  "사람 검증 없이 기존 프로젝트를 자동 수정하지 않는다"  │
└─────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────┐
│              Project Pipeline (per project)              │
│                                                         │
│  Feature Intake → Discovery → Reuse Decision →          │
│  Impact Analysis → Arch Diff → Migration →              │
│  Queue Generator → Planner → Builder →                  │
│  Reviewer → Human Gate → Learning Recorder →            │
│  Regression Guardian → Committer                        │
└─────────────────────────────────────────────────────────┘
```

---

## 2. 컴포넌트 매핑

| # | 컴포넌트 | 구현 파일 | 데이터 저장 |
|---|----------|-----------|-------------|
| COMP1 | Global Module Library | `CrossProject/GlobalModuleLibrary.cs` | `GlobalModules/` |
| COMP2 | Cross-Project Learning | `CrossProject/CrossProjectLearning.cs` | `docs/ai/global_learning/` |
| COMP3 | Pattern Recognition | `CrossProject/PatternRecognitionEngine.cs` | 8개 내장 템플릿 |
| COMP4 | Project Bootstrap | `CrossProject/ProjectBootstrapGenerator.cs` | 동적 생성 |
| COMP5 | Intelligence Feedback | `CrossProject/IntelligenceFeedbackLoop.cs` | 자동 업데이트 |

---

## 3. 데이터 흐름

```
프로젝트 A 완료
  ↓
[Intelligence Feedback Loop]
  ├── 안정 모듈 → GlobalModules/ 내보내기
  ├── 학습 이벤트 → global_learning/ 기록
  └── 규칙 강화 → GLOBAL_RULE_MEMORY.yaml 업데이트
  ↓
프로젝트 B 시작
  ↓
[Pattern Recognition Engine]
  └── 게임 설명 → 8개 템플릿 매칭
  ↓
[Project Bootstrap Generator]
  ├── 매칭된 패턴 → 모듈 구성 제안
  ├── GlobalModules/ → 재사용 가능 모듈 식별
  ├── TASK_QUEUE.yaml 자동 생성
  └── 의존성 그래프 추론
  ↓
[Project Pipeline 시작]
  └── CPIL 지식 활용하여 Builder/Planner 향상
```

---

## 4. Global Validation Rule

**CPIL은 기존 프로젝트를 자동으로 수정할 수 없다.**

| 허용 | 금지 |
|------|------|
| 개선 제안 | 코드 자동 수정 |
| 패턴 매칭 결과 표시 | 강제 아키텍처 변경 |
| 모듈 내보내기/가져오기 제안 | 무단 의존성 추가 |
| TASK_QUEUE 초안 생성 | 사람 검증 없이 커밋 |

---

## 5. 지원 범위

| 기능 | 설명 |
|------|------|
| Cross-project architectural learning | 아키텍처 패턴/안티패턴이 프로젝트 간 공유 |
| Cross-project module reuse | 검증된 모듈을 새 프로젝트에서 재사용 |
| Cross-project coding pattern memory | 코딩 패턴이 프로젝트 간 일관성 유지 |
| Cross-project failure prevention | 한 프로젝트의 실패가 다른 프로젝트에서 반복 방지 |

---

## 6. Unity Editor 통합

| 메뉴 | 기능 |
|------|------|
| `Tools/AI/CPIL/Show Global Module Library` | 글로벌 라이브러리 카탈로그 표시 |
| `Tools/AI/CPIL/Search Global Modules` | 키워드 기반 모듈 검색 |
| `Tools/AI/CPIL/Show Global Rules` | 글로벌 규칙 요약 |
| `Tools/AI/CPIL/Analyze Cross-Project Patterns` | 패턴 인식 테스트 |
| `Tools/AI/CPIL/Bootstrap New Project` | 신규 프로젝트 스캐폴딩 |
| `Tools/AI/CPIL/Run Intelligence Feedback Loop` | 피드백 루프 수동 실행 |
| Control Panel > CPIL Section | 통합 UI 패널 |

---

## 7. 참조 문서

| 문서 | 내용 |
|------|------|
| `GLOBAL_MODULE_LIBRARY.md` | Global Module Library 상세 명세 |
| `GLOBAL_LEARNING_SYSTEM.md` | Cross-Project Learning 상세 명세 |
| `GAME_FACTORY_ARCHITECTURE.md` | 전체 팩토리 아키텍처 |
| `PIPELINE_AUTOMATION.md` | 파이프라인 자동화 캡빌리티 |
| `ORCHESTRATION_RULES.md` | 파이프라인 오케스트레이션 |
