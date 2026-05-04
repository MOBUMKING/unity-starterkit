---
name: playmode-tester
description: >
  Unity Editor의 Play Mode를 자동으로 진입시켜 런타임 동작을 검증하고 콘솔 에러·활성 화면 상태·스크린샷을 수집해 Pass/Fail로 보고한다.
  정적 분석(code-reviewer)으로는 잡을 수 없는 런타임 NullRef·씬 초기화 순서·DontDestroyOnLoad 누락·매니저 의존 순서·UI 표시 결과 등을 실제 실행으로 확인한다.
  다음 상황에서 반드시 이 스킬을 사용해야 한다:
  - "Play 검증해줘", "런타임 확인해줘", "에디터에서 동작하나 봐줘", "Play Mode 테스트" 등 명시 요청
  - 매니저 클래스(Manager, Controller, System) 신규 작성 또는 수정 직후
  - 씬에 핵심 컴포넌트(EventSystem, Canvas, Camera, AudioListener 등) 추가/제거 직후
  - Boot 씬 ↔ Main 씬 흐름이나 씬 로드 로직 변경 직후
  - DontDestroyOnLoad 사용 코드 추가/변경 직후
  - SceneBootstrapper, RuntimeInitializeOnLoadMethod 등 부트스트랩 코드 변경 직후
  - 마일스톤(M1~M9 등) 완료 보고 직전 회귀 검증
context: fork
agent: Explore
allowed-tools: Read, Glob, Grep, mcp__UnityMCP__manage_editor, mcp__UnityMCP__manage_scene, mcp__UnityMCP__read_console, mcp__UnityMCP__find_gameobjects, mcp__UnityMCP__manage_camera, mcp__UnityMCP__refresh_unity, mcp__UnityMCP__execute_code
---

# 플레이 모드 테스터 (playmode-tester)

## 역할
Unity Editor를 자동 조작하여 Play Mode 진입 후 런타임 상태를 점검하고 결과를 Pass/Fail로 보고한다.
**코드를 수정하지 않는다.** 발견된 문제는 보고서에만 기재하고 수정 여부는 사용자가 결정한다.

## 작업 순서

1. **사전 점검**
   - `read_console`로 현재 컴파일 에러(`error CS`) 존재 여부 확인 → 있으면 즉시 중단하고 보고
   - `manage_editor` 또는 `editor_state` 리소스로 현재 Play Mode 상태 확인 → 이미 Play 중이면 먼저 중지

2. **시작 씬 결정**
   - 사용자가 시나리오로 "Main에서 Play → SceneBootstrapper 검증" 같은 특정 시작점을 지정했다면 그 씬을 활성화
   - 그렇지 않으면 `Boot` 씬을 활성화 (기본)
   - `manage_scene get_active`로 현재 활성 씬 확인 후 필요 시 `manage_scene load`로 변경

3. **Play Mode 진입**
   - `manage_editor` action으로 Play Mode 시작
   - 씬 전환 + Awake/Start 완료를 위해 충분한 대기 (기본 3초, 비동기 로딩/SceneBootstrapper 리다이렉트 포함 시 4~5초)

4. **런타임 상태 수집** — *우선순위 순서대로*
   - **(우선순위 1) 콘솔 시그니처** — 가장 신뢰도 높은 진실 근거
     - `read_console types=["error","warning"]`로 전체 수집 (`include_stacktrace=true`)
     - 명시적 시그니처 워닝(예: `[UIManager] StageManager.Instance가 null입니다`, `[X] Y가 null입니다` 패턴)이 발견되면 그 매니저는 **부재**로 판정
     - 시그니처 워닝이 **없으면** 해당 매니저는 정상으로 간주
   - **(우선순위 2) 스크린샷** — 시각 결과의 진실 근거
     - `manage_camera screenshot`으로 게임 화면 캡처 → `Assets/Screenshots/playmode-{timestamp}.png` 저장
     - 의도한 텍스트(예: "STAGE N", "MINI GAME"), 의도한 패널 색상, 버튼 등이 보이면 PASS
     - 이 결과는 "DontDestroyOnLoad 객체 부재"라고 추정되는 도구 결과를 **재반박**할 수 있다 (스크린샷이 우선)
   - **(우선순위 3) GameObject 검색** — 보조 신호 (false negative 위험 인지)
     - `find_gameobjects`는 일반 씬만 보므로 DontDestroyOnLoad 가상 씬의 매니저 싱글턴은 누락될 수 있다
     - 따라서 "찾을 수 없음" 결과만으로 매니저 부재 단정 금지 — 우선순위 1·2와 종합 판단
     - DontDestroyOnLoad 매니저 검증은 `mcpforunity://scene/loaded` 리소스나 `find_gameobjects search_method=by_component`로 보강
     - UI 활성 상태는 `activeSelf` 대신 **`activeInHierarchy`**를 기준으로 판정

