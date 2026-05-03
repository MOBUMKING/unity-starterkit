# Development Guidelines — Unity Mobile Game Prototype

## 1. Project Overview

- **Engine**: Unity 6000.4.3f1, URP, UGUI
- **Platform**: iOS / Android (모바일)
- **Scene 구성**: `Boot` (초기화 전용) → `Main` (단일 씬, 패널 전환 방식)
- **핵심 패키지**: DOTween, UniTask, Newtonsoft JSON, MCP for Unity

---

## 2. Directory Structure

```
Assets/
├── Scripts/
│   ├── Managers/         # GameFlowController, StageManager, UIManager, SaveManager, AnimationController
│   ├── UI/               # SafeAreaHandler, View/Presenter 컴포넌트
│   └── Data/             # StageDataSO (ScriptableObject 클래스 정의)
├── ScriptableObjects/    # StageDataSO 에셋 20개 (Stage01 ~ Stage20)
├── Scenes/               # Boot.unity, Main.unity
└── Plugins/              # DOTween (직접 임포트)
docs/
├── PRD.md
├── roadmap.md
└── ARCHITECTURE.md       # 클래스 추가/삭제 시 반드시 갱신
```

- 새 Manager 클래스는 반드시 `Assets/Scripts/Managers/`에 생성
- 새 UI 컴포넌트는 반드시 `Assets/Scripts/UI/`에 생성
- ScriptableObject 클래스 정의는 `Assets/Scripts/Data/`, 에셋 파일은 `Assets/ScriptableObjects/`

---

## 3. Code Conventions

### 네이밍
- **클래스 / 메서드 / 프로퍼티**: `PascalCase`
- **지역 변수 / 파라미터**: `camelCase`
- **private 필드**: `_camelCase`
- **상수**: `UPPER_SNAKE_CASE`

### 포맷
- 들여쓰기: **스페이스 4칸** (탭 금지)
- 주석: **한국어**로 작성
- 코드 파일 인코딩: UTF-8

### 필드 노출
- Inspector 노출은 `[SerializeField]` 사용
- `public` 필드 직접 선언 **금지** — 프로퍼티(`{ get; private set; }`) 또는 `[SerializeField] private` 사용

### 비동기
- 비동기 처리는 **UniTask** 사용
- `IEnumerator` / `StartCoroutine` **금지**
- `async UniTaskVoid` 또는 `async UniTask` 반환 타입 사용
- `MonoBehaviour` 클래스는 `OnDestroy`에서 반드시 `_cts.Cancel(); _cts.Dispose();` 처리

```csharp
// 올바른 패턴
private CancellationTokenSource _cts;

private void Awake() => _cts = new CancellationTokenSource();
private void OnDestroy() { _cts.Cancel(); _cts.Dispose(); }

private async UniTaskVoid LoadAsync()
{
    await SomeAsyncMethod(_cts.Token);
}
```

---

## 4. System Dependency Rules

의존 방향 (단방향, 역방향 참조 **금지**):

```
GameFlowController
  ├── StageManager   (읽기/갱신)
  ├── UIManager      (화면 전환 명령)
  └── SaveManager    (저장 요청)

UIManager
  └── AnimationController  (연출 재생 요청)

StageManager
  └── SaveManager  (저장/불러오기)
```

- `StageManager`가 `GameFlowController`를 참조하는 것 **금지**
- `SaveManager`가 다른 Manager를 참조하는 것 **금지**
- `AnimationController`가 `GameFlowController`를 직접 호출하는 것 **금지**

---

## 5. Feature Implementation Rules

### GameFlowController
- `OnStageClear()` / `OnStageFail()`은 **판정 결과만 수신**하는 진입점
- 이 메서드 내부에 클리어/실패 판정 로직 구현 **금지**
- 프로토타입 버튼(클리어/실패 버튼)은 이 메서드만 호출; 판정 로직 보유 **금지**

### StageManager
- 스테이지 번호 유효 범위: **1 ~ 20**
- 범위 초과 체크는 `StageManager` 내부에서만 처리
- `StageDataSO` 배열은 `[SerializeField]`로 Inspector에서 직접 할당 (`Resources.Load` **금지**)

### SaveManager
- `PlayerPrefs` 키 `"MaxClearedStage"`는 `SaveManager`를 통해서만 읽기/쓰기
- 다른 클래스에서 `PlayerPrefs.GetInt("MaxClearedStage")` 직접 호출 **금지**
- 저장 타이밍: 클리어 판정 발생 즉시 (연출 재생 전)

