# ARCHITECTURE.md — 모바일 게임 프로토타입

> 마지막 갱신: 2026-05-04 (M5 완료, GameFlowController 구현 및 버튼 연결)

---

## 현재 구현된 클래스

### Data 레이어 (`Assets/_Project/Scripts/Data/`)

#### `AnimationType` (enum)
- **파일**: `AnimationType.cs`
- **역할**: 스테이지 클리어/실패 연출 종류 정의
- **값**: `Default = 0`, `Special = 1`
- **의존**: 없음

#### `StageDataSO` (ScriptableObject)
- **파일**: `StageDataSO.cs`
- **역할**: 각 스테이지의 고정 데이터 보관 (에셋 20개: Stage01~Stage20)
- **필드** (모두 `[SerializeField] private`):
  - `_stageNumber: int` — 스테이지 번호 (1~20)
  - `_stageName: string` — 화면 표시용 이름 (예: "STAGE 1")
  - `_clearAnimationType: AnimationType`
  - `_failAnimationType: AnimationType`
- **프로퍼티**: `StageNumber`, `StageName`, `ClearAnimationType`, `FailAnimationType` (get-only)
- **에셋 경로**: `Assets/_Project/ScriptableObjects/Stage01.asset` ~ `Stage20.asset`
- **의존**: `AnimationType`

---

### UI 레이어 (`Assets/_Project/Scripts/UI/`)

#### `SafeAreaHandler` (MonoBehaviour) ✅
- **파일**: `SafeAreaHandler.cs`
- **역할**: 디바이스의 Safe Area에 맞춰 RectTransform 앵커를 자동 조정
- **부착 대상**: Canvas 자식인 SafeArea GameObject
- **주요 멤버**:
  - `Awake()` — `ApplySafeArea()` 호출
  - `ApplySafeArea(): void` — Screen.safeArea 픽셀 좌표를 0~1 앵커 좌표로 변환해 적용
- **의존**: 없음 (순수 UI 조정)
- **참고**: 노치/홈바 있는 기기에서 UI 침범 방지

---

### Manager 레이어 (`Assets/_Project/Scripts/Managers/`)

#### `StageManager` (MonoBehaviour, 싱글턴) ✅
- **파일**: `StageManager.cs`
- **역할**: 현재 스테이지 번호(1~20) 관리, 스테이지 데이터 배열 보관
- **싱글턴**: `DontDestroyOnLoad`, `Instance` 정적 프로퍼티
- **주요 멤버**:
  - `[SerializeField] private StageDataSO[] _stageData` — Inspector에서 20개 할당
  - `CurrentStageNumber: int` (get-only 프로퍼티)
  - `SetCurrentStage(int)` — 범위 클램프 (Mathf.Clamp 1~20)
  - `AdvanceToNextStage()` — IsLastStage() 체크 후 +1
  - `IsLastStage(): bool` — `_currentStageNumber >= 20`
  - `GetCurrentStageData(): StageDataSO` — 인덱스 범위 체크 포함
- **의존**: `StageDataSO`
- **역참조 금지**: `GameFlowController`, `UIManager`, `SaveManager` 직접 참조 불가

#### `SaveManager` (MonoBehaviour, 싱글턴) ✅
- **파일**: `SaveManager.cs`
- **역할**: PlayerPrefs "MaxClearedStage" 키 캡슐화
- **싱글턴**: `DontDestroyOnLoad`, `Instance` 정적 프로퍼티
- **주요 멤버**:
  - `private const string SAVE_KEY = "MaxClearedStage"`
  - `Save(int maxClearedStage)` — PlayerPrefs.SetInt + Save
  - `Load(): int` — PlayerPrefs.GetInt(SAVE_KEY, 0)
  - `Reset()` — PlayerPrefs.DeleteKey + Save
- **의존**: 없음 (다른 Manager 참조 금지)

#### `BootLoader` (MonoBehaviour) ✅
- **파일**: `BootLoader.cs`
- **역할**: Boot 씬 전용 진입점. 저장 데이터를 복원하고 Main 씬으로 전환한다.
- **싱글턴 아님**: Boot 씬과 함께 파괴되므로 DontDestroyOnLoad 불필요
- **주요 멤버**:
  - `Awake()` — CancellationTokenSource 초기화, `InitializeAsync().Forget()`
  - `InitializeAsync(CancellationToken): UniTask` — `SaveManager.Load()` → `StageManager.SetCurrentStage(maxCleared+1)` → `SceneManager.LoadSceneAsync("Main")`
  - `OnDestroy()` — `_cts?.Cancel()` / `_cts?.Dispose()`
