using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CutCornerPanel : MaskableGraphic
{
    [SerializeField] private Material panelMaterial;
    [SerializeField, Tooltip("When enabled, this object uses Border Color instead of the material _BorderColor.")]
    private bool overrideBorderColor = true;
    [SerializeField, ColorUsage(false, true)] private Color borderColor = new Color32(0x00, 0xBF, 0xE8, 0xEB);
    private static readonly int OuterGlowRangeId = Shader.PropertyToID("_OuterGlowRange");
    private static readonly int OuterGlowRangeRatioId = Shader.PropertyToID("_OuterGlowRangeRatio");
    private static readonly int OuterGlowRangeMinId = Shader.PropertyToID("_OuterGlowRangeMin");
    private static readonly int OuterGlowRangeMaxId = Shader.PropertyToID("_OuterGlowRangeMax");
    private static readonly int LegacyGlowWidthId = Shader.PropertyToID("_GlowWidth");
    [SerializeField, Min(0f)] private float minimumGlowPadding = 12f;

    public Color BorderColor
    {
        get => borderColor;
        set
        {
            overrideBorderColor = true;
            borderColor = value;
            SetVerticesDirty();
        }
    }

    public bool HasBorderColorOverride => overrideBorderColor;

    public Material PanelMaterial => panelMaterial;

    public void SetBorderColor(Color value)
    {
        BorderColor = value;
    }

    public void SetPanelMaterial(Material value)
    {
        if (panelMaterial == value && material == value)
            return;

        panelMaterial = value;
        material = value;
        SetMaterialDirty();
        SetVerticesDirty();
    }

    public void ClearBorderColorOverride()
    {
        if (!overrideBorderColor)
            return;

        overrideBorderColor = false;
        SetVerticesDirty();
    }

    public void SetStyle(Color fill, Color border, float lineWidth, float cut)
    {
        overrideBorderColor = true;
        borderColor = border;
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        maskable = true;
        ApplyPresetMaterial();
        EnsureCanvasVertexChannels();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
        SetMaterialDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0.01f || rect.height <= 0.01f)
            return;

        float expansion = GetGlowPadding(rect);
        Rect drawRect = Expand(rect, expansion);

        Vector3 lossyScale = transform.lossyScale;
        Vector4 panelMetrics = new Vector4(
            Mathf.Max(rect.width, 0.001f),
            Mathf.Max(rect.height, 0.001f),
            Mathf.Max(Mathf.Abs(lossyScale.x), 0.001f),
            Mathf.Max(Mathf.Abs(lossyScale.y), 0.001f));
        Vector4 border = overrideBorderColor ? ColorToVector(borderColor) : Vector4.zero;

        AddVertex(vh, new Vector2(drawRect.xMin, drawRect.yMin), GetPanelUv(rect, drawRect.xMin, drawRect.yMin), panelMetrics, border);
        AddVertex(vh, new Vector2(drawRect.xMin, drawRect.yMax), GetPanelUv(rect, drawRect.xMin, drawRect.yMax), panelMetrics, border);
        AddVertex(vh, new Vector2(drawRect.xMax, drawRect.yMax), GetPanelUv(rect, drawRect.xMax, drawRect.yMax), panelMetrics, border);
        AddVertex(vh, new Vector2(drawRect.xMax, drawRect.yMin), GetPanelUv(rect, drawRect.xMax, drawRect.yMin), panelMetrics, border);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    private static Rect Expand(Rect rect, float amount)
    {
        amount = Mathf.Max(0f, amount);
        return new Rect(
            rect.xMin - amount,
            rect.yMin - amount,
            rect.width + amount * 2f,
            rect.height + amount * 2f);
    }

    private void ApplyPresetMaterial()
    {
        if (panelMaterial == null || material == panelMaterial)
            return;

        material = panelMaterial;
    }

    private void EnsureCanvasVertexChannels()
    {
        Canvas parentCanvas = canvas;
        if (parentCanvas == null)
            return;

        AdditionalCanvasShaderChannels requiredChannels =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2;
        if ((parentCanvas.additionalShaderChannels & requiredChannels) != requiredChannels)
            parentCanvas.additionalShaderChannels |= requiredChannels;
    }

    private float GetGlowPadding(Rect rect)
    {
        if (panelMaterial != null && panelMaterial.HasProperty(OuterGlowRangeId))
            return Mathf.Max(minimumGlowPadding, GetResolvedOuterGlowRange(rect) / GetMinimumLossyScale());
        if (panelMaterial != null && panelMaterial.HasProperty(LegacyGlowWidthId))
            return Mathf.Max(minimumGlowPadding, panelMaterial.GetFloat(LegacyGlowWidthId) / GetMinimumLossyScale());

        return minimumGlowPadding;
    }

    private float GetResolvedOuterGlowRange(Rect rect)
    {
        float fallbackRange = panelMaterial.GetFloat(OuterGlowRangeId);
        if (!panelMaterial.HasProperty(OuterGlowRangeRatioId))
            return fallbackRange;

        float ratio = panelMaterial.GetFloat(OuterGlowRangeRatioId);
        if (ratio <= 0.0001f)
            return fallbackRange;

        Vector3 lossyScale = transform.lossyScale;
        float referenceSize = Mathf.Min(
            Mathf.Max(rect.width * Mathf.Abs(lossyScale.x), 0.001f),
            Mathf.Max(rect.height * Mathf.Abs(lossyScale.y), 0.001f));
        float minRange = panelMaterial.HasProperty(OuterGlowRangeMinId)
            ? panelMaterial.GetFloat(OuterGlowRangeMinId)
            : 0f;
        float maxRange = panelMaterial.HasProperty(OuterGlowRangeMaxId)
            ? panelMaterial.GetFloat(OuterGlowRangeMaxId)
            : fallbackRange;

        maxRange = Mathf.Max(maxRange, minRange);
        return Mathf.Clamp(referenceSize * ratio, minRange, maxRange);
    }

    private float GetMinimumLossyScale()
    {
        Vector3 lossyScale = transform.lossyScale;
        return Mathf.Max(Mathf.Min(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y)), 0.001f);
    }

    private static Vector2 GetPanelUv(Rect rect, float x, float y)
    {
        return new Vector2(
            (x - rect.xMin) / Mathf.Max(rect.width, 0.001f),
            (y - rect.yMin) / Mathf.Max(rect.height, 0.001f));
    }

    private static Vector4 ColorToVector(Color color)
    {
        return new Vector4(color.r, color.g, color.b, color.a);
    }

    private static void AddVertex(VertexHelper vh, Vector2 position, Vector2 uv, Vector4 panelMetrics, Vector4 border)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = Color.white;
        vertex.uv0 = uv;
        vertex.uv1 = panelMetrics;
        vertex.uv2 = border;
        vh.AddVert(vertex);
    }
}