### AnimationController (DOTween)
- DOTween 호출은 반드시 **AnimationController 내부에서만** 수행
- 다른 클래스에서 `DOTween.To`, `transform.DOMove` 등 직접 호출 **금지**
- Sequence 생성 시 반드시 `_sequence?.Kill()` 후 재생성
- `OnDestroy`에서 `_sequence?.Kill()` 처리 필수

```csharp
// 올바른 DOTween 패턴
private Sequence _sequence;

public async UniTask PlayClearAsync(CancellationToken ct)
{
    _sequence?.Kill();
    _sequence = DOTween.Sequence();
    _sequence.Append(...);
    await _sequence.WithCancellation(ct);
}

private void OnDestroy() => _sequence?.Kill();
```

### UIManager
- 화면 전환은 패널 `GameObject`의 `SetActive(true/false)` 방식 사용
- `SceneManager.LoadScene` 호출 **금지** (Main 씬 내부 전환에 한함)
- 씬 전환은 Boot→Main 1회만 허용

### SafeAreaHandler
- `Screen.safeArea`를 기반으로 `RectTransform`을 조정
- `Canvas Scaler`: Scale With Screen Size, 기준 해상도 1080×1920, Match = 0.5

---

## 6. Scene Rules

- **Boot 씬**: `SaveManager.Load()` 호출 후 즉시 `SceneManager.LoadScene("Main")` 전환
- **Main 씬**: 패널 Show/Hide로 화면 전환, 추가 씬 로드 **금지**
- Additive 씬 로드, Addressables **금지**

---

## 7. Document Synchronization Rules

**기능 구현 완료 시 반드시 동시에 수행:**

| 조건 | 갱신 대상 | 갱신 내용 |
|------|-----------|-----------|
| 기능 완료 | `docs/roadmap.md` | 해당 기능 체크박스 `[ ]` → `[x]` |
| 기능 완료 | `docs/PRD.md` | 해당 기능 `**상태**: 미구현` → `**상태**: 구현완료` |
| 새 클래스/시스템 추가 | `docs/ARCHITECTURE.md` | 클래스 목록 및 의존 관계 갱신 |
| 클래스/시스템 삭제 | `docs/ARCHITECTURE.md` | 해당 항목 제거 |

---

## 8. Task Completion Checklist

모든 구현 작업 완료 후 아래 항목을 **실제로 확인**한 뒤 완료 보고 (추측 통과 금지):

1. **컴파일 에러 없음** — Unity MCP `get_console_logs`로 콘솔 조회, `"error CS"` 검색
2. **코드 재독** — 작성한 코드를 다시 읽고 PRD 요구사항과 대조
3. **문서 갱신** — roadmap.md / PRD.md / ARCHITECTURE.md 동기화 완료

---

## 9. Prohibited Actions

- `public` 필드 직접 선언
- `IEnumerator` / `StartCoroutine` 사용
- `PlayerPrefs` 직접 접근 (SaveManager 우회)
- DOTween을 AnimationController 외부에서 호출
- `Resources.Load`로 StageDataSO 로드
- Main 씬 내부에서 `SceneManager.LoadScene` 호출
- Additive 씬 로드 / Addressables 사용
- `StageManager` / `SaveManager` / `AnimationController`가 `GameFlowController`를 역참조
- `OnStageClear` / `OnStageFail` 내부에 판정 로직 구현
- 확실하지 않은 API를 추측으로 사용 (Grep/Glob으로 먼저 확인)
- 존재하지 않는 클래스/컴포넌트를 임의로 생성 (반드시 Grep 검색 후 결정)

---

## 10. AI Decision Tree

**기능을 추가/수정할 때:**
1. `docs/PRD.md` → 기능 명세 및 관련 시스템 확인
2. `docs/ARCHITECTURE.md` → 존재하는 클래스 및 의존 관계 확인
3. `docs/roadmap.md` → 현재 마일스톤 및 우선순위 확인
4. Grep/Glob으로 관련 파일 검색 후 구현
5. 구현 완료 후 문서 3종 동기화

**에러 발생 시:**
- 원인 파악 불가 → 수정 시도 **금지**, 현상과 로그를 그대로 보고
- 추측으로 코드 변경 **금지**

**새 클래스 필요 여부 판단:**
- 기존 Manager에 메서드 추가로 해결 가능한지 먼저 검토
- 새 클래스 생성이 필요하면 의존 방향 규칙(섹션 4) 위반 여부 확인 후 생성
