#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VictoryScreenBuilder
{
    private static readonly Color Blocker = new Color32(0x02, 0x06, 0x0E, 0xFF);
    private static readonly Color PanelBg = new Color32(0x05, 0x1C, 0x2D, 0xD8);
    private static readonly Color LineCyan = new Color32(0x00, 0xBF, 0xE8, 0xEA);
    private static readonly Color AccentCyan = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color MainText = new Color32(0xD5, 0xFB, 0xFF, 0xFF);
    private static readonly Color SecondaryText = new Color32(0x5D, 0x91, 0xA4, 0xFF);
    private static readonly Color Success = new Color32(0x52, 0xFF, 0xB8, 0xFF);
    private static readonly Color BtnGhost = new Color32(0x08, 0x2F, 0x48, 0xF2);
    private static readonly Color BtnPrimary = new Color32(0x00, 0xBF, 0xE8, 0x40);

    [MenuItem("Tools/NewWorld/Build Victory Screen")]
    public static void Build()
    {
        GameObject existing = GameObject.Find("VictoryScreen");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        GameObject rootGo = new GameObject("VictoryScreen",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(VictoryScreenController));
        Undo.RegisterCreatedObjectUndo(rootGo, "Build Victory Screen");

        Canvas canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;

        CanvasScaler scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelRoot = NewUI("Panel", rootGo.transform, out RectTransform panelRect);
        StretchFull(panelRect);

        GameObject blockerGo = NewUI("Blocker", panelRoot.transform, out RectTransform blockerRect);
        StretchFull(blockerRect);
        Image blocker = blockerGo.AddComponent<Image>();
        blocker.color = Blocker;
        blocker.raycastTarget = true;

        GameObject content = NewUI("Content", panelRoot.transform, out RectTransform contentRect);
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(720f, 520f);
        CanvasGroup contentGroup = content.AddComponent<CanvasGroup>();

        TMP_Text title = NewText("Title", content.transform, "VICTORY", 72, Success, FontStyles.Bold);
        Place(title.rectTransform, 0f, 200f, 700f, 90f);
        title.characterSpacing = 12f;

        TMP_Text reason = NewText("Reason", content.transform, "모든 도시를 장악했습니다.", 24, Success, FontStyles.Normal);
        Place(reason.rectTransform, 0f, 120f, 700f, 36f);
        reason.characterSpacing = 4f;

        GameObject dividerGo = NewUI("Divider", content.transform, out RectTransform dividerRect);
        Place(dividerRect, 0f, 80f, 560f, 2f);
        Image divider = dividerGo.AddComponent<Image>();
        divider.color = LineCyan;
        divider.raycastTarget = false;

        TMP_Text survivalValue = MakeStat(content.transform, "Stat_Survival", "생존 기간", "-", -190f, 0f);
        TMP_Text scoreValue = MakeStat(content.transform, "Stat_Score", "최종 점수", "0", 0f, 0f);
        TMP_Text cityValue = MakeStat(content.transform, "Stat_City", "점령 도시", "0 / 0", 190f, 0f);

        TMP_Text flavor = NewText("Flavor", content.transform,
            "모든 도시가 당신의 네트워크 안으로 편입되었습니다.", 18, SecondaryText, FontStyles.Italic);
        Place(flavor.rectTransform, 0f, -120f, 640f, 30f);

        Button loadButton = MakeButton(content.transform, "LoadButton", "불러오기", BtnPrimary, AccentCyan, -120f, -200f);
        Button menuButton = MakeButton(content.transform, "MainMenuButton", "메인메뉴로", BtnGhost, LineCyan, 120f, -200f);

        GameObject overlayGo = NewUI("GlitchOverlay", panelRoot.transform, out RectTransform overlayRect);
        StretchFull(overlayRect);
        Image glitchOverlay = overlayGo.AddComponent<Image>();
        glitchOverlay.color = Color.white;
        glitchOverlay.raycastTarget = false;
        overlayGo.SetActive(false);

        VictoryScreenController controller = rootGo.GetComponent<VictoryScreenController>();
        SerializedObject so = new SerializedObject(controller);
        SetRef(so, "panelRoot", panelRoot);
        SetRef(so, "canvasGroup", contentGroup);
        SetRef(so, "blockerImage", blocker);
        SetRef(so, "contentRect", contentRect);
        SetRef(so, "glitchOverlay", glitchOverlay);
        SetRef(so, "reasonText", reason);
        SetRef(so, "survivalValueText", survivalValue);
        SetRef(so, "scoreValueText", scoreValue);
        SetRef(so, "cityValueText", cityValue);
        SetRef(so, "flavorText", flavor);
        SetRef(so, "loadButton", loadButton);
        SetRef(so, "mainMenuButton", menuButton);
        SetRef(so, "timeController", Object.FindFirstObjectByType<TimeController>());
        SetRef(so, "saveManager", Object.FindFirstObjectByType<SaveManager>());
        SetRef(so, "loadSlotPanel", Object.FindFirstObjectByType<SaveSlotPanel>(FindObjectsInactive.Include));
        so.ApplyModifiedPropertiesWithoutUndo();

        panelRoot.SetActive(false);

        Selection.activeGameObject = rootGo;
        EditorSceneManager.MarkSceneDirty(rootGo.scene);
        Debug.Log("[VictoryScreenBuilder] Victory screen created.");
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

    private static void SetRef(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }
}
#endif
