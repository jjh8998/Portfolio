using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class VictoryScreenController : MonoBehaviour
{
    private static readonly Color RuntimeBlocker = new Color32(0x02, 0x06, 0x0E, 0xFF);
    private static readonly Color RuntimePanelBg = new Color32(0x05, 0x1C, 0x2D, 0xD8);
    private static readonly Color RuntimeLineCyan = new Color32(0x00, 0xBF, 0xE8, 0xEA);
    private static readonly Color RuntimeAccentCyan = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color RuntimeMainText = new Color32(0xD5, 0xFB, 0xFF, 0xFF);
    private static readonly Color RuntimeSecondaryText = new Color32(0x5D, 0x91, 0xA4, 0xFF);
    private static readonly Color RuntimeSuccess = new Color32(0x52, 0xFF, 0xB8, 0xFF);
    private static readonly Color RuntimeBtnGhost = new Color32(0x08, 0x2F, 0x48, 0xF2);
    private static readonly Color RuntimeBtnPrimary = new Color32(0x00, 0xBF, 0xE8, 0x40);

    [Header("Root / Overlay")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image blockerImage;

    [Header("Texts")]
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text survivalValueText;
    [SerializeField] private TMP_Text scoreValueText;
    [SerializeField] private TMP_Text cityValueText;
    [SerializeField] private TMP_Text flavorText;

    [Header("Buttons")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Dependencies")]
    [SerializeField] private TimeController timeController;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private SaveSlotPanel loadSlotPanel;

    [Header("Content / Intro")]
    [SerializeField] private RectTransform contentRect;

    [Header("Options")]
    [SerializeField] private string flavorLine = "모든 도시가 당신의 네트워크 안으로 편입되었습니다.";
    [SerializeField] private float glitchInDuration = 0.55f;
    [SerializeField] private float glitchJitter = 14f;
    [SerializeField] private Image glitchOverlay;

    private Vector2 contentBasePos;
    private float glitchTimer;
    private bool isGlitching;
    private Material glitchMat;
    private static readonly int GlitchStrengthID = Shader.PropertyToID("_GlitchStrength");

    public static VictoryScreenController CreateRuntimeScreen()
    {
        GameObject rootGo = new GameObject("VictoryScreen",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(VictoryScreenController));

        Canvas canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;

        CanvasScaler scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelRoot = NewRuntimeUI("Panel", rootGo.transform, out RectTransform panelRect);
        StretchFull(panelRect);

        GameObject blockerGo = NewRuntimeUI("Blocker", panelRoot.transform, out RectTransform blockerRect);
        StretchFull(blockerRect);
        Image blocker = blockerGo.AddComponent<Image>();
        blocker.color = RuntimeBlocker;
        blocker.raycastTarget = true;

        GameObject content = NewRuntimeUI("Content", panelRoot.transform, out RectTransform contentRect);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(720f, 520f);
        CanvasGroup contentGroup = content.AddComponent<CanvasGroup>();

        TMP_Text title = NewRuntimeText("Title", content.transform, "VICTORY", 72, RuntimeSuccess, FontStyles.Bold);
        Place(title.rectTransform, 0f, 200f, 700f, 90f);
        title.characterSpacing = 12f;

        TMP_Text reason = NewRuntimeText("Reason", content.transform, "모든 도시를 장악했습니다.", 24, RuntimeSuccess, FontStyles.Normal);
        Place(reason.rectTransform, 0f, 120f, 700f, 36f);
        reason.characterSpacing = 4f;

        GameObject dividerGo = NewRuntimeUI("Divider", content.transform, out RectTransform dividerRect);
        Place(dividerRect, 0f, 80f, 560f, 2f);
        Image divider = dividerGo.AddComponent<Image>();
        divider.color = RuntimeLineCyan;
        divider.raycastTarget = false;

        TMP_Text survivalValue = MakeRuntimeStat(content.transform, "Stat_Survival", "생존 기간", "-", -190f, 0f);
        TMP_Text scoreValue = MakeRuntimeStat(content.transform, "Stat_Score", "최종 점수", "0", 0f, 0f);
        TMP_Text cityValue = MakeRuntimeStat(content.transform, "Stat_City", "점령 도시", "0 / 0", 190f, 0f);

        TMP_Text flavor = NewRuntimeText("Flavor", content.transform,
            "모든 도시가 당신의 네트워크 안으로 편입되었습니다.", 18, RuntimeSecondaryText, FontStyles.Italic);
        Place(flavor.rectTransform, 0f, -120f, 640f, 30f);

        Button loadButton = MakeRuntimeButton(content.transform, "LoadButton", "불러오기", RuntimeBtnPrimary, RuntimeAccentCyan, -120f, -200f);
        Button menuButton = MakeRuntimeButton(content.transform, "MainMenuButton", "메인메뉴로", RuntimeBtnGhost, RuntimeLineCyan, 120f, -200f);

        GameObject overlayGo = NewRuntimeUI("GlitchOverlay", panelRoot.transform, out RectTransform overlayRect);
        StretchFull(overlayRect);
        Image glitchOverlay = overlayGo.AddComponent<Image>();
        glitchOverlay.color = Color.white;
        glitchOverlay.raycastTarget = false;
        overlayGo.SetActive(false);

        VictoryScreenController controller = rootGo.GetComponent<VictoryScreenController>();
        controller.panelRoot = panelRoot;
        controller.canvasGroup = contentGroup;
        controller.blockerImage = blocker;
        controller.contentRect = contentRect;
        controller.glitchOverlay = glitchOverlay;
        controller.reasonText = reason;
        controller.survivalValueText = survivalValue;
        controller.scoreValueText = scoreValue;
        controller.cityValueText = cityValue;
        controller.flavorText = flavor;
        controller.loadButton = loadButton;
        controller.mainMenuButton = menuButton;
        controller.timeController = FindFirstObjectByType<TimeController>();
        controller.saveManager = FindFirstObjectByType<SaveManager>();
        controller.loadSlotPanel = FindFirstObjectByType<SaveSlotPanel>(FindObjectsInactive.Include);

        controller.InitializeScreen();
        return controller;
    }

    private void Awake()
    {
        InitializeScreen();
    }

    private void InitializeScreen()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(LoadGame);
            loadButton.onClick.AddListener(LoadGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (glitchOverlay != null)
        {
            Shader glitchShader = Shader.Find("UI/Glitch Overlay");
            if (glitchShader != null)
            {
                glitchMat = new Material(glitchShader);
                glitchOverlay.material = glitchMat;
            }
            else
            {
                Debug.LogWarning("[VictoryScreenController] 'UI/Glitch Overlay' shader not found.");
            }

            glitchOverlay.gameObject.SetActive(false);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (loadButton != null)
            loadButton.onClick.RemoveListener(LoadGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
    }

    public void Show(string reason, int ownedCityCount, int totalCityCount, int finalScore = 0)
    {
        if (panelRoot == null)
        {
            Debug.LogError("[VictoryScreenController] panelRoot is not assigned.");
            return;
        }

        if (reasonText != null)
            reasonText.text = string.IsNullOrWhiteSpace(reason) ? "승리했습니다." : reason;
        if (survivalValueText != null)
            survivalValueText.text = BuildSurvivalText();
        if (scoreValueText != null)
            scoreValueText.text = finalScore.ToString("N0");
        if (cityValueText != null)
            cityValueText.text = $"{ownedCityCount:N0} / {totalCityCount:N0}";
        if (flavorText != null)
            flavorText.text = flavorLine;

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();

        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (timeController != null)
            timeController.SetPaused(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BeginGlitchIn();
    }

    public void LoadGame()
    {
        if (loadSlotPanel != null)
        {
            loadSlotPanel.SetMode(SaveSlotPanel.PanelMode.Load);
            loadSlotPanel.Open();
            return;
        }

        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        if (saveManager == null)
        {
            Debug.LogWarning("[VictoryScreenController] SaveManager not found.");
            return;
        }

        int slot = SaveManager.CurrentSessionSlot >= 0
            ? SaveManager.CurrentSessionSlot
            : JsonIOScript.AutoSaveSlotIndex;
        saveManager.TryLoadCurrentGame(slot);
    }

    public void ReturnToMainMenu()
    {
        if (timeController != null)
            timeController.SetPaused(false);

        SceneManager.LoadScene("MainMenu");
    }

    private string BuildSurvivalText()
    {
        CalendarScript calendar = FindFirstObjectByType<CalendarScript>();
        if (calendar == null || calendar.CurrentDate == null || calendar.startDate == null)
            return "-";

        GameDate start = calendar.startDate;
        GameDate now = calendar.CurrentDate;
        int totalMonths = (now.year - start.year) * 12 + (now.month - start.month);
        if (totalMonths < 0)
            totalMonths = 0;

        int years = totalMonths / 12;
        int months = totalMonths % 12;

        if (years > 0 && months > 0)
            return $"{years}년 {months}개월";
        if (years > 0)
            return $"{years}년";
        return $"{months}개월";
    }

    private void BeginGlitchIn()
    {
        if (contentRect != null)
            contentBasePos = contentRect.anchoredPosition;

        if (glitchInDuration <= 0f)
        {
            SettleGlitch();
            isGlitching = false;
            return;
        }

        if (glitchOverlay != null && glitchMat != null)
            glitchOverlay.gameObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        glitchTimer = 0f;
        isGlitching = true;
    }

    private void Update()
    {
        if (!isGlitching)
            return;

        glitchTimer += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(glitchTimer / glitchInDuration);

        if (contentRect != null && glitchJitter > 0f)
        {
            float jitter = (1f - progress) * glitchJitter;
            contentRect.anchoredPosition = contentBasePos + new Vector2(
                Random.Range(-jitter, jitter),
                Random.Range(-jitter * 0.25f, jitter * 0.25f));
        }

        if (glitchMat != null)
            glitchMat.SetFloat(GlitchStrengthID, 1f - progress);

        if (progress >= 1f)
        {
            SettleGlitch();
            isGlitching = false;
        }
    }

    private void SettleGlitch()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (contentRect != null)
            contentRect.anchoredPosition = contentBasePos;
        if (glitchMat != null)
            glitchMat.SetFloat(GlitchStrengthID, 0f);
        if (glitchOverlay != null)
            glitchOverlay.gameObject.SetActive(false);
    }

    private static TMP_Text MakeRuntimeStat(Transform parent, string name, string label, string value, float x, float y)
    {
        GameObject box = NewRuntimeUI(name, parent, out RectTransform boxRect);
        Place(boxRect, x, y, 170f, 96f);
        Image bg = box.AddComponent<Image>();
        bg.color = RuntimePanelBg;
        bg.raycastTarget = false;

        TMP_Text labelText = NewRuntimeText("Label", box.transform, label, 14, RuntimeSecondaryText, FontStyles.Normal);
        Place(labelText.rectTransform, 0f, 26f, 160f, 20f);
        labelText.characterSpacing = 2f;

        TMP_Text valueText = NewRuntimeText("Value", box.transform, value, 30, RuntimeAccentCyan, FontStyles.Bold);
        Place(valueText.rectTransform, 0f, -12f, 160f, 40f);

        return valueText;
    }

    private static Button MakeRuntimeButton(Transform parent, string name, string label, Color bgColor, Color border, float x, float y)
    {
        GameObject go = NewRuntimeUI(name, parent, out RectTransform rect);
        Place(rect, x, y, 220f, 56f);

        Image bg = go.AddComponent<Image>();
        bg.color = bgColor;
        bg.raycastTarget = true;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(1f, 1f);

        Button button = go.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.9f, 0.95f, 1f);
        cb.fadeDuration = 0.12f;
        button.colors = cb;

        TMP_Text text = NewRuntimeText("Text", go.transform, label, 20, RuntimeMainText, FontStyles.Bold);
        StretchFull(text.rectTransform);
        text.characterSpacing = 2f;

        return button;
    }

    private static GameObject NewRuntimeUI(string name, Transform parent, out RectTransform rect)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        rect = go.GetComponent<RectTransform>();
        return go;
    }

    private static TMP_Text NewRuntimeText(string name, Transform parent, string content, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
