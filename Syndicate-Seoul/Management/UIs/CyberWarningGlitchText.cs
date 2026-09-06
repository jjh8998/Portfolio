using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CyberWarningGlitchText : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private float interval = 2.4f;
    [SerializeField] private float burstDuration = 0.12f;
    [SerializeField] private float horizontalJitter = 5f;
    [SerializeField] private float characterSpacingJitter = 7f;
    [SerializeField, ColorUsage(false, true)] private Color warningColor = new Color32(0xFF, 0x4B, 0x63, 0xFF);

    private RectTransform rectTransform;
    private Vector2 basePosition;
    private float baseCharacterSpacing;
    private float timer;
    private float burstTimer;

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        rectTransform = transform as RectTransform;
        if (rectTransform != null)
            basePosition = rectTransform.anchoredPosition;

        if (targetText != null)
            baseCharacterSpacing = targetText.characterSpacing;

        timer = Random.Range(0f, interval);
    }

    private void OnDisable()
    {
        Restore();
    }

    private void Update()
    {
        if (targetText == null || rectTransform == null)
            return;

        timer += Time.unscaledDeltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            burstTimer = burstDuration;
        }

        if (burstTimer > 0f)
        {
            burstTimer -= Time.unscaledDeltaTime;
            float strength = Mathf.Clamp01(burstTimer / burstDuration);
            rectTransform.anchoredPosition = basePosition + new Vector2(Random.Range(-horizontalJitter, horizontalJitter) * strength, 0f);
            targetText.characterSpacing = baseCharacterSpacing + Random.Range(-characterSpacingJitter, characterSpacingJitter) * strength;
            targetText.color = Color.Lerp(warningColor, new Color32(0xD5, 0xFB, 0xFF, 0xFF), Random.Range(0f, 0.18f));
        }
        else
        {
            Restore();
        }
    }

    private void Restore()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = basePosition;

        if (targetText != null)
        {
            targetText.characterSpacing = baseCharacterSpacing;
            targetText.color = warningColor;
        }
    }
}
