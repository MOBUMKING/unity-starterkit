using UnityEngine;

/// <summary>
/// 각 스테이지의 고정 데이터를 보관하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData")]
public class StageDataSO : ScriptableObject
{
    [SerializeField] private int _stageNumber;
    [SerializeField] private string _stageName;
    [SerializeField] private AnimationType _clearAnimationType;
    [SerializeField] private AnimationType _failAnimationType;

    /// <summary>스테이지 번호 (1~20)</summary>
    public int StageNumber => _stageNumber;

    /// <summary>화면에 표시할 스테이지 이름 (예: "STAGE 1")</summary>
    public string StageName => _stageName;

    /// <summary>클리어 연출 종류</summary>
    public AnimationType ClearAnimationType => _clearAnimationType;

    /// <summary>실패 연출 종류</summary>
    public AnimationType FailAnimationType => _failAnimationType;
}