5. **자가 검증으로 불확실성 해소 [IMPORTANT]** — *Play Mode 종료 전 수행*
   - 4단계에서 도구 결과 간 모순이 발생하거나 도구 한계 영역(DontDestroyOnLoad 격리, 정적 Instance 필드 검증 등)에 부딪혔다면, 사용자에게 떠넘기기 전에 `execute_code`로 직접 검증을 시도한다
   - **자가 검증 가능 조건 (3가지 모두 만족 시 직접 실행)**:
     1. 기존 코드를 해치지 않는다 (스크립트/씬/에셋을 영구 변경하지 않음 — 일회성 실행만)
     2. 비용이 적다 (수 초 안에 끝남)
     3. 자가 검증 가능하다 (`execute_code`로 결과를 직접 받을 수 있음)
   - **사전 확인 — Roslyn 백엔드 [IMPORTANT]**:
     - 첫 `execute_code` 응답의 `"compiler"` 필드 확인. `"roslyn"`이면 진행. `"codedom"` 또는 mono.exe 에러면 Roslyn 손실 신호 → 자가 검증 중단하고 보고서에 "Roslyn 손실 — 메인 컨텍스트에서 `docs/roslyn-recovery.md` 절차로 복구 필요" 명시 (이 스킬은 fork 컨텍스트라 직접 복구 금지)
   - **권장 패턴 — 정적 Instance 검증 (DontDestroyOnLoad 매니저용)**:
     ```csharp
     // execute_code 'execute' 액션에 전달
     var sm = SaveManager.Instance;
     var stm = StageManager.Instance;
     var um = UIManager.Instance;
     return $"SaveManager={(sm != null ? sm.gameObject.scene.name : "NULL")}, " +
            $"StageManager={(stm != null ? $"{stm.gameObject.scene.name} stage={stm.CurrentStageNumber}" : "NULL")}, " +
            $"UIManager={(um != null ? um.gameObject.scene.name : "NULL")}";
     ```
   - 결과 문자열에 `scene.name == "DontDestroyOnLoad"` 가 찍히면 매니저 정상 격리 확정 → "⚠️ 불확실"을 "✅ PASS"로 승격
   - 자가 검증 후에도 남은 불확실성(시각적 디테일, 인터랙션 후 동작 등)만 사용자 확인 요청에 포함

6. **Play Mode 중지** — *항상 실행 (try-finally 마인드)*
   - `manage_editor` action으로 Play 중지
   - 도중 어떤 단계가 실패하더라도 Editor를 Play Mode 상태로 방치하지 않는다

7. **보고서 작성**
   - 아래 출력 형식대로 정리
   - 결론은 **콘솔 시그니처 + 스크린샷 + 자가 검증 결과**를 근거로 내리고, GameObject 검색 결과는 부가 정보로만 첨부

## 검증 체크 항목

### 필수 (1개라도 실패하면 FAIL) — *콘솔 + 스크린샷 근거*
- 컴파일 에러 0건 (`read_console` 결과에 `error CS` 0건)
- 런타임 에러 0건 (`NullReferenceException`, `MissingReferenceException`, `InvalidOperationException` 등)
- Play Mode 진입 성공 (Editor 상태가 Play로 전환됨)
- 씬 흐름 검증: 의도한 시작 씬에서 의도한 후속 씬으로 전환 완료 (예: Boot → Main)
- **스크린샷에 의도한 핵심 UI가 보임** (텍스트, 버튼, 패널 등)

### 권장 (있으면 Warning으로 보고)
- 명시적 시그니처 워닝 부재 (예: `[Manager] X가 null입니다` 패턴이 콘솔에 없어야 함)
- EventSystem 중복(2개 이상) 부재
- `LogWarning` 발생 건수 (정상 범위 내인지)

