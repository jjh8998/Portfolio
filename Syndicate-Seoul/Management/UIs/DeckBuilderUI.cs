using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 하스스톤 스타일 덱 빌더 패널 (사이버펑크 테마).
///
/// 필터 체계:
///   - 팩 필터  : base / overclocked / dismantle / network / bionic / gamble / 전체
///   - 티어 필터 : 1~5 / 전체
///   - 타입 필터  : Attack / Skill / Core / 전체
///   - 텍스트 검색
///
/// 덱 규칙:
///   - 최소 장수: Deck.MinDeckSize (10장)
///   - Core 타입: 최대 1장
///   - Attack / Skill 타입: 인벤토리 보유량까지
/// </summary>
public class DeckBuilderUI : MonoBehaviour, IEscapeClosable
{
    // -- 싱글톤 ----------------------------------------------
    public static DeckBuilderUI Instance { get; private set; }

    // -- 패널 --------------------------------------------------
    [Header("패널")]
    [SerializeField] private GameObject panel;

    // -- 컬렉션 (좌측) -----------------------------------------
    [Header("컬렉션 (좌측)")]
    [SerializeField] private Transform collectionContent;
    [SerializeField] private HandCardUI inventoryCardPrefab; // Row 대신 실제 카드 프리팹 사용
    [SerializeField] private TMP_InputField searchInput;

    [Header("타입 필터 버튼")]
    [SerializeField] private Button filterAllButton;
    [SerializeField] private Button filterAttackButton;
    [SerializeField] private Button filterSkillButton;
    [FormerlySerializedAs("filter" + "PowerButton")]
    [SerializeField] private Button filterCoreButton;

    [Header("팩 필터 버튼")]
    [SerializeField] private Button filterPackAllButton;
    [SerializeField] private Button filterPackBaseButton;
    [SerializeField] private Button filterPackOCButton;
    [SerializeField] private Button filterPackDismantleButton;
    [SerializeField] private Button filterPackNetworkButton;
    [SerializeField] private Button filterPackBionicButton;
    [SerializeField] private Button filterPackGambleButton;
    [SerializeField] private Button filterPackIPButton;

    [Header("티어 필터 버튼 (ALL, T1, T2, T3, T4, T5)")]
    [FormerlySerializedAs("filterCostAllButton")]
    [SerializeField] private Button filterTierAllButton;
    [FormerlySerializedAs("filterCost0Button")]
    [SerializeField] private Button filterTier1Button;
    [FormerlySerializedAs("filterCost1Button")]
    [SerializeField] private Button filterTier2Button;
    [FormerlySerializedAs("filterCost2Button")]
    [SerializeField] private Button filterTier3Button;
    [FormerlySerializedAs("filterCost3Button")]
    [SerializeField] private Button filterTier4Button;
    [FormerlySerializedAs("filterCost4Button")]
    [SerializeField] private Button filterTier5Button;

    [Header("카드 레이아웃 설정")]
    [SerializeField] private Vector2 cardCellSize = new Vector2(240, 340);
    [SerializeField] private Vector2 cardSpacing = new Vector2(40, 40);
    [SerializeField] private RectOffset cardPadding;
    [SerializeField] private int cardColumnCount = 4;

    // -- 덱 (우측) ---------------------------------------------
    [Header("덱 (우측)")]
    [SerializeField] private Transform deckContent;
    [SerializeField] private DeckCardRow deckCardRowPrefab;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button exitButton;

    [Header("덱 슬롯 & 이름")]
    [SerializeField] private Button[] slotButtons = new Button[5];
    [SerializeField] private TMP_InputField deckNameInput;

    [Header("확인 팝업")]
    [SerializeField] private GameObject confirmPopup;
    [SerializeField] private Button popupSaveButton;
    [SerializeField] private Button popupCancelButton;

