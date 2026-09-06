using TMPro;
using UnityEngine;

public class FactoryProductionPopUpUIController : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private CardPackDatabaseSO cardPackDatabase;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text packNameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private CardPackInventoryUIController cardPackInventoryUIController;

    private const string CompletedTitle = "카드팩 생산 완료";

    public bool IsOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
        ClosePopup();
    }

    private void OnEnable()
    {
        FactoryProductionService.CardPackProduced -= OnCardPackProduced;
        FactoryProductionService.CardPackProduced += OnCardPackProduced;
    }

    private void OnDisable()
    {
        FactoryProductionService.CardPackProduced -= OnCardPackProduced;
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        ClosePopup();
        return true;
    }

    public void OpenCardPackInventoryFromPopup()
    {
        if (cardPackInventoryUIController == null)
            return;

        cardPackInventoryUIController.OpenCardPackInventoryPanel();
        ClosePopup();
    }

    private void OnCardPackProduced(FactionManager _owner, string _packId, int _packCount)
    {
        if (factionManager == null || _owner != factionManager)
            return;

        CardPackDatabaseSO database = GetCardPackDatabase();
        CardPackData packData = database != null ? database.GetCardPackByID(_packId) : null;
        string packName = packData != null && !string.IsNullOrWhiteSpace(packData.name)
            ? packData.name
            : _packId;

        if (titleText != null)
            titleText.text = CompletedTitle;

        if (packNameText != null)
            packNameText.text = packName;

        if (countText != null)
            countText.text = _packCount > 1 ? $"x{_packCount}" : string.Empty;

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    private CardPackDatabaseSO GetCardPackDatabase()
    {
        if (cardPackDatabase == null)
        {
            cardPackDatabase = Resources.Load<CardPackDatabaseSO>("Databases/CardPackDatabase");
            if (cardPackDatabase == null)
            {
                cardPackDatabase = ScriptableObject.CreateInstance<CardPackDatabaseSO>();
                cardPackDatabase.LoadCSV();
            }
        }

        return cardPackDatabase;
    }
}