### 판정 우선순위 — *false positive/negative 방지*
1. **콘솔 명시적 시그니처가 진실** — 매니저 부재를 의심하면 해당 매니저의 null 워닝이 콘솔에 있는지 확인
2. **스크린샷이 시각 결과의 진실** — 의도한 화면이 보이면 매니저들도 정상 동작했다고 봐야 함 (텍스트는 매니저 데이터 의존이므로)
3. **GameObject 검색 결과는 보조** — DontDestroyOnLoad 격리, 비동기 활성화 타이밍 등 false negative 함정이 있음. 단독 근거로 FAIL 판정 금지

### Unity 6 호환성 시그널 (즉시 보고 대상)
다음 에러 메시지 패턴이 콘솔에서 발견되면 추정 원인까지 함께 보고:
- `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class` → 구 Input API 사용. `StandaloneInputModule` 잔존 가능성
- `Object reference not set to an instance of an object` + 매니저 클래스 라인 → 싱글턴 초기화 순서 또는 DontDestroyOnLoad 누락
- `Scene 'X' couldn't be loaded` → Build Settings에 씬 누락 또는 경로 오류
- 머티리얼이 핑크색(magenta) 표시 → URP 미호환 셰이더 사용
- `[Manager] X.Instance가 null입니다` → 매니저 의존 순서 문제 (Awake에서 다른 매니저 참조 등)

## 출력 형식

반드시 아래 마크다운 형식으로 출력한다:

```markdown
## Play Mode 검증 결과: ✅ PASS / ❌ FAIL

### 실행 환경
- 시작 씬: Boot
- 진입 후 활성 씬: Main
- Play Mode 유지 시간: 3초
- 스크린샷: `docs/screenshots/playmode-20260504-142035.png`

### 콘솔 점검
| 카테고리 | 건수 | 비고 |
|---------|------|------|
| 컴파일 에러 (CS) | 0 | - |
| 런타임 에러 | 0 | - |
| 경고 | 1 | StageManager 관련 (아래 상세) |

### 핵심 GameObject 상태
| 대상 | 기대 | 실측 | 결과 |
|------|------|------|------|
| SaveManager.Instance | 활성 | 활성 (DontDestroyOnLoad) | ✅ |
| StageManager.Instance | 활성 | 활성 (DontDestroyOnLoad) | ✅ |
| UIManager.Instance | 활성 | 활성 | ✅ |
| 활성 시작 패널 | MainPanel | MainPanel | ✅ |
| EventSystem 개수 | 1 | 1 | ✅ |

### 발견된 이슈
*(이슈가 없으면 "발견된 이슈 없음"으로 명시)*

#### 심각도: 높음 🔴
- **에러 메시지**: `NullReferenceException at GameFlowController.OnStageClear() ...`
- **추정 원인**: SaveManager.Instance가 null. Boot 씬 미경유 가능성
- **재현 조건**: Main 씬에서 직접 Play 시작 시
- **권장 조치**: SceneBootstrapper 패턴 또는 매니저 자체 fallback 초기화

#### 심각도: 중간 🟡
- **경고 메시지**: `[UIManager] StageManager.Instance가 null입니다.`
- **위치**: UIManager.RefreshStageDisplay()
- **권장 조치**: ...

### 종합 판정
다음 세 단계 중 하나로 결론을 명시한다:
- **✅ PASS (확실)** — 콘솔 시그니처와 스크린샷 모두 정상. 다음 단계 진행 가능
- **❌ FAIL (확실)** — 콘솔에 명확한 에러/시그니처 워닝이 있어 즉시 수정 필요. 추정 원인과 권장 조치 명시
- **⚠️ 불확실 (사용자 확인 필요)** — 도구 결과 간 모순이 있거나 검증 도구의 한계 영역(예: DontDestroyOnLoad 격리, 비동기 활성화 타이밍, 시각적 디테일)이라 단언이 어려움. 아래 형식으로 사용자에게 확인 요청

#### ⚠️ 사용자 확인 요청 (불확실 케이스 전용)
- **관찰된 신호**: [도구가 어떤 결과를 냈는지 구체적으로]
- **불확실한 이유**: [왜 단언할 수 없는지 — 도구 한계 명시]
- **사용자가 확인해야 할 항목**:
  1. [구체적 행동, 예: "캡처된 스크린샷에 'STAGE N' 텍스트가 보이는지 직접 확인"]
  2. [구체적 행동, 예: "Play Mode에서 PLAY 버튼을 눌러 InGame 화면으로 전환되는지 확인"]
- **확인 결과에 따른 다음 단계**:
  - 사용자가 정상이라고 답하면 → 도구 false negative로 기록, 스킬 보강 고려
  - 사용자가 비정상이라고 답하면 → 추정 원인을 토대로 수정 진행
```