    [Header("페이지 네비게이션")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TMP_Text pageText;

    [Header("전쟁 모드")]
    [SerializeField] private Button warStartButton;

    // -- 색상 상수 ----------------------------------------------
    private static readonly Color CyanActive   = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color CyanInactive = new Color32(0x5D, 0x91, 0xA4, 0xAD);
    private static readonly Color WarRedActive = new Color32(0xFF, 0x4B, 0x63, 0xFF);

    // -- 상태 --------------------------------------------------
    public static bool IsOpen { get; private set; }
    bool IEscapeClosable.IsOpen => IsOpen || IsPanelOpen();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticStateOnLoad()
    {
        Instance = null;
        IsOpen = false;
    }

    private CardInventory inventory;
    private Action<List<string>> onConfirmCallback;
    private FactionManager ownerFaction;
    private TimeController timeController;

    // 전쟁 모드 컨텍스트
    private CityScript  _pendingWarCity;
    private WarManager  _pendingWarManager;
    // 스캐빈저 슬롯 해금 전투 컨텍스트 (>= 0 이면 스캐빈저 모드)
    private int         _pendingScavengerSlotIndex = -1;

    private Dictionary<string, int> deckMap = new Dictionary<string, int>();
    private string currentDeckName = "";
    private int currentSlotIndex = 0;
    private bool isDirty = false;
    private int? pendingSlotIndex = null;
    private bool pendingClose = false;
    private bool isRequiredDefenseDeckMode = false;

    private CardType? activeTypeFilter = null;
    private string   activePackFilter  = null; // null = 전체
    private int      activeTierFilter  = -1;   // -1 = 전체, 1~5 = 해당 티어
    private string   searchKeyword     = "";

    private int currentPage = 0;
    private const int ItemsPerPage = 8; // 한 페이지에 8장 (4x2 예상)
    private List<string> filteredCardIds = new List<string>();
    private List<CardData> sortedCards = new List<CardData>(); // 정렬된 원본 목록 보관

    private Dictionary<string, HandCardUI> collectionRows = new Dictionary<string, HandCardUI>();
    private Dictionary<string, DeckCardRow>      deckRows       = new Dictionary<string, DeckCardRow>();

    // -- 초기화 ------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        // panel/confirmPopup의 초기 비활성 상태는 씬/프리팹에서 설정.
        // 여기서 SetActive(false)를 호출하면 Open()이 panel을 활성화할 때
        // Awake가 처음 실행되는 경우 패널이 다시 꺼지는 버그가 생김.
        IsOpen = false;

        if (cardPadding == null) cardPadding = new RectOffset(50, 50, 50, 50);
        if (cardColumnCount <= 0) cardColumnCount = 4;

        if (inventoryCardPrefab == null)
        {
            inventoryCardPrefab = Resources.Load<HandCardUI>("Prefabs/card");
            if (inventoryCardPrefab == null)
            {
                // Resources.Load<GameObject>로 시도 후 HandCardUI 가져오기
                var go = Resources.Load<GameObject>("Prefabs/card");
                if (go != null) inventoryCardPrefab = go.GetComponent<HandCardUI>();
            }
            
            if (inventoryCardPrefab != null)
                Debug.Log("<color=cyan>[DeckBuilderUI]</color> inventoryCardPrefab was null. Loaded 'Prefabs/card' from Resources.");
            else
                Debug.LogError("<color=red>[DeckBuilderUI]</color> Failed to load 'Prefabs/card' from Resources!");
        }

        // 전쟁 시작 버튼 (기본 숨김 - 부모 WarBar 비활성화)
        if (warStartButton != null)
        {
            warStartButton.onClick.AddListener(OnWarStartClicked);
            SetWarBarActive(false);
        }

        // 타입 필터
        if (saveButton          != null) saveButton.onClick.AddListener(OnSaveClicked);
        if (exitButton          != null) exitButton.onClick.AddListener(OnExitClicked);
        if (searchInput         != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
        if (filterAllButton     != null) filterAllButton.onClick.AddListener(() => SetTypeFilter(null));
        if (filterAttackButton  != null) filterAttackButton.onClick.AddListener(() => SetTypeFilter(CardType.Attack));
        if (filterSkillButton   != null) filterSkillButton.onClick.AddListener(() => SetTypeFilter(CardType.Skill));
        if (filterCoreButton   != null) filterCoreButton.onClick.AddListener(() => SetTypeFilter(CardType.Core));

        // 팩 필터
        if (filterPackAllButton      != null) filterPackAllButton.onClick.AddListener(() => SetPackFilter(null));
        if (filterPackBaseButton     != null) filterPackBaseButton.onClick.AddListener(() => SetPackFilter("base"));
        if (filterPackOCButton       != null) filterPackOCButton.onClick.AddListener(() => SetPackFilter("overclock"));
        if (filterPackDismantleButton!= null) filterPackDismantleButton.onClick.AddListener(() => SetPackFilter("dismantle"));
        if (filterPackNetworkButton  != null) filterPackNetworkButton.onClick.AddListener(() => SetPackFilter("network"));
        if (filterPackBionicButton   != null) filterPackBionicButton.onClick.AddListener(() => SetPackFilter("biohazard"));
        if (filterPackGambleButton   != null) filterPackGambleButton.onClick.AddListener(() => SetPackFilter("russian_roulette"));
        EnsurePackIPButton();
        if (filterPackIPButton       != null) filterPackIPButton.onClick.AddListener(() => SetPackFilter("IP"));

        // 티어 필터
        EnsureTierFilterButtons();
        if (filterTierAllButton != null) filterTierAllButton.onClick.AddListener(() => SetTierFilter(-1));
        if (filterTier1Button   != null) filterTier1Button.onClick.AddListener(() => SetTierFilter(1));
        if (filterTier2Button   != null) filterTier2Button.onClick.AddListener(() => SetTierFilter(2));
        if (filterTier3Button   != null) filterTier3Button.onClick.AddListener(() => SetTierFilter(3));
        if (filterTier4Button   != null) filterTier4Button.onClick.AddListener(() => SetTierFilter(4));
        if (filterTier5Button   != null) filterTier5Button.onClick.AddListener(() => SetTierFilter(5));

        // 슬롯 버튼
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => TrySwitchSlot(index));
        }

        // 덱 이름 입력
        if (deckNameInput != null) deckNameInput.onValueChanged.AddListener(OnDeckNameChanged);

        // 확인 팝업 버튼
        if (popupSaveButton != null) popupSaveButton.onClick.AddListener(OnPopupSaveClicked);
        if (popupCancelButton != null) popupCancelButton.onClick.AddListener(OnPopupCancelClicked);

        // 페이지 네비게이션
        if (prevPageButton != null) prevPageButton.onClick.AddListener(OnPrevPageClicked);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(OnNextPageClicked);

