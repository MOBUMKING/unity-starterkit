# ARCHITECTURE.md — 모바일 게임 프로토타입

> 마지막 갱신: 2026-05-04 (M6 완료, AnimationController 구현 및 통합)

---

## 현재 구현된 클래스

### Data 레이어 (Assets/_Project/Scripts/Data/)

#### AnimationType (enum)
- **파일**: AnimationType.cs
- **역할**: 스테이지 클리어/실패 연출 종류 정의
- **값**: Default = 0, Special = 1
- **의존**: 없음

#### StageDataSO (ScriptableObject)
- **파일**: StageDataSO.cs
- **역할**: 각 스테이지의 고정 데이터 보관 (에셋 20개: Stage01~Stage20)
- **필드** (모두 [SerializeField] private):
  - _stageNumber: int — 스테이지 번호 (1~20)
  - _stageName: string — 화면 표시용 이름 (예: "STAGE 1")
  - _clearAnimationType: AnimationType
  - _failAnimationType: AnimationType
- **프로퍼티**: StageNumber, StageName, ClearAnimationType, FailAnimationType (get-only)
- **에셋 경로**: Assets/_Project/ScriptableObjects/Stage01.asset ~ Stage20.asset
- **의존**: AnimationType

---

### UI 레이어 (Assets/_Project/Scripts/UI/)

#### SafeAreaHandler (MonoBehaviour) ✅
- **파일**: SafeAreaHandler.cs
- **역할**: 디바이스의 Safe Area에 맞춰 RectTransform 앵커를 자동 조정
- **부착 대상**: Canvas 자식인 SafeArea GameObject
- **주요 멤버**:
  - Awake() — ApplySafeArea() 호출
  - ApplySafeArea(): void — Screen.safeArea 픽셀 좌표를 0~1 앵커 좌표로 변환해 적용
- **의존**: 없음 (순수 UI 조정)
- **참고**: 노치/홈바 있는 기기에서 UI 침범 방지

---

### Manager 레이어 (Assets/_Project/Scripts/Managers/)

#### StageManager (MonoBehaviour, 싱글턴) ✅
- **파일**: StageManager.cs
- **역할**: 현재 스테이지 번호(1~20) 관리, 스테이지 데이터 배열 보관
- **싱글턴**: DontDestroyOnLoad, Instance 정적 프로퍼티
- **주요 멤버**:
  - [SerializeField] private StageDataSO[] _stageData — Inspector에서 20개 할당
  - CurrentStageNumber: int (get-only 프로퍼티)
  - SetCurrentStage(int) — 범위 클램프 (Mathf.Clamp 1~20)
  - AdvanceToNextStage() — IsLastStage() 체크 후 +1
  - IsLastStage(): bool — _currentStageNumber >= 20
  - GetCurrentStageData(): StageDataSO — 인덱스 범위 체크 포함
  - _cts: CancellationTokenSource — 비동기 안정성 (M6 추가)
- **의존**: StageDataSO
- **역참조 금지**: GameFlowController, UIManager, SaveManager 직접 참조 불가

#### SaveManager (MonoBehaviour, 싱글턴) ✅
- **파일**: SaveManager.cs
- **역할**: PlayerPrefs "MaxClearedStage" 키 캡슐화
- **싱글턴**: DontDestroyOnLoad, Instance 정적 프로퍼티
- **주요 멤버**:
  - private const string SAVE_KEY = "MaxClearedStage"
  - Save(int maxClearedStage) — PlayerPrefs.SetInt + Save
  - Load(): int — PlayerPrefs.GetInt(SAVE_KEY, 0)
  - Reset() — PlayerPrefs.DeleteKey + Save
  - _cts: CancellationTokenSource — 비동기 안정성 (M6 추가)
- **의존**: 없음 (다른 Manager 참조 금지)

#### BootLoader (MonoBehaviour) ✅
- **파일**: BootLoader.cs
- **역할**: Boot 씬 전용 진입점. 저장 데이터를 복원하고 Main 씬으로 전환한다.
- **싱글턴 아님**: Boot 씬과 함께 파괴되므로 DontDestroyOnLoad 불필요
- **주요 멤버**:
  - Start() — InitializeAsync().Forget() 호출 (M6 변경: Awake → Start)
  - InitializeAsync(CancellationToken): UniTask — SaveManager.Load() → StageManager.SetCurrentStage(maxCleared+1) → SceneManager.LoadSceneAsync("Main")
  - OnDestroy() — _cts?.Cancel() / _cts?.Dispose()
