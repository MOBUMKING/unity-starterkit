# 개발 로드맵 — 모바일 게임 프로토타입 (Arrows 참조 레벨 선택형)

> PRD 기반 작성 | ARCHITECTURE.md 미존재 → 전체 미구현 상태로 초기화
> 2026-05-03 점검: 개발 효율 순서로 단계 세분화 재배치
> 2026-05-04 업데이트: M3 완료 반영 (BootLoader 구현 및 Boot 씬 배치)
> 2026-05-04 업데이트: M4 완료 반영 (UIManager & SafeAreaHandler 구현)
> 2026-05-04 업데이트: 코드 리뷰·성능 분석 결과를 토대로 M7(중규모 확장 대비 리팩토링) 신설
> 2026-05-04 업데이트: M7-A에 StageManager.IsLastStage 비교 연산자 정밀화 항목 추가
> 2026-05-04 업데이트: M5 완료 반영 (GameFlowController 구현 및 버튼 연결)

---

## 현재 단계

- **현재 마일스톤**: M6 — 클리어·실패 연출 (AnimationController & DOTween)
- **전체 진행률**: 5 / 7 마일스톤 완료 (M1 씬 구조, M2 데이터/저장, M3 BootLoader, M4 UIManager, M5 GameFlowController)

---

## 마일스톤 목록

---

### M1: 프로젝트 기반 & 씬 구조
> **목표**: 씬 구성, Canvas 기준 해상도, 폴더 구조가 완성되어 이후 모든 작업의 기반이 갖춰진 상태
> **상태**: 완료
> **선행 조건**: 없음
> **완료 기준**: Boot 씬 → Main 씬 전환이 Play Mode에서 정상 작동

| 상태 | 항목 | 설명 |
|------|------|------|
| [x] | 폴더 구조 생성 | Assets/_Project/Scripts, UI, Data, Scenes 등 폴더 생성 완료 |
| [x] | 씬 파일 생성 | Boot.unity, Main.unity 생성 완료 |
| [x] | Canvas & Canvas Scaler 설정 | Main 씬에 Canvas 생성, Scale With Screen Size / 1080×1920 / Match 0.5 설정 — **F008 선행 적용** |
| [x] | Safe Area 패널 배치 | Canvas 자식에 SafeArea 패널 배치 (SafeAreaHandler 컴포넌트 붙일 자리 확보) |
| [x] | UI 패널 계층 구조 생성 | MainPanel, InGamePanel, ClearPanel, FailPanel, GameClearPanel 빈 오브젝트 생성 및 비활성화 |

> **설계 의도**: Canvas Scaler와 Safe Area를 가장 먼저 설정해야 이후 모든 UI 작업이 올바른 해상도 기준 위에서 이루어진다. 나중에 적용하면 배치된 UI를 전부 재조정해야 한다.

---

### M2: 데이터 & 저장 시스템 (F005, F006)
> **목표**: 스테이지 데이터(ScriptableObject)와 PlayerPrefs 저장·복원이 코드 레벨에서 동작하는 기반 완성
> **상태**: 완료
> **선행 조건**: M1 완료 (폴더 구조, 씬 파일)

| 상태 | 기능 | 설명 |
|------|------|------|
| [x] | F006-a — StageDataSO 정의 | ScriptableObject 클래스 작성 (stageNumber, stageName, clearAnimationType, failAnimationType) |
| [x] | F006-b — StageDataSO 20개 생성 | 에셋 생성 및 인스펙터에서 각 스테이지 데이터 입력 |
| [x] | F006-c — StageManager 구현 | 현재 스테이지 번호 관리, 범위(1~20) 제한, 다음 스테이지 계산, StageDataSO 배열 보관 |
| [x] | F005 — SaveManager 구현 | PlayerPrefs "MaxClearedStage" 저장 / 불러오기 / 초기화, StageManager에 복원값 전달 |