        // 페이지 버튼 arrow SFX
        AddArrowSFX(prevPageButton);
        AddArrowSFX(nextPageButton);

        // 필터/액션 버튼들에 hover SFX 등록
        Button[] sfxButtons =
        {
            filterAllButton, filterAttackButton, filterSkillButton, filterCoreButton,
            filterPackAllButton, filterPackBaseButton, filterPackOCButton,
            filterPackDismantleButton, filterPackNetworkButton, filterPackBionicButton, filterPackGambleButton, filterPackIPButton,
            filterTierAllButton, filterTier1Button, filterTier2Button, filterTier3Button,
            filterTier4Button, filterTier5Button,
            saveButton, exitButton, warStartButton,
            popupSaveButton, popupCancelButton,
        };
        foreach (Button btn in sfxButtons)
            AddHoverSFX(btn);
        foreach (Button btn in slotButtons)
            AddHoverSFX(btn);

        RefreshPackButtonVisuals(null);
        RefreshTierButtonVisuals(-1);
        RefreshTypeButtonVisuals(null);
    }

    private void OnDisable()
    {
        if (ownerFaction != null)
            ownerFaction.CardInventoryChanged -= OnCardInventoryChanged;

        if (Instance == this)
        {
            if (IsOpen)
                ResumeGameTime();

            IsOpen = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -- 오픈 --------------------------------------------------

    public void Open(FactionManager faction) => Open(faction, null);

    public void Open(FactionManager faction, Action<List<string>> onConfirm)
    {
        if (faction == null) return;

        if (isRequiredDefenseDeckMode && IsPanelOpen())
        {
            ClearCurrentSelection();
            return;
        }

        SetRequiredDefenseDeckMode(false);

        if (IsPanelOpen() && ownerFaction == faction && onConfirmCallback == onConfirm)
        {
            ClearCurrentSelection();
            return;
        }

        ownerFaction      = faction;
        onConfirmCallback = onConfirm;
        inventory         = faction.GetCardInventory();

        if (inventory == null)
        {
            Debug.LogError("[DeckBuilderUI] Faction's inventory is null!");
            return;
        }

        // 필터 초기화 (모든 카드가 보이도록)
        activePackFilter = null;
        activeTypeFilter = null;
        activeTierFilter = -1;
        searchKeyword = "";
        if (searchInput != null) searchInput.text = "";
        RefreshPackButtonVisuals(null);
        RefreshTypeButtonVisuals(null);
        RefreshTierButtonVisuals(-1);

        faction.CardInventoryChanged -= OnCardInventoryChanged;
        faction.CardInventoryChanged += OnCardInventoryChanged;

        currentSlotIndex = ownerFaction.LastSelectedSlot;
        LoadSlot(currentSlotIndex);

        if (panel != null) panel.SetActive(true);
        if (confirmPopup != null) confirmPopup.SetActive(false);
        IsOpen = true;
        ClearCurrentSelection();
        ManagementSFXManager.Instance?.PlayDeckBuilderOpen();

        PauseGameTime();

        SetWarBarActive(false);

        BuildCollectionUI();
        BuildDeckUI();
        RefreshSaveButton();
    }

    // -- 전쟁 모드 오픈 ----------------------------------------

    public void OpenForWar(FactionManager faction, CityScript targetCity, WarManager warManager)
    {
        if (isRequiredDefenseDeckMode && IsPanelOpen())
            return;

        _pendingWarCity    = targetCity;
        _pendingWarManager = warManager;
        _pendingScavengerSlotIndex = -1;

        Open(faction);

        SetWarBarActive(true);
        RefreshWarStartButton();
    }

    // -- 스캐빈저 슬롯 해금 전투 오픈 ---------------------------

    public void OpenForScavenger(FactionManager faction, CityScript targetCity, int slotIndex, WarManager warManager)
    {
        if (isRequiredDefenseDeckMode && IsPanelOpen())
            return;

        _pendingWarCity    = targetCity;
        _pendingWarManager = warManager;
        _pendingScavengerSlotIndex = slotIndex;

        Open(faction);

        SetWarBarActive(true);
        RefreshWarStartButton();
    }

    public void OpenForRequiredDefenseDeck(FactionManager faction, Action<List<string>> onConfirm)
    {
        if (faction == null)
            return;

        _pendingWarCity = null;
        _pendingWarManager = null;
        _pendingScavengerSlotIndex = -1;

        Open(faction, onConfirm);

        if (!IsPanelOpen())
            return;

        SetRequiredDefenseDeckMode(true);
        SetWarBarActive(false);
        RefreshSaveButton();
    }

    private void OnWarStartClicked()
    {
        if (_pendingWarCity == null || _pendingWarManager == null) return;

        List<string> ids = BuildDeckIdList();
        if (ids.Count < Deck.MinDeckSize)
        {
            Debug.LogWarning($"[DeckBuilderUI] 덱이 최소 장수({Deck.MinDeckSize}장)를 충족하지 못해 전쟁을 시작할 수 없습니다.");
            return;
        }

        var city    = _pendingWarCity;
        var manager = _pendingWarManager;
        int scavengerSlotIndex = _pendingScavengerSlotIndex;
        _pendingWarCity    = null;
        _pendingWarManager = null;
        _pendingScavengerSlotIndex = -1;

        Close();

        if (scavengerSlotIndex >= 0)
        {
            // 스캐빈저 슬롯 해금 전투
            manager.DeclareScavengerBattle(city, ownerFaction, scavengerSlotIndex, ids);
            return;
        }

        // 전쟁 선포 시 덱 ID 리스트를 함께 전달 (WarManager 내부의 Clear() 이후에 다시 채워짐)
        if (manager.DeveloperModePlayerAsDefender)
            manager.DeclareDeveloperWarAgainstPlayer(city, ownerFaction, ids);
        else
            manager.DeclareWar(city, ownerFaction, ids);
    }

    private void SetWarBarActive(bool active)
    {
        if (warStartButton == null) return;
        // WarBar 컨테이너(부모)를 토글 - 버튼만 토글하면 레이아웃 공간이 남음
        var bar = warStartButton.transform.parent?.gameObject ?? warStartButton.gameObject;
        bar.SetActive(active);
    }

    private void RefreshWarStartButton()
    {
        if (warStartButton == null) return;
        bool ready = GetTotalDeckCount() >= Deck.MinDeckSize;
        warStartButton.interactable = ready;
        var img = warStartButton.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = ready ? WarRedActive : new Color32(0x03, 0x0F, 0x1C, 0xE0);
    }

    private void LoadSlot(int index)
    {
        currentSlotIndex = index;
        deckMap.Clear();
        
        var savedIds = ownerFaction.GetSavedDeck(index);
        foreach (var id in savedIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            deckMap[id] = deckMap.TryGetValue(id, out int n) ? n + 1 : 1;
        }

        currentDeckName = ownerFaction.GetSavedDeckName(index);
        if (deckNameInput != null)
        {
            deckNameInput.onValueChanged.RemoveListener(OnDeckNameChanged);
            deckNameInput.text = currentDeckName;
            deckNameInput.onValueChanged.AddListener(OnDeckNameChanged);
        }

        isDirty = false;
        RefreshSlotButtonVisuals();
        RefreshSaveButton();
    }

    private void RefreshSlotButtonVisuals()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null) continue;
            SetButtonTextColor(slotButtons[i], i == currentSlotIndex ? CyanActive : CyanInactive);
        }
    }

    // -- 컬렉션 UI ---------------------------------------------

    private void BuildCollectionUI(bool preservePage = false)
    {
        foreach (var row in collectionRows.Values)
        {
            if (row != null)
            {
                row.transform.SetParent(null); // GridLayoutGroup에서 즉시 분리
                Destroy(row.gameObject);
            }
        }
        collectionRows.Clear();

        if (inventory == null || inventoryCardPrefab == null || collectionContent == null)
        {
            Debug.LogWarning($"[DeckBuilderUI] Cannot build collection: inv={inventory!=null}, prefab={inventoryCardPrefab!=null}, content={collectionContent!=null}");
            return;
        }

        var existingLayout = collectionContent.GetComponent<LayoutGroup>();
        if (existingLayout != null && !(existingLayout is GridLayoutGroup))
            DestroyImmediate(existingLayout);

        var glg = collectionContent.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = collectionContent.gameObject.AddComponent<GridLayoutGroup>();
        if (glg == null) return;

        glg.cellSize       = cardCellSize;
        glg.spacing        = cardSpacing;
        glg.padding        = cardPadding;
        glg.childAlignment = TextAnchor.MiddleCenter;
        glg.constraint     = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = cardColumnCount;

        // 전체 카드 목록 기준 (미보유도 회색으로 표시)
        var allData = CardDatabase.Instance.GetAll();
        if (allData == null) 
        {
            Debug.LogError("[DeckBuilderUI] CardDatabase.Instance.GetAll() returned null!");
            return;
        }
        var allCards = new List<CardData>(allData);

        allCards.Sort(CompareCardsForCollectionWithOwnership);

        sortedCards.Clear();
        sortedCards.AddRange(allCards);

        foreach (var cardData in allCards)
        {
            if (cardData == null) continue;
            int ownedCount = inventory.GetCount(cardData.id);
            var cardUI = Instantiate(inventoryCardPrefab, collectionContent);
            
            // 그리드 레이아웃 내에서 카드 정렬을 위한 기본 설정만 유지
            var le = cardUI.GetComponent<LayoutElement>();
            if (le == null) le = cardUI.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 0;
            le.flexibleWidth = 0;

            cardUI.Setup(cardData, -1, ownedCount); // 인벤토리 카드이므로 handIndex는 -1, 보유 수량 전달
            cardUI.SetClickCallback(ui => OnInventoryCardAdd(ui.CardData)); // 클릭 시 덱 추가 연결

            // 보유 상태에 따른 시각화 (미보유 시 유령 모드)
            cardUI.SetGhostMode(ownedCount <= 0);

            collectionRows[cardData.id] = cardUI;
        }

        RefreshCollectionFilter(preservePage);
    }

    private void RefreshCollectionFilter(bool preservePage = false)
    {
        filteredCardIds.Clear();

        // 필터가 null이거나 공백이면 '전체'로 간주
        bool isPackFilterEmpty = string.IsNullOrWhiteSpace(activePackFilter);
        bool isTypeFilterEmpty = activeTypeFilter == null;

        // 딕셔너리 대신 정렬된 리스트를 순회하여 순서 유지
        foreach (var cardData in sortedCards)
        {
            if (cardData == null) continue;
            if (!collectionRows.TryGetValue(cardData.id, out var row)) continue;

            bool typeMatch = isTypeFilterEmpty || cardData.type == activeTypeFilter;

            // 팩 필터링 시 대소문자 및 공백 무시
            bool packMatch = isPackFilterEmpty ||
                             (cardData.pack != null && string.Equals(cardData.pack.Trim(), activePackFilter.Trim(), StringComparison.OrdinalIgnoreCase));

            bool tierMatch = activeTierFilter == -1 || cardData.tier == activeTierFilter;

            bool searchMatch = string.IsNullOrWhiteSpace(searchKeyword) ||
                               cardData.cardName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase);

            bool shouldShow = typeMatch && packMatch && tierMatch && searchMatch;

            // 모든 로우는 UpdatePage에서 제어하므로 여기선 상태만 관리
            row.gameObject.SetActive(false);

            if (shouldShow)
            {
                filteredCardIds.Add(cardData.id);
                RefreshCollectionRowAddable(cardData.id, row, cardData);
            }
        }

        if (!preservePage)
            currentPage = 0;
        UpdatePage();

        // 레이아웃 강제 갱신
        Canvas.ForceUpdateCanvases();

        string packLog = isPackFilterEmpty ? "전체" : activePackFilter;
        string typeLog = isTypeFilterEmpty ? "전체" : activeTypeFilter.ToString();
        string tierLog = activeTierFilter == -1 ? "전체" : activeTierFilter.ToString();
        Debug.Log($"[DeckBuilderUI] 필터 갱신: 총 {filteredCardIds.Count}/{sortedCards.Count}개 필터링됨 (팩: {packLog}, 타입: {typeLog}, 티어: {tierLog}, 검색: '{searchKeyword}')");
    }

    private void UpdatePage()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCardIds.Count / ItemsPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        // 현재 페이지에 해당하는 카드만 활성화
        int start = currentPage * ItemsPerPage;
        int end = Mathf.Min(start + ItemsPerPage, filteredCardIds.Count);

        // 모든 행을 끈 후 현재 페이지 범위만 켬
        foreach (var row in collectionRows.Values)
            if (row != null) row.gameObject.SetActive(false);

        for (int i = start; i < end; i++)
        {
            string id = filteredCardIds[i];
            if (collectionRows.TryGetValue(id, out var row))
            {
                row.gameObject.SetActive(true);
            }
        }

        if (pageText != null)
            pageText.text = $"PAGE {currentPage + 1:D2} / {totalPages:D2}";

        if (prevPageButton != null) prevPageButton.interactable = currentPage > 0;
        if (nextPageButton != null) nextPageButton.interactable = currentPage < totalPages - 1;
    }

    private void OnPrevPageClicked()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    private void OnNextPageClicked()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCardIds.Count / ItemsPerPage));
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    private void RefreshCollectionRowAddable(string cardId, HandCardUI cardUI, CardData cardData)
    {
        if (cardUI == null || cardData == null) return;

        int owned = inventory.GetCount(cardId);
        int inDeck = deckMap.TryGetValue(cardId, out int d) ? d : 0;
        int remaining = owned - inDeck;

        bool reachedInventoryLimit = inDeck >= owned;
        bool isCoreCapped         = cardData.type == CardType.Core && inDeck >= 1;

        // 보유하지 않았거나, 보유했더라도 남은 수량이 0이면 유령 모드 (하스스톤 스타일)
        cardUI.SetGhostMode(remaining <= 0);

        // 추가 불가 상태일 때 추가적인 투명도 조절
        var cg = cardUI.GetComponent<CanvasGroup>();
        if (cg != null && owned > 0)
        {
            // 덱 한도 도달 시 약간 더 반투명하게 (유령 모드 위에 덮어씌움)
            cg.alpha = (reachedInventoryLimit || isCoreCapped) ? 0.35f : 1.0f;
        }
    }

    // -- 덱 UI -------------------------------------------------

    private void BuildDeckUI()
    {
        foreach (var row in deckRows.Values)
            if (row != null) Destroy(row.gameObject);
        deckRows.Clear();

        if (deckCardRowPrefab == null || deckContent == null) return;

        foreach (var kv in deckMap)
        {
            if (kv.Value <= 0) continue;
            var cardData = CardDatabase.Instance.GetById(kv.Key);
            if (cardData == null) continue;
            CreateDeckRow(cardData, kv.Value);
        }

        RefreshDeckCount();
    }

    private void CreateDeckRow(CardData cardData, int count)
    {
        if (cardData == null || count <= 0) return;
        var row = Instantiate(deckCardRowPrefab, deckContent);
        row.Setup(cardData, count, OnDeckCardRemove);
        deckRows[cardData.id] = row;
    }

    private void RefreshDeckCount()
    {
        int total = GetTotalDeckCount();
        if (deckCountText != null)
            deckCountText.text = $"{total} / {Deck.MinDeckSize}";

        RefreshWarStartButton();
    }

    // -- 카드 추가 / 제거 --------------------------------------

    private void OnInventoryCardAdd(CardData cardData)
    {
        if (cardData == null) return;

        int owned  = inventory.GetCount(cardData.id);
        int inDeck = deckMap.TryGetValue(cardData.id, out int d) ? d : 0;

        if (inDeck >= owned)
        {
            ManagementSFXManager.Instance?.PlayNegative();
            return;
        }
        if (cardData.type == CardType.Core && inDeck >= 1)
        {
            ManagementSFXManager.Instance?.PlayNegative();
            return;
        }

        int newCount = inDeck + 1;
        deckMap[cardData.id] = newCount;

        if (deckRows.TryGetValue(cardData.id, out var deckRow))
            deckRow.UpdateCount(newCount);
        else
            CreateDeckRow(cardData, newCount);

        // 컬렉션 시각화 실시간 업데이트
        if (collectionRows.TryGetValue(cardData.id, out var collRow))
        {
            int remaining = owned - newCount;
            collRow.Setup(cardData, -1, remaining); // 남은 수량 표시
            RefreshCollectionRowAddable(cardData.id, collRow, cardData);
        }

        ManagementSFXManager.Instance?.PlayAddToDeck();
        isDirty = true;
        RefreshDeckCount();
        RefreshSaveButton();
    }

    private void OnDeckCardRemove(CardData cardData)
    {
        if (cardData == null || !deckMap.TryGetValue(cardData.id, out int inDeck) || inDeck <= 0) return;

        inDeck--;
        int owned = inventory.GetCount(cardData.id);

        if (inDeck <= 0)
        {
            deckMap.Remove(cardData.id);

            if (deckRows.TryGetValue(cardData.id, out var row))
            {
                if (row != null) Destroy(row.gameObject);
                deckRows.Remove(cardData.id);
            }
            else
            {
                foreach (Transform child in deckContent)
                {
                    var r = child.GetComponent<DeckCardRow>();
                    if (r != null && r.name.Contains(cardData.id))
                        Destroy(child.gameObject);
                }
            }
        }
        else
        {
            deckMap[cardData.id] = inDeck;
            if (deckRows.TryGetValue(cardData.id, out var row))
                row.UpdateCount(inDeck);
            else
            {
                BuildDeckUI();
                return;
            }
        }

        // 컬렉션 시각화 실시간 업데이트
        if (collectionRows.TryGetValue(cardData.id, out var collRow))
        {
            int remaining = owned - inDeck;
            collRow.Setup(cardData, -1, remaining); // 남은 수량 표시
            RefreshCollectionRowAddable(cardData.id, collRow, cardData);
        }

        ManagementSFXManager.Instance?.PlayRemoveFromDeck();
        isDirty = true;
        RefreshDeckCount();
        RefreshSaveButton();
    }

    private void TrySwitchSlot(int index)
    {
        if (index == currentSlotIndex) return;

        if (isDirty)
        {
            pendingSlotIndex = index;
            pendingClose = false;
            if (confirmPopup != null) confirmPopup.SetActive(true);
        }
        else
        {
            LoadSlot(index);
            BuildCollectionUI();
            BuildDeckUI();
        }
    }

    private void SaveCurrentSlot()
    {
        if (ownerFaction == null) return;
        
        List<string> ids = BuildDeckIdList();
        ownerFaction.SaveDeck(ids, currentDeckName, currentSlotIndex);
        ownerFaction.LastSelectedSlot = currentSlotIndex;
        
        // 배틀 씬 연동을 위해 데이터 전송
        BattleSceneData.PlayerDeckIds = ids;
        
        isDirty = false;
        Debug.Log($"<color=green>[DeckBuilderUI]</color> Slot {currentSlotIndex + 1} Saved: {currentDeckName} ({ids.Count} cards)");
    }

    private void OnDeckNameChanged(string newName)
    {
        currentDeckName = newName;
        isDirty = true;
        RefreshSaveButton();
    }

    private void RefreshSaveButton()
    {
        if (saveButton != null)
        {
            bool canSave = isRequiredDefenseDeckMode
                ? GetTotalDeckCount() >= Deck.MinDeckSize
                : isDirty;
            saveButton.interactable = canSave;
            SetButtonTextColor(saveButton, canSave ? CyanActive : CyanInactive);
        }
    }

    // -- 확인 팝업 핸들러 ------------------------------------

    private void OnPopupSaveClicked()
    {
        if (isRequiredDefenseDeckMode && GetTotalDeckCount() < Deck.MinDeckSize)
        {
            Debug.LogWarning($"[DeckBuilderUI] 방어 덱이 최소 장수({Deck.MinDeckSize}장)를 충족하지 못해 저장할 수 없습니다.");
            return;
        }

        SaveCurrentSlot();
        if (confirmPopup != null) confirmPopup.SetActive(false);
        ProcessPendingAction();
    }

    private void OnPopupCancelClicked()
    {
        pendingSlotIndex = null;
        pendingClose = false;
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }

    private void ProcessPendingAction()
    {
        if (pendingClose)
        {
            Close();
            pendingClose = false;
        }
        else if (pendingSlotIndex.HasValue)
        {
            LoadSlot(pendingSlotIndex.Value);
            BuildCollectionUI();
            BuildDeckUI();
            pendingSlotIndex = null;
        }
    }

    // -- 필터 설정 ---------------------------------------------

    private void SetTypeFilter(CardType? type)
    {
        activeTypeFilter = type;
        RefreshTypeButtonVisuals(type);
        RefreshCollectionFilter();
    }

    private void SetPackFilter(string pack)
    {
        activePackFilter = pack;
        RefreshPackButtonVisuals(pack);
        RefreshCollectionFilter();
    }

    private void SetTierFilter(int tier)
    {
        activeTierFilter = tier;
        RefreshTierButtonVisuals(tier);
        RefreshCollectionFilter();
    }

    private void OnSearchChanged(string value)
    {
        searchKeyword = value;
        RefreshCollectionFilter();
    }

    // -- 버튼 비주얼 ------------------------------------------

    private void RefreshTypeButtonVisuals(CardType? active)
    {
        SetButtonTextColor(filterAllButton,    active == null              ? CyanActive : CyanInactive);
        SetButtonTextColor(filterAttackButton, active == CardType.Attack   ? CyanActive : CyanInactive);
        SetButtonTextColor(filterSkillButton,  active == CardType.Skill    ? CyanActive : CyanInactive);
        SetButtonTextColor(filterCoreButton,  active == CardType.Core    ? CyanActive : CyanInactive);
    }

    private void RefreshPackButtonVisuals(string active)
    {
        SetButtonTextColor(filterPackAllButton,       active == null           ? CyanActive : CyanInactive);
        SetButtonTextColor(filterPackBaseButton,      active == "base"         ? CyanActive : CyanInactive);
        SetButtonTextColor(filterPackOCButton,        active == "overclock"    ? CyanActive : CyanInactive);
        SetButtonTextColor(filterPackDismantleButton, active == "dismantle"    ? CyanActive : CyanInactive);
        SetButtonTextColor(filterPackNetworkButton,   active == "network"      ? CyanActive : CyanInactive);
        SetButtonTextColor(filterPackBionicButton,    active == "biohazard"    ? CyanActive : CyanInactive);
        SetButtonTextColor(filterPackGambleButton,    active == "russian_roulette" ? CyanActive : CyanInactive);
        SetButtonTextColor(filterPackIPButton,        active == "IP"           ? CyanActive : CyanInactive);
    }

    private void EnsurePackIPButton()
    {
        if (filterPackIPButton != null)
            return;

        Button template = filterPackGambleButton != null
            ? filterPackGambleButton
            : filterPackNetworkButton != null
                ? filterPackNetworkButton
                : filterPackBaseButton;

        if (template == null || template.transform.parent == null)
            return;

        filterPackIPButton = Instantiate(template, template.transform.parent);
        filterPackIPButton.name = "FilterPackIPButton";
        filterPackIPButton.onClick.RemoveAllListeners();

        var label = filterPackIPButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = "IP";

        filterPackIPButton.transform.SetAsLastSibling();
    }

    private void EnsureTierFilterButtons()
    {
        SetButtonLabel(filterTierAllButton, "ALL");
        SetButtonLabel(filterTier1Button, "T1");
        SetButtonLabel(filterTier2Button, "T2");
        SetButtonLabel(filterTier3Button, "T3");
        SetButtonLabel(filterTier4Button, "T4");
        SetButtonLabel(filterTier5Button, "T5");
    }

    private void RefreshTierButtonVisuals(int active)
    {
        SetButtonTextColor(filterTierAllButton, active == -1 ? CyanActive : CyanInactive);
        SetTierButtonVisual(filterTier1Button, 1, active == 1);
        SetTierButtonVisual(filterTier2Button, 2, active == 2);
        SetTierButtonVisual(filterTier3Button, 3, active == 3);
        SetTierButtonVisual(filterTier4Button, 4, active == 4);
        SetTierButtonVisual(filterTier5Button, 5, active == 5);
    }

    private static void SetButtonLabel(Button btn, string label)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;
    }

    private static void SetTierButtonVisual(Button btn, int tier, bool active)
    {
        if (btn == null) return;

        Color tierColor = CardTierColors.GetNameColor(tier);
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.color = tierColor;

        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            float alpha = active ? 0.18f : 0.08f;
            img.color = new Color(tierColor.r, tierColor.g, tierColor.b, alpha);
        }
    }

    private static void SetButtonTextColor(Button btn, Color color)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.color = color;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = new Color(color.r, color.g, color.b, color.a * 0.08f);
    }

    // -- 확정 / 취소 -------------------------------------------

    private void OnSaveClicked()
    {
        if (isRequiredDefenseDeckMode)
        {
            ConfirmRequiredDefenseDeck();
            return;
        }

        SaveCurrentSlot();
        onConfirmCallback?.Invoke(BuildDeckIdList());
        RefreshSaveButton();
    }

    private void OnExitClicked()
    {
        if (isRequiredDefenseDeckMode)
            return;

        if (isDirty)
        {
            pendingClose = true;
            pendingSlotIndex = null;
            if (confirmPopup != null) confirmPopup.SetActive(true);
        }
        else
        {
            Close();
        }
    }

    public bool CloseByEscape()
    {
        if (!IsPanelOpen())
            return false;

        if (isRequiredDefenseDeckMode)
            return true;

        OnExitClicked();
        return true;
    }

    private void Close()
    {
        bool wasOpen = IsOpen;

        if (ownerFaction != null)
            ownerFaction.CardInventoryChanged -= OnCardInventoryChanged;

        SetRequiredDefenseDeckMode(false);

        if (panel != null) panel.SetActive(false);
        IsOpen = false;

        ResumeGameTime();

        if (wasOpen && TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.CloseDeckBuilder);
    }

    private bool IsPanelOpen()
    {
        return panel != null && panel.activeInHierarchy;
    }

    private void ConfirmRequiredDefenseDeck()
    {
        List<string> ids = BuildDeckIdList();
        if (ids.Count < Deck.MinDeckSize)
        {
            Debug.LogWarning($"[DeckBuilderUI] 방어 덱이 최소 장수({Deck.MinDeckSize}장)를 충족하지 못해 확정할 수 없습니다.");
            RefreshSaveButton();
            return;
        }

        SaveCurrentSlot();
        Action<List<string>> callback = onConfirmCallback;
        Close();
        callback?.Invoke(ids);
    }

    private void SetRequiredDefenseDeckMode(bool active)
    {
        isRequiredDefenseDeckMode = active;

        if (exitButton != null)
            exitButton.interactable = !active;

        if (active && confirmPopup != null)
            confirmPopup.SetActive(false);
    }

    private static void ClearCurrentSelection()
    {
        if (EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnCardInventoryChanged(string cardId, int currentCount)
    {
        if (!IsOpen || panel == null || !panel.activeInHierarchy) return;
        if (string.IsNullOrEmpty(cardId)) return;

        var data = CardDatabase.Instance.GetById(cardId);
        if (data == null) return;

        if (collectionRows.TryGetValue(cardId, out var cardUI) && cardUI != null && cardUI.gameObject != null)
        {
            cardUI.Setup(data, -1, currentCount);
            RefreshCollectionRowAddable(cardId, cardUI, data);

            // 처음 획득한 카드(0→1)는 정렬 순서가 바뀌므로 전체 재빌드
            // 이미 보유 중이던 카드는 필터·페이지만 갱신
            if (currentCount == 1)
                BuildCollectionUI(preservePage: true);
            else
                RefreshCollectionFilter(preservePage: true);
        }
        else
        {
            BuildCollectionUI();
        }
    }

    // -- 유틸 --------------------------------------------------

    private void PauseGameTime()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (timeController != null)
            timeController.SetPaused(true);
    }

    private void ResumeGameTime()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (timeController != null)
            timeController.SetPaused(false);
    }

    private int GetTotalDeckCount()
    {
        int total = 0;
        foreach (var kv in deckMap) total += kv.Value;
        return total;
    }

    private List<string> BuildDeckIdList()
    {
        var result = new List<string>();
        foreach (var kv in deckMap)
            for (int i = 0; i < kv.Value; i++)
                result.Add(kv.Key);
        return result;
    }

    private static int GetTypeOrder(CardType type) => type switch
    {
        CardType.Attack => 0,
        CardType.Skill  => 1,
        CardType.Core  => 2,
        _               => 3,
    };

    /// <summary>
    /// 보유 카드를 먼저 배치하되, 보유/미보유 그룹 내에서는 기존 정렬 순서를 유지한다.
    /// </summary>
    private int CompareCardsForCollectionWithOwnership(CardData a, CardData b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        if (inventory != null)
        {
            bool aOwned = inventory.GetCount(a.id) > 0;
            bool bOwned = inventory.GetCount(b.id) > 0;
            if (aOwned != bOwned)
                return aOwned ? -1 : 1;
        }

        return CompareCardsForCollection(a, b);
    }

    private static int CompareCardsForCollection(CardData a, CardData b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        int tierCompare = a.tier.CompareTo(b.tier);
        if (tierCompare != 0) return tierCompare;

        int packCompare = GetPackOrder(a.pack).CompareTo(GetPackOrder(b.pack));
        if (packCompare != 0) return packCompare;

        int normalizedPackCompare = string.Compare(
            NormalizePackId(a.pack),
            NormalizePackId(b.pack),
            StringComparison.OrdinalIgnoreCase);
        if (normalizedPackCompare != 0) return normalizedPackCompare;

        int typeCompare = GetTypeOrder(a.type).CompareTo(GetTypeOrder(b.type));
        if (typeCompare != 0) return typeCompare;

        int costCompare = a.cost.CompareTo(b.cost);
        if (costCompare != 0) return costCompare;

        int nameCompare = string.Compare(a.cardName, b.cardName, StringComparison.OrdinalIgnoreCase);
        if (nameCompare != 0) return nameCompare;

        return string.Compare(a.id, b.id, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetPackOrder(string pack) => NormalizePackId(pack) switch
    {
        "base" => 0,
        "overclock" => 1,
        "dismantle" => 2,
        "network" => 3,
        "biohazard" => 4,
        "russian_roulette" => 5,
        "ip" => 6,
        _ => 100,
    };

    private static string NormalizePackId(string pack)
    {
        return string.IsNullOrWhiteSpace(pack) ? string.Empty : pack.Trim().ToLowerInvariant();
    }

    // -- SFX 헬퍼 ---------------------------------------------

    private static void AddHoverSFX(Button btn)
    {
        if (btn == null) return;
        EventTrigger trigger = btn.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => ManagementSFXManager.Instance?.PlayHover());
        trigger.triggers.Add(entry);
    }

    private static void AddArrowSFX(Button btn)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() => ManagementSFXManager.Instance?.PlayArrow());
    }
}
