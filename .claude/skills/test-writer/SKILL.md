---
name: test-writer
description: >
  Unity Test Framework 기반 유닛/통합 테스트를 작성할 때 사용. 다음 상황에서 반드시 이 스킬을 사용해야 한다:
  - "테스트 작성해줘", "유닛 테스트 만들어줘", "테스트 코드 써줘" 등 테스트 생성 요청
  - 기능 구현 완료 후 테스트 보충 요청
  - "EditMode 테스트", "PlayMode 테스트", "NUnit 테스트" 관련 요청
  - "[클래스명] 테스트해줘" 등 특정 클래스에 대한 테스트 작성 요청
  - 기존 테스트 케이스가 부족하거나 예외 케이스를 추가해야 할 때
---

# 테스트 작성기

## 역할
Unity Test Framework 기반 유닛 테스트 및 통합 테스트를 작성한다.

## 작업 순서
1. ARCHITECTURE.md를 읽고 대상 클래스의 의존성과 연결 관계를 파악
2. 대상 스크립트를 읽고 구조, 메서드 시그니처, 접근 제한자 파악
3. Tests/ 폴더에 기존 테스트가 있으면 패턴을 확인하고 동일하게 작성
4. EditMode/PlayMode 판단
5. 테스트 작성
6. 자체 검토 체크리스트 확인

## 규칙
- CLAUDE.md 코딩 스타일 및 코드 컨벤션 준수
- 타입: `object`나 `dynamic` 남용 금지, 명시적 타입 사용
- 서드파티 의존성이 있는 클래스 테스트 시 docs/ 폴더의 관련 문서 참고

## 파일 규칙
- EditMode 테스트: `Assets/_Project/Tests/EditMode/`
- PlayMode 테스트: `Assets/_Project/Tests/PlayMode/`
- 파일명: `[대상클래스명]Tests.cs`
- Assembly Definition 파일이 없으면 생성 여부를 사용자에게 안내

## NUnit 테스트 규칙
- 어트리뷰트: `[TestFixture]`, `[Test]`, `[SetUp]`, `[TearDown]`, `[TestCase]`
- 테스트 메서드명: `[메서드명]_[상황]_[기대결과]` 패턴
- 한 테스트당 Assert 하나 원칙 (여러 조건은 테스트 분리)
- `Assert.That()` 문법 선호
- 경계값, 예외 케이스, 정상 케이스 모두 포함

## EditMode vs PlayMode

| 케이스 | 유형 |
|--------|------|
| 순수 로직 (계산, 데이터 처리, 알고리즘) | EditMode |
| ScriptableObject 데이터 검증 | EditMode |
| MonoBehaviour, 씬, 물리, 애니메이션 관련 | PlayMode |
| 여러 시스템이 연결되는 기능 | PlayMode 통합 테스트 |

가능하면 EditMode를 우선한다. EditMode는 Play 버튼 없이 수백ms 안에 실행되어 빠른 피드백을 제공한다.

## 비동기 테스트
- UniTask 기반 비동기 테스트 우선 사용
- `[UnityTest]` + `IEnumerator`는 물리/프레임 대기 등 반드시 필요한 경우에만 허용

## 자체 검토 체크리스트

작성 완료 후 반드시 확인:

- [ ] CLAUDE.md 코드 컨벤션 준수 (들여쓰기 4칸, 네이밍 등)
- [ ] 파일 경로가 `Assets/_Project/Tests/EditMode` 또는 `PlayMode`인가?
- [ ] 파일명이 `[클래스명]Tests.cs` 형식인가?
- [ ] 모든 테스트 메서드명이 `[메서드명]_[상황]_[기대결과]` 패턴인가?
- [ ] 각 테스트에 Assert가 하나인가?
- [ ] `[SetUp]`과 `[TearDown]`이 적절히 사용되었는가?
- [ ] 정상, 경계값, 예외 케이스가 모두 포함되었는가?
- [ ] EditMode/PlayMode 구분이 올바른가?
- [ ] 주석이 한국어로 작성되었는가?

## 출력 형식

테스트 작성 완료 후 보고:
1. 생성된 파일 경로
2. 테스트 유형 (EditMode/PlayMode) 및 선택 이유
3. 작성된 테스트 목록 및 커버 케이스 요약
4. 추가로 고려할 수 있는 테스트 케이스 제안
