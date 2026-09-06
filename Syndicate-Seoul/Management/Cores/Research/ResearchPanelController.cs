using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연구 패널 전체를 관리하며, 현재 연구/대기열 UI와 연구 패널 상태를 갱신하는 스크립트.
/// </summary>
public class ResearchPanelController : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private ResearchDatabaseSO researchDatabase;
    [SerializeField] private GameObject researchPanel;
    [SerializeField] private ResearchLineDrawer researchLineDrawer;
    [SerializeField] private TMP_Text currentResearchNameText;
    [SerializeField] private TMP_Text currentResearchProgressText;
    [SerializeField] private TMP_Text currentResearchProgressPercentText;
    [SerializeField] private Image currentResearchProgressImage;
    [SerializeField] private float currentResearchProgressFillDuration = 0.15f;
    [SerializeField] private TMP_Text selectedResearchNameText;
    [SerializeField] private TMP_Text selectedResearchDescriptionText;
    [SerializeField] private TMP_Text selectedResearchAbilityDescriptionText;
    [SerializeField] private TMP_Text selectedResearchUnlockConditionText;
    [SerializeField] private Button startResearchButton;

    [Header("Research View Panels")]
    [SerializeField] private Button normalResearchButton;
    [SerializeField] private Button patentResearchButton;
    [SerializeField] private GameObject normalResearchPanel;
    [SerializeField] private GameObject patentResearchPanel;

    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color queuedColor = new Color(0.85f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color inProgressColor = new Color(0.45f, 0.8f, 1f, 1f);
    [SerializeField] private Color completedColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.25f, 1f);

    private ResearchButtonScript[] researchButtons = System.Array.Empty<ResearchButtonScript>();
    private string selectedResearchId;
    private Coroutine currentResearchProgressFillCoroutine;

    public bool IsOpen => researchPanel != null && researchPanel.activeSelf;

    // 패널이 활성화될 때 이벤트를 연결하고 UI를 초기화한다.
    private void OnEnable()
    {
        BindStartResearchButton();
        BindResearchViewButtons();
        SubscribeEvents();
        SetResearchView(false, false, false);
        CollectResearchButtons();
        SelectCurrentResearchOnOpen();
        RefreshAll();
    }

    // 패널이 비활성화될 때 등록한 이벤트를 해제한다.
    private void OnDisable()
    {
        StopCurrentResearchProgressFillCoroutine();
        UnbindResearchViewButtons();
        UnbindStartResearchButton();
        UnsubscribeEvents();
    }

    // 연구 패널의 활성화 상태를 토글한다.
    public void ToggleResearchPanel()
    {
        if (researchPanel == null)
            return;

        bool shouldOpen = !researchPanel.activeSelf;
        researchPanel.SetActive(shouldOpen);

        if (shouldOpen)
        {
            SetResearchView(false, false, false);
            SelectCurrentResearchOnOpen();
            RefreshAll();
        }
    }

    // 연구 패널을 연다.
    public void OpenResearchPanel()
    {
        if (researchPanel == null || researchPanel.activeSelf)
            return;

        researchPanel.SetActive(true);
        SetResearchView(false, false, false);
        SelectCurrentResearchOnOpen();
        RefreshAll();
    }

    public void OpenNormalResearchPanel()
    {
        SetResearchView(false, true, true);
    }

    public void OpenPatentResearchPanel()
    {
        SetResearchView(true, true, true);
    }

    // 연구 패널을 닫는다.
    public void CloseResearchPanel()
    {
        if (researchPanel == null || !researchPanel.activeSelf)
            return;

        StopCurrentResearchProgressFillCoroutine();
        researchPanel.SetActive(false);
    }

    public bool CloseByEscape()
    {
        CloseResearchPanel();
        return true;
    }

    // 연구 패널에 표시되는 모든 UI를 한 번에 갱신한다.
    public void RefreshAll()
    {
        CollectResearchButtons();
        RefreshCurrentResearch();
        RefreshButtonStates();
        RefreshStartResearchButtonState();
        if (researchLineDrawer != null)
            researchLineDrawer.RefreshLines(researchButtons);
    }

    // 연구 상태 변경 이벤트를 구독한다.
    private void SubscribeEvents()
    {
        if (factionManager == null)
            return;

        factionManager.ResearchStateChanged += RefreshAll;
        factionManager.ResearchQueueChanged += RefreshAll;
        factionManager.ResearchStarted += OnResearchStarted;
        factionManager.ResearchCompleted += OnResearchCompleted;
    }

    // 구독 중인 연구 상태 변경 이벤트를 해제한다.
    private void UnsubscribeEvents()
    {
        if (factionManager == null)
            return;

        factionManager.ResearchStateChanged -= RefreshAll;
        factionManager.ResearchQueueChanged -= RefreshAll;
        factionManager.ResearchStarted -= OnResearchStarted;
        factionManager.ResearchCompleted -= OnResearchCompleted;
    }

    private void BindStartResearchButton()
    {
        if (startResearchButton == null)
            return;

        startResearchButton.onClick.RemoveListener(OnClickStartResearchButton);
        startResearchButton.onClick.AddListener(OnClickStartResearchButton);
    }

    private void UnbindStartResearchButton()
    {
        if (startResearchButton == null)
            return;

        startResearchButton.onClick.RemoveListener(OnClickStartResearchButton);
    }

    private void BindResearchViewButtons()
    {
        if (normalResearchButton != null)
        {
            normalResearchButton.onClick.RemoveListener(OpenNormalResearchPanel);
            normalResearchButton.onClick.AddListener(OpenNormalResearchPanel);
        }

        if (patentResearchButton != null)
        {
            patentResearchButton.onClick.RemoveListener(OpenPatentResearchPanel);
            patentResearchButton.onClick.AddListener(OpenPatentResearchPanel);
        }
    }

    private void UnbindResearchViewButtons()
    {
        if (normalResearchButton != null)
            normalResearchButton.onClick.RemoveListener(OpenNormalResearchPanel);

        if (patentResearchButton != null)
            patentResearchButton.onClick.RemoveListener(OpenPatentResearchPanel);
    }

    private void SetResearchView(bool _showPatentPanel, bool _clearSelection, bool _refresh)
    {
        if (normalResearchPanel != null)
            normalResearchPanel.SetActive(!_showPatentPanel);

        if (patentResearchPanel != null)
            patentResearchPanel.SetActive(_showPatentPanel);

        if (_clearSelection)
            ClearSelectedResearch();

        if (_refresh)
            RefreshAll();
    }

    // 현재 진행 중인 연구 이름과 진행도를 UI에 반영한다.
    private void RefreshCurrentResearch(bool _animateProgress = false)
    {
        if (factionManager == null)
            return;

        ResearchData displayResearch = null;
        if (!string.IsNullOrWhiteSpace(selectedResearchId))
        {
            ResearchDatabaseSO db = GetResearchDatabase();
            displayResearch = db != null ? db.GetResearchById(selectedResearchId) : null;
        }

        if (displayResearch == null)
            displayResearch = factionManager.GetCurrentResearch();

        if (displayResearch == null)
        {
            if (currentResearchNameText != null)
                currentResearchNameText.text = "None";

            if (currentResearchProgressText != null)
                currentResearchProgressText.text = "0 / 0";

            if (currentResearchProgressPercentText != null)
                currentResearchProgressPercentText.text = "0%";

            if (currentResearchProgressImage != null)
                SetCurrentResearchProgressFillImmediate(0f);

            return;
        }

        if (currentResearchNameText != null)
            currentResearchNameText.text = displayResearch.name;

        int currentProgress = GetResearchProgress(displayResearch);

        if (currentResearchProgressText != null)
            currentResearchProgressText.text = $"{currentProgress:N0} / {displayResearch.costRP:N0}";

        float progressRate = displayResearch.costRP > 0
            ? (float)currentProgress / displayResearch.costRP
            : 0f;

        float targetFillAmount = Mathf.Clamp01(progressRate);

        if (currentResearchProgressPercentText != null)
            currentResearchProgressPercentText.text = $"{Mathf.RoundToInt(targetFillAmount * 100f)}%";

        if (_animateProgress)
            AnimateCurrentResearchProgress(targetFillAmount);
        else
            SetCurrentResearchProgressFillImmediate(targetFillAmount);
    }

    private void SetCurrentResearchProgressFillImmediate(float _fillAmount)
    {
        StopCurrentResearchProgressFillCoroutine();

        if (currentResearchProgressImage != null)
            currentResearchProgressImage.fillAmount = Mathf.Clamp01(_fillAmount);
    }

    private void AnimateCurrentResearchProgress(float _targetFillAmount)
    {
        StopCurrentResearchProgressFillCoroutine();

        if (currentResearchProgressImage == null)
            return;

        float targetFillAmount = Mathf.Clamp01(_targetFillAmount);
        if (currentResearchProgressFillDuration <= 0f)
        {
            currentResearchProgressImage.fillAmount = targetFillAmount;
            return;
        }

        currentResearchProgressImage.fillAmount = 0f;
        currentResearchProgressFillCoroutine = StartCoroutine(AnimateCurrentResearchProgressRoutine(targetFillAmount));
    }

    private IEnumerator AnimateCurrentResearchProgressRoutine(float _targetFillAmount)
    {
        float elapsedTime = 0f;

        while (elapsedTime < currentResearchProgressFillDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / currentResearchProgressFillDuration);

            if (currentResearchProgressImage != null)
                currentResearchProgressImage.fillAmount = Mathf.Lerp(0f, _targetFillAmount, progress);

            yield return null;
        }

        if (currentResearchProgressImage != null)
            currentResearchProgressImage.fillAmount = _targetFillAmount;

        currentResearchProgressFillCoroutine = null;
    }

    private void StopCurrentResearchProgressFillCoroutine()
    {
        if (currentResearchProgressFillCoroutine == null)
            return;

        StopCoroutine(currentResearchProgressFillCoroutine);
        currentResearchProgressFillCoroutine = null;
    }

    // 직접 배치된 버튼을 수집해 각 버튼의 상태를 갱신한다.
    private void RefreshButtonStates()
    {
        if (factionManager == null)
        {
            ClearQueueOrders();
            return;
        }

        ResearchDatabaseSO db = GetResearchDatabase();
        if (db == null || researchButtons == null || researchButtons.Length == 0)
        {
            ClearQueueOrders();
            return;
        }

        System.Collections.Generic.Dictionary<string, int> queueOrderByResearchId = BuildQueueOrderMap();

        for (int i = 0; i < researchButtons.Length; i++)
        {
            ResearchButtonScript buttonView = researchButtons[i];
            if (buttonView == null)
                continue;

            ResearchData research = db.GetResearchById(buttonView.ResearchId);
            if (research == null)
            {
                ResearchVisualState lockedState = ResearchVisualState.Locked;
                buttonView.RefreshState(lockedState, GetButtonColor(lockedState, false), true, GetNameTextColor(lockedState));
                buttonView.SetQueueOrder(0);
                continue;
            }

            buttonView.Setup(research, OnClickResearch);
            bool isSelected = buttonView.ResearchId == selectedResearchId;
            ResearchVisualState state = GetVisualState(research.id);
            bool showLockedImage = ShouldShowLockedImage(research.id, state);
            buttonView.RefreshState(state, GetButtonColor(state, isSelected), showLockedImage, GetNameTextColor(state));
            buttonView.SetQueueOrder(queueOrderByResearchId.TryGetValue(research.id, out int order) ? order : 0);
        }
    }

    private System.Collections.Generic.Dictionary<string, int> BuildQueueOrderMap()
    {
        System.Collections.Generic.Dictionary<string, int> queueOrderByResearchId =
            new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        int nextOrder = 1;

        ResearchData currentResearch = factionManager.GetCurrentResearch();
        if (currentResearch != null && !string.IsNullOrWhiteSpace(currentResearch.id))
        {
            queueOrderByResearchId.Add(currentResearch.id, nextOrder);
            nextOrder++;
        }

        System.Collections.Generic.IReadOnlyList<string> queuedResearchIds = factionManager.GetQueuedResearchIds();
        if (queuedResearchIds == null)
            return queueOrderByResearchId;

        for (int i = 0; i < queuedResearchIds.Count; i++)
        {
            string researchId = queuedResearchIds[i];
            if (string.IsNullOrWhiteSpace(researchId) || queueOrderByResearchId.ContainsKey(researchId))
                continue;

            queueOrderByResearchId.Add(researchId, nextOrder);
            nextOrder++;
        }

        return queueOrderByResearchId;
    }

    private void ClearQueueOrders()
    {
        if (researchButtons == null)
            return;

        for (int i = 0; i < researchButtons.Length; i++)
        {
            if (researchButtons[i] != null)
                researchButtons[i].SetQueueOrder(0);
        }
    }

    // 연구 버튼 클릭 시 해당 연구 정보를 선택한다.
    private void OnClickResearch(ResearchData _researchData)
    {
        if (factionManager == null || _researchData == null)
            return;

        ResearchVisualState state = GetVisualState(_researchData.id);
        bool isSameResearchSelected = selectedResearchId == _researchData.id;
        selectedResearchId = _researchData.id;

        RefreshSelectedResearchInfo(_researchData);
        RefreshCurrentResearch(!isSameResearchSelected);

        if (state == ResearchVisualState.InProgress || state == ResearchVisualState.Queued)
        {
            if (!isSameResearchSelected)
            {
                RefreshButtonStates();
                RefreshStartResearchButtonState();
                return;
            }

            ClearSelectedResearch();
            factionManager.CancelResearch(_researchData.id);
            RefreshAll();
            return;
        }

        if (isSameResearchSelected && (state == ResearchVisualState.Available || state == ResearchVisualState.Locked))
        {
            TryQueueSelectedResearch();
            return;
        }

        RefreshButtonStates();
        RefreshStartResearchButtonState();
    }

    private int GetResearchProgress(ResearchData _researchData)
    {
        if (_researchData == null || factionManager == null)
            return 0;

        FactionResearchState researchState = factionManager.GetResearchState;
        if (researchState == null)
            return 0;

        if (factionManager.IsResearchInProgress(_researchData.id))
            return Mathf.Max(0, researchState.currentResearchProgressRP);

        if (researchState.researchProgressEntries == null)
            return 0;

        for (int i = 0; i < researchState.researchProgressEntries.Count; i++)
        {
            ResearchProgressEntry entry = researchState.researchProgressEntries[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.researchId, _researchData.id, System.StringComparison.OrdinalIgnoreCase))
                return Mathf.Max(0, entry.currentRP);
        }

        return 0;
    }

    private void OnClickStartResearchButton()
    {
        TryQueueSelectedResearch();
    }

    private void TryQueueSelectedResearch()
    {
        if (string.IsNullOrWhiteSpace(selectedResearchId) || factionManager == null)
            return;

        ResearchDatabaseSO db = GetResearchDatabase();
        ResearchData research = db != null ? db.GetResearchById(selectedResearchId) : null;
        if (research == null)
            return;

        ResearchVisualState state = GetVisualState(selectedResearchId);
        if (state != ResearchVisualState.Available && state != ResearchVisualState.Locked)
            return;

        if (factionManager.QueueResearchPath(selectedResearchId))
        {
            selectedResearchId = research.id;
            RefreshSelectedResearchInfo(research);
            RefreshAll();
        }
    }

    private void RefreshSelectedResearchInfo(ResearchData _researchData)
    {
        if (_researchData == null)
            return;

        if (selectedResearchNameText != null)
            selectedResearchNameText.text = _researchData.name;

        if (selectedResearchDescriptionText != null)
            selectedResearchDescriptionText.text = _researchData.description;

        if (selectedResearchAbilityDescriptionText != null)
            selectedResearchAbilityDescriptionText.text = _researchData.abilityDescription;

        if (selectedResearchUnlockConditionText != null)
            selectedResearchUnlockConditionText.text = BuildUnlockConditionText(_researchData);

        RefreshStartResearchButtonState();
    }

    private void ClearSelectedResearch()
    {
        selectedResearchId = string.Empty;

        if (selectedResearchNameText != null)
            selectedResearchNameText.text = string.Empty;

        if (selectedResearchDescriptionText != null)
            selectedResearchDescriptionText.text = string.Empty;

        if (selectedResearchAbilityDescriptionText != null)
            selectedResearchAbilityDescriptionText.text = string.Empty;

        if (selectedResearchUnlockConditionText != null)
            selectedResearchUnlockConditionText.text = string.Empty;

        RefreshStartResearchButtonState();
    }

    private void SelectCurrentResearchOnOpen()
    {
        ResearchData currentResearch = factionManager != null
            ? factionManager.GetCurrentResearch()
            : null;

        if (currentResearch == null)
        {
            selectedResearchId = string.Empty;

            if (selectedResearchNameText != null)
                selectedResearchNameText.text = "현재 연구 없음";

            if (selectedResearchDescriptionText != null)
                selectedResearchDescriptionText.text = string.Empty;

            if (selectedResearchAbilityDescriptionText != null)
                selectedResearchAbilityDescriptionText.text = string.Empty;

            if (selectedResearchUnlockConditionText != null)
                selectedResearchUnlockConditionText.text = string.Empty;

            RefreshStartResearchButtonState();
            return;
        }

        selectedResearchId = currentResearch.id;
        RefreshSelectedResearchInfo(currentResearch);
    }

    private void RefreshStartResearchButtonState()
    {
        if (startResearchButton == null)
            return;

        bool canStart = false;
        if (!string.IsNullOrWhiteSpace(selectedResearchId) && factionManager != null)
        {
            ResearchDatabaseSO db = GetResearchDatabase();
            ResearchData research = db != null ? db.GetResearchById(selectedResearchId) : null;
            if (research != null)
            {
                ResearchVisualState state = GetVisualState(selectedResearchId);
                canStart = state == ResearchVisualState.Available || state == ResearchVisualState.Locked;
            }
        }

        startResearchButton.interactable = canStart;
    }

    // 연구 시작 이벤트가 발생하면 패널 UI를 갱신한다.
    private void OnResearchStarted(string _researchId)
    {
        RefreshAll();
    }

    // 연구 완료 이벤트가 발생하면 패널 UI를 갱신한다.
    private void OnResearchCompleted(string _researchId)
    {
        RefreshAll();
    }

    // 씬에 직접 배치된 연구 버튼들을 수집한다.
    private void CollectResearchButtons()
    {
        Transform root = researchPanel != null ? researchPanel.transform : transform;
        researchButtons = root.GetComponentsInChildren<ResearchButtonScript>(true);
    }

    private ResearchVisualState GetVisualState(string _researchId)
    {
        if (factionManager.IsResearchCompleted(_researchId))
            return ResearchVisualState.Completed;

        if (factionManager.IsResearchInProgress(_researchId))
            return ResearchVisualState.InProgress;

        if (factionManager.IsResearchQueued(_researchId))
            return ResearchVisualState.Queued;

        if (factionManager.CanStartResearchNow(_researchId))
            return ResearchVisualState.Available;

        return ResearchVisualState.Locked;
    }

    private Color GetButtonColor(ResearchVisualState _state, bool _isSelected)
    {
        bool canUseSelectedColor = _state == ResearchVisualState.Available
            || _state == ResearchVisualState.Locked
            || _state == ResearchVisualState.Queued
            || _state == ResearchVisualState.InProgress;

        if (_isSelected && canUseSelectedColor)
            return selectedColor;

        switch (_state)
        {
            case ResearchVisualState.Available:
                return availableColor;

            case ResearchVisualState.Queued:
                return queuedColor;

            case ResearchVisualState.InProgress:
                return inProgressColor;

            case ResearchVisualState.Completed:
                return completedColor;

            default:
                return lockedColor;
        }
    }

    private Color GetNameTextColor(ResearchVisualState _state)
    {
        return _state == ResearchVisualState.Completed
            ? completedColor
            : lockedColor;
    }

    // 연구 데이터베이스를 가져오고 필요 시 fallback 인스턴스를 생성한다.
    private bool ShouldShowLockedImage(string _researchId, ResearchVisualState _state)
    {
        if (factionManager == null || string.IsNullOrWhiteSpace(_researchId))
            return false;

        if (_state == ResearchVisualState.Completed || _state == ResearchVisualState.InProgress)
            return false;

        return !factionManager.CanStartResearchNow(_researchId);
    }

    private string BuildUnlockConditionText(ResearchData _researchData)
    {
        if (_researchData == null)
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool hasCondition = AppendUnlockConditionLines(builder, _researchData);

        if (!hasCondition)
            builder.AppendLine("개방 조건: 없음");

        return builder.ToString().TrimEnd();
    }

    private bool AppendUnlockConditionLines(System.Text.StringBuilder builder, ResearchData _researchData)
    {
        if (builder == null || _researchData == null)
            return false;

        _researchData.EnsureInitialized();
        bool hasCondition = false;

        if (_researchData.prerequisiteResearchIds != null)
        {
            for (int i = 0; i < _researchData.prerequisiteResearchIds.Count; i++)
            {
                string prerequisiteId = _researchData.prerequisiteResearchIds[i];
                if (string.IsNullOrWhiteSpace(prerequisiteId))
                    continue;

                string researchName = ResolveResearchName(prerequisiteId);
                builder.AppendLine($"선행 연구: {researchName}");
                hasCondition = true;
            }
        }

        if (_researchData.unlockCategoryRequirements != null)
        {
            for (int i = 0; i < _researchData.unlockCategoryRequirements.Count; i++)
            {
                ResearchCategoryRequirement requirement = _researchData.unlockCategoryRequirements[i];
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.category) || requirement.count <= 0)
                    continue;

                int completedCount = CountCompletedResearchesByCategory(requirement.category);
                builder.AppendLine($"{requirement.category} 연구 {requirement.count}개 완료 ({completedCount}/{requirement.count})");
                hasCondition = true;
            }
        }

        return hasCondition;
    }

    private string ResolveResearchName(string _researchId)
    {
        ResearchDatabaseSO db = GetResearchDatabase();
        ResearchData research = db != null ? db.GetResearchById(_researchId) : null;
        if (research != null && !string.IsNullOrWhiteSpace(research.name))
            return research.name;

        return _researchId;
    }

    private int CountCompletedResearchesByCategory(string _category)
    {
        if (string.IsNullOrWhiteSpace(_category) || factionManager == null)
            return 0;

        ResearchDatabaseSO db = GetResearchDatabase();
        if (db == null || db.allResearches == null)
            return 0;

        int count = 0;
        for (int i = 0; i < db.allResearches.Count; i++)
        {
            ResearchData research = db.allResearches[i];
            if (research == null || string.IsNullOrWhiteSpace(research.id) || string.IsNullOrWhiteSpace(research.category))
                continue;

            if (!string.Equals(research.category.Trim(), _category.Trim(), System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (factionManager.IsResearchCompleted(research.id))
                count++;
        }

        return count;
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
