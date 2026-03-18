# Worktree & Task Isolation Strategy — AGF v3.0

> 병렬 실행 시 파일 충돌을 방지하기 위한 작업 격리 전략

---

## 1. 격리가 필요한 이유

병렬 에이전트가 동일 워크스페이스에서 동시에 파일을 수정하면:
- 파일 덮어쓰기 경쟁 조건 (race condition)
- 불완전한 중간 상태의 파일이 다른 에이전트에게 노출
- Git 충돌 해결 불가

**결론**: 진정한 병렬 실행은 작업 격리가 필수다.

---

## 2. 격리 전략 비교

| 전략 | 격리 수준 | 복잡도 | 제한사항 |
|---|---|---|---|
| **L0: 폴더 분리** | 낮음 | 최소 | Shared/ 충돌 가능 |
| **L1: 브랜치 분리** | 중간 | 낮음 | 동시 작업 시 전환 필요 |
| **L2: 워크트리 분리** | 높음 | 중간 | 디스크 공간, Unity 프로젝트 크기 |
| **L3: 컨테이너/VM** | 최대 | 높음 | 인프라 필요 |

---

## 3. L0 — 폴더 기반 격리 (즉시 사용 가능)

AGF 모듈 구조는 자연적으로 폴더가 분리되어 있다.

### 3.1 안전 영역 (동시 수정 가능)

```
Assets/Game/Modules/<ModuleName>/  ← 모듈별 독립 폴더
docs/ai/plans/<Module>_PLAN.md     ← 모듈별 독립 파일
docs/ai/generated_specs/<Module>_SPEC.md ← 모듈별 독립 파일
```

### 3.2 위험 영역 (직렬 처리 필수)

```
Assets/Game/Shared/              ← 공유 인터페이스
docs/ai/TASK_QUEUE.yaml          ← 전역 큐
docs/ai/MODULE_REGISTRY.yaml     ← 전역 레지스트리
docs/ai/FEATURE_QUEUE.yaml       ← 전역 피처 큐
```

### 3.3 충돌 방지 규칙

| 규칙 | 설명 |
|---|---|
| 모듈 폴더 전용 수정 | 에이전트는 임대된 모듈 폴더만 수정 |
| Shared/ 직렬 잠금 | Shared/ 수정은 오케스트레이터만 수행 |
| 전역 YAML 직렬 잠금 | TASK_QUEUE, REGISTRY 업데이트는 Join 단계에서만 |
| 읽기는 자유 | 모든 파일 읽기는 언제나 가능 |

### 3.4 구현

Subagent에게 태스크를 전달할 때 명시적으로 수정 허용 경로를 지정:

```
"당신은 Assets/Game/Modules/CurrencyWallet/ 폴더만 생성/수정할 수 있습니다.
 다른 폴더의 파일은 읽기만 가능합니다."
```

---

## 4. L1 — 브랜치 기반 격리

### 4.1 브랜치 네이밍

```
ai_test                              ← base branch
├── agf/build/<feature_group>/<module>  ← 빌드 브랜치
│   예: agf/build/economy/CurrencyWallet
├── agf/review/<feature_group>         ← 리뷰 브랜치
│   예: agf/review/economy
└── agf/merge/<feature_group>          ← 머지 브랜치
    예: agf/merge/economy
```

### 4.2 흐름

```
1. ai_test에서 agf/build/economy/CurrencyWallet 브랜치 생성
2. Agent가 해당 브랜치에서 모듈 빌드
3. 빌드 완료 → agf/review/economy에 PR 또는 머지
4. 모든 모듈 완료 → agf/merge/economy → ai_test 머지
```

### 4.3 제한사항

- 단일 워크스페이스에서는 동시 브랜치 작업 불가
- 브랜치 전환 시 Unity .meta 파일 충돌 위험
- **현재 Cursor 환경에서는 L0가 더 실용적**

---

## 5. L2 — 워크트리 기반 격리

### 5.1 워크트리 생성

MCP GitKraken `git_worktree` 도구 사용:

```bash
git worktree add ../luna_lumberchopper-wt-CurrencyWallet agf/build/economy/CurrencyWallet
```

### 5.2 구조

```
/Users/supercent/luna_lumberchopper/                    ← 메인 워크트리
/Users/supercent/luna_lumberchopper-wt-CurrencyWallet/  ← 워크트리 1
/Users/supercent/luna_lumberchopper-wt-ShopSystem/      ← 워크트리 2
```

### 5.3 장점

- 완전한 파일 시스템 격리
- 각 워크트리에서 독립적인 에이전트 세션 가능
- Git이 자동으로 브랜치 매핑 관리

### 5.4 단점

- Unity 프로젝트: Library/ 폴더 재생성 필요 (시간 + 디스크)
- 워크트리 수만큼 디스크 사용
- 각 워크트리에 별도 Cursor 창 또는 Background Agent 필요
- **사람의 수동 설정 필요**

### 5.5 실현 가능성

| 조건 | 상태 |
|---|---|
| git worktree 명령 | ✅ 사용 가능 |
| MCP git_worktree 도구 | ✅ 존재 (list/add) |
| 별도 Cursor 창에서 워크트리 열기 | ❓ 사람 확인 필요 |
| Background Agent가 워크트리에서 실행 | ❓ Cursor 기능 확인 필요 |
| 워크트리 자동 정리 | 구현 필요 |

---

## 6. 권장 전략

### 6.1 즉시 적용 (Tier 1)

**L0 폴더 격리 + Cursor Subagent**

```
Orchestrator (Primary Agent)
  ↓
  ├── Subagent 1: "Module A를 Assets/Game/Modules/A/ 에 생성"
  ├── Subagent 2: "Module B를 Assets/Game/Modules/B/ 에 생성"
  └── Subagent 3: "Module C를 Assets/Game/Modules/C/ 에 생성"
  ↓
Primary Agent: Join → Shared/ 업데이트 → YAML 업데이트 → Review
```

- 동시 최대 4개 서브에이전트
- 모듈 폴더 격리로 충돌 최소화
- 전역 리소스(Shared/, YAML)는 Primary Agent만 수정

### 6.2 확인 후 적용 (Tier 2)

**L2 워크트리 + Background Agent** (Cursor 기능 확인 후)

```
1. git worktree add (MCP 도구 사용)
2. Background Agent를 워크트리에서 실행
3. 완료 후 머지 + 워크트리 제거
```

---

## 7. 워크트리 생명 주기

```
[Create]
  ↓ 태스크 실행 시작 시 워크트리 생성
[Active]
  ↓ 에이전트가 해당 워크트리에서 작업
[Complete]
  ↓ 빌드 + 검증 완료
[Merge]
  ↓ base 브랜치에 머지
[Cleanup]
  ↓ 워크트리 제거 + 브랜치 정리
```

---

## 8. 관련 문서

| 문서 | 역할 |
|---|---|
| `PARALLEL_ORCHESTRATION.md` | 병렬 실행 아키텍처 |
| `TASK_EXECUTION_SCHEMA.md` | 실행 단위 스키마 |
| `JOIN_AND_MERGE_REVIEW.md` | 합류/머지 프로세스 |