- **비동기**: UniTask + CancellationToken 패턴
- **의존**: SaveManager, StageManager
- **변경사항**: Awake()에서 Start()로 이동 (모든 Awake() 완료 후 실행 보장)

#### GameFlowController (MonoBehaviour, 싱글턴) ✅
- **파일**: GameFlowController.cs
- **역할**: 스테이지 클리어/실패 판정을 처리하고 StageManager·SaveManager·UIManager를 오케스트레이션한다
- **싱글턴**: 중복 인스턴스 제거만, DontDestroyOnLoad 없음 (Main 씬 전용)
- **주요 멤버**:
  - OnStageClear(): void — SaveManager.Save → UIManager.PlayClearAndThen(콜백) → 콜백: IsLastStage 분기 → (true) ShowPanel(GameClear) / (false) AdvanceToNextStage → RefreshStageDisplay → ShowPanel(Main)
  - OnStageFail(): void — UIManager.PlayFailAndThen(콜백) → 콜백: RefreshStageDisplay → ShowPanel(Main)
- **의존**: StageManager, SaveManager, UIManager
- **변경사항**: M6 완료 - AnimationController 콜백 체인 구현, TODO 주석 제거
- **배치**: Main 씬 루트 (Canvas 외부)

#### UIManager (MonoBehaviour, 싱글턴) ✅
- **파일**: UIManager.cs
- **역할**: Main 씬의 UI 패널(Main, InGame, Clear, Fail, GameClear) 활성화/비활성화 관리 및 애니메이션 위임
- **싱글턴**: 중복 인스턴스 제거만, DontDestroyOnLoad 없음 (Main 씬 전용)
- **주요 멤버**:
  - [SerializeField] private GameObject _mainPanel
  - [SerializeField] private GameObject _inGamePanel
  - [SerializeField] private GameObject _clearPanel
  - [SerializeField] private GameObject _failPanel
  - [SerializeField] private GameObject _gameClearPanel
  - [SerializeField] private TMP_Text _mainStageText
  - [SerializeField] private TMP_Text _inGameStageText
  - [SerializeField] private AnimationController _animationController (M6 추가)
  - private Dictionary<PanelType, GameObject> _panelMap — 패널 맵
  - ShowPanel(PanelType): void — 지정 패널만 활성화, 나머지 비활성화
  - RefreshStageDisplay(): void — StageManager에서 현재 스테이지 번호를 읽어 스테이지 텍스트 갱신
  - OnPlayButtonClick(): void — 플레이 버튼 클릭 시, 인게임 화면으로 전환
  - OnRestartButtonClick(): void — 처음부터 다시 시작 버튼 클릭 시, 데이터 초기화 후 메인 화면으로 복귀
  - PlayClearAndThen(Action): void (M6 추가) — ClearPanel 표시 후 AnimationController.PlayClear 위임, 완료 콜백 호출
  - PlayFailAndThen(Action): void (M6 추가) — FailPanel 표시 후 AnimationController.PlayFail 위임, 완료 콜백 호출
- **의존**: StageManager, SaveManager, AnimationController
- **참고**: AnimationController 통합으로 클리어/실패 연출 완전 자동화

#### AnimationController (MonoBehaviour, 싱글턴) ✅ (M6 구현)
- **파일**: AnimationController.cs
- **역할**: DOTween Sequence 기반 클리어/실패 UI 연출을 담당
- **싱글턴**: 중복 인스턴스 제거만, DontDestroyOnLoad 없음 (Main 씬 전용)
- **주요 멤버**:
  - [SerializeField] private CanvasGroup _clearOverlay — 클리어 오버레이
  - [SerializeField] private Transform _clearTextTransform — 클리어 텍스트
  - [SerializeField] private CanvasGroup _failOverlay — 실패 오버레이
  - [SerializeField] private Transform _failTextTransform — 실패 텍스트
  - private Sequence _activeSequence — 현재 재생 중인 시퀀스 (재시작 시 킬)
  - PlayClear(Action): void — 클리어 연출 재생 (CanvasGroup 페이드인 0→0.85 / 0.4초 + 텍스트 스케일 인 0→1 / 0.4초 OutBack + 1.0초 대기 + 콜백)
  - PlayFail(Action): void — 실패 연출 재생 (CanvasGroup 페이드인 0→0.85 / 0.4초 + 텍스트 흔들기 0.5초 강도 20 + 0.8초 대기 + 콜백)
