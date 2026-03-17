# 설치 가이드

## 이 저장소는 무엇인가?

이 저장소는 **AI Game Dev Framework**의 **소스 저장소**입니다.
실제 게임 프로젝트가 아니라, Unity + Cursor 기반 AI 자율 개발 환경을 **다양한 프로젝트에 재사용**하기 위한 프레임워크입니다.

> 이 저장소를 직접 Unity에서 열어 게임을 만들지 마세요.
> 대신 이 프레임워크를 **실제 Unity 프로젝트에 주입**해서 사용합니다.

---

## 저장소 구조

```
/
  .cursor/rules/              ← Cursor AI 에이전트 규칙 (자율 개발 워크플로)
  framework/
    editor/AI/                ← Unity Editor 검증 도구 (모듈 구조·아키텍처·컴파일 검증)
    templates/ModuleTemplate/ ← 표준 모듈 템플릿 (Interface, Config, Runtime, Factory, Bootstrap, Tests)
  docs/
    guides/                   ← 설치·적용·진화 가이드
    examples/                 ← 프로젝트별 AI 문서 예시 (PROJECT_OVERVIEW, CODING_RULES, MODULE_REGISTRY 등)
    framework/                ← 프레임워크 설계 문서
  scripts/
    install/                  ← 프로젝트에 프레임워크를 적용하는 방법 안내
```

---

## 필수 환경

| 항목 | 최소 버전 |
|------|-----------|
| **Unity** | 2021.3 LTS 이상 |
| **Cursor IDE** | 최신 안정 버전 |
| **.NET** | Unity 프로젝트 기본 설정 |

---

## 빠른 시작

1. 이 저장소를 클론합니다:
   ```bash
   git clone <이 저장소 URL>
   ```

2. `docs/guides/HOW_TO_APPLY_TO_PROJECT.md`를 읽고, 실제 Unity 프로젝트에 프레임워크를 주입합니다.

3. `docs/examples/` 폴더의 예시 문서를 참고하여, 프로젝트에 맞는 AI 문서를 작성합니다.

4. Cursor에서 프로젝트를 열면, `.cursor/rules/autonomous-developer.mdc` 규칙이 AI 에이전트의 행동을 제어합니다.

---

## 다음 단계

- [HOW_TO_APPLY_TO_PROJECT.md](HOW_TO_APPLY_TO_PROJECT.md) — 실제 프로젝트에 적용하는 방법
- [HOW_TO_EVOLVE_RULES.md](HOW_TO_EVOLVE_RULES.md) — 규칙과 검증을 프로젝트에 맞게 진화시키는 방법
- [../framework/FRAMEWORK_OVERVIEW.md](../framework/FRAMEWORK_OVERVIEW.md) — 프레임워크 전체 구조 이해