> **설계 의도**: SaveManager와 StageManager는 UI와 무관하게 독립적으로 동작해야 한다. UI 작업 전에 완성하여 이후 단계에서 데이터를 그냥 가져다 쓸 수 있도록 한다.

---

### M3: Boot 씬 진입 & 초기화 흐름
> **목표**: 앱 실행 시 Boot 씬에서 저장 데이터를 불러오고 Main 씬으로 전환하는 진입점 완성
> **상태**: 완료
> **선행 조건**: M2 완료 (SaveManager, StageManager 필요)

| 상태 | 항목 | 설명 |
|------|------|------|
| [x] | BootLoader 구현 | Boot 씬 전용 MonoBehaviour. SaveManager.Load() 호출 → SceneManager로 Main 씬 전환 (UniTask 비동기) |
| [x] | Boot 씬에 BootLoader 배치 | Boot 씬 GameObject에 BootLoader 컴포넌트 부착, 씬 전환 흐름 완성 |

> **설계 의도**: 진입점을 명확히 분리해야 이후 UIManager가 MainPanel부터 정확한 스테이지 번호를 표시할 수 있다. Boot 씬이 없으면 저장 데이터 복원 타이밍이 모호해진다.

---

### M4: UIManager & 화면 전환 골격 (F001, F002, F007)
> **목표**: 메인 화면 → 인게임 화면 → 게임 완료 화면 전환이 버튼 클릭으로 동작하는 UI 골격 완성
> **상태**: 완료
> **선행 조건**: M3 완료 (BootLoader → Main 씬 진입 흐름 확립 후)

| 상태 | 기능 | 설명 |
|------|------|------|
| [x] | UIManager 구현 | 패널 참조 보관, ShowPanel(PanelType) 메서드로 단일 패널 활성화/비활성화, RefreshStageDisplay(), OnPlayButtonClick(), OnRestartButtonClick() 구현 |
| [x] | F001 — 메인 화면 UI | MainPanel: 타이틀 텍스트, 현재 도전 스테이지 번호(StageManager에서 조회), 플레이 버튼 |
| [x] | F002 — 인게임 화면 UI | InGamePanel: 스테이지 번호 표시, 빈 게임 영역, 클리어 버튼(초록), 실패 버튼(빨강) |
| [x] | F007 — 게임 완료 화면 UI | GameClearPanel: 전체 클리어 축하 텍스트, 처음부터 다시 시작 버튼 |
| [x] | SafeAreaHandler 구현 및 적용 | SafeArea RectTransform을 디바이스 safeArea에 맞게 자동 조정 (F008 완성) |

> **설계 의도**: UIManager는 GameFlowController의 명령을 받아 패널을 전환하므로, GameFlowController보다 먼저 구현한다. UI가 준비된 후 컨트롤러를 연결하면 순서가 명확하고 테스트가 쉽다.

---

### M5: GameFlowController & 판정 로직 연결 (F003, F004 로직 포함)
> **목표**: 클리어/실패 버튼 → GameFlowController → UIManager 화면 전환 전체 흐름이 실제로 동작
> **상태**: 완료
> **선행 조건**: M4 완료 (UIManager, 모든 패널 UI 준비 필요)

| 상태 | 항목 | 설명 |
|------|------|------|
| [x] | GameFlowController 구현 | OnStageClear() / OnStageFail() 메서드, SaveManager 저장 호출, StageManager 스테이지 증가, UIManager 패널 전환 명령, 20스테이지 클리어 분기(→ GameClearPanel) |
| [x] | 클리어 버튼 연결 | InGamePanel 클리어 버튼 → GameFlowController.OnStageClear() 호출 |
| [x] | 실패 버튼 연결 | InGamePanel 실패 버튼 → GameFlowController.OnStageFail() 호출 |
| [x] | 처음부터 다시 시작 연결 | GameClearPanel RestartButton → UIManager.OnRestartButtonClick() (SaveManager.Reset → StageManager 초기화 → MainPanel 표시) |
| [x] | 전체 흐름 통합 테스트 | 1스테이지 → 클리어 → 2스테이지 메인화면, 실패 → 동일 스테이지 복귀, 20스테이지 클리어 → 완료 화면 (Play Mode PASS) |

