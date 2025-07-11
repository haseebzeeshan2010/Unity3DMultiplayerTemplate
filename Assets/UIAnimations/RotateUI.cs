using UnityEngine;
using DG.Tweening;

/// <summary>
/// Rotates a UI element using DOTween, driven by a serialized AnimationCurve.
/// Uses RectTransform for UI rotation.
/// </summary>
public class RotateUI : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float duration = 1f;
    [SerializeField] private Vector3 targetRotation = new Vector3(0, 0, 360);
    [SerializeField] private bool playOnStart = true;

    private RectTransform rectTransform;
    private Vector3 initialRotation;
    private Tween rotationTween;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RotateUI requires a RectTransform component.");
        }
    }

    void Start()
    {
        initialRotation = rectTransform.localEulerAngles;
        if (playOnStart)
        {
            PlayRotation();
        }
    }

    /// <summary>
    /// Plays the rotation animation using the AnimationCurve.
    /// </summary>
    public void PlayRotation()
    {
        rotationTween?.Kill();

        float t = 0f;
        rotationTween = DOTween.To(() => t, x => {
            t = x;
            float curveValue = rotationCurve.Evaluate(t);
            rectTransform.localEulerAngles = initialRotation + targetRotation * curveValue;
        }, 1f, duration)
        .SetEase(Ease.Linear)
        .OnComplete(() => t = 1f);
    }

    /// <summary>
    /// Optionally, call this to reset and replay the animation.
    /// </summary>
    public void ResetAndPlay()
    {
        rectTransform.localEulerAngles = initialRotation;
        PlayRotation();
    }
}
