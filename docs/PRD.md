# PRD - 모바일 게임 프로토타입 (Arrows 참조 레벨 선택형)

## 핵심 정보

- **게임 장르**: 모바일 캐주얼 퍼즐 / 범용 스테이지 진행 프로토타입
- **타겟 플랫폼**: iOS, Android (모바일)
- **개발 환경**: Unity 6000.4.3f1, URP, UGUI
- **핵심 컨셉**: 플레이어가 스테이지를 순서대로 도전하고, 클리어 시 다음 스테이지가 개방되는 레벨 선택 구조. Arrows 게임의 UI/UX를 참고한 심플하고 직관적인 인터페이스. 최대 20스테이지로 구성되며, 진행 상태는 로컬에 저장되어 앱 재실행 시 복원된다.

---

## 핵심 게임 루프

```
앱 시작
  → 저장 데이터 불러오기 (Boot 씬)
    → 메인 화면 (게임 타이틀 + 현재 도전 스테이지 번호 + 플레이 버튼)
      → 인게임 화면 (현재 스테이지 번호 + 게임 영역 + 클리어 판정 / 실패 판정)
        → 클리어 판정: 클리어 연출 → 다음 스테이지 메인 화면
                       (20스테이지 클리어 시 → 게임 완료 화면)
        → 실패 판정: 실패 연출 → 현재 스테이지 메인 화면으로 복귀
```

> **프로토타입 단계**: 인게임 화면에 "클리어 버튼"과 "실패 버튼"을 배치하여 클리어/실패 판정을 수동으로 트리거한다.
> 이 버튼들은 추후 실제 게임플레이 규칙 구현 시 제거되며, 버튼이 호출하는 클리어/실패 판정 로직(GameFlowController)은 그대로 재사용된다.

---

## 기능 명세

### F001 - 메인 화면 (타이틀 화면)

- **설명**: 앱 실행 시 가장 먼저 보이는 화면. 게임 타이틀 텍스트와 현재 도전 가능한 스테이지의 플레이 버튼이 표시된다. 앱 재실행 시 저장된 진행 상태를 기반으로 플레이해야 할 스테이지 번호를 자동으로 표시한다.
- **MVP 필수 이유**: 플레이어가 게임에 진입하기 위한 첫 진입점.
- **구현 시스템**: UIManager, StageManager
- **UI 구성**:
  - 화면 중앙 상단: 게임 타이틀 텍스트
  - 화면 중앙: 현재 도전 스테이지 번호 표시 (예: "STAGE 1")
  - 화면 중앙 하단: 플레이 버튼 (터치 → 인게임 진입)
- **레이아웃 규칙**: Arrows 참조 — 배경은 단색 또는 간단한 그라디언트, 버튼은 화면 중앙에 크게 배치
- **상태**: 구현완료

---

### F002 - 인게임 화면

- **설명**: 실제 게임이 진행되는 화면. 프로토타입 단계에서는 실제 게임 콘텐츠 없이 클리어 판정을 직접 트리거하는 "클리어 버튼"과 "실패 버튼"을 표시한다.
- **MVP 필수 이유**: 게임 진행 판정 및 결과 분기의 핵심 화면.
- **구현 시스템**: UIManager, StageManager, GameFlowController
- **UI 구성**:
  - 화면 상단: 현재 스테이지 번호 표시 (예: "STAGE 1")
  - 화면 중앙: 게임 영역 (프로토타입에서는 빈 영역)
  - 화면 하단: 클리어 버튼 (초록 계열) / 실패 버튼 (빨강 계열)
- **버튼 동작**:
  - 클리어 버튼 터치 → `GameFlowController.OnStageClear()` 호출
  - 실패 버튼 터치 → `GameFlowController.OnStageFail()` 호출
- **프로토타입 버튼 설계 원칙**:
  - 클리어/실패 버튼은 판정 로직을 직접 보유하지 않는다.
  - 버튼은 오직 `GameFlowController`의 공개 메서드(`OnStageClear`, `OnStageFail`)를 호출하는 역할만 담당한다.
  - 추후 실제 게임플레이 규칙이 구현되면, 게임플레이 시스템이 동일한 메서드를 호출하고 이 버튼들은 제거된다.
