using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// DOTween 기반 클리어/실패 UI 연출을 담당하는 컨트롤러.
/// UIManager의 위임 요청을 받아 Sequence 애니메이션을 실행하고 완료 콜백을 호출한다.
/// </summary>
public class AnimationController : MonoBehaviour
{
    public static AnimationController Instance { get; private set; }

    [SerializeField] private CanvasGroup _clearOverlay;
    [SerializeField] private Transform _clearTextTransform;

    [SerializeField] private CanvasGroup _failOverlay;
    [SerializeField] private Transform _failTextTransform;

    private Sequence _activeSequence;

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
    /// 클리어 연출을 재생한다. 오버레이 페이드인 + 텍스트 스케일 인 후 onComplete 콜백을 호출한다.
    /// </summary>
    public void PlayClear(Action onComplete)
    {
        _activeSequence?.Kill();

        _clearOverlay.alpha = 0f;
        _clearTextTransform.localScale = Vector3.zero;

        Sequence s = DOTween.Sequence();
        s.Append(_clearOverlay.DOFade(0.85f, 0.4f));
        s.Join(_clearTextTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        s.AppendInterval(1.0f);
        s.AppendCallback(() => onComplete?.Invoke());

        _activeSequence = s;
    }

    /// <summary>
    /// 실패 연출을 재생한다. 오버레이 페이드인 + 텍스트 흔들기 후 onComplete 콜백을 호출한다.
    /// </summary>
    public void PlayFail(Action onComplete)
    {
        _activeSequence?.Kill();

        _failOverlay.alpha = 0f;

        Sequence s = DOTween.Sequence();
        s.Append(_failOverlay.DOFade(0.85f, 0.4f));
        s.Join(_failTextTransform.DOShakePosition(0.5f, 20f, 15));
        s.AppendInterval(0.8f);
        s.AppendCallback(() => onComplete?.Invoke());

        _activeSequence = s;
    }
}
