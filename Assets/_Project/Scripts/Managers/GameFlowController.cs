using System;
using UnityEngine;

/// <summary>
/// 스테이지 클리어/실패 판정을 처리하고 화면 전환을 지시하는 컨트롤러.
/// StageManager·SaveManager·UIManager를 오케스트레이션하며, 역참조는 없다.
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    private void Awake()
    {
        // 싱글턴 중복 인스턴스 제거 (DontDestroyOnLoad 없음 — Main 씬 전용)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 스테이지 클리어 판정 시 호출. 즉시 저장 후 클리어 연출을 재생하고 완료 시 다음 화면으로 전환한다.
    /// </summary>
    public void OnStageClear()
    {
        SaveManager.Instance.Save(StageManager.Instance.CurrentStageNumber);

        UIManager.Instance.PlayClearAndThen(() =>
        {
            if (StageManager.Instance.IsLastStage())
            {
                UIManager.Instance.ShowPanel(PanelType.GameClear);
            }
            else
            {
                StageManager.Instance.AdvanceToNextStage();
                UIManager.Instance.RefreshStageDisplay();
                UIManager.Instance.ShowPanel(PanelType.Main);
            }
        });
    }

    /// <summary>
    /// 스테이지 실패 판정 시 호출. 실패 연출을 재생하고 완료 시 동일 스테이지 메인화면으로 복귀한다.
    /// </summary>
    public void OnStageFail()
    {
        UIManager.Instance.PlayFailAndThen(() =>
        {
            UIManager.Instance.RefreshStageDisplay();
            UIManager.Instance.ShowPanel(PanelType.Main);
        });
    }
}