- **상태**: 구현완료

---

### F003 - 스테이지 클리어 판정 및 연출

- **설명**: 클리어 판정 발생 시(`GameFlowController.OnStageClear()`) 클리어를 알리는 UI 연출을 표시하고, 다음 스테이지로 진행한다.
- **MVP 필수 이유**: 플레이어에게 성공을 인지시키는 피드백 제공 및 스테이지 진행 처리.
- **구현 시스템**: GameFlowController, UIManager, StageManager, SaveManager, AnimationController (DOTween 활용)
- **UI 구성**:
  - 화면 전체를 덮는 반투명 오버레이 패널
  - 클리어 텍스트 (예: "CLEAR!" 또는 "STAGE CLEAR")
  - 연출 완료 후 자동으로 다음 스테이지 메인 화면으로 전환
- **연출 방식**: DOTween Sequence — CanvasGroup 페이드인(0→0.85, 0.4초) + 텍스트 스케일 인(0→1, OutBack), 1.0초 대기 후 콜백
- **전환 타이밍**: 연출 재생(약 1.4초) → 다음 스테이지 메인 화면으로 전환
- **진행 처리**:
  - 클리어한 스테이지 번호를 SaveManager를 통해 즉시 저장 (연출 재생 전)
  - 연출 완료 콜백에서 IsLastStage 분기: 아니면 AdvanceToNextStage → MainPanel, 맞으면 GameClearPanel
- **구현 구조**: GameFlowController → UIManager.PlayClearAndThen(콜백) → ShowPanel(Clear) → AnimationController.PlayClear(콜백) → 콜백에서 다음 화면 전환
- **상태**: 구현완료

---

### F004 - 스테이지 실패 판정 및 연출

- **설명**: 실패 판정 발생 시(`GameFlowController.OnStageFail()`) 실패를 알리는 UI 연출을 표시하고, 동일 스테이지로 복귀한다.
- **MVP 필수 이유**: 플레이어에게 실패를 인지시키는 피드백 제공.
- **구현 시스템**: GameFlowController, UIManager, AnimationController (DOTween 활용)
- **UI 구성**:
  - 화면 전체를 덮는 반투명 오버레이 패널
  - 실패 텍스트 (예: "FAILED" 또는 "TRY AGAIN")
  - 연출 완료 후 자동으로 현재 스테이지 메인 화면으로 복귀
- **연출 방식**: DOTween Sequence — CanvasGroup 페이드인(0→0.85, 0.4초) + 텍스트 DOShakePosition(0.5초, 강도 20), 0.8초 대기 후 콜백
- **전환 타이밍**: 연출 재생(약 1.3초) → 동일 스테이지 메인 화면으로 복귀
- **진행 처리**:
  - 스테이지 번호 변경 없음 (현재 스테이지 유지)
  - 저장 데이터 변경 없음
- **구현 구조**: GameFlowController → UIManager.PlayFailAndThen(콜백) → ShowPanel(Fail) → AnimationController.PlayFail(콜백) → 콜백에서 MainPanel 복귀
- **상태**: 구현완료

---

### F005 - 스테이지 진행 저장 및 복원 ✅

- **설명**: 클리어한 최고 스테이지 번호를 로컬에 저장하고, 앱 재실행 시 복원하여 플레이어가 이어서 플레이해야 할 스테이지 번호를 자동으로 표시한다.
- **MVP 필수 이유**: 앱을 껐다 켜도 진행 상태를 유지해야 한다.
- **구현 시스템**: SaveManager, StageManager
- **저장 방식**: PlayerPrefs 사용 (키: `SaveManager.SAVE_KEY` 상수, 기본값: 0)
- **복원 로직**:
  - 저장된 최고 클리어 스테이지 + 1 = 현재 도전 스테이지
  - 예) MaxClearedStage = 3 → 메인 화면에 "STAGE 4" 표시
  - 예) MaxClearedStage = 0 (초기 상태) → 메인 화면에 "STAGE 1" 표시
  - 예) MaxClearedStage = 20 (전체 클리어) → 게임 완료 화면 표시
