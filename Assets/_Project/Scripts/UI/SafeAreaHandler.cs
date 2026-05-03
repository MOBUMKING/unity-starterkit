using UnityEngine;

/// <summary>
/// 디바이스 Safe Area에 맞춰 RectTransform 앵커를 자동 조정하는 컴포넌트.
/// Canvas 자식인 SafeArea GameObject에 부착한다.
/// </summary>
public class SafeAreaHandler : MonoBehaviour
{
    private void Awake()
    {
        ApplySafeArea();
    }

    /// <summary>
    /// Screen.safeArea 픽셀 좌표를 0~1 앵커 좌표로 변환해 RectTransform에 적용한다.
    /// </summary>
    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        RectTransform rt = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("[SafeAreaHandler] 부모 Canvas를 찾을 수 없습니다.");
            return;
        }

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        rt.anchorMin = safeArea.position / screenSize;
        rt.anchorMax = (safeArea.position + safeArea.size) / screenSize;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