---

### M6: 클리어·실패 연출 (F003, F004 연출)
> **목표**: DOTween 기반 클리어/실패 오버레이 애니메이션이 판정 흐름 중간에 재생되고 완료 후 자동 전환
> **상태**: 예정
> **선행 조건**: M5 완료 (GameFlowController 흐름 동작 확인 후 연출 레이어 추가)

| 상태 | 기능 | 설명 |
|------|------|------|
| [ ] | AnimationController 구현 | DOTween 기반 PlayClear(callback) / PlayFail(callback) 메서드, 콜백으로 화면 전환 트리거 |
| [ ] | F003 — 클리어 연출 패널 | ClearPanel: 반투명 오버레이 + "CLEAR!" 텍스트, 페이드인 + 스케일 애니메이션(골드/옐로우 계열), 약 1.5~2초 후 콜백 |
| [ ] | F004 — 실패 연출 패널 | FailPanel: 반투명 오버레이 + "TRY AGAIN" 텍스트, 페이드인 + shake 애니메이션(붉은 계열), 약 1.5~2초 후 콜백 |
| [ ] | GameFlowController에 연출 삽입 | OnStageClear/OnStageFail에서 즉시 전환하던 부분을 AnimationController 콜백 방식으로 교체 |

> **설계 의도**: M5에서 연출 없이 전체 흐름을 먼저 검증한다. 흐름이 확인된 후 M6에서 연출을 삽입하면, 연출 관련 버그와 흐름 버그를 명확하게 분리할 수 있다.

---

### M7: 중규모 확장 대비 리팩토링 (코드 리뷰 / 성능 분석 반영)
> **목표**: MVP 흐름이 검증된 코드를 중규모(100+ 스테이지, 다수 패널, 다양한 디바이스 환경) 확장에 견딜 수 있도록 견고화
> **상태**: 예정
> **선행 조건**: M6 완료 (게임 흐름과 연출이 모두 검증된 후 적용해야 리팩토링 영향 범위를 좁힐 수 있음)
> **출처**: 2026-05-04 코드 리뷰 보고 + 성능 분석 보고

#### M7-A: 안정성 보강 (방어 코드 / 검증)

| 상태 | 항목 | 대상 | 설명 |
|------|------|------|------|
| [ ] | SafeAreaHandler null 체크 추가 | `SafeAreaHandler.cs` | `GetComponent<RectTransform>()` 결과를 null 체크하고 누락 시 Debug.LogError 후 early return. RectTransform이 없는 GameObject 부착 시 NullReferenceException 방지 |
| [ ] | UIManager 패널 할당 검증 | `UIManager.cs` (Awake) | `_panelMap` 구성 후 모든 값에 대해 null 체크 루프를 돌고 누락된 패널이 있으면 Debug.LogError로 보고. 인스펙터 할당 누락 추적성 향상 |
| [ ] | StageManager.IsLastStage 비교 연산자 정밀화 | `StageManager.cs` | `_currentStageNumber >= 20` → `_currentStageNumber == 20`으로 변경. 현재 SetCurrentStage()의 Clamp로 범위 초과는 차단되지만, 의도가 "마지막 스테이지"인 만큼 등호 단독 비교가 더 명확. 향후 최대 스테이지 수 변경 시 리팩토링 안전성 향상 |

#### M7-B: 코드 정리 (미사용 코드 / 매직 스트링 제거)