- **비동기**: UniTask + CancellationToken 패턴
- **의존**: `SaveManager`, `StageManager`

#### `GameFlowController` (MonoBehaviour, 싱글턴) ✅
- **파일**: `GameFlowController.cs`
- **역할**: 스테이지 클리어/실패 판정을 처리하고 StageManager·SaveManager·UIManager를 오케스트레이션한다
- **싱글턴**: 중복 인스턴스 제거만, `DontDestroyOnLoad` 없음 (Main 씬 전용)
- **주요 멤버**:
  - `OnStageClear(): void` — SaveManager.Save → IsLastStage 분기 → (true) ShowPanel(GameClear) / (false) AdvanceToNextStage → RefreshStageDisplay → ShowPanel(Main)
  - `OnStageFail(): void` — RefreshStageDisplay → ShowPanel(Main) (스테이지 번호 변경 없음)
- **의존**: `StageManager`, `SaveManager`, `UIManager`, `PanelType`
- **M6 준비**: OnStageClear/OnStageFail 내부에 `// TODO(M6): AnimationController 콜백 삽입 위치` 주석 포함
- **배치**: Main 씬 루트 (Canvas 외부)

#### `UIManager` (MonoBehaviour, 싱글턴) ✅
- **파일**: `UIManager.cs`
- **역할**: Main 씬의 UI 패널(Main, InGame, Clear, Fail, GameClear) 활성화/비활성화 관리
- **싱글턴**: 중복 인스턴스 제거만, `DontDestroyOnLoad` 없음 (Main 씬 전용)
- **주요 멤버**:
  - `[SerializeField] private GameObject _mainPanel`
  - `[SerializeField] private GameObject _inGamePanel`
  - `[SerializeField] private GameObject _clearPanel`
  - `[SerializeField] private GameObject _failPanel`
  - `[SerializeField] private GameObject _gameClearPanel`
  - `[SerializeField] private TMP_Text _mainStageText`
  - `[SerializeField] private TMP_Text _inGameStageText`
  - `private Dictionary<PanelType, GameObject> _panelMap` — 패널 맵
  - `ShowPanel(PanelType): void` — 지정 패널만 활성화, 나머지 비활성화
  - `RefreshStageDisplay(): void` — StageManager에서 현재 스테이지 번호를 읽어 스테이지 텍스트 갱신
  - `OnPlayButtonClick(): void` — 플레이 버튼 클릭 시, 인게임 화면으로 전환
  - `OnRestartButtonClick(): void` — 처음부터 다시 시작 버튼 클릭 시, 데이터 초기화 후 메인 화면으로 복귀
- **의존**: `StageManager`, `SaveManager`, `PanelType`
- **참고**: GameFlowController(M5 구현 완료) — 클리어/실패 판정 호출은 InGamePanel 버튼 → GameFlowController.OnStageClear/OnStageFail 경로로 연결됨

---

### Enum 정의 (`Assets/_Project/Scripts/Managers/UIManager.cs` 최상단)

#### `PanelType` (enum)
- **파일**: `UIManager.cs` (최상단)
- **역할**: UI 패널 전환 유형 정의
- **값**: `Main`, `InGame`, `Clear`, `Fail`, `GameClear`
- **사용처**: `UIManager.ShowPanel(PanelType)`, `GameFlowController(예정)` 에서 참조

---

## 시스템 의존 관계

```
BootLoader (구현됨 — Boot 씬 전용)
  ├── SaveManager    ← 구현됨 ✅
  └── StageManager   ← 구현됨 ✅

GameFlowController (구현됨) ✅
  ├── StageManager      ← 구현됨 ✅
  ├── UIManager         ← 구현됨 ✅
  └── SaveManager       ← 구현됨 ✅

UIManager (구현됨) ✅
  ├── StageManager      ← 구현됨 ✅
  ├── SaveManager       ← 구현됨 ✅
  └── AnimationController (미구현)

SafeAreaHandler (구현됨) ✅
  └── (의존 없음)

StageManager ──(의존 없음, 독립) ✅
SaveManager  ──(의존 없음, 독립) ✅
```

> **단방향 의존 규칙**: 상위 → 하위 방향만 허용. 역참조 금지.

---

## 에셋 구조

