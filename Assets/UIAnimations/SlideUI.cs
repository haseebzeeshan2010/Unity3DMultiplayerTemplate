using UnityEngine;
using DG.Tweening;

public class SlideUI : MonoBehaviour
{
    public enum SlideDirection { Left, Right, Up, Down }

    [Header("Slide In Settings")]
    [Tooltip("Direction from which the UI element will slide in.")]
    [SerializeField] private SlideDirection slideInDirection = SlideDirection.Left;

    [Tooltip("Duration of the slide-in animation in seconds.")]
    [SerializeField] private float slideInDuration = 1f;

    [Tooltip("Delay before the slide-in animation starts (seconds).")]
    [SerializeField] private float slideInDelay = 0f;

    [Tooltip("Custom AnimationCurve for slide-in.")]
    [SerializeField] private AnimationCurve slideInCustomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Slide Out Settings")]
    [Tooltip("Direction to which the UI element will slide out.")]
    [SerializeField] private SlideDirection slideOutDirection = SlideDirection.Left;

    [Tooltip("Duration of the slide-out animation in seconds.")]
    [SerializeField] private float slideOutDuration = 1f;

    [Tooltip("Delay before the slide-out animation starts (seconds).")]
    [SerializeField] private float slideOutDelay = 0f;

    [Tooltip("Custom AnimationCurve for slide-out.")]
    [SerializeField] private AnimationCurve slideOutCustomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional: Auto-play on Start")]
    [Tooltip("If true, animation plays automatically on Start.")]
    [SerializeField] private bool playOnStart = true;

    [Header("Optional: Start Off-Screen")]
    [Tooltip("If true, UI element starts at the off-screen position (according to slide-in direction).")]
    [SerializeField] private bool startOffScreen = false;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("SlideIn: This component requires a RectTransform (UI element).");
            enabled = false;
            return;
        }
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // If toggled, set to off-screen position at startup
        if (startOffScreen && rectTransform.parent != null)
        {
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                rectTransform.anchoredPosition = GetOffScreenPosition(parentRect, slideInDirection);
            }
        }
    }

    void Start()
    {
        if (playOnStart)
            PlaySlideIn();
    }

    /// <summary>
    /// Triggers the slide-in animation.
    /// </summary>
    public void PlaySlideIn()
    {
        if (rectTransform == null || rectTransform.parent == null)
            return;

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogError("SlideIn: Parent must be a RectTransform.");
            return;
        }

        // Calculate off-screen start position based on slide-in direction
        Vector2 startPos = GetOffScreenPosition(parentRect, slideInDirection);

        // Set initial position only if not already off-screen (prevents jump if already set)
        if (!startOffScreen)
            rectTransform.anchoredPosition = startPos;

        // Kill any running tweens on this rectTransform
        rectTransform.DOKill();

        // Animate to original position with delay, always using custom curve
        rectTransform.DOAnchorPos(originalAnchoredPosition, slideInDuration)
            .SetEase(slideInCustomCurve)
            .SetDelay(slideInDelay);
    }

    /// <summary>
    /// Triggers the slide-out animation.
    /// </summary>
    [ContextMenu("Slide Out")]
    public void SlideOut()
    {
        if (rectTransform == null || rectTransform.parent == null)
            return;

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogError("SlideIn: Parent must be a RectTransform.");
            return;
        }

        // Kill any running tweens on this rectTransform
        rectTransform.DOKill();

        // Calculate off-screen target position based on slide-out direction
        Vector2 targetPos = GetOffScreenPosition(parentRect, slideOutDirection);

        // Animate to off-screen position with delay, always using custom curve
        rectTransform.DOAnchorPos(targetPos, slideOutDuration)
            .SetEase(slideOutCustomCurve)
            .SetDelay(slideOutDelay);
    }

    /// <summary>
    /// Helper to calculate the off-screen position based on direction.
    /// </summary>
    private Vector2 GetOffScreenPosition(RectTransform parentRect, SlideDirection direction)
    {
        Vector2 pos = originalAnchoredPosition;
        switch (direction)
        {
            case SlideDirection.Left:
                pos.x = -parentRect.rect.width;
                break;
            case SlideDirection.Right:
                pos.x = parentRect.rect.width;
                break;
            case SlideDirection.Up:
                pos.y = parentRect.rect.height;
                break;
            case SlideDirection.Down:
                pos.y = -parentRect.rect.height;
                break;
        }
        return pos;
    }
}