| 상태 | 항목 | 대상 | 설명 |
|------|------|------|------|
| [ ] | StageManager 미사용 _cts 제거 또는 활용 | `StageManager.cs` | 현재 async 작업이 없는데 CancellationTokenSource만 생성/해제 중. 향후 비동기 로직(스테이지 데이터 로드 등)이 추가되지 않으면 제거. 추가 예정이면 그 시점까지 유지 |
| [ ] | SaveManager 미사용 _cts 제거 또는 활용 | `SaveManager.cs` | 동기 I/O만 수행하므로 현재 _cts는 불필요. M7-D의 비동기 Save와 함께 활용 여부 결정 |
| [ ] | UIManager 매직 스트링 상수화 | `UIManager.cs` | "STAGE " 같은 UI 텍스트 접두사를 `private const string` 으로 분리 (다국어 대응 / 변경 추적 용이) |
| [ ] | BootLoader 씬 이름 상수화 | `BootLoader.cs` | "Main" 씬 이름을 `private const string MAIN_SCENE_NAME` 으로 분리 |

#### M7-C: UI 성능 최적화 (중규모 확장 대비)

| 상태 | 항목 | 대상 | 설명 |
|------|------|------|------|
| [ ] | UIManager.ShowPanel O(n) → O(1) 개선 | `UIManager.cs` | 현재 활성 패널을 추적하는 `_currentPanelType` 캐시 도입. 동일 패널 재호출 시 early return으로 불필요한 SetActive 호출 회피. 패널 20+개로 확장 시 선형 성능 저하 차단 |
| [ ] | SafeAreaHandler 동적 모니터링 (선택) | `SafeAreaHandler.cs` | 화면 회전 / 멀티 윈도우(iPad) 대응이 필요한 게임에 한해 화면 크기 변경 이벤트를 구독하고 ApplySafeArea를 재호출. 정적 레이아웃이면 skip |

#### M7-D: 데이터·I/O 견고화 (확장 시 누적 부하 차단)

| 상태 | 항목 | 대상 | 설명 |
|------|------|------|------|
| [ ] | StageManager 데이터 캐싱 | `StageManager.cs` | `CurrentStageData` 프로퍼티 추가, `_currentStageData` 필드에 캐시. `SetCurrentStage()` 호출 시 캐시 무효화. 매 프레임 호출 패턴이 생겨도 배열 경계 검사 오버헤드 제거 |
| [ ] | SaveManager 비동기 Save 옵션 | `SaveManager.cs` | 다중 저장 항목 추가 시 `SaveAsync(int)` 메서드를 UniTask 기반으로 추가. 모바일 스토리지 I/O로 인한 단발성 프리징 회피. 현재 동기 Save도 유지 |
| [ ] | BootLoader 씬 로딩 동기화 | `BootLoader.cs` | `SceneManager.LoadSceneAsync` 완료 후 `UniTask.WaitUntil(() => UIManager.Instance != null)` 으로 Main 씬 매니저 준비를 명시적으로 대기. 멀티 씬 로딩 확장 시 레이스 컨디션 방지 |

> **설계 의도**:
> - M5/M6에서 흐름과 연출이 검증된 후 적용해야 리팩토링이 기능 회귀를 일으켜도 원인 파악이 쉽다.
> - 안정성(M7-A) → 정리(M7-B) → 성능(M7-C) → 데이터/I/O(M7-D) 순으로 영향 범위가 작은 항목부터 진행한다.
> - 모든 항목은 "현재 동작에는 문제 없으나, 중규모로 확장될 때 누적 부하나 디버깅 난이도가 커지는 지점"을 사전에 정리하는 목적이다.

---

## 기능 의존성 순서 (재배치 후)

```
M1: 씬 구조 & Canvas 설정 (F008 기반 선행 적용)
  ↓
M2: StageDataSO + StageManager + SaveManager (F005, F006)
  ↓
M3: BootLoader — 저장 데이터 불러오기 → Main 씬 전환
  ↓
M4: UIManager + 메인/인게임/완료 화면 패널 UI (F001, F002, F007) + SafeAreaHandler (F008 완성)
  ↓
M5: GameFlowController — 클리어/실패 판정 연결 및 전체 흐름 통합
  ↓
M6: AnimationController — DOTween 클리어/실패 연출 삽입 (F003, F004)
  ↓
M7: 중규모 확장 대비 리팩토링 (안정성 보강 / 코드 정리 / 성능 최적화 / 데이터·I/O 견고화)
```