```
Assets/_Project/
├── Scripts/
│   ├── Data/
│   │   ├── AnimationType.cs       ← M2 구현 ✅
│   │   └── StageDataSO.cs         ← M2 구현 ✅
│   ├── Managers/
│   │   ├── StageManager.cs        ← M2 구현 ✅
│   │   ├── SaveManager.cs         ← M2 구현 ✅
│   │   ├── BootLoader.cs          ← M3 구현 ✅
│   │   ├── UIManager.cs           ← M4 구현 ✅
│   │   └── GameFlowController.cs  ← M5 구현 ✅
│   └── UI/
│       └── SafeAreaHandler.cs     ← M4 구현 ✅
├── ScriptableObjects/
│   └── Stage01.asset ~ Stage20.asset  ← M2 생성
└── Scenes/
    ├── Boot.unity
    └── Main.unity
```

---

## Main 씬 UI 계층 구조 (M1~M4 완성)

```
Main.unity
└── Canvas  [CanvasScaler: ScaleWithScreenSize / 1080×1920 / Match 0.5]
    ├── SafeArea  [RectTransform: anchorMin(0,0) anchorMax(1,1) offset(0,0)]
    │             [SafeAreaHandler 스크립트 부착 (M4 구현)]
    │   ├── MainPanel      [비활성화 / RectTransform 전체 stretch]
    │   ├── InGamePanel    [비활성화 / RectTransform 전체 stretch]
    │   ├── ClearPanel     [비활성화 / RectTransform 전체 stretch]
    │   ├── FailPanel      [비활성화 / RectTransform 전체 stretch]
    │   └── GameClearPanel [비활성화 / RectTransform 전체 stretch]
    └── [UIManager 스크립트 부착 (M4 구현)]
```

> **M4 구현 완료**: SafeAreaHandler가 런타임에 SafeArea의 RectTransform을 Screen.safeArea로 재조정한다. UIManager는 각 패널의 활성화/비활성화를 ShowPanel() 메서드로 제어하며, 버튼 이벤트(플레이, 처음부터 다시)를 연결한다.

---

## 주요 흐름

### 앱 시작 → Main 씬 로드까지 (Boot 씬)
```
Boot.unity (BootLoader 스크립트)
  ├── Awake() — InitializeAsync() 시작
  ├── SaveManager.Load() — MaxClearedStage 읽기
  ├── StageManager.SetCurrentStage(maxCleared + 1) — 현재 스테이지 설정
  └── SceneManager.LoadSceneAsync("Main") — Main 씬 로드
```

### Main 씬 초기화 (UIManager)
```
Main.unity (UIManager 싱글턴)
  ├── Awake() — _panelMap 초기화
  ├── Start() — RefreshStageDisplay() 호출, ShowPanel(Main)
  └── SafeArea (SafeAreaHandler)
      └── Awake() — ApplySafeArea() 호출 (노치/홈바 대응)
```

### 패널 전환 (UIManager 제어)
```
Main 패널 (활성) — 플레이 버튼 클릭
  → OnPlayButtonClick() 호출
    → RefreshStageDisplay() (스테이지 텍스트 갱신)
    → ShowPanel(InGame) 호출
      → InGame 패널만 활성화

InGame 패널 — 클리어 버튼 / 실패 버튼 (프로토타입, 아직 연결 안됨)
  → 향후 GameFlowController.OnStageClear() / OnStageFail() 호출 예정
```

---

## 미구현 클래스 (예정)

| 클래스 | 마일스톤 | 역할 | 의존 대상 |
|--------|---------|------|----------|
| `AnimationController` | M6 | DOTween 클리어/실패 연출 | (독립) |

---

## 구현 요약 (M4까지)

### 완료된 항목
- ✅ M2: Data 계층 (AnimationType, StageDataSO), Manager 계층 기초 (StageManager, SaveManager), 20개 Stage 에셋
- ✅ M3: Boot 씬 및 BootLoader (저장 데이터 복원 로직)
- ✅ M4: UIManager (패널 전환, 스테이지 텍스트 갱신, 버튼 이벤트), SafeAreaHandler (노치/홈바 대응)
- ✅ UI 계층 구조 (5개 패널 배치)
- ✅ M5: GameFlowController (클리어/실패 판정, 다음 스테이지 진행, 저장 처리, 버튼 연결)

### 미완료 항목
- M6: AnimationController (DOTween 연출)

---

## 현재 상태

**요약**: M5 완료 단계. 앱 시작 → 저장 데이터 복원 → 메인 화면 → 인게임 → 클리어/실패 판정 → 다음 스테이지 메인화면 또는 게임 완료 화면까지 전체 흐름 동작 완료.
연출(AnimationController/DOTween)은 M6에서 추가 예정이며, GameFlowController 내부 TODO 주석 위치에 콜백 방식으로 삽입된다.