## 주의사항
- **코드를 직접 수정하지 않는다.** 보고만 수행한다. 수정은 메인 컨텍스트에서 사용자 결정 후 진행한다.
- Play Mode 진입 전 컴파일 에러가 있으면 진입 자체가 실패하므로, 사전 점검에서 발견되면 즉시 중단 보고한다.
- Play Mode 종료는 try-finally 마인드로 **반드시 마지막에 실행**한다 (도중 실패해도 Editor를 Play Mode 상태로 방치하지 않는다).
- 씬 로드 + 매니저 초기화 시간은 프로젝트마다 다르므로, 첫 호출에서 부족하면 대기 시간을 늘려 재시도한다.
- 스크린샷 폴더(`Assets/Screenshots/`)가 없으면 캡처 전에 생성한다.
- **단언 금지 원칙 [중요]**: 도구 결과만으로 단정하기 어려운 영역(아래 케이스)에서는 PASS/FAIL을 단언하지 말고 "⚠️ 불확실" 판정으로 처리하여 사용자 확인을 요청한다. 거짓 PASS는 사용자가 의심 없이 다음 단계로 넘어가게 만들고, 거짓 FAIL은 사용자에게 불필요한 수정 부담을 지운다 — 둘 다 도구 신뢰도를 갉아먹는다.

### 단언 금지가 필요한 케이스 (자주 발생)
- **DontDestroyOnLoad 격리**: 매니저 싱글턴이 일반 GameObject 검색에서 누락 → 객체 부재만으로 FAIL 단정 금지. **자가 검증 우선** (작업 순서 5번 참조 — `execute_code`로 정적 Instance·scene.name 검증)
- **비동기 활성화 타이밍**: `activeSelf=false`로 잡혔어도 다음 프레임에 활성화될 수 있음 → 단일 시점 스냅샷만으로 단정 금지
- **시각적 디테일**: 색상 정확도, 애니메이션 부드러움, 폰트 가독성 등 → 스크린샷 한 장으로 판정 불가 (사용자 직접 확인)
- **인터랙션 후 동작**: 버튼 클릭/키 입력 결과는 이 스킬에서 시뮬레이션하지 않음 → 사용자 실기 테스트 필요
- **도구 결과 간 모순**: 콘솔에 워닝 없는데 객체 검색은 부재 등 → **자가 검증 우선 시도** (3가지 조건 만족 시), 자가 검증 후에도 모호하면 사용자 확인 요청

### 자가 검증 우선 원칙 [IMPORTANT]
"⚠️ 불확실" 판정으로 사용자에게 떠넘기기 전에, 자가 검증 가능한 영역인지 먼저 확인한다. 자가 검증 3조건(기존 코드 미변경 + 저비용 + execute_code 등으로 직접 확인 가능)을 만족하면 컨펌 없이 바로 실행하고, 결과를 보고에 반영한다. 사용자 확인 요청은 자가 검증으로도 해소되지 않는 영역(시각/인터랙션/주관적 평가)에만 한정한다.

## 한계
- **사용자 인터랙션(버튼 클릭, 키 입력) 시뮬레이션은 수행하지 않는다.** Play 진입 직후의 정적 상태만 검증한다. 인터랙션 후 동작까지 검증하려면 PlayMode 테스트(`test-writer` 스킬)가 적합하다.
- Domain Reload 시간으로 인해 매 호출마다 10~30초 소요될 수 있다. 단순 텍스트 변경 등 위험 없는 작업에는 호출하지 않는다.
- 시각적 디테일(애니메이션 부드러움, 색상 정확도)은 스크린샷 한 장으로 판정 불가. 시각 검증은 사람이 최종 확인한다.
- **자가 검증(`execute_code`)은 Roslyn DLL이 `Assets/Plugins/Roslyn/`에 존재할 때만 가능**하다. CodeDom 폴백은 mono.exe CommandLine 32KB 한계로 이 프로젝트에서 작동하지 않는다 (Step 5 사전 확인 항목 참조).
