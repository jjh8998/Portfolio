using TMPro;
using UnityEngine;

public class ResearchCompletePopUpUIController : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private ResearchDatabaseSO researchDatabase;
    [SerializeField] private ResearchPanelController researchPanelController;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text researchNameText;
    [SerializeField] private TMP_Text descriptionText;

    private const string CompletedTitle = "연구 완료";

    public bool IsOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
        ClosePopup();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
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

    public void OpenResearchPanelFromPopup()
    {
        ClosePopup();

        if (researchPanelController != null)
            researchPanelController.OpenResearchPanel();
    }

    private void SubscribeEvents()
    {
        if (factionManager == null)
            return;

        factionManager.ResearchCompleted -= OnResearchCompleted;
        factionManager.ResearchCompleted += OnResearchCompleted;
    }

    private void UnsubscribeEvents()
    {
        if (factionManager == null)
            return;

        factionManager.ResearchCompleted -= OnResearchCompleted;
    }

    private void OnResearchCompleted(string _researchId)
    {
        ResearchDatabaseSO database = GetResearchDatabase();
        ResearchData research = database != null
            ? database.GetResearchById(_researchId)
            : null;

        if (titleText != null)
            titleText.text = CompletedTitle;

        if (researchNameText != null)
            researchNameText.text = research != null ? research.name : _researchId;

        if (descriptionText != null)
            descriptionText.text = research != null
                ? string.IsNullOrWhiteSpace(research.abilityDescription) ? research.description : research.abilityDescription
                : string.Empty;

        if (popupRoot != null)
            popupRoot.SetActive(true);
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
}
