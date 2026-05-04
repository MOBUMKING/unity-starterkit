using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot 씬 전용 진입점. 저장 데이터를 복원하고 Main 씬으로 전환한다.
/// </summary>
public class BootLoader : MonoBehaviour
{
    private const string MAIN_SCENE_NAME = "Main"; // M7-B: 씬 이름 매직 스트링 상수화

    private CancellationTokenSource _cts;

    private void Awake()
    {
        _cts = new CancellationTokenSource();
    }

    private void Start()
    {
        // Start()에서 호출: 모든 Awake() 완료 후 실행이 보장되므로 SaveManager.Instance가 null이 아님
        InitializeAsync(_cts.Token).Forget();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>
    /// 저장 데이터를 불러와 StageManager에 복원한 뒤 Main 씬으로 비동기 전환한다.
    /// 씬 전환 후 UIManager.Instance 준비를 명시적으로 대기해 멀티 씬 확장 시 레이스 컨디션을 방지한다.
    /// </summary>
    private async UniTask InitializeAsync(CancellationToken ct)
    {
        int maxCleared = SaveManager.Instance.Load();
        StageManager.Instance.SetCurrentStage(maxCleared + 1);

        await SceneManager.LoadSceneAsync(MAIN_SCENE_NAME).ToUniTask(cancellationToken: ct);

        // M7-D: Main 씬 매니저 준비 대기 (단일 씬 로딩에서는 즉시 통과, 멀티 씬 확장 대비)
        await UniTask.WaitUntil(() => UIManager.Instance != null, cancellationToken: ct);
    }
}
