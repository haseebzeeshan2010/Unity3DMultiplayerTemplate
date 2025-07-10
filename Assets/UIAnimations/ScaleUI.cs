using UnityEngine;
using DG.Tweening;

/// <summary>
/// Scales a UI element in/out using a custom AnimationCurve.
/// </summary>
public class ScaleUI : MonoBehaviour
{
    [Header("Scale In Settings")]
    [Tooltip("Target scale for scale-in animation.")]
    [SerializeField] private Vector3 scaleInTarget = Vector3.one;

    [Tooltip("Duration of the scale-in animation in seconds.")]
    [SerializeField] private float scaleInDuration = 1f;

    [Tooltip("Delay before the scale-in animation starts (seconds).")]
    [SerializeField] private float scaleInDelay = 0f;

    [Tooltip("Custom AnimationCurve for scale-in.")]
    [SerializeField] private AnimationCurve scaleInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scale Out Settings")]
    [Tooltip("Target scale for scale-out animation.")]
    [SerializeField] private Vector3 scaleOutTarget = Vector3.zero;

    [Tooltip("Duration of the scale-out animation in seconds.")]
    [SerializeField] private float scaleOutDuration = 1f;

    [Tooltip("Delay before the scale-out animation starts (seconds).")]
    [SerializeField] private float scaleOutDelay = 0f;

    [Tooltip("Custom AnimationCurve for scale-out.")]
    [SerializeField] private AnimationCurve scaleOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional: Auto-play on Start")]
    [Tooltip("If true, animation plays automatically on Start.")]
    [SerializeField] private bool playOnStart = true;

    private RectTransform rectTransform;
    private Vector3 originalScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("ScaleUI: This component requires a RectTransform (UI element).");
            enabled = false;
            return;
        }
        originalScale = rectTransform.localScale;
    }

    void Start()
    {
        if (playOnStart)
            PlayScaleIn();
    }

    /// <summary>
    /// Triggers the scale-in animation.
    /// </summary>
    public void PlayScaleIn()
    {
        if (rectTransform == null)
            return;

        // Set initial scale to scaleOutTarget (e.g., Vector3.zero)
        rectTransform.localScale = scaleOutTarget;

        // Kill any running tweens on this rectTransform
        rectTransform.DOKill();

        // Animate to scaleInTarget with delay and custom curve
        rectTransform.DOScale(scaleInTarget, scaleInDuration)
            .SetEase(scaleInCurve)
            .SetDelay(scaleInDelay);
    }

    /// <summary>
    /// Triggers the scale-out animation.
    /// </summary>
    [ContextMenu("Scale Out")]
    public void ScaleOut()
    {
        if (rectTransform == null)
            return;

        // Kill any running tweens on this rectTransform
        rectTransform.DOKill();

        // Animate to scaleOutTarget with delay and custom curve
        rectTransform.DOScale(scaleOutTarget, scaleOutDuration)
            .SetEase(scaleOutCurve)
            .SetDelay(scaleOutDelay);
    }
}
