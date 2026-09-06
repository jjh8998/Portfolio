#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임오버 화면 UI 계층 전체를 한 번에 생성하고 GameOverScreenController의
/// 참조를 자동 배선하는 에디터 도구. 메뉴: Tools/NewWorld/Build Game Over Screen.
/// 사이버펑크 팔레트(ManagementUIDesignSystem)를 따른다.
/// </summary>
public static class GameOverScreenBuilder
{
    // ManagementUIDesignSystem 팔레트
    private static readonly Color Blocker = new Color32(0x02, 0x06, 0x0E, 0xFF); // 완전 불투명 차단
    private static readonly Color PanelBg = new Color32(0x05, 0x1C, 0x2D, 0xD8);
    private static readonly Color LineCyan = new Color32(0x00, 0xBF, 0xE8, 0xEA);
    private static readonly Color AccentCyan = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color MainText = new Color32(0xD5, 0xFB, 0xFF, 0xFF);
    private static readonly Color SecondaryText = new Color32(0x5D, 0x91, 0xA4, 0xFF);
    private static readonly Color Danger = new Color32(0xFF, 0x4B, 0x63, 0xFF);
    private static readonly Color BtnGhost = new Color32(0x08, 0x2F, 0x48, 0xF2);
    private static readonly Color BtnPrimary = new Color32(0x00, 0xBF, 0xE8, 0x40);

    [MenuItem("Tools/NewWorld/Build Game Over Screen")]
    public static void Build()
    {
        // 최상위 Canvas
        GameObject rootGo = new GameObject("GameOverScreen",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameOverScreenController));
        Undo.RegisterCreatedObjectUndo(rootGo, "Build Game Over Screen");

        Canvas canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 모든 UI 위에

        CanvasScaler scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // panelRoot: 컨트롤러가 켜고 끄는 컨테이너 + 페이드용 CanvasGroup
        GameObject panelRoot = NewUI("Panel", rootGo.transform, out RectTransform panelRect);
        StretchFull(panelRect);

        // 1) 차단 오버레이 — 완전 불투명 + raycastTarget(클릭 차단)
        GameObject blockerGo = NewUI("Blocker", panelRoot.transform, out RectTransform blockerRect);
        StretchFull(blockerRect);
        Image blocker = blockerGo.AddComponent<Image>();
        blocker.color = Blocker;
        blocker.raycastTarget = true; // 뒤로 클릭 통과 차단

        // 콘텐츠 컨테이너 (중앙)
        GameObject content = NewUI("Content", panelRoot.transform, out RectTransform contentRect);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(720f, 520f);
        // 콘텐츠 전용 CanvasGroup — 등장 글리치 깜빡임은 여기에만 적용(Blocker는 항상 불투명).
        CanvasGroup contentGroup = content.AddComponent<CanvasGroup>();

        // 2) 타이틀
        TMP_Text title = NewText("Title", content.transform, "GAME OVER", 72, MainText, FontStyles.Bold);
        Place(title.rectTransform, 0f, 200f, 700f, 90f);
        title.characterSpacing = 12f;

        // 3) 패배 사유
        TMP_Text reason = NewText("Reason", content.transform, "패배 사유 / 날짜", 24, Danger, FontStyles.Normal);
        Place(reason.rectTransform, 0f, 120f, 700f, 36f);
        reason.characterSpacing = 4f;

        // 구분선
        GameObject dividerGo = NewUI("Divider", content.transform, out RectTransform dividerRect);
        Place(dividerRect, 0f, 80f, 560f, 2f);
        Image divider = dividerGo.AddComponent<Image>();
        divider.color = LineCyan;
        divider.raycastTarget = false;

        // 4) 스탯 3칸
        TMP_Text survivalValue = MakeStat(content.transform, "Stat_Survival", "생존 기간", "-", -190f, 0f);
        TMP_Text scoreValue = MakeStat(content.transform, "Stat_Score", "최종 점수", "0", 0f, 0f);
        TMP_Text cityValue = MakeStat(content.transform, "Stat_City", "보유 도시", "0", 190f, 0f);

