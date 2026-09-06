using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public sealed class ManagementUIDesignSystem : MonoBehaviour
{
    public static readonly Color Background = new Color32(0x03, 0x09, 0x14, 0xE0);
    public static readonly Color Panel = new Color32(0x03, 0x0F, 0x1C, 0xE0);
    public static readonly Color StrongPanel = new Color32(0x05, 0x1C, 0x2D, 0xF0);
    public static readonly Color LineCyan = new Color32(0x00, 0xBF, 0xE8, 0xEA);
    public static readonly Color AccentCyan = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    public static readonly Color MainText = new Color32(0xD5, 0xFB, 0xFF, 0xFF);
    public static readonly Color SecondaryText = new Color32(0x5D, 0x91, 0xA4, 0xFF);
    public static readonly Color MutedText = new Color32(0x5D, 0x91, 0xA4, 0xA8);
    public static readonly Color Danger = new Color32(0xFF, 0x4B, 0x63, 0xFF);
    public static readonly Color Warning = new Color32(0xE8, 0xC3, 0x5A, 0xFF);

    private const string SceneName = "Management";
    private const string RootName = "__ManagementUIDesignSystem";
    private const float CompactTracking = 0f;
    private const float LabelTracking = 0.8f;
    private const float TitleTracking = 1.8f;
    private const float CityPanelWidth = 320f;
    private const float CityPanelTopInset = 56f;
    private const float CityPanelBottomInset = 12f;
    private const float CityPanelPadding = 12f;
    private const float CityInfoHeight = 176f;
    private const float CityTabsTop = 184f;
    private const float CityTabsHeight = 36f;
    private const float CityBuildSectionTop = 230f;
    private const float CityBuildListTop = 258f;
    private const float CityContentBottom = 104f;
    private const float CityActionHeight = 38f;
    private const float CitySlotGap = 8f;
    private const float CitySlotHeight = 128f;

    private static bool isListening;
    private static ManagementUIDesignSystem instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!isListening)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isListening = true;
        }

        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneName)
            EnsureInstance();
    }

    public static void ApplyNow()
    {
        ManagementUIDesignSystem system = EnsureInstance();
        if (system != null)
            system.ApplySceneStyle();
    }

    private static ManagementUIDesignSystem EnsureInstance()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != SceneName)
            return null;

        // 1) 캐시 우선. 파괴된 객체는 Unity의 == 오버로드로 null 취급되어 자동 폴백된다.
        if (instance != null)
            return instance;

        // 2) Find 폴백: Domain Reload 비활성화 등으로 static이 비었지만 객체가 남은 경우 중복 생성 방지.
        ManagementUIDesignSystem existing = FindAnyObjectByType<ManagementUIDesignSystem>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        // 3) 없을 때만 생성.
        GameObject root = new GameObject(RootName);
        root.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        instance = root.AddComponent<ManagementUIDesignSystem>();
        return instance;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        ApplySceneStyle();
    }

    private void ApplySceneStyle()
    {
        // Scene-authored UI layout and component values are the source of truth.
        // Runtime data binders may update text content, but this system no longer
        // repositions City UI objects or rewrites component styling.
    }

    private static void StyleAllText()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
            StyleText(texts[i]);
    }

    public static void StyleText(TMP_Text text)
    {
        // Intentionally no-op. Preserve TMP values authored in the scene.
    }

    public static void StyleResourceValue(TMP_Text text)
    {
        // Intentionally no-op. Preserve TMP values authored in the scene.
    }

    public static void StyleResourceLabel(TMP_Text text, string label)
    {
        if (text == null)
            return;

        text.text = label;
    }

    public static void StyleSlot(BuildingUIController slot, bool locked, bool hasBuilding)
    {
        // Intentionally no-op. BuildingUIController owns data-driven text/icon
        // visibility; authored slot styling remains untouched.
    }

    private static void StyleButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            RectTransform rect = button.transform as RectTransform;
            if (rect != null && rect.sizeDelta.y > 0f && rect.sizeDelta.y < 32f)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32f);

            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(0x08, 0x2F, 0x48, 0xF2);
            colors.highlightedColor = new Color32(0x0B, 0x3D, 0x5C, 0xFF);
            colors.pressedColor = new Color32(0x00, 0xBF, 0xE8, 0xB8);
            colors.selectedColor = new Color32(0x0B, 0x3D, 0x5C, 0xFF);
            colors.disabledColor = new Color32(0x03, 0x0F, 0x1C, 0x80);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }
    }

    private static void LayoutCityPanel()
    {
        CityUIController city = FindAnyObjectByType<CityUIController>(FindObjectsInactive.Include);
        if (city == null || city.cityPanel == null)
            return;

        RectTransform panelRect = city.cityPanel.transform as RectTransform;
        if (panelRect == null)
            return;

        SetRightDock(panelRect, CityPanelWidth, CityPanelTopInset, CityPanelBottomInset);
        LayoutCityInfo(city, panelRect);
        LayoutCityTabs(panelRect);
        LayoutCityContent(city, panelRect);
        LayoutCityActions(panelRect);
        LayoutFloatingCityPanels(city);
    }

    private static void LayoutCityInfo(CityUIController city, RectTransform panelRect)
    {
        Transform info = FindDescendant(panelRect, "CityInfos");
        RectTransform infoRect = info as RectTransform;
        if (infoRect == null)
            return;

        SetTopLeft(infoRect, 0f, 0f, CityPanelWidth, CityInfoHeight);

        RectTransform backingGroup = EnsureUiGroup(info, "CityInfo_Backplates");
        if (backingGroup != null)
        {
            backingGroup.SetAsFirstSibling();
            SetFullStretch(backingGroup);
            EnsureImage(backingGroup, "HeaderWash", 0f, 0f, CityPanelWidth, CityInfoHeight, new Color32(0x03, 0x0F, 0x1C, 0x58));
            EnsureImage(backingGroup, "StatCard_Credit", 12f, 128f, 92f, 42f, new Color32(0x05, 0x1C, 0x2D, 0xD8));
            EnsureImage(backingGroup, "StatCard_Research", 114f, 128f, 92f, 42f, new Color32(0x05, 0x1C, 0x2D, 0xD8));
            EnsureImage(backingGroup, "StatCard_Power", 216f, 128f, 92f, 42f, new Color32(0x05, 0x1C, 0x2D, 0xD8));
        }

        RectTransform labelGroup = EnsureUiGroup(info, "CityInfo_Labels");
        if (labelGroup != null)
        {
            labelGroup.SetAsLastSibling();
            SetFullStretch(labelGroup);
            EnsureStaticLabel(labelGroup, "StatLabel_Credit", "CREDIT", new Vector2(18f, -133f), new Vector2(80f, 14f), TextAlignmentOptions.Left);
            EnsureStaticLabel(labelGroup, "StatLabel_Research", "RESEARCH", new Vector2(120f, -133f), new Vector2(80f, 14f), TextAlignmentOptions.Left);
            EnsureStaticLabel(labelGroup, "StatLabel_Power", "POWER", new Vector2(222f, -133f), new Vector2(80f, 14f), TextAlignmentOptions.Left);
        }

        RectTransform ceoRect = city.ceoIconImage != null ? city.ceoIconImage.rectTransform : null;
        SetTopLeft(ceoRect, 12f, 46f, 54f, 54f);

        RectTransform nameRect = city.nameText != null ? city.nameText.rectTransform : null;
        SetTopLeft(nameRect, 76f, 44f, 196f, 30f);
        ConfigureText(city.nameText, TextAlignmentOptions.Left, false);

        RectTransform ownerRect = city.ownerText != null ? city.ownerText.rectTransform : null;
        SetTopLeft(ownerRect, 76f, 74f, 196f, 22f);
        ConfigureText(city.ownerText, TextAlignmentOptions.Left, false);

        RectTransform incomeRect = city.incomeText != null ? city.incomeText.rectTransform : null;
        SetTopLeft(incomeRect, 18f, 148f, 80f, 18f);
        ConfigureText(city.incomeText, TextAlignmentOptions.Left, false);

        RectTransform rpRect = city.rpText != null ? city.rpText.rectTransform : null;
        SetTopLeft(rpRect, 120f, 148f, 80f, 18f);
        ConfigureText(city.rpText, TextAlignmentOptions.Left, false);

        RectTransform powerRect = city.powerText != null ? city.powerText.rectTransform : null;
        SetTopLeft(powerRect, 222f, 148f, 80f, 18f);
        ConfigureText(city.powerText, TextAlignmentOptions.Left, false);
    }

    private static void LayoutCityTabs(RectTransform panelRect)
    {
        RectTransform buildingTab = FindDescendant(panelRect, "ShowBuildingButton") as RectTransform;
        RectTransform shareTab = FindDescendant(panelRect, "ShowShareButton") as RectTransform;

        SetTopLeft(buildingTab, CityPanelPadding, CityTabsTop, 145f, CityTabsHeight);
        SetTopLeft(shareTab, 163f, CityTabsTop, 145f, CityTabsHeight);
        ConfigureButtonLabel(buildingTab);
        ConfigureButtonLabel(shareTab);
    }

    private static void LayoutCityContent(CityUIController city, RectTransform panelRect)
    {
        RectTransform listRect = city.myBuildingUIParent != null
            ? city.myBuildingUIParent.transform as RectTransform
            : FindDescendant(panelRect, "BuildingButtons") as RectTransform;
        SetStretch(listRect, CityPanelPadding, CityBuildListTop, CityPanelPadding, CityContentBottom);

        if (listRect != null)
        {
            BuildingUIController[] slots = listRect.GetComponentsInChildren<BuildingUIController>(true);
            System.Array.Sort(slots, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            float slotWidth = (CityPanelWidth - CityPanelPadding * 2f - CitySlotGap) * 0.5f;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                int column = i % 2;
                int row = i / 2;
                RectTransform slotRect = slots[i].transform as RectTransform;
                SetTopLeft(slotRect, column * (slotWidth + CitySlotGap), row * (CitySlotHeight + CitySlotGap), slotWidth, CitySlotHeight);
                LayoutBuildingSlot(slots[i], i, slotWidth);
            }
        }

        RectTransform sharePanel = FindDescendant(panelRect, "ShareInfoPanel") as RectTransform;
        SetStretch(sharePanel, CityPanelPadding, CityBuildListTop, CityPanelPadding, CityContentBottom);

        RectTransform corporatePanel = FindDescendant(panelRect, "Corparate_Panel") as RectTransform;
        SetStretch(corporatePanel, CityPanelPadding, CityBuildListTop, CityPanelPadding, CityContentBottom);

    }

    private static void LayoutBuildingSlot(BuildingUIController slot, int index, float slotWidth)
    {
        if (slot == null)
            return;

        TMP_Text header = FindDescendant(slot.transform, "Header_Text")?.GetComponent<TMP_Text>();
        if (header != null)
        {
            header.text = $"SLOT {index + 1:00}";
            SetTopLeft(header.rectTransform, 10f, 8f, slotWidth - 20f, 16f);
            ConfigureText(header, TextAlignmentOptions.Left, false);
        }

        RectTransform topDivider = FindDescendant(slot.transform, "TopDivider") as RectTransform;
        SetTopLeft(topDivider, 10f, 28f, slotWidth - 20f, 1f);
        SetImageColor(topDivider, new Color32(0x00, 0xBF, 0xE8, 0x68));

        RectTransform preview = FindDescendant(slot.transform, "HologramPreview") as RectTransform;
        SetTopLeft(preview, 16f, 36f, slotWidth - 32f, 58f);

        RectTransform icon = FindDescendant(slot.transform, "IconImage") as RectTransform;
        SetTopLeft(icon, 28f, 40f, slotWidth - 56f, 52f);

        RectTransform bottomDivider = FindDescendant(slot.transform, "BottomDivider") as RectTransform;
        SetTopLeft(bottomDivider, 10f, 98f, slotWidth - 20f, 1f);
        SetImageColor(bottomDivider, new Color32(0x00, 0xBF, 0xE8, 0x48));

        RectTransform categoryIcon = FindDescendant(slot.transform, "CategoryIconImage") as RectTransform;
        SetBottomRight(categoryIcon, 10f, 10f, 24f, 24f);

        bool hasDescriptionText = slot.buildingDescText != null && slot.buildingDescText != slot.buildingNameText;

        if (slot.buildingNameText != null)
        {
            SetTopLeft(slot.buildingNameText.rectTransform, 10f, hasDescriptionText ? 84f : 104f, slotWidth - 20f, 20f);
            ConfigureText(slot.buildingNameText, TextAlignmentOptions.Center, false);
        }

        if (hasDescriptionText)
        {
            SetTopLeft(slot.buildingDescText.rectTransform, 10f, 104f, slotWidth - 20f, 20f);
            ConfigureText(slot.buildingDescText, TextAlignmentOptions.Center, false);
        }

        RectTransform toggle = FindDescendant(slot.transform, "DeactivateBuildingButton") as RectTransform;
        SetTopRight(toggle, 8f, 8f, 24f, 24f);
    }

    private static void LayoutCityActions(RectTransform panelRect)
    {
        RectTransform exit = FindDescendant(panelRect, "ExitButton") as RectTransform;
        SetTopRight(exit, 12f, 12f, 34f, 32f);

        RectTransform trade = FindDescendant(panelRect, "GoTradeButton") as RectTransform;
        SetBottomStretch(trade, CityPanelPadding, 56f, CityPanelPadding, 34f);
        ConfigureButtonLabel(trade);

        RectTransform claim = FindDescendant(panelRect, "Claim_Button") as RectTransform;
        SetBottomStretch(claim, CityPanelPadding, 12f, CityPanelPadding, CityActionHeight);
        ConfigureButtonLabel(claim);
    }

    private static void LayoutFloatingCityPanels(CityUIController city)
    {
        if (city == null || city.possibleBuildingPanel == null)
            return;

        RectTransform possibleRect = city.possibleBuildingPanel.transform as RectTransform;
        if (possibleRect == null)
            return;

        possibleRect.anchorMin = new Vector2(1f, 0.5f);
        possibleRect.anchorMax = new Vector2(1f, 0.5f);
        possibleRect.pivot = new Vector2(1f, 0.5f);
        possibleRect.anchoredPosition = new Vector2(-(CityPanelWidth + 12f), 0f);
        possibleRect.sizeDelta = new Vector2(760f, 720f);
    }

    private static void AddPanelSections()
    {
        CityUIController city = FindAnyObjectByType<CityUIController>(FindObjectsInactive.Include);
        if (city == null || city.cityPanel == null)
            return;

        Transform panel = city.cityPanel.transform;
        EnsureSectionLabel(panel, "Section_CityInfo", "// CITY INFO", new Vector2(12f, -12f));
        EnsureSectionLabel(panel, "Section_ResourceOutput", "// OUTPUT", new Vector2(12f, -104f));
        EnsureSectionLabel(panel, "Section_BuildSlots", "// BUILDINGS", new Vector2(12f, -CityBuildSectionTop));
        EnsureDivider(panel, "Divider_CityInfo", new Vector2(12f, -38f));
        EnsureDivider(panel, "Divider_ResourceOutput", new Vector2(12f, -126f));
        EnsureDivider(panel, "Divider_BuildSlots", new Vector2(12f, -(CityBuildSectionTop + 22f)));
    }

    private static void EnsureSectionLabel(Transform parent, string name, string text, Vector2 anchoredPosition)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (existing == null)
        {
            go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            go.transform.SetParent(parent, false);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 18f);

        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = text;
        label.alignment = TextAlignmentOptions.Left;
        StyleText(label);
    }

    private static void EnsureDivider(Transform parent, string name, Vector2 anchoredPosition)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (existing == null)
        {
            go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            go.transform.SetParent(parent, false);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.offsetMin = new Vector2(CityPanelPadding, rect.offsetMin.y);
        rect.offsetMax = new Vector2(-CityPanelPadding, rect.offsetMax.y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 1f);

        Image line = go.GetComponent<Image>();
        line.color = new Color32(0x00, 0xBF, 0xE8, 0x58);
        line.raycastTarget = false;
    }

    private static void StyleSlotContainers()
    {
        BuildingUIController[] slots = FindObjectsByType<BuildingUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < slots.Length; i++)
        {
            BuildingUIController slot = slots[i];
            if (slot == null)
                continue;

            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);
            for (int j = 0; j < texts.Length; j++)
                StyleText(texts[j]);
        }
    }

    private static RectTransform EnsureUiGroup(Transform parent, string name)
    {
        if (parent == null)
            return null;

        Transform existing = parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform));
        if (existing == null)
        {
            go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            go.transform.SetParent(parent, false);
        }

        return go.transform as RectTransform;
    }

    private static Image EnsureImage(RectTransform parent, string name, float x, float y, float width, float height, Color color)
    {
        if (parent == null)
            return null;

        Transform existing = parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (existing == null)
        {
            go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            go.transform.SetParent(parent, false);
        }

        RectTransform rect = go.transform as RectTransform;
        SetTopLeft(rect, x, y, width, height);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text EnsureStaticLabel(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
    {
        if (parent == null)
            return null;

        Transform existing = parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (existing == null)
        {
            go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            go.transform.SetParent(parent, false);
        }

        RectTransform rect = go.transform as RectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = text;
        label.alignment = alignment;
        StyleResourceLabel(label, text);
        return label;
    }

    private static void SetRightDock(RectTransform rect, float width, float top, float bottom)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.offsetMin = new Vector2(-width, bottom);
        rect.offsetMax = new Vector2(0f, -top);
    }

    private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetTopRight(RectTransform rect, float right, float top, float width, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-right, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomRight(RectTransform rect, float right, float bottom, float width, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-right, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomStretch(RectTransform rect, float left, float bottom, float right, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, bottom + height);
    }

    private static void SetStretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetFullStretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ConfigureText(TMP_Text text, TextAlignmentOptions alignment, bool wrapping)
    {
        if (text == null)
            return;

        text.alignment = alignment;
        text.enableWordWrapping = wrapping;
        text.overflowMode = TextOverflowModes.Truncate;
        StyleText(text);
    }

    private static void ConfigureButtonLabel(RectTransform buttonRect)
    {
        if (buttonRect == null)
            return;

        TMP_Text label = buttonRect.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        RectTransform labelRect = label.rectTransform;
        SetStretch(labelRect, 8f, 4f, 8f, 4f);
        ConfigureText(label, TextAlignmentOptions.Center, false);
    }

    private static void SetImageColor(RectTransform rect, Color color)
    {
        if (rect == null)
            return;

        Image image = rect.GetComponent<Image>();
        if (image == null)
            return;

        image.color = color;
        image.raycastTarget = false;
    }

    private static Transform FindDescendant(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name))
            return null;

        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform result = FindDescendant(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    private static bool HasParentComponent<T>(Transform transform) where T : Component
    {
        return transform != null && transform.GetComponentInParent<T>() != null;
    }

    private static bool IsResourceText(string value)
    {
        return IsResourceLabel(value) || value.Contains("/") || value.StartsWith("+");
    }

    private static bool IsResourceLabel(string value)
    {
        string normalized = value.Trim().Trim('\'').ToUpperInvariant();
        return normalized == "CREDIT" || normalized == "CR" || normalized == "RP" || normalized == "POWER" || normalized == "PWR" || normalized == "INCOME";
    }

    private static bool Contains(string value, string token)
    {
        return !string.IsNullOrEmpty(value) && value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
