using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardPackInventoryUIController : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private FactionManager owner;
    [SerializeField] private CardPackDatabaseSO cardPackDatabase;
    [SerializeField] private GameObject cardPackInventoryPanel;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private CardPackInventoryRow rowPrefab;
    [SerializeField] private CardPackDetailPanelUIController detailPanelController;
    [SerializeField] private TMP_Text emptyText;

    private TimeController timeController;
    private bool wasTimePausedByThis;
    private readonly List<string> visiblePackIds = new List<string>();
    private string selectedPackId;

    public bool IsOpen => IsPanelOpen(cardPackInventoryPanel) ||
                          (detailPanelController != null && detailPanelController.IsOpen);

    private void OnEnable()
    {
        BindOwnerEvents();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindOwnerEvents();

        if (wasTimePausedByThis)
            ResumeGameTime();
    }

    private void OnDestroy()
    {
        UnbindOwnerEvents();
    }

    public void OpenCardPackInventoryPanel()
    {
        if (cardPackInventoryPanel == null)
        {
            Debug.LogWarning("[CardPackInventoryUI] Card pack inventory panel is null.");
            return;
        }

        cardPackInventoryPanel.SetActive(true);
        PauseGameTime();
        Refresh();
    }

    public void CloseCardPackInventoryPanel()
    {
        if (detailPanelController != null)
            detailPanelController.CloseAll();

        if (cardPackInventoryPanel == null)
        {
            Debug.LogWarning("[CardPackInventoryUI] Card pack inventory panel is null.");
            return;
        }

        cardPackInventoryPanel.SetActive(false);
        ResumeGameTime();
    }

    private void PauseGameTime()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (timeController != null && !timeController.IsPaused())
        {
            timeController.SetPaused(true);
            wasTimePausedByThis = true;
        }
    }

    private void ResumeGameTime()
    {
        if (!wasTimePausedByThis)
            return;

        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (timeController != null)
            timeController.SetPaused(false);

        wasTimePausedByThis = false;
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        // 획득 카드 목록(ShowCardListPanel)이 열려 있을 때만 그 패널을 먼저 닫고 인벤토리는 유지한다.
        // 카드 등장 연출 중에는 CloseTopmost가 닫지 못하더라도 ESC를 소비하여 통째로 닫히는 것을 막는다.
        // 상세 패널만 열려 있는 경우에는 카드팩 패널 전체를 닫는다.
        if (detailPanelController != null && detailPanelController.IsShowCardListOpen)
        {
            detailPanelController.CloseTopmost();
            return true;
        }

        CloseCardPackInventoryPanel();
        return true;
    }

    public void Refresh()
    {
        ClearRows();
        visiblePackIds.Clear();

        if (contentRoot == null)
        {
            Debug.LogWarning("[CardPackInventoryUI] Content root is null.");
            SetEmptyState(true);
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogWarning("[CardPackInventoryUI] Row prefab is null.");
            SetEmptyState(true);
            return;
        }

        if (owner == null)
        {
            Debug.LogWarning("[CardPackInventoryUI] Owner is null.");
            SetEmptyState(true);
            return;
        }

        FactionCardPackInventoryScript packInventory = owner.GetCardPackInventory();
        if (packInventory == null)
        {
            SetEmptyState(true);
            return;
        }

        CardPackDatabaseSO database = GetCardPackDatabase();
        List<CardStack> allCardPacks = packInventory.GetAllCardPacks();
        int createdCount = 0;

        for (int i = 0; i < allCardPacks.Count; i++)
        {
            CardStack stack = allCardPacks[i];
            if (stack == null || string.IsNullOrWhiteSpace(stack.cardId) || stack.count <= 0)
                continue;

            CardPackData packData = database != null ? database.GetCardPackByID(stack.cardId) : null;
            if (packData == null)
            {
                Debug.LogWarning($"[CardPackInventoryUI] Card pack data not found. packId={stack.cardId}");
                packData = new CardPackData
                {
                    id = stack.cardId,
                    name = stack.cardId
                };
            }

            CardPackInventoryRow row = Instantiate(rowPrefab, contentRoot);
            row.Setup(packData, stack.count, OnOpenPackClicked);
            visiblePackIds.Add(stack.cardId);
            createdCount++;
        }

        SetEmptyState(createdCount <= 0);
    }

    private void BindOwnerEvents()
    {
        if (owner == null)
            return;

        owner.CardPackInventoryChanged -= OnCardPackInventoryChanged;
        owner.CardPackInventoryChanged += OnCardPackInventoryChanged;
    }

    private void UnbindOwnerEvents()
    {
        if (owner == null)
            return;

        owner.CardPackInventoryChanged -= OnCardPackInventoryChanged;
    }

    private void OnCardPackInventoryChanged(string _packId, int _currentCount)
    {
        Refresh();
    }

    private void OnOpenPackClicked(string _packId)
    {
        selectedPackId = _packId;
        OpenDetailPanel(_packId);
    }

    private void OpenDetailPanel(string packId)
    {
        if (owner == null)
        {
            Debug.LogWarning($"[CardPackInventoryUI] Failed to open detail panel. packId={packId}, reason=Owner is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(packId))
        {
            Debug.LogWarning("[CardPackInventoryUI] Failed to open detail panel. packId=, reason=Pack id is empty.");
            return;
        }

        if (detailPanelController == null)
        {
            Debug.LogWarning($"[CardPackInventoryUI] Failed to open detail panel. packId={packId}, reason=Detail panel controller is null.");
            return;
        }

        CardPackDatabaseSO database = GetCardPackDatabase();
        CardPackData packData = database != null ? database.GetCardPackByID(packId) : null;
        if (packData == null)
        {
            Debug.LogWarning($"[CardPackInventoryUI] Card pack data not found. packId={packId}");
            packData = new CardPackData
            {
                id = packId,
                name = packId
            };
        }

        int currentCount = owner.GetCardPackCount(packId);
        int safeDrawCount = Mathf.Max(1, CardPackOpenService.DefaultDrawCount);
        detailPanelController.Open(owner, database, packData, currentCount, safeDrawCount, OnPackOpened);
    }

    private void OnPackOpened(string _packId, int _remainingCount)
    {
        string nextPackId = _remainingCount > 0
            ? _packId
            : FindNextPackIdFromPreviousOrder(_packId);

        Refresh();

        if (detailPanelController == null || !detailPanelController.IsOpen)
            return;

        if (string.IsNullOrWhiteSpace(nextPackId))
        {
            selectedPackId = null;
            detailPanelController.Close();
            return;
        }

        selectedPackId = nextPackId;
        OpenDetailPanel(nextPackId);
    }

    private string FindNextPackIdFromPreviousOrder(string packId)
    {
        if (visiblePackIds.Count == 0)
            return null;

        int currentIndex = visiblePackIds.IndexOf(packId);
        if (currentIndex < 0)
            return visiblePackIds[0];

        for (int i = currentIndex + 1; i < visiblePackIds.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(visiblePackIds[i]))
                return visiblePackIds[i];
        }

        for (int i = currentIndex - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(visiblePackIds[i]))
                return visiblePackIds[i];
        }

        return null;
    }

    private CardPackDatabaseSO GetCardPackDatabase()
    {
        if (cardPackDatabase != null)
            return cardPackDatabase;

        cardPackDatabase = Resources.Load<CardPackDatabaseSO>("Databases/CardPackDatabase");
        if (cardPackDatabase != null)
            return cardPackDatabase;

        Debug.LogWarning("[CardPackInventoryUI] CardPackDatabase asset not found. Created runtime fallback instance.");
        cardPackDatabase = ScriptableObject.CreateInstance<CardPackDatabaseSO>();
        cardPackDatabase.LoadCSV();
        return cardPackDatabase;
    }

    private void ClearRows()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }

    private void SetEmptyState(bool _isEmpty)
    {
        if (emptyText == null)
            return;

        emptyText.text = "보유한 카드팩이 없습니다.";
        emptyText.gameObject.SetActive(_isEmpty);
    }

    private static bool IsPanelOpen(GameObject panelObject)
    {
        return panelObject != null && panelObject.activeInHierarchy;
    }
}
