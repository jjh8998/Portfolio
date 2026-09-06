using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if DOTWEEN || DOTWEEN_ENABLED || DOTWEEN_PRESENT
using DG.Tweening;
#endif

[DisallowMultipleComponent]
public sealed class UIButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private List<Graphic> targetGraphics = new List<Graphic>();

    [Header("Building State")]
    [SerializeField] private bool hasActiveBuilding;
    [SerializeField] private float emptyEmissionIntensity;
    [SerializeField] private float activeEmissionIntensity = 0.75f;
    [SerializeField, ColorUsage(false, true)] private Color activeEmissionColor = new Color32(0x00, 0xE5, 0xFF, 0xFF);

    [Header("Hover")]
    [SerializeField] private float hoverEmissionIntensity = 1.25f;
    [SerializeField, ColorUsage(false, true)] private Color hoverEmissionColor = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;
    [SerializeField] private bool includeSelection = true;

    [Header("Frame")]
    [SerializeField, ColorUsage(false, true)] private Color idleLineColor = new Color32(0x00, 0xBF, 0xE8, 0xD1);
    [SerializeField, Min(0f)] private float intensityColorScale = 0.12f;
    [SerializeField, Range(0f, 1f)] private float minLineAlpha = 0.72f;
    [SerializeField, Range(0f, 0.2f)] private float intensityAlphaScale = 0.025f;

    private float currentEmissionIntensity;
    private Color currentEmissionColor;
    private bool isPointerInside;
    private bool isSelected;
    private Coroutine fadeRoutine;
    private readonly Dictionary<CutCornerPanel, PanelBorderState> originalPanelBorderStates = new Dictionary<CutCornerPanel, PanelBorderState>();
#if DOTWEEN || DOTWEEN_ENABLED || DOTWEEN_PRESENT
    private Tween intensityTween;
    private Tween colorTween;
