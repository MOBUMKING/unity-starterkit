using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// PlayerPrefs 기반 진행 상태 저장/불러오기를 담당하는 매니저.
/// "MaxClearedStage" 키는 이 클래스를 통해서만 읽고 쓴다.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "MaxClearedStage";

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
    }

    /// <summary>
    /// 클리어한 최고 스테이지 번호를 저장한다. 클리어 판정 즉시 호출.
    /// </summary>
    public void Save(int maxClearedStage)
    {
        PlayerPrefs.SetInt(SAVE_KEY, maxClearedStage);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 클리어한 최고 스테이지 번호를 비동기로 저장한다.
    /// 다중 저장 항목 추가 시 PlayerPrefs.Save()를 스레드 풀에서 실행해 메인 스레드 블로킹을 회피한다.
    /// </summary>
    public async UniTask SaveAsync(int maxClearedStage)
    {
        PlayerPrefs.SetInt(SAVE_KEY, maxClearedStage);
        await UniTask.RunOnThreadPool(() => PlayerPrefs.Save());
    }

    /// <summary>
    /// 저장된 최고 클리어 스테이지 번호를 반환한다. 저장 데이터 없으면 0 반환.
    /// </summary>
    public int Load()
    {
        return PlayerPrefs.GetInt(SAVE_KEY, 0);
    }

    /// <summary>
    /// 저장 데이터를 초기화한다. 처음부터 다시 시작 시 호출.
    /// </summary>
    public void Reset()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
    }
}