- **저장 타이밍**: 스테이지 클리어 판정 발생 즉시 저장 (연출 재생 전)
- **저장 방식 (M7 보강)**: 동기 `Save(int)` 외 `SaveAsync(int)` 비동기 메서드(UniTask.RunOnThreadPool 기반) 추가 제공. 모바일 스토리지 I/O로 인한 프리징 방지가 필요한 경우 비동기 경로 사용 가능
- **상태**: 구현완료

---

### F006 - 스테이지 범위 제한 (1~20스테이지) ✅

- **설명**: 게임의 총 스테이지는 1에서 20까지로 고정한다. StageManager는 이 범위를 초과하는 스테이지 번호를 허용하지 않는다.
- **MVP 필수 이유**: 게임의 전체 분량과 진행 구조를 명확히 정의.
- **구현 시스템**: StageManager
- **범위 규칙**:
  - 최소 스테이지: 1
  - 최대 스테이지: 20
  - 20스테이지 클리어 시 다음 스테이지로 진행하지 않고 게임 완료 처리
  - `IsLastStage()`: `== 20` 등호 단독 비교 (M7 정밀화 — SetCurrentStage의 Clamp로 범위 초과 차단, 의도를 명확히 표현)
- **데이터 구성**: StageDataSO를 20개 생성하여 각 스테이지 데이터를 정의
- **데이터 캐싱 (M7 보강)**: `CurrentStageData` 프로퍼티가 `_currentStageData` 필드에 결과를 캐시하여, 매 호출마다 배열 인덱싱 없이 O(1) 접근 가능. `SetCurrentStage()` 호출 시 캐시 무효화
- **상태**: 구현완료

---

### F007 - 게임 완료 화면

- **설명**: 20스테이지를 클리어했을 때 표시되는 엔딩 화면. 게임 전체 클리어를 축하하는 메시지를 표시한다.
- **MVP 필수 이유**: 20스테이지 클리어 후 자연스러운 게임 종료 지점 제공.
- **구현 시스템**: UIManager, GameFlowController
- **UI 구성**:
  - 축하 텍스트 (예: "ALL CLEAR!" 또는 "CONGRATULATIONS")
  - 처음부터 다시 시작하는 버튼 (선택적) 또는 타이틀 복귀 버튼
- **처음부터 다시 시작 시**: SaveManager.Reset() → StageManager.SetCurrentStage(1) → RefreshStageDisplay() → ShowPanel(Main)
- **상태**: 구현완료

---

### F008 - 반응형 UI 레이아웃

- **설명**: 다양한 모바일 화면 비율(16:9, 19.5:9, 20:9 등)에서 UI가 올바르게 표시되도록 Canvas Scaler와 Safe Area를 적용한다.
- **MVP 필수 이유**: 모바일 타겟 플랫폼 특성상 다양한 기기 해상도 대응 필수.
- **구현 시스템**: UIManager, SafeAreaHandler
- **적용 방식**:
  - Canvas Scaler: Scale With Screen Size, 기준 해상도 1080×1920, Match Width Or Height = 0.5
  - Safe Area 처리: SafeAreaHandler가 Awake()에서 Screen.safeArea 픽셀 좌표를 0~1 앵커 좌표로 변환하여 RectTransform에 적용
- **안정성 (M7 보강)**: SafeAreaHandler Awake()에서 RectTransform null 체크 추가. RectTransform이 없는 GameObject에 부착 시 NullReferenceException 대신 Debug.LogError 후 early return 처리
- **동적 모니터링 범위**: 화면 회전·멀티 윈도우 미지원 방침으로 정적 레이아웃 적용만 수행 (M7 확정)
- **상태**: 구현완료

---

## 시스템 구성