---

## 기능 번호 ↔ 마일스톤 매핑

| 기능 | 마일스톤 | 비고 |
|------|---------|------|
| F001 — 메인 화면 | M4 | UIManager와 함께 구현 |
| F002 — 인게임 화면 | M4 | UIManager와 함께 구현 |
| F003 — 클리어 연출 | M6 | 흐름 검증(M5) 후 추가 |
| F004 — 실패 연출 | M6 | 흐름 검증(M5) 후 추가 |
| F005 — 저장 & 복원 | M2 | SaveManager (M7-D에서 비동기 옵션 보강) |
| F006 — 스테이지 범위 제한 | M2 | StageManager + StageDataSO (M7-D에서 캐싱 보강) |
| F007 — 게임 완료 화면 | M4 | UIManager와 함께 구현 |
| F008 — 반응형 UI | M1(Canvas Scaler) + M4(SafeAreaHandler) + M7-C(동적 모니터링) | 초기 설정이 핵심 |

---

## 재배치 근거

| 변경 내용 | 이유 |
|-----------|------|
| Canvas Scaler를 M1(가장 먼저)로 이동 | 이후 모든 UI 작업이 올바른 해상도 기준 위에서 이루어져야 함. 나중에 적용하면 배치된 UI 전체를 재조정해야 함 |
| Boot 씬 초기화를 M3으로 독립 | 진입점이 명확해야 UIManager가 정확한 스테이지 번호를 표시할 수 있음 |
| UIManager를 GameFlowController보다 먼저(M4→M5) | UI가 준비된 후 컨트롤러를 연결해야 테스트 가능. UIManager 없이 GameFlowController만 있으면 동작 확인 불가 |
| 연출(AnimationController)을 M6으로 분리 | 흐름 버그와 연출 버그를 단계별로 분리하여 디버깅 효율 향상 |
| GameFlowController를 M5로 이동 (기존 M2) | UI 없이 컨트롤러만 구현하면 동작 검증이 불가. UI 준비 후 연결하는 것이 실질적 순서 |
| 중규모 확장 리팩토링을 M7로 신설 | 코드 리뷰·성능 분석에서 도출된 개선사항은 MVP 흐름이 검증된 후 적용해야 리팩토링 영향과 기능 회귀를 분리해 추적할 수 있음 |

---

## 완료 이력

| 날짜 | 기능 | 비고 |
|------|------|------|
| 2026-05-03 | F006 — 스테이지 범위 제한 | AnimationType enum, StageDataSO 클래스, StageManager, SO 에셋 20개 |
| 2026-05-03 | F005 — 저장 & 복원 | SaveManager (PlayerPrefs MaxClearedStage) |
| 2026-05-04 | M3 — Boot 씬 진입 & 초기화 흐름 | BootLoader (UniTask 비동기), Boot 씬 배치, Boot→Main 씬 전환 완성 |
| 2026-05-04 | M1 — 프로젝트 기반 & 씬 구조 | Canvas CanvasScaler(1080×1920, Match 0.5), SafeArea 패널, MainPanel/InGamePanel/ClearPanel/FailPanel/GameClearPanel 비활성화 배치 |
| 2026-05-04 | M4 — UIManager & 화면 전환 골격 | UIManager(ShowPanel/RefreshStageDisplay/버튼이벤트), SafeAreaHandler(노치/홈바 대응), 5개 패널 UI 골격, PanelType enum |
| 2026-05-04 | M5 — GameFlowController & 판정 로직 연결 | GameFlowController(OnStageClear/OnStageFail), 클리어·실패·재시작 버튼 Persistent Listener 연결, Play Mode 3시나리오 PASS |
