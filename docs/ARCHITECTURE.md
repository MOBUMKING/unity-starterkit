# ARCHITECTURE.md — 모바일 게임 프로토타입

> 마지막 갱신: 2026-05-03 (M2 완료 기준)

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

### Manager 레이어 (`Assets/_Project/Scripts/Managers/`)

#### `StageManager` (MonoBehaviour, 싱글턴)
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

#### `SaveManager` (MonoBehaviour, 싱글턴)
- **파일**: `SaveManager.cs`
- **역할**: PlayerPrefs "MaxClearedStage" 키 캡슐화
- **싱글턴**: `DontDestroyOnLoad`, `Instance` 정적 프로퍼티
- **주요 멤버**:
  - `private const string SAVE_KEY = "MaxClearedStage"`
  - `Save(int maxClearedStage)` — PlayerPrefs.SetInt + Save
  - `Load(): int` — PlayerPrefs.GetInt(SAVE_KEY, 0)
  - `Reset()` — PlayerPrefs.DeleteKey + Save
- **의존**: 없음 (다른 Manager 참조 금지)

---

## 시스템 의존 관계

```
GameFlowController (미구현)
  ├── StageManager   ← 현재 구현됨
  ├── UIManager      (미구현)
  └── SaveManager    ← 현재 구현됨

UIManager (미구현)
  └── AnimationController (미구현)

StageManager ──(의존 없음, 독립)
SaveManager  ──(의존 없음, 독립)
```

> **단방향 의존 규칙**: 상위 → 하위 방향만 허용. 역참조 금지.

---

## 에셋 구조

```
Assets/_Project/
├── Scripts/
│   ├── Data/
│   │   ├── AnimationType.cs       ← M2 구현
│   │   └── StageDataSO.cs         ← M2 구현
│   └── Managers/
│       ├── StageManager.cs        ← M2 구현
│       └── SaveManager.cs         ← M2 구현
├── ScriptableObjects/
│   └── Stage01.asset ~ Stage20.asset  ← M2 생성
└── Scenes/
    ├── Boot.unity
    └── Main.unity
```

---

## 미구현 클래스 (예정)

| 클래스 | 마일스톤 | 역할 |
|--------|---------|------|
| `BootLoader` | M3 | Boot 씬 저장 데이터 로드 → Main 씬 전환 |
| `UIManager` | M4 | 패널 Show/Hide, 버튼 이벤트 연결 |
| `SafeAreaHandler` | M4 | Screen.safeArea 기반 RectTransform 조정 |
| `GameFlowController` | M5 | OnStageClear/OnStageFail 판정 흐름 제어 |
| `AnimationController` | M6 | DOTween 클리어/실패 연출 |
