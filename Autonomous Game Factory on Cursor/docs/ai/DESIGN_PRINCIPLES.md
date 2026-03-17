# 설계 원칙

## 1. 핵심 철학

- **AI는 도구, 규칙은 사람이 만든다** — AI가 자율적으로 규칙을 변경하지 않는다.
- **안전하고 점진적으로** — 한 기능 = 한 모듈 단위로 개발·검증·확인.

## 2. 모듈 아키텍처

각 모듈은 5가지 파일로 구성:

| 파일 | 역할 | 상속 |
|------|------|------|
| `I<Module>.cs` | 공개 계약 | - |
| `<Module>Config.cs` | 설정 데이터 | `ScriptableObject` |
| `<Module>Runtime.cs` | 비즈니스 로직 | 없음 (순수 C#) |
| `<Module>Factory.cs` | 생성 | `static class` |
| `<Module>Bootstrap.cs` | 씬 진입점 | `MonoBehaviour` (얇게) |

왜 이 구조인가:
1. Runtime이 MonoBehaviour를 상속하지 않으므로 순수 유닛 테스트 가능
2. 설정/로직/생성/바인딩이 분리됨
3. Factory를 통한 의존성 주입
4. AI가 템플릿을 복사하기만 하면 됨

## 3. 검증 시스템

- `IModuleValidator` 인터페이스 기반 (개방-폐쇄 원칙)
- 각 검증기 독립적
- 하나의 `ValidationReport`에 결과 누적
- Error = 반드시 수정, Warning = 참고

## 4. Unity-Compatible 구조

프레임워크 저장소 = Unity 프로젝트 구조:
- 경로 변환 불필요
- Rule/Validator/Template 경로가 프레임워크와 프로젝트에서 동일
- `cp -r`로 주입 완료

## 5. 프로젝트 독립성

프레임워크가 강제하는 것:
- 모듈 구조 패턴
- AI 워크플로
- 검증 인프라

프레임워크가 강제하지 않는 것:
- 게임 장르
- 구체적 코딩 규칙 (예시로 제공)
- 모듈 목록 (프로젝트가 정의)
- 네임스페이스 (기본 `Game`은 변경 가능)