| 시스템 | 역할 | 관련 기능 |
|--------|------|-----------|
| **GameFlowController** | 게임 전체 흐름 제어. `OnStageClear()` / `OnStageFail()` 공개 메서드를 통해 클리어/실패 판정 처리 및 UIManager에 화면 전환 위임 | F002, F003, F004, F007 |
| **StageManager** | 현재 스테이지 번호 관리, 스테이지 범위(1~20) 제한(IsLastStage `==` 정밀화), 다음 스테이지 계산, StageDataSO 배열 보관 및 `CurrentStageData` O(1) 캐시 접근 | F001, F002, F005, F006 |
| **UIManager** | 화면별 UI 패널 활성화/비활성화(현재 패널 캐시로 O(1) ShowPanel), 버튼 이벤트 연결, AnimationController 위임(PlayClearAndThen / PlayFailAndThen), 인스펙터 패널 할당 검증 | F001, F002, F003, F004, F007, F008 |
| **SaveManager** | PlayerPrefs 기반 진행 상태 저장/불러오기(`SAVE_KEY` 상수화), 데이터 초기화, `SaveAsync(int)` 비동기 저장 옵션 | F005, F007 |
| **AnimationController** | DOTween Sequence 기반 클리어/실패 오버레이 연출. UIManager의 위임 요청을 받아 재생하고 완료 콜백 호출 | F003, F004 |
| **SafeAreaHandler** | 디바이스 Safe Area에 따라 UI RectTransform 앵커를 자동 조정. RectTransform null 체크 방어 코드 포함 | F008 |
| **BootLoader** | Boot 씬 전용 진입점. 저장 데이터 복원 후 Main 씬 로드. 씬 이름 `MAIN_SCENE_NAME` 상수화. `WaitUntil(UIManager.Instance != null)` 로 Main 씬 매니저 준비 대기 | — |
| **SceneBootstrapper** | 에디터 전용 유틸리티. Boot 이외의 씬에서 Play 시 자동으로 Boot 씬으로 리다이렉트 (`#if UNITY_EDITOR`) | — |

### 시스템 의존 관계

```
GameFlowController
  ├── StageManager  (스테이지 번호 조회/갱신, 범위 초과 여부 확인)
  ├── UIManager     (PlayClearAndThen / PlayFailAndThen / ShowPanel 호출)
  └── SaveManager   (클리어 저장 요청)

UIManager
  ├── StageManager        (RefreshStageDisplay에서 현재 스테이지 번호 조회)
  ├── SaveManager         (OnRestartButtonClick에서 Reset 호출)
  └── AnimationController (PlayClear / PlayFail 위임)

BootLoader
  ├── SaveManager         (Load 호출)
  └── StageManager        (SetCurrentStage 호출)

StageManager  ── (의존 없음, 독립)
SaveManager   ── (의존 없음, 독립)
SceneBootstrapper ── (에디터 전용, 의존 없음)
```

### 프로토타입 버튼과 GameFlowController의 관계

```
[현재 — 프로토타입 단계 (구현완료)]
클리어 버튼 (UI) → GameFlowController.OnStageClear()
                     → UIManager.PlayClearAndThen(콜백)
                       → AnimationController.PlayClear(콜백)
                         → 콜백: IsLastStage? → GameClearPanel / AdvanceToNextStage → MainPanel

실패 버튼 (UI)   → GameFlowController.OnStageFail()
                     → UIManager.PlayFailAndThen(콜백)
                       → AnimationController.PlayFail(콜백)
                         → 콜백: RefreshStageDisplay → MainPanel

[실제 게임플레이 구현 후]
클리어 버튼 제거
실패 버튼 제거
게임플레이 시스템 → GameFlowController.OnStageClear() / OnStageFail()
                    (동일한 메서드 재사용, 내부 로직 변경 없음)
```

---

## 데이터 모델

### ScriptableObject

#### StageDataSO
각 스테이지의 고정 데이터를 정의한다. 총 20개를 생성한다.

| 필드 | 타입 | 설명 |
|------|------|------|
| stageNumber | int | 스테이지 번호 (1~20) |
| stageName | string | 화면에 표시할 스테이지 이름 (예: "STAGE 1") |
| clearAnimationType | enum | 클리어 연출 종류 (기본값: Default) |
| failAnimationType | enum | 실패 연출 종류 (기본값: Default) |

### 런타임 저장 데이터 (PlayerPrefs)

| 키 | 타입 | 설명 | 기본값 |
|----|------|------|--------|
| MaxClearedStage | int | 클리어한 최고 스테이지 번호 | 0 |

