using System.Collections;
using UnityEngine;

public class GameEndConditionManager : MonoBehaviour
{
    private const string DefeatReason = "\uBAA8\uB4E0 \uB3C4\uC2DC\uB97C \uC783\uC5C8\uC2B5\uB2C8\uB2E4.";

    [SerializeField] private FactionManager playerFaction;
    [SerializeField] private VictoryScreenController victoryScreenController;
    [SerializeField] private GameOverScreenController gameOverScreenController;
    [SerializeField] private int initialCheckDelayFrames = 3;

    private bool gameEnded;
    private bool playerHasEverOwnedCity;
    private bool ownershipEventBound;
    private CityOwnershipManager boundOwnershipManager;
    private Coroutine checkEndConditionsCoroutine;

    private void OnEnable()
    {
        SaveManager.LoadCompleted -= OnLoadCompleted;
        SaveManager.LoadCompleted += OnLoadCompleted;
        BindCityOwnershipEvent();
    }

    private void Start()
    {
        ResolveReferences();
        BindCityOwnershipEvent();
        ScheduleCheckEndConditionsNextFrame();
    }

    private void OnDisable()
    {
        SaveManager.LoadCompleted -= OnLoadCompleted;
        UnbindCityOwnershipEvent();
        StopCheckEndConditionsCoroutine();
    }

    private void OnLoadCompleted()
    {
        ScheduleCheckEndConditionsNextFrame();
    }

    private void ScheduleCheckEndConditionsNextFrame()
    {
        StopCheckEndConditionsCoroutine();
        checkEndConditionsCoroutine = StartCoroutine(CheckEndConditionsNextFrame());
    }

    private void StopCheckEndConditionsCoroutine()
    {
        if (checkEndConditionsCoroutine == null)
            return;

        StopCoroutine(checkEndConditionsCoroutine);
        checkEndConditionsCoroutine = null;
    }

    private IEnumerator CheckEndConditionsNextFrame()
    {
        int delayFrames = Mathf.Max(1, initialCheckDelayFrames);
        for (int i = 0; i < delayFrames; i++)
            yield return null;

        checkEndConditionsCoroutine = null;
        CheckEndConditions();
    }

    private void BindCityOwnershipEvent()
    {
        if (ownershipEventBound || CityOwnershipManager.instance == null)
            return;

        boundOwnershipManager = CityOwnershipManager.instance;
        boundOwnershipManager.AnyCityOwnerChanged += OnCityOwnerChanged;
        ownershipEventBound = true;
    }

    private void UnbindCityOwnershipEvent()
    {
        if (!ownershipEventBound)
            return;

        if (boundOwnershipManager != null)
            boundOwnershipManager.AnyCityOwnerChanged -= OnCityOwnerChanged;

        boundOwnershipManager = null;
        ownershipEventBound = false;
    }

    private void OnCityOwnerChanged(CityScript _city, FactionManager _oldOwner, FactionManager _newOwner)
    {
        if (SaveManager.IsLoadingGame)
            return;

        CheckEndConditions();
    }

    private void CheckEndConditions()
    {
        if (SaveManager.IsLoadingGame)
            return;

        if (gameEnded)
            return;

        ResolveReferences();

        if (playerFaction == null)
            return;

        CountCities(out int totalCityCount, out int ownedCityCount);
        if (totalCityCount <= 0)
            return;

        if (ownedCityCount > 0)
            playerHasEverOwnedCity = true;

        if (ownedCityCount >= totalCityCount)
        {
            ShowVictory(ownedCityCount, totalCityCount);
            return;
        }

        if (ownedCityCount <= 0)
        {
            if (!CanEvaluateDefeat())
                return;

            ShowDefeat(ownedCityCount);
        }
    }

    private bool CanEvaluateDefeat()
    {
        return playerHasEverOwnedCity;
    }

    private void CountCities(out int totalCityCount, out int ownedCityCount)
    {
        totalCityCount = 0;
        ownedCityCount = 0;

        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (cities == null)
            return;

        for (int i = 0; i < cities.Length; i++)
        {
            CityScript city = cities[i];
            if (city == null || city.cityData == null)
                continue;

            totalCityCount++;
            if (city.cityData.owner == playerFaction)
                ownedCityCount++;
        }
    }

    private void ShowVictory(int ownedCityCount, int totalCityCount)
    {
        gameEnded = true;
        if (victoryScreenController == null)
            victoryScreenController = VictoryScreenController.CreateRuntimeScreen();

        if (victoryScreenController == null)
            return;

        victoryScreenController.Show("紐⑤뱺 ?꾩떆瑜??μ븙?덉뒿?덈떎.", ownedCityCount, totalCityCount);
    }

    private void ShowDefeat(int ownedCityCount)
    {
        gameEnded = true;

        if (gameOverScreenController == null)
            gameOverScreenController = FindFirstObjectByType<GameOverScreenController>(FindObjectsInactive.Include);

        if (gameOverScreenController == null)
        {
            Debug.LogWarning($"{nameof(GameEndConditionManager)}: {nameof(gameOverScreenController)} is not assigned.");
            return;
        }

        gameOverScreenController.Show(DefeatReason, 0, ownedCityCount);
    }

    private void ResolveReferences()
    {
        if (playerFaction == null)
        {
            FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < factions.Length; i++)
            {
                if (factions[i] != null && factions[i].IsPlayerFaction)
                {
                    playerFaction = factions[i];
                    break;
                }
            }
        }

        if (victoryScreenController == null)
            victoryScreenController = FindFirstObjectByType<VictoryScreenController>(FindObjectsInactive.Include);

        if (gameOverScreenController == null)
            gameOverScreenController = FindFirstObjectByType<GameOverScreenController>(FindObjectsInactive.Include);
    }
}
