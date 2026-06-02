using DG.Tweening;
using System;
using UnityEngine;

public class PopUpMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform panel = null;
    [SerializeField] protected CanvasGroup canvasGroup = null;

    [Header("Animation Settings")]
    [SerializeField] protected Vector3 panelOffset = Vector3.zero;
    [SerializeField] protected bool doesFadeAnimate = true;
    [SerializeField] protected float fadeAnimationDuration = 0.5f;
    [SerializeField] protected float appearAnimationDuration = 0.3f;
    [SerializeField] protected Ease fadeEaseType = Ease.InOutCubic;
    [SerializeField] protected Ease appearEaseType = Ease.InOutCubic;
    [SerializeField] protected Ease disappearEaseType = Ease.InOutCubic;

    public Action OnPopUpMenuAppear;
    public Action OnPopUpMenuDisappear;

    protected void OnEnable()
    {
        CheckReferences();

        panel.transform.localPosition = panelOffset;
        canvasGroup.blocksRaycasts = false;
    }

    public void Appear()
    {
        if (!doesFadeAnimate) canvasGroup.alpha = 1f;
        else canvasGroup.DOFade(1f, fadeAnimationDuration).SetEase(fadeEaseType);

        canvasGroup.blocksRaycasts = true;
        panel.DOLocalMove(Vector3.zero, appearAnimationDuration)
            .SetEase(appearEaseType);

        OnPopUpMenuAppear?.Invoke();
    }

    public void Disappear()
    {
        canvasGroup.blocksRaycasts = false;

        if (doesFadeAnimate) canvasGroup.DOFade(0f, fadeAnimationDuration).SetEase(fadeEaseType);

        panel.DOLocalMove(panelOffset, appearAnimationDuration)
            .SetEase(disappearEaseType)
            .OnComplete(() =>
            {
                if (!doesFadeAnimate)
                    canvasGroup.alpha = 0f;
            });

        OnPopUpMenuDisappear?.Invoke();
    }

    protected void CheckReferences()
    {
        if (panel == null) Debug.LogError($"PopUpMenu: Panel reference is missing in {gameObject.name}.");
        if (canvasGroup == null) Debug.LogError($"PopUpMenu: CanvasGroup reference is missing in {gameObject.name}.");
    }
}
