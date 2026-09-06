using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIHologramGlitchEffect : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private CanvasGroup targetCanvasGroup;
    [SerializeField] private Image glitchOverlay;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField, Min(0f)] private float duration = 0.45f;
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Reveal")]
    [SerializeField] private bool playFromHidden;
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;
    [SerializeField] private bool fadeIn = true;

    [Header("Glitch")]
    [SerializeField, Min(0f)] private float jitterStrength = 5f;
    [SerializeField, Range(0f, 1f)] private float flickerStrength = 0.22f;
    [SerializeField, Range(0f, 1f)] private float overlayMaxAlpha = 0.35f;
    [SerializeField, Range(0f, 0.15f)] private float scalePulseStrength = 0.035f;
    [SerializeField, Min(0.01f)] private float burstInterval = 0.045f;
    [SerializeField, Min(0.01f)] private float burstDuration = 0.025f;

    private Coroutine playCoroutine;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private float originalCanvasAlpha = 1f;
    private float originalOverlayAlpha;
    private bool hasOriginalState;

    private void Reset()
    {
        ResolveTargets();
    }

    private void Awake()
    {
        ResolveTargets();
        CaptureOriginalState();
        Restore();
    }

    private void OnEnable()
    {
        ResolveTargets();
        CaptureOriginalState();

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        ResolveTargets();

        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
            Restore();
        }

        CaptureOriginalState();

        if (duration <= 0f)
        {
            Restore();
            return;
        }

        if (playFromHidden && targetCanvasGroup != null)
            targetCanvasGroup.alpha = hiddenAlpha;

        playCoroutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        Restore();
    }

    public void Restore()
    {
        if (!hasOriginalState)
            CaptureOriginalState();

        if (targetRect != null)
        {
            targetRect.anchoredPosition = originalAnchoredPosition;
            targetRect.localScale = originalLocalScale;
        }

        if (targetCanvasGroup != null)
            targetCanvasGroup.alpha = originalCanvasAlpha;

        if (glitchOverlay != null)
        {
            Color color = glitchOverlay.color;
            color.a = originalOverlayAlpha;
            glitchOverlay.color = color;
        }
    }

    private IEnumerator PlayRoutine()
    {
        float elapsed = 0f;
        float nextBurstTime = 0f;
        float burstEndTime = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float intensity = intensityCurve != null ? Mathf.Max(0f, intensityCurve.Evaluate(progress)) : 1f - progress;

            if (elapsed >= nextBurstTime)
            {
                burstEndTime = elapsed + burstDuration;
                nextBurstTime = elapsed + burstInterval;
            }

            bool isBursting = elapsed <= burstEndTime;
            float burstMultiplier = isBursting ? 1f : 0.35f;
            ApplyFrame(intensity, burstMultiplier, progress);

            yield return null;
        }

        playCoroutine = null;
        Restore();
    }

    private void ApplyFrame(float intensity, float burstMultiplier, float progress)
    {
        if (targetRect != null)
        {
            Vector2 jitter = Random.insideUnitCircle * jitterStrength * intensity * burstMultiplier;
            targetRect.anchoredPosition = originalAnchoredPosition + jitter;

            float pulse = 1f + scalePulseStrength * intensity * Mathf.Sin(Time.unscaledTime * 80f);
            targetRect.localScale = originalLocalScale * pulse;
        }

        if (targetCanvasGroup != null)
        {
            float baseAlpha = originalCanvasAlpha;
            if (playFromHidden)
                baseAlpha = Mathf.Lerp(hiddenAlpha, originalCanvasAlpha, fadeIn ? progress : 1f);

            float flicker = Random.Range(0f, flickerStrength) * intensity * burstMultiplier;
            targetCanvasGroup.alpha = Mathf.Clamp01(baseAlpha - flicker);
        }

        if (glitchOverlay != null)
        {
            Color color = glitchOverlay.color;
            color.a = Mathf.Max(originalOverlayAlpha, overlayMaxAlpha * intensity * burstMultiplier);
            glitchOverlay.color = color;
        }
    }

    private void ResolveTargets()
    {
        if (targetRect == null)
            targetRect = transform as RectTransform;

        if (targetCanvasGroup == null)
            targetCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void CaptureOriginalState()
    {
        if (targetRect != null)
        {
            originalAnchoredPosition = targetRect.anchoredPosition;
            originalLocalScale = targetRect.localScale;
        }

        if (targetCanvasGroup != null)
            originalCanvasAlpha = targetCanvasGroup.alpha;

        if (glitchOverlay != null)
            originalOverlayAlpha = glitchOverlay.color.a;

        hasOriginalState = true;
    }
}