- **의존**: DOTween (UnityEngine.CanvasGroup, UnityEngine.Transform)
- **설계**: UIManager의 위임 요청을 받아 Sequence를 재생하고 완료 시 콜백 호출

#### SceneBootstrapper (Static Utility) ✅
- **파일**: SceneBootstrapper.cs
- **역할**: 에디터에서 Boot 이외의 씬에서 Play할 때 자동으로 Boot 씬부터 시작하도록 리다이렉트 (프로덕션 빌드에서 제거)
- **실행 시점**: RuntimeInitializeOnLoadMethod(BeforeSceneLoad) — 씬 로드 직전
- **프로덕션 안전**: #if UNITY_EDITOR 조건으로 에디터에만 작동
- **의존**: 없음 (독립)

---

### Enum 정의 (Assets/_Project/Scripts/Managers/UIManager.cs 최상단)

#### PanelType (enum)
- **파일**: UIManager.cs (최상단)
- **역할**: UI 패널 전환 유형 정의
- **값**: Main, InGame, Clear, Fail, GameClear
- **사용처**: UIManager.ShowPanel(PanelType), GameFlowController.OnStageClear/OnStageFail 에서 참조

---

## 시스템 의존 관계

```
BootLoader (구현됨 — Boot 씬 전용) ✅
  ├── SaveManager    ← 구현됨 ✅
  └── StageManager   ← 구현됨 ✅

GameFlowController (구현됨) ✅
  ├── StageManager      ← 구현됨 ✅
  ├── UIManager         ← 구현됨 ✅
  └── SaveManager       ← 구현됨 ✅
    └── (UIManager이 AnimationController 위임)

UIManager (구현됨) ✅
  ├── StageManager      ← 구현됨 ✅
  ├── SaveManager       ← 구현됨 ✅
  └── AnimationController ← 구현됨 ✅ (M6)

AnimationController (구현됨) ✅ (M6)
  └── (의존 없음 — DOTween만 사용)

SafeAreaHandler (구현됨) ✅
  └── (의존 없음)

SceneBootstrapper (구현됨) ✅
  └── (의존 없음 — 에디터 전용 유틸)

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
│   │   ├── StageManager.cs        ← M2 구현 ✅ (M6 비동기 강화)
│   │   ├── SaveManager.cs         ← M2 구현 ✅ (M6 비동기 강화)
│   │   ├── BootLoader.cs          ← M3 구현 ✅ (M6 Awake→Start 변경)
│   │   ├── UIManager.cs           ← M4 구현 ✅ (M6 AnimationController 통합)
│   │   ├── GameFlowController.cs  ← M5 구현 ✅ (M6 완전 통합)
│   │   ├── AnimationController.cs ← M6 구현 ✅
│   │   └── SceneBootstrapper.cs   ← M6 구현 ✅
│   └── UI/
│       └── SafeAreaHandler.cs     ← M4 구현 ✅
├── ScriptableObjects/
│   └── Stage01.asset ~ Stage20.asset  ← M2 생성
└── Scenes/
    ├── Boot.unity
    └── Main.unity
```

---

## Main 씬 UI 계층 구조 (M1~M6 완성)

```
Main.unity
├── Canvas  [CanvasScaler: ScaleWithScreenSize / 1080×1920 / Match 0.5]
│   ├── SafeArea  [RectTransform: anchorMin(0,0) anchorMax(1,1) offset(0,0)]
│   │             [SafeAreaHandler 스크립트 부착 (M4 구현)]
│   │   ├── MainPanel      [비활성화 / RectTransform 전체 stretch]
│   │   ├── InGamePanel    [비활성화 / RectTransform 전체 stretch]
│   │   ├── ClearPanel     [비활성화 / RectTransform 전체 stretch / CanvasGroup + 클리어 텍스트]
│   │   ├── FailPanel      [비활성화 / RectTransform 전체 stretch / CanvasGroup + 실패 텍스트]
│   │   └── GameClearPanel [비활성화 / RectTransform 전체 stretch]
│   ├── [UIManager 스크립트 부착 (M4 구현, M6 AnimationController 연결)]
│   └── [AnimationController 스크립트 부착 (M6 구현 — UIManager에서 Inspector 참조)]
└── GameFlowController (게임 오브젝트) [Canvas 외부 — Main 씬 루트]
    └── [GameFlowController 스크립트 부착 (M5 구현)]
```

