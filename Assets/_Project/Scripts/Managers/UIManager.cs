using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 화면 패널 전환 유형을 정의한다.
/// GameFlowController(M5)에서도 참조하므로 파일 최상단에 배치.
/// </summary>
public enum PanelType
{
    Main,
    InGame,
    Clear,
    Fail,
    GameClear
}

/// <summary>
/// Main 씬의 UI 패널 활성화/비활성화를 관리하는 매니저.
/// GameFlowController의 명령을 받아 ShowPanel()로 단일 패널을 표시한다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _inGamePanel;
    [SerializeField] private GameObject _clearPanel;
    [SerializeField] private GameObject _failPanel;
    [SerializeField] private GameObject _gameClearPanel;

    [SerializeField] private TMP_Text _mainStageText;
    [SerializeField] private TMP_Text _inGameStageText;

    [SerializeField] private AnimationController _animationController;

    private Dictionary<PanelType, GameObject> _panelMap;

    private void Awake()
    {
        // 싱글턴 중복 인스턴스 제거 (DontDestroyOnLoad 없음 — Main 씬 전용)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _panelMap = new Dictionary<PanelType, GameObject>
        {
            { PanelType.Main,      _mainPanel },
            { PanelType.InGame,    _inGamePanel },
            { PanelType.Clear,     _clearPanel },
            { PanelType.Fail,      _failPanel },
            { PanelType.GameClear, _gameClearPanel }
        };
    }

    private void Start()
    {
        RefreshStageDisplay();
        ShowPanel(PanelType.Main);
    }

    /// <summary>
    /// 지정한 패널만 활성화하고 나머지 패널은 비활성화한다.
    /// </summary>
    public void ShowPanel(PanelType type)
    {
        foreach (var pair in _panelMap)
        {
            if (pair.Value != null)
            {
                pair.Value.SetActive(pair.Key == type);
            }
        }
    }

    /// <summary>
    /// StageManager에서 현재 스테이지 번호를 읽어 모든 스테이지 텍스트를 갱신한다.
    /// </summary>
    public void RefreshStageDisplay()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogWarning("[UIManager] StageManager.Instance가 null입니다. Boot 씬을 거쳐 실행하세요.");
            return;
        }

        string stageText = "STAGE " + StageManager.Instance.CurrentStageNumber;

        if (_mainStageText != null)
            _mainStageText.text = stageText;

        if (_inGameStageText != null)
            _inGameStageText.text = stageText;
    }

    /// <summary>
    /// 플레이 버튼 클릭 시 호출. 스테이지 텍스트를 갱신하고 인게임 화면으로 전환한다.
    /// </summary>
    public void OnPlayButtonClick()
    {
        RefreshStageDisplay();
        ShowPanel(PanelType.InGame);
    }

    /// <summary>
    /// 처음부터 다시 시작 버튼 클릭 시 호출. 저장 데이터를 초기화하고 1스테이지 메인 화면으로 돌아간다.
    /// </summary>
    public void OnRestartButtonClick()
    {
        SaveManager.Instance.Reset();
        StageManager.Instance.SetCurrentStage(1);
        RefreshStageDisplay();
        ShowPanel(PanelType.Main);
    }

    /// <summary>
    /// 클리어 연출 패널을 표시하고 AnimationController에 재생을 위임한다. 완료 후 onComplete 콜백을 호출한다.
    /// </summary>
    public void PlayClearAndThen(Action onComplete)
    {
        ShowPanel(PanelType.Clear);

        if (_animationController == null)
        {
            Debug.LogWarning("[UIManager] AnimationController가 연결되지 않았습니다.");
            onComplete?.Invoke();
            return;
        }

        _animationController.PlayClear(onComplete);
    }

    /// <summary>
    /// 실패 연출 패널을 표시하고 AnimationController에 재생을 위임한다. 완료 후 onComplete 콜백을 호출한다.
    /// </summary>
    public void PlayFailAndThen(Action onComplete)
    {
        ShowPanel(PanelType.Fail);

        if (_animationController == null)
        {
            Debug.LogWarning("[UIManager] AnimationController가 연결되지 않았습니다.");
            onComplete?.Invoke();
            return;
        }

        _animationController.PlayFail(onComplete);
    }
}
