using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 에디터에서 Boot 이외의 씬에서 Play를 누를 때 자동으로 Boot 씬부터 시작하도록 리다이렉트한다.
/// RuntimeInitializeOnLoadMethod(BeforeSceneLoad)는 씬 로드 직전에 실행되며,
/// 프로덕션 빌드에서는 #if UNITY_EDITOR 조건에 의해 아무 작업도 수행하지 않는다.
/// </summary>
public static class SceneBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
#if UNITY_EDITOR
        if (SceneManager.GetActiveScene().name != "Boot")
        {
            Debug.Log("[SceneBootstrapper] Boot 씬 이외에서 실행 감지 → Boot 씬으로 리다이렉트합니다.");
            SceneManager.LoadScene("Boot");
        }
#endif
    }
}
