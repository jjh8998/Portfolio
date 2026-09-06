using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Management 씬의 게임오버 화면. 전체 화면을 불투명 오버레이로 덮어
/// 뒤 배경을 완전히 가리고 클릭을 차단한다. 패배 조건이 Show()를 호출해 표시한다.
/// 버튼 동작(불러오기 / 메인메뉴로)은 PauseMenuController와 동일한 흐름을 따른다.
/// </summary>
public sealed class GameOverScreenController : MonoBehaviour
{
    [Header("Root / Overlay")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("전체 화면을 덮는 불투명 차단 이미지. 알파 1.0 + raycastTarget=true 권장.")]
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
    [Tooltip("중앙 콘텐츠 컨테이너(타이틀·사유·스탯·버튼). 등장 글리치 때 흔들리는 대상. Blocker는 제외해 배경이 새지 않게 한다.")]
    [SerializeField] private RectTransform contentRect;

    [Header("Options")]
    [SerializeField] private string flavorLine = "시장은 기억하지 않는다. 다음 분기엔 더 나은 선택을.";
    [Tooltip("등장 글리치 연출 길이(초).")]
    [SerializeField] private float glitchInDuration = 0.55f;
    [Tooltip("글리치 때 콘텐츠 좌우 흔들림 최대 폭(px). 셰이더가 글리치를 담당하므로 작게 둔다.")]
    [SerializeField] private float glitchJitter = 14f;

    [Tooltip("전체 화면 글리치 셰이더 오버레이(UI/Glitch Overlay). 런타임에 머티리얼을 생성해 _GlitchStrength를 구동한다.")]
    [SerializeField] private Image glitchOverlay;

    [Header("Debug")]
    [Tooltip("켜면 게임 시작 시 게임오버 연출이 자동으로 뜬다(디버깅용). 빌드 전 반드시 끌 것.")]
    [SerializeField] private bool debugShowOnStart = false;
    [SerializeField] private string debugReason = "디버그 — 게임오버 연출 테스트";

    private Vector2 contentBasePos;
    private float glitchTimer;
    private bool isGlitching;
    private Material glitchMat;
    private static readonly int GlitchStrengthID = Shader.PropertyToID("_GlitchStrength");

    void Awake()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();
        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        if (loadButton != null)
            loadButton.onClick.AddListener(LoadGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

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
                Debug.LogWarning("[GameOverScreenController] 'UI/Glitch Overlay' 셰이더를 찾지 못했습니다. (빌드 시 Always Included Shaders 등록 필요)");
            }
            glitchOverlay.gameObject.SetActive(false);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Start()
    {
        if (debugShowOnStart)
            StartCoroutine(DebugShowNextFrame());
    }

    // 다른 컴포넌트의 Start()(TimeController의 calendar 초기화 등)가 끝나도록 한 프레임 지연 후 표시.
    private System.Collections.IEnumerator DebugShowNextFrame()
    {
        yield return null;
        Show(debugReason, 0, 0);
    }

    /// <summary>
    /// 게임오버 화면을 띄운다. 패배 조건 측에서 사유와 집계값을 넘긴다.
    /// finalScore는 점수 시스템이 생기기 전까지 0(placeholder).
    /// </summary>
    public void Show(string reason, int finalScore = 0, int ownedCityCount = 0)
    {
        if (panelRoot == null)
        {
            Debug.LogError("[GameOverScreenController] panelRoot가 연결되지 않았습니다.");
            return;
        }

        if (reasonText != null)
            reasonText.text = string.IsNullOrEmpty(reason) ? "게임 오버" : reason;

        if (survivalValueText != null)
            survivalValueText.text = BuildSurvivalText();
        if (scoreValueText != null)
            scoreValueText.text = finalScore.ToString("N0");
        if (cityValueText != null)
            cityValueText.text = ownedCityCount.ToString();
        if (flavorText != null)
            flavorText.text = flavorLine;

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();

        if (timeController != null)
        {
            timeController.SetInputLocked(true);
            timeController.SetPaused(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BeginGlitchIn();
    }

    private string BuildSurvivalText()
    {
        CalendarScript calendar = FindFirstObjectByType<CalendarScript>();
        if (calendar == null || calendar.CurrentDate == null)
            return "-";

        GameDate start = calendar.startDate;
        GameDate now = calendar.CurrentDate;
        if (start == null)
            return "-";

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

        // 콘텐츠는 흔들림·깜빡임 없이 처음부터 또렷하게. 글리치는 셰이더 오버레이가 담당.
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        glitchTimer = 0f;
        isGlitching = true;
    }

    void Update()
    {
        if (!isGlitching)
            return;

        glitchTimer += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(glitchTimer / glitchInDuration);

        if (p >= 1f)
        {
            SettleGlitch();
            isGlitching = false;
            return;
        }

        // 진행할수록 안정화: 보일 확률↑, 좌우 흔들림↓
        // 글리치 셰이더 강도만 1→0으로 구동(흔들림·깜빡임 없음).
        if (glitchMat != null)
            glitchMat.SetFloat(GlitchStrengthID, 1f - p);
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

    public void LoadGame()
    {
        ResolveLoadSlotPanel();
        if (loadSlotPanel != null)
        {
            loadSlotPanel.SetMode(SaveSlotPanel.PanelMode.Load);
            loadSlotPanel.SetUserCloseAllowed(false);
            HideGameOverPanelForLoad();
            loadSlotPanel.Open();
            loadSlotPanel.transform.SetAsLastSibling();
            return;
        }

        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        if (saveManager == null)
        {
            Debug.LogWarning("[GameOverScreenController] SaveManager not found.");
            return;
        }

        int slot = SaveManager.CurrentSessionSlot >= 0
            ? SaveManager.CurrentSessionSlot
            : JsonIOScript.AutoSaveSlotIndex;
        saveManager.TryLoadCurrentGame(slot);
    }

    private void HideGameOverPanelForLoad()
    {
        isGlitching = false;
        SettleGlitch();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ResolveLoadSlotPanel()
    {
        if (loadSlotPanel != null && loadSlotPanel.Mode == SaveSlotPanel.PanelMode.Load)
            return;

        SaveSlotPanel[] panels = FindObjectsByType<SaveSlotPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < panels.Length; i++)
        {
            SaveSlotPanel panel = panels[i];
            if (panel != null && panel.Mode == SaveSlotPanel.PanelMode.Load)
            {
                loadSlotPanel = panel;
                return;
            }
        }

        for (int i = 0; i < panels.Length; i++)
        {
            SaveSlotPanel panel = panels[i];
            if (panel != null && panel.name == "LoadSlotPanel")
            {
                loadSlotPanel = panel;
                return;
            }
        }
    }

    public void ReturnToMainMenu()
    {
        if (timeController != null)
        {
            timeController.SetInputLocked(false);
            timeController.SetPaused(false);
        }

        SceneManager.LoadScene("MainMenu");
    }
}
