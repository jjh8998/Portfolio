using UnityEngine;

public class CitySharePieInteractionController : MonoBehaviour
{
    [SerializeField] private PIChartUIController pieChart;
    [SerializeField] private DiplomacyTradePanelController tradePanel;
    [SerializeField] private DiplomacyManager diplomacyManager;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        UnsubscribePieChart();
        SubscribePieChart();
    }

    private void OnDisable()
    {
        UnsubscribePieChart();
    }

    private void SubscribePieChart()
    {
        if (pieChart != null)
            pieChart.EntryClicked += OnPieEntryClicked;
    }

    private void UnsubscribePieChart()
    {
        if (pieChart != null)
            pieChart.EntryClicked -= OnPieEntryClicked;
    }

    private void OnPieEntryClicked(PIChartUIController.PIChartEntry _entry)
    {
        if (_entry == null)
            return;

        FactionManager targetFaction = _entry.userData as FactionManager;
        if (targetFaction == null)
            return;

        FactionManager playerFaction = ResolvePlayerFaction();
        if (targetFaction == playerFaction || targetFaction.IsPlayerFaction)
            return;

        if (tradePanel == null)
            ResolveTradePanel();

        if (tradePanel == null)
        {
            Debug.LogWarning($"{nameof(CitySharePieInteractionController)}: tradePanel is not assigned.", this);
            return;
        }

        tradePanel.OpenTradeWith(targetFaction);
    }

    private FactionManager ResolvePlayerFaction()
    {
        if (diplomacyManager == null)
            ResolveDiplomacyManager();

        return diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
    }

    private void ResolveReferences()
    {
        ResolvePieChart();
        ResolveTradePanel();
        ResolveDiplomacyManager();
    }

    private void ResolvePieChart()
    {
        if (pieChart != null)
            return;

        pieChart = GetComponentInChildren<PIChartUIController>(true);
        if (pieChart == null)
            pieChart = GetComponentInParent<PIChartUIController>(true);
        if (pieChart == null)
            pieChart = FindAnyObjectByType<PIChartUIController>(FindObjectsInactive.Include);
    }

    private void ResolveTradePanel()
    {
        if (tradePanel != null)
            return;

        tradePanel = FindAnyObjectByType<DiplomacyTradePanelController>(FindObjectsInactive.Include);
    }

    private void ResolveDiplomacyManager()
    {
        if (diplomacyManager != null)
            return;

        diplomacyManager = FindAnyObjectByType<DiplomacyManager>(FindObjectsInactive.Include);
    }
}