> **M6 구현 완료**: AnimationController가 실제 로직을 담당하고, UIManager가 위임 메서드로 호출한다. 클리어/실패 연출은 완전히 자동화되었으며, GameFlowController의 콜백 체인으로 화면 전환이 순차적으로 진행된다.

---

## 주요 흐름

### 앱 시작 → Main 씬 로드까지 (Boot 씬)
```
Boot.unity (BootLoader 스크립트)
  ├── Start() — InitializeAsync() 시작 (M6 변경: Awake → Start)
  ├── SaveManager.Load() — MaxClearedStage 읽기
  ├── StageManager.SetCurrentStage(maxCleared + 1) — 현재 스테이지 설정
  └── SceneManager.LoadSceneAsync("Main") — Main 씬 로드
```

### Main 씬 초기화 (UIManager)
```
Main.unity (UIManager 싱글턴)
  ├── Awake() — _panelMap 초기화, AnimationController Inspector 할당 (M6)
  ├── Start() — RefreshStageDisplay() 호출, ShowPanel(Main)
  └── SafeArea (SafeAreaHandler)
      └── Awake() — ApplySafeArea() 호출 (노치/홈바 대응)
```

### 클리어 판정 흐름 (M6 완전 통합)
```
InGame 패널 — 클리어 버튼 클릭
  → GameFlowController.OnStageClear()
    ├── SaveManager.Save(currentStage) — 즉시 저장
    └── UIManager.PlayClearAndThen(콜백)
        ├── ShowPanel(Clear) — 클리어 패널 표시
        └── AnimationController.PlayClear(콜백)
            ├── CanvasGroup 페이드인 (0→0.85, 0.4초)
            ├── 텍스트 스케일 인 (0→1, 0.4초, OutBack)
            ├── 1.0초 대기
            └── 콜백 실행
              ├── IsLastStage() 분기
              ├─ true: ShowPanel(GameClear)
              └─ false: AdvanceToNextStage() → RefreshStageDisplay() → ShowPanel(Main)
```

### 실패 판정 흐름 (M6 완전 통합)
```
InGame 패널 — 실패 버튼 클릭
  → GameFlowController.OnStageFail()
    └── UIManager.PlayFailAndThen(콜백)
        ├── ShowPanel(Fail) — 실패 패널 표시
        └── AnimationController.PlayFail(콜백)
            ├── CanvasGroup 페이드인 (0→0.85, 0.4초)
            ├── 텍스트 흔들기 (0.5초, 강도 20)
            ├── 0.8초 대기
            └── 콜백 실행
              ├── RefreshStageDisplay()
              └── ShowPanel(Main) — 동일 스테이지 메인 화면 복귀
```

---

## 구현 요약 (M1~M6 완료)

### 완료된 항목
- ✅ M2: Data 계층 (AnimationType, StageDataSO), Manager 계층 기초 (StageManager, SaveManager), 20개 Stage 에셋
- ✅ M3: Boot 씬 및 BootLoader (저장 데이터 복원 로직)
- ✅ M4: UIManager (패널 전환, 스테이지 텍스트 갱신, 버튼 이벤트), SafeAreaHandler (노치/홈바 대응)
- ✅ UI 계층 구조 (5개 패널 배치)
- ✅ M5: GameFlowController (클리어/실패 판정, 다음 스테이지 진행, 저장 처리, 버튼 연결)
- ✅ M6: AnimationController (DOTween 클리어/실패 연출), SceneBootstrapper (에디터 리다이렉트)
- ✅ M6: BootLoader Awake→Start 변경, StageManager/SaveManager 비동기 강화
- ✅ M6: UIManager + GameFlowController 완전 통합 (콜백 체인 완성)

### 미완료 항목
- 없음 (MVP 범위 완료)

---

## 현재 상태

**요약**: M6 완료 단계. 앱 시작 → 저장 데이터 복원 → 메인 화면 → 인게임 → 클리어/실패 판정 → 연출 자동 재생 → 다음 스테이지 메인화면 또는 게임 완료 화면까지 **전체 흐름 완전히 구현 및 통합 완료**.

AnimationController의 DOTween 연출이 GameFlowController의 콜백 체인에 완전히 통합되었으며, UIManager의 위임 메서드(PlayClearAndThen, PlayFailAndThen)를 통해 깔끔한 아키텍처를 유지한다.

MVP의 모든 기능이 구현되었으며, 실제 게임플레이 로직 구현 시 프로토타입 버튼(클리어/실패 버튼)을 대체하기만 하면 된다.
