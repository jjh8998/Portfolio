using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatentResearchNotificationUIController : MonoBehaviour, IEscapeClosable
{
    private const string CompletedTitle = "\uD2B9\uD5C8 \uC5F0\uAD6C \uC644\uB8CC";
    private const string UnknownFactionName = "\uC54C \uC218 \uC5C6\uB294 \uC138\uB825";
    private const string UnknownResearchName = "\uC54C \uC218 \uC5C6\uB294 \uC5F0\uAD6C";

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text factionNameText;
    [SerializeField] private TMP_Text researchNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private ResearchDatabaseSO researchDatabase;

    private readonly Dictionary<FactionManager, Action<string>> researchCompletedHandlers = new Dictionary<FactionManager, Action<string>>();

    public bool IsOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
        ClosePopup();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        BindButtonEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        UnbindButtonEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        UnbindButtonEvents();
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

    private void SubscribeEvents()
    {
        UnsubscribeEvents();

        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null || researchCompletedHandlers.ContainsKey(faction))
                continue;

            Action<string> handler = _researchId => OnResearchCompleted(faction, _researchId);
            researchCompletedHandlers.Add(faction, handler);
            faction.ResearchCompleted -= handler;
            faction.ResearchCompleted += handler;
        }
    }

    private void UnsubscribeEvents()
    {
        foreach (KeyValuePair<FactionManager, Action<string>> pair in researchCompletedHandlers)
        {
            if (pair.Key != null && pair.Value != null)
                pair.Key.ResearchCompleted -= pair.Value;
        }

        researchCompletedHandlers.Clear();
    }

    private void BindButtonEvents()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(ClosePopup);
        closeButton.onClick.AddListener(ClosePopup);
    }

    private void UnbindButtonEvents()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(ClosePopup);
    }

    private void OnResearchCompleted(FactionManager faction, string _researchId)
    {
        ResearchDatabaseSO database = GetResearchDatabase();
        ResearchData research = database != null
            ? database.GetResearchById(_researchId)
            : null;

        if (research == null || !research.isPatentResearch)
            return;

        string factionName = GetFactionName(faction);
        string researchName = !string.IsNullOrWhiteSpace(research.name)
            ? research.name
            : !string.IsNullOrWhiteSpace(_researchId) ? _researchId : UnknownResearchName;

        if (titleText != null)
            titleText.text = CompletedTitle;

        if (factionNameText != null)
            factionNameText.text = factionName;

        if (researchNameText != null)
            researchNameText.text = researchName;

        if (descriptionText != null)
            descriptionText.text = BuildDescription(factionName, researchName, research);

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    private string BuildDescription(string factionName, string researchName, ResearchData research)
    {
        string description = $"{factionName}\uC774 {researchName} \uD2B9\uD5C8\uB97C \uC120\uC810\uD588\uC2B5\uB2C8\uB2E4.";
        string detail = research != null && !string.IsNullOrWhiteSpace(research.abilityDescription)
            ? research.abilityDescription
            : research != null ? research.description : string.Empty;

        return string.IsNullOrWhiteSpace(detail)
            ? description
            : $"{description}\n{detail}";
    }

    private ResearchDatabaseSO GetResearchDatabase()
    {
        if (researchDatabase == null)
        {
            researchDatabase = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");
            if (researchDatabase == null)
            {
                researchDatabase = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
                researchDatabase.LoadCSV();
            }
        }

        return researchDatabase;
    }

    private string GetFactionName(FactionManager faction)
    {
        return faction != null && !string.IsNullOrWhiteSpace(faction.factionName)
            ? faction.factionName
            : UnknownFactionName;
    }
}
