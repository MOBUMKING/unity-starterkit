using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class ButtonWireSetup
{
    // 비활성 오브젝트를 포함하여 이름으로 Button 컴포넌트를 찾는다.
    private static Button FindButton(string name)
    {
        var all = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (var b in all)
            if (b.gameObject.name == name) return b;
        return null;
    }

    // Persistent Listener를 모두 제거한다 (RemoveAllListeners는 런타임 리스너만 제거).
    private static void ClearPersistentListeners(UnityEngine.Events.UnityEvent evt)
    {
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(evt, i);
    }

    [MenuItem("Tools/M5/Wire Buttons")]
    public static void WireButtons()
    {
        var clearBtn = FindButton("ClearButton");
        var failBtn = FindButton("FailButton");
        var restartBtn = FindButton("RestartButton");
        var gfc = Object.FindAnyObjectByType<GameFlowController>(FindObjectsInactive.Include);
        var uiMgr = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);

        if (clearBtn == null || failBtn == null || restartBtn == null || gfc == null || uiMgr == null)
        {
            Debug.LogError("[ButtonWireSetup] 필수 오브젝트를 찾지 못했습니다.");
            return;
        }

        ClearPersistentListeners(clearBtn.onClick);
        UnityEventTools.AddVoidPersistentListener(clearBtn.onClick, gfc.OnStageClear);
        EditorUtility.SetDirty(clearBtn);

        ClearPersistentListeners(failBtn.onClick);
        UnityEventTools.AddVoidPersistentListener(failBtn.onClick, gfc.OnStageFail);
        EditorUtility.SetDirty(failBtn);

        ClearPersistentListeners(restartBtn.onClick);
        UnityEventTools.AddVoidPersistentListener(restartBtn.onClick, uiMgr.OnRestartButtonClick);
        EditorUtility.SetDirty(restartBtn);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[ButtonWireSetup] 완료 — Clear:{clearBtn.onClick.GetPersistentEventCount()}, Fail:{failBtn.onClick.GetPersistentEventCount()}, Restart:{restartBtn.onClick.GetPersistentEventCount()}");
    }

    // ── Play Mode 통합 테스트 메서드 ──────────────────────────────────────

    // Main 씬에서 직접 Play 시 Boot 씬을 거치지 않아 매니저가 없을 수 있으므로 보장한다.
    private static void EnsureManagers()
    {
        if (StageManager.Instance == null)
            new GameObject("__StageManager_Test").AddComponent<StageManager>();
        if (SaveManager.Instance == null)
            new GameObject("__SaveManager_Test").AddComponent<SaveManager>();
    }

    [MenuItem("Tools/M5/Test 1 - Stage Clear (1→2)")]
    public static void TestStageClear()
    {
        if (!Application.isPlaying) { Debug.LogError("[M5 Test] Play Mode에서 실행하세요."); return; }
        EnsureManagers();
        StageManager.Instance.SetCurrentStage(1);
        GameFlowController.Instance.OnStageClear();
        int stage = StageManager.Instance.CurrentStageNumber;
        Debug.Log($"[M5 Test1] 클리어 후 스테이지={stage} (기대값:2) → {(stage == 2 ? "PASS" : "FAIL")}");
    }

    [MenuItem("Tools/M5/Test 2 - Stage Fail (유지)")]
    public static void TestStageFail()
    {
        if (!Application.isPlaying) { Debug.LogError("[M5 Test] Play Mode에서 실행하세요."); return; }
        EnsureManagers();
        StageManager.Instance.SetCurrentStage(3);
        GameFlowController.Instance.OnStageFail();
        int stage = StageManager.Instance.CurrentStageNumber;
        Debug.Log($"[M5 Test2] 실패 후 스테이지={stage} (기대값:3) → {(stage == 3 ? "PASS" : "FAIL")}");
    }

    [MenuItem("Tools/M5/Test 3 - Stage 20 Clear → GameClear")]
    public static void TestLastStageClear()
    {
        if (!Application.isPlaying) { Debug.LogError("[M5 Test] Play Mode에서 실행하세요."); return; }
        EnsureManagers();
        StageManager.Instance.SetCurrentStage(20);
        GameFlowController.Instance.OnStageClear();
        int stage = StageManager.Instance.CurrentStageNumber;
        Debug.Log($"[M5 Test3] 20스테이지 클리어 후 스테이지={stage} (기대값:20) → {(stage == 20 ? "PASS" : "FAIL")}");
    }

    [MenuItem("Tools/M5/Reset PlayerPrefs")]
    public static void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[M5 Test] PlayerPrefs 초기화 완료.");
    }
}