#endif

    private void Reset()
    {
        ResolveTargetFrames();
    }

    private void Awake()
    {
        ResolveTargetFrames();
        ApplyGlowStateImmediate();
    }

    private void OnEnable()
    {
        ResolveTargetFrames();
        ApplyGlowStateImmediate();
    }

    private void OnDisable()
    {
        StopFade();
        isPointerInside = false;
        isSelected = false;
        ClearFrameColorOverride();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        RefreshGlow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        RefreshGlow();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!includeSelection)
            return;

        isSelected = true;
        RefreshGlow();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!includeSelection)
            return;

        isSelected = false;
        RefreshGlow();
    }

    public void SetGlow(bool glow)
    {
        isPointerInside = glow;
        RefreshGlow();
    }

    public void SetActiveBuilding(bool value)
    {
        if (hasActiveBuilding == value)
            return;

        hasActiveBuilding = value;
        RefreshGlow();
    }

    private void RefreshGlow()
    {
        if (!ResolveTargetGraphics())
            return;

        bool shouldGlow = isPointerInside || (includeSelection && isSelected);
        GlowState state = GetGlowState(shouldGlow);
        AnimateGlowState(state.intensity, state.color);
    }

    private bool ResolveTargetFrames() => ResolveTargetGraphics();

    private bool ResolveTargetGraphics()
    {
        if (targetGraphics == null)
            targetGraphics = new List<Graphic>();

        RemoveNullTargetGraphics();

        if (targetGraphics.Count == 0)
            AddTargetGraphic(GetComponent<CutCornerPanel>());

        if (targetGraphics.Count == 0)
        {
            Selectable selectable = GetComponent<Selectable>();
            if (selectable != null)
                AddTargetGraphic(selectable.targetGraphic);
        }

        return targetGraphics.Count > 0;
    }

    private void AnimateGlowState(float targetIntensity, Color targetColor)
    {
        StopFade();

#if DOTWEEN || DOTWEEN_ENABLED || DOTWEEN_PRESENT
        intensityTween = DOTween.To(
                () => currentEmissionIntensity,
                value =>
                {
                    currentEmissionIntensity = value;
                    ApplyFrameColor(ComposeLineColor(currentEmissionColor, currentEmissionIntensity));
                },
                targetIntensity,
                fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetTarget(this);

        colorTween = DOTween.To(
                () => currentEmissionColor,
                value =>
                {
                    currentEmissionColor = value;
                    ApplyFrameColor(ComposeLineColor(currentEmissionColor, currentEmissionIntensity));
                },
                targetColor,
                fadeDuration)
            .SetEase(Ease.OutQuad)
            .SetTarget(this);
#else
        fadeRoutine = StartCoroutine(FadeGlowState(targetIntensity, targetColor));
#endif
    }

    private System.Collections.IEnumerator FadeGlowState(float targetIntensity, Color targetColor)
    {
        float startIntensity = currentEmissionIntensity;
        Color startColor = currentEmissionColor;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            t = 1f - ((1f - t) * (1f - t));
            currentEmissionIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            currentEmissionColor = Color.Lerp(startColor, targetColor, t);
            ApplyFrameColor(ComposeLineColor(currentEmissionColor, currentEmissionIntensity));
            yield return null;
        }

        currentEmissionIntensity = targetIntensity;
        currentEmissionColor = targetColor;
        if (currentEmissionIntensity <= 0.001f)
            ClearFrameColorOverride();
        else
            ApplyFrameColor(ComposeLineColor(currentEmissionColor, currentEmissionIntensity));
        fadeRoutine = null;
    }

    private void ApplyGlowStateImmediate()
    {
        bool shouldGlow = isPointerInside || (includeSelection && isSelected);
        GlowState state = GetGlowState(shouldGlow);
        currentEmissionIntensity = state.intensity;
        currentEmissionColor = state.color;
        if (currentEmissionIntensity <= 0.001f)
            ClearFrameColorOverride();
        else
            ApplyFrameColor(ComposeLineColor(currentEmissionColor, currentEmissionIntensity));
    }

    private GlowState GetGlowState(bool hover)
    {
        if (hover)
            return new GlowState(hoverEmissionIntensity, hoverEmissionColor);

        return hasActiveBuilding
            ? new GlowState(activeEmissionIntensity, activeEmissionColor)
            : new GlowState(emptyEmissionIntensity, idleLineColor);
    }

    private Color ComposeLineColor(Color sourceColor, float intensity)
    {
        if (intensity <= 0.001f)
            return idleLineColor;

        float colorScale = 1f + (Mathf.Max(0f, intensity) * intensityColorScale);
        float alpha = Mathf.Clamp01(Mathf.Max(sourceColor.a, minLineAlpha) + (intensity * intensityAlphaScale));
        return new Color(
            sourceColor.r * colorScale,
            sourceColor.g * colorScale,
            sourceColor.b * colorScale,
            alpha);
    }

    private void ApplyFrameColor(Color color)
    {
        if (!ResolveTargetGraphics())
            return;

        for (int i = 0; i < targetGraphics.Count; i++)
        {
            if (targetGraphics[i] == null)
                continue;

            if (targetGraphics[i] is CutCornerPanel panel)
            {
                RememberPanelBorderState(panel);
                panel.SetBorderColor(color);
            }
            else
            {
                targetGraphics[i].color = color;
            }

            targetGraphics[i].SetVerticesDirty();
        }
    }

    private void ClearFrameColorOverride()
    {
        if (!ResolveTargetGraphics())
            return;

        for (int i = 0; i < targetGraphics.Count; i++)
        {
            if (targetGraphics[i] == null)
                continue;

            if (targetGraphics[i] is CutCornerPanel panel)
            {
                RestorePanelBorderState(panel);
                panel.SetVerticesDirty();
            }
        }

        originalPanelBorderStates.Clear();
    }

    private void RememberPanelBorderState(CutCornerPanel panel)
    {
        if (panel == null || originalPanelBorderStates.ContainsKey(panel))
            return;

        originalPanelBorderStates.Add(panel, new PanelBorderState(panel.HasBorderColorOverride, panel.BorderColor));
    }

    private void RestorePanelBorderState(CutCornerPanel panel)
    {
        if (panel == null)
            return;

        if (!originalPanelBorderStates.TryGetValue(panel, out PanelBorderState state))
        {
            panel.ClearBorderColorOverride();
            return;
        }

        if (state.hasOverride)
            panel.SetBorderColor(state.borderColor);
        else
            panel.ClearBorderColorOverride();
    }

    private readonly struct PanelBorderState
    {
        public readonly bool hasOverride;
        public readonly Color borderColor;

        public PanelBorderState(bool hasOverride, Color borderColor)
        {
            this.hasOverride = hasOverride;
            this.borderColor = borderColor;
        }
    }

    private void StopFade()
    {
#if DOTWEEN || DOTWEEN_ENABLED || DOTWEEN_PRESENT
        if (intensityTween != null && intensityTween.IsActive())
            intensityTween.Kill();

        if (colorTween != null && colorTween.IsActive())
            colorTween.Kill();

        intensityTween = null;
        colorTween = null;
#endif

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private void AddTargetGraphic(Graphic graphic)
    {
        if (graphic == null || targetGraphics.Contains(graphic))
            return;

        targetGraphics.Add(graphic);
    }

    private void RemoveNullTargetGraphics()
    {
        for (int i = targetGraphics.Count - 1; i >= 0; i--)
        {
            if (targetGraphics[i] == null)
                targetGraphics.RemoveAt(i);
        }
    }

    private readonly struct GlowState
    {
        public readonly float intensity;
        public readonly Color color;

        public GlowState(float intensity, Color color)
        {
            this.intensity = intensity;
            this.color = color;
        }
    }
}
