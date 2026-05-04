## 개발 환경
- Unity 6000.4.3f1, URP, UGUI, **New Input System** (구 `UnityEngine.Input` API 사용 불가)

## Unity 호환성 [IMPORTANT]
- 새 Unity 컴포넌트·패키지·API 사용 직후 **반드시 code-reviewer 스킬을 호출**하여 호환성 점검을 수행한다
- 상세 체크리스트(Input System / URP / DontDestroyOnLoad / 패키지 의존성 등)는 `.claude/skills/code-reviewer/SKILL.md`의 "6. Unity 6 호환성" 섹션 참조

## 언어 규칙
- 한국어 응답, 한국어 주석, 영어 변수명/함수명

## 코딩 스타일
- 들여쓰기: 4칸
- 네이밍: PascalCase / camelCase / _camelCase
- 비동기 처리: UniTask 사용

## 작업 규칙
- 요청이 명확하지 않으면 확인 질문
- 구현 전에 접근 방식 설명 후 진행
- 요청한 것만 구현

## 폴더/파일 생성 규칙 [IMPORTANT]
- 폴더나 파일을 생성하기 전에 반드시 Glob/PowerShell로 기존 구조를 먼저 파악한다
- 이 프로젝트의 게임 에셋은 모두 `Assets/_Project/` 하위에 위치한다:
  - 스크립트: `Assets/_Project/Scripts/` (하위: Managers, UI, Data)
  - 씬: `Assets/_Project/Scenes/`
  - ScriptableObject 에셋: `Assets/_Project/ScriptableObjects/`
  - UI 프리팹/에셋: `Assets/_Project/UI/`
  - 프리팹: `Assets/_Project/Prefabs/`
  - 애니메이션: `Assets/_Project/Animations/`
  - 머티리얼: `Assets/_Project/Materials/`
  - 에디터 스크립트: `Assets/_Project/Editor/`
  - 데이터 에셋: `Assets/_Project/Data/`
- `Assets/` 루트에 새 폴더를 직접 생성하지 않는다 (Plugins, Resources, Settings 등 Unity 예약 폴더 제외)

## Unity 씬/에셋 편집 규칙 [IMPORTANT]
- Unity 씬(.unity), 프리팹(.prefab), 에셋(.asset) 파일을 YAML로 직접 편집하는 것을 절대 금지한다
- GameObject 생성, 컴포넌트 추가/설정, 씬 저장 등 모든 Unity Editor 작업은 반드시 Unity MCP를 통해 수행한다
- Unity MCP를 사용할 수 없는 상황이면 작업을 중단하고 사용자에게 보고한다

## 작업 완료 체크리스트 [IMPORTANT]
모든 항목을 실제로 확인한 뒤 완료 보고. 추측으로 통과 처리 금지.
1. 컴파일 에러 없음 — Unity MCP로 콘솔 조회, "error CS" 검색
2. 작성한 코드를 다시 읽고 요청 사항과 대조
3. 매니저/씬 흐름/부트스트랩/DontDestroyOnLoad 관련 변경이 있으면 **playmode-tester 스킬을 호출하여 런타임 검증**을 수행한다 (단순 주석/텍스트 수정 등 위험 없는 변경은 생략)

## 정확성 규칙 [IMPORTANT]
- 확실하지 않은 API는 추측하지 말고 확인
- 존재 여부가 불확실한 클래스/컴포넌트를 임의로 생성하지 않음 (Grep/Glob으로 먼저 검색)
- 에러 해결 시 원인 파악 못 하면 추측 수정하지 말고 보고
- **불확실성 보고 원칙**: 자동 검증·분석 도구(playmode-tester, code-reviewer, perf-analyzer 등)의 결과가 도구 한계로 인한 false negative/positive 가능성이 있거나 신호 간 모순이 있을 때는 단언하지 않는다. "도구 결과는 X이지만 Y 가능성도 있어 확신하기 어렵다"고 솔직히 명시하고, 사용자가 직접 확인해야 할 항목(스크린샷 시각 검증, 실기 클릭 테스트, 데이터 무결성 점검 등)을 구체적으로 안내한다

## 설계 원칙
- 에디터 친화적 설계: 코드 수정 없이 인스펙터에서 조정 가능하도록 구성
- [SerializeField] 사용, public 필드 최소화

## 참조 문서
코드 구현 전 다음 파일이 존재하면 반드시 읽고 맥락 파악:
- docs/ARCHITECTURE.md — 클래스 구조 및 의존성
- docs/PRD.md — 기능 명세 및 요구사항
- docs/roadmap.md — 현재 구현 단계 및 우선순위

> 모든 PRD / roadmap / ARCHITECTURE 문서는 프로젝트 루트의 docs/ 폴더 안에 위치한다.

## 작업 관리
- 복잡한 멀티스텝 작업 시 Shrimp Task Manager(mcp__shrimp-task-manager)를 사용해 계획 및 추적