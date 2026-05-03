using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

/// <summary>
/// 현재 스테이지 번호를 관리하고 스테이지 데이터를 제공하는 매니저
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private StageDataSO[] _stageData;

    private int _currentStageNumber = 1;
    private CancellationTokenSource _cts;

    /// <summary>현재 도전 중인 스테이지 번호 (1~20)</summary>
    public int CurrentStageNumber => _currentStageNumber;

    private void Awake()
    {
        // 싱글턴 중복 인스턴스 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _cts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>
    /// 현재 스테이지 번호를 지정값으로 변경한다. 범위(1~20)를 초과하면 클램프.
    /// </summary>
    public void SetCurrentStage(int stageNumber)
    {
        _currentStageNumber = Mathf.Clamp(stageNumber, 1, 20);
    }

    /// <summary>
    /// 다음 스테이지로 진행한다. 마지막 스테이지(20)면 변경하지 않는다.
    /// </summary>
    public void AdvanceToNextStage()
    {
        if (!IsLastStage())
        {
            _currentStageNumber++;
        }
    }

    /// <summary>현재 스테이지가 마지막(20)인지 여부를 반환한다.</summary>
    public bool IsLastStage()
    {
        return _currentStageNumber >= 20;
    }

    /// <summary>
    /// 현재 스테이지의 StageDataSO를 반환한다. 배열 미설정 또는 인덱스 이상 시 null 반환.
    /// </summary>
    public StageDataSO GetCurrentStageData()
    {
        if (_stageData == null || _stageData.Length == 0)
        {
            Debug.LogWarning("[StageManager] _stageData 배열이 비어 있습니다.");
            return null;
        }

        int index = _currentStageNumber - 1;
        if (index < 0 || index >= _stageData.Length)
        {
            Debug.LogWarning($"[StageManager] 스테이지 인덱스 {index} 범위 초과 (배열 크기: {_stageData.Length})");
            return null;
        }

        return _stageData[index];
    }
}
