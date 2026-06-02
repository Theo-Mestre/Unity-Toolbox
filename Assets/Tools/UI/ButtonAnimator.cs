using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonAnimator
    : MonoBehaviour
    , IPointerEnterHandler
    , IPointerExitHandler
    , IPointerClickHandler
{
    [Header("Animation Target")]
    [SerializeField] private RectTransform scaleAnimationTarget;
    [SerializeField] private RectTransform punchAnimationTarget;
    [SerializeField] private Vector3 originalScale = Vector3.one;

    [Header("Pointer Enter Punch")]
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private float punchStrenght = 5;
    [SerializeField] private int punchVibrato = 20;
    [SerializeField] private float punchElasticity = 1.0f;

    [Header("Hover Settings")]
    [SerializeField] private Vector3 hoverScale = new(1.05f, 1.05f, 1f);
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private Ease hoverEaseType = Ease.OutElastic;

    [Header("Click Settings")]
    [SerializeField] private Vector3 clickScale = new(0.95f, 0.95f, 1f);
    [SerializeField] private float clickDuration = 0.05f;
    [SerializeField] private Ease clickEaseType = Ease.OutCubic;

    [Space]

    [Header("Button Callbacks")]
    [SerializeField, Space] public UnityEvent OnButtonPressed = null;
    [SerializeField, Space] public UnityEvent OnButtonEntered = null;
    [SerializeField, Space] public UnityEvent OnButtonExited = null;

    private Tween currentTween;
    private bool isPointerOver = false;

    private void OnEnable()
    {
        if (scaleAnimationTarget == null)
        {
            scaleAnimationTarget = GetComponent<RectTransform>();
        }
        if (punchAnimationTarget == null)
        {
            punchAnimationTarget = scaleAnimationTarget;
        }

        scaleAnimationTarget.localScale = originalScale;
        isPointerOver = false;
    }

    private void OnDisable()
    {
        DOTween.Kill(scaleAnimationTarget);
        DOTween.Kill(punchAnimationTarget);
        scaleAnimationTarget.localScale = originalScale;
        punchAnimationTarget.eulerAngles = Vector3.zero;
        isPointerOver = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        AnimateToScale(hoverScale, hoverDuration, hoverEaseType);

        if (punchDuration <= 0.0f) return;

        punchAnimationTarget
            .DOPunchRotation(Vector3.forward * punchStrenght,
            punchDuration, punchVibrato, punchElasticity)
            .OnComplete(() => punchAnimationTarget.DORotate(Vector3.zero, punchDuration / 2.0f));

        OnButtonEntered?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        AnimateToScale(originalScale, hoverDuration, hoverEaseType);

        OnButtonExited?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        currentTween?.Kill();
        scaleAnimationTarget.DOScale(clickScale, clickDuration).SetEase(clickEaseType).OnComplete(() =>
        {
            Vector3 targetScale = isPointerOver ? hoverScale : originalScale;
            AnimateToScale(targetScale, hoverDuration, clickEaseType);
        });

        OnButtonPressed?.Invoke();
    }

    private void AnimateToScale(Vector3 targetScale, float duration, Ease easeType)
    {
        currentTween?.Kill();
        currentTween = scaleAnimationTarget.DOScale(targetScale, duration).SetEase(easeType);
    }
}
