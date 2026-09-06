using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CyberHudEffects : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Material overlayMaterial;
    [SerializeField] private bool ensureOverlay = true;
    [SerializeField, Range(0f, 1f)] private float overlayAlpha = 1f;
    [SerializeField, ColorUsage(false, true)] private Color primaryTextColor = new Color32(0xD5, 0xFB, 0xFF, 0xFF);
    [SerializeField, ColorUsage(false, true)] private Color warningTextColor = new Color32(0xFF, 0x4B, 0x63, 0xFF);

    private const string OverlayName = "CRT_Overlay";

    private void Reset()
    {
        targetCanvas = GetComponentInParent<Canvas>();
    }

    private void Awake()
    {
        ResolveCanvas();
        ApplyEffects();
    }

    private void OnEnable()
    {
        ResolveCanvas();
        ApplyEffects();
    }

    public void ApplyEffects()
    {
        if (targetCanvas == null)
            return;

        RemoveLegacyOutlines();
        ApplyTextColors();

        if (ensureOverlay)
            EnsureOverlay();
    }

    private void ResolveCanvas()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();
    }

    private void ApplyTextColors()
    {
        TMP_Text[] texts = targetCanvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            bool isWarning = text.name.IndexOf("Warning", System.StringComparison.OrdinalIgnoreCase) >= 0
                || text.text.IndexOf("WARNING", System.StringComparison.OrdinalIgnoreCase) >= 0;
            Color source = isWarning ? warningTextColor : primaryTextColor;
            Color current = text.color;
            text.color = new Color(source.r, source.g, source.b, current.a);
        }
    }

    private void RemoveLegacyOutlines()
    {
        Outline[] outlines = targetCanvas.GetComponentsInChildren<Outline>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] == null)
                continue;

            if (Application.isPlaying)
                Destroy(outlines[i]);
            else
                DestroyImmediate(outlines[i]);
        }
    }

    private void EnsureOverlay()
    {
        Transform existing = targetCanvas.transform.Find(OverlayName);
        GameObject overlayObject;
        if (existing != null)
        {
            overlayObject = existing.gameObject;
        }
        else
        {
            overlayObject = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(targetCanvas.transform, false);
        }

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        overlayObject.transform.SetAsLastSibling();

        Image image = overlayObject.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.material = overlayMaterial;
        image.color = new Color(1f, 1f, 1f, overlayAlpha);
        image.raycastTarget = false;
    }
}