> `MaxClearedStage = 0`: 아직 클리어한 스테이지 없음 → 1스테이지 도전
> `MaxClearedStage = N`: N스테이지까지 클리어 → (N+1)스테이지 도전
> `MaxClearedStage = 20`: 전체 클리어 → 게임 완료 화면 표시

### 씬 구성

| 씬 이름 | 설명 |
|---------|------|
| Boot | 초기화 전용 씬 (저장 데이터 불러오기 후 Main으로 전환) |
| Main | 메인 화면 + 인게임 화면 + 연출 패널 + 게임 완료 화면을 모두 포함하는 단일 씬 |

> 단일 씬 구성(Main 1개)으로 씬 전환 없이 패널 활성화/비활성화 방식으로 화면을 전환한다.
> SceneBootstrapper(에디터 전용)를 통해 Main 씬에서 직접 Play 버튼을 눌러도 자동으로 Boot 씬부터 시작된다.

---

## UI 화면 전환 흐름

```
[앱 실행]
    ↓ Boot 씬: 저장 데이터 불러오기
[메인 패널] (타이틀 + 스테이지 번호 + 플레이 버튼)
    ↓ 플레이 버튼 터치
[인게임 패널] (스테이지 번호 + 게임 영역 + 클리어 버튼 + 실패 버튼)
    ↓ OnStageClear()            ↓ OnStageFail()
[클리어 연출 패널]          [실패 연출 패널]
    ↓ 연출 완료                     ↓ 연출 완료
    ├── 스테이지 < 20:          [메인 패널] 동일 스테이지
    │   [메인 패널] 스테이지+1
    └── 스테이지 = 20:
        [게임 완료 패널]
```

---

## UI/UX 레퍼런스 (Arrows 참조)

- **배경**: 진한 단색 배경 (검정 또는 다크 계열)
- **버튼 스타일**: 원형 또는 사각형 버튼, 강조색(화이트/골드) 텍스트
- **스테이지 표시**: 화면 중앙 상단, 큰 폰트로 스테이지 번호 표시
- **플레이 버튼**: 화면 중앙~하단에 크게 배치, 탭 유도형 디자인
- **클리어 연출**: 밝고 긍정적인 색상(골드/옐로우) + 스케일 인 애니메이션
- **실패 연출**: 어둡고 붉은 계열 색상 + 흔들기(shake) 애니메이션
- **전체 레이아웃**: Safe Area를 고려한 수직 중앙 정렬, 여백 충분

---

## MVP 이후 기능

다음 기능은 현재 MVP 범위에서 제외된다:

- 실제 인게임 콘텐츠 (퍼즐, 액션 등 게임 플레이) — 구현 시 프로토타입 버튼(클리어/실패 버튼) 제거
- BGM / 효과음 시스템
- 스테이지 선택 맵 화면 (스테이지 목록 UI)
- 별점(Star) 평가 시스템
- 광고 연동 (보상형 광고, 배너)
- 인앱 결제
- 소셜 기능 (리더보드, 업적)
- 튜토리얼 시스템
- 스테이지 에디터

---

## 정합성 검증

| 번호 | 항목 | 결과 |
|------|------|------|
| 1 | 모든 기능이 시스템 구성에 매핑되어 있는가? | 통과 (F001~F008 모두 매핑) |
| 2 | 시스템 구성에 기능 명세 없는 항목이 있는가? | BootLoader·SceneBootstrapper는 기반 인프라/에디터 전용 유틸리티로 기능 명세 외 항목 — 정상 |
| 3 | ARCHITECTURE.md와 구현 상태 일치하는가? | 통과 (M7 완료 기준 동기화 — F001~F008 모두 구현완료, M7 보강 사항(SafeAreaHandler null 체크·UIManager 패널 검증·ShowPanel O(1)·STAGE_PREFIX 상수·StageManager CurrentStageData 캐시·SaveAsync·BootLoader WaitUntil·MAIN_SCENE_NAME 상수) 반영) |
| 4 | 기능 간 의존 관계가 명시되어 있는가? | 통과 (시스템 의존 관계 다이어그램 및 콜백 체인 흐름 포함) |
| 5 | 누락되거나 고아 상태인 항목이 없는가? | 없음 |