        // 5) 플레이버
        TMP_Text flavor = NewText("Flavor", content.transform,
            "시장은 기억하지 않는다. 다음 분기엔 더 나은 선택을.", 18, SecondaryText, FontStyles.Italic);
        Place(flavor.rectTransform, 0f, -120f, 640f, 30f);

        // 6) 버튼 2개
        Button loadButton = MakeButton(content.transform, "LoadButton", "▶ 불러오기", BtnPrimary, AccentCyan, -120f, -200f);
        Button menuButton = MakeButton(content.transform, "MainMenuButton", "메인메뉴로", BtnGhost, LineCyan, 120f, -200f);

        // 전체 화면 글리치 셰이더 오버레이 (콘텐츠 위). 머티리얼은 컨트롤러가 런타임에 생성·적용한다.
        GameObject overlayGo = NewUI("GlitchOverlay", panelRoot.transform, out RectTransform overlayRect);
        StretchFull(overlayRect);
        Image glitchOverlay = overlayGo.AddComponent<Image>();
        glitchOverlay.color = Color.white;
        glitchOverlay.raycastTarget = false; // 클릭은 버튼으로 통과
        overlayGo.SetActive(false);

        // 컨트롤러 참조 자동 배선
        GameOverScreenController controller = rootGo.GetComponent<GameOverScreenController>();
        SerializedObject so = new SerializedObject(controller);
        SetRef(so, "panelRoot", panelRoot);
        SetRef(so, "canvasGroup", contentGroup);
        SetRef(so, "contentRect", contentRect);
        SetRef(so, "blockerImage", blocker);
        SetRef(so, "glitchOverlay", glitchOverlay);
        SetRef(so, "reasonText", reason);
        SetRef(so, "survivalValueText", survivalValue);
        SetRef(so, "scoreValueText", scoreValue);
        SetRef(so, "cityValueText", cityValue);
        SetRef(so, "flavorText", flavor);
        SetRef(so, "loadButton", loadButton);
        SetRef(so, "mainMenuButton", menuButton);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 편의를 위해 기본은 꺼둠 (런타임 Awake에서도 꺼지지만 에디터 가독성용)
        panelRoot.SetActive(false);

        Selection.activeGameObject = rootGo;
        EditorSceneManager.MarkSceneDirty(rootGo.scene);
        Debug.Log("[GameOverScreenBuilder] 게임오버 화면 생성 완료. GameOverScreenController 참조가 배선되었습니다.");
    }

    private static TMP_Text MakeStat(Transform parent, string name, string label, string value, float x, float y)
    {
        GameObject box = NewUI(name, parent, out RectTransform boxRect);
        Place(boxRect, x, y, 170f, 96f);
        Image bg = box.AddComponent<Image>();
        bg.color = PanelBg;
        bg.raycastTarget = false;

        TMP_Text labelText = NewText("Label", box.transform, label, 14, SecondaryText, FontStyles.Normal);
        Place(labelText.rectTransform, 0f, 26f, 160f, 20f);
        labelText.characterSpacing = 2f;

        TMP_Text valueText = NewText("Value", box.transform, value, 30, AccentCyan, FontStyles.Bold);
        Place(valueText.rectTransform, 0f, -12f, 160f, 40f);

        return valueText;
    }

    private static Button MakeButton(Transform parent, string name, string label, Color bgColor, Color border, float x, float y)
    {
        GameObject go = NewUI(name, parent, out RectTransform rect);
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

        TMP_Text text = NewText("Text", go.transform, label, 20, MainText, FontStyles.Bold);
        StretchFull(text.rectTransform);
        text.characterSpacing = 2f;

        return button;
    }

    // ── 헬퍼 ──────────────────────────────────────────────
    private static GameObject NewUI(string name, Transform parent, out RectTransform rect)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        rect = go.GetComponent<RectTransform>();
        return go;
    }

    private static TMP_Text NewText(string name, Transform parent, string content, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
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

    private static void SetRef(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
        else
            Debug.LogWarning($"[GameOverScreenBuilder] 프로퍼티 '{propName}'를 찾지 못했습니다.");
    }
}
#endif
