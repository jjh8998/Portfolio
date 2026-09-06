using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EmployeeStatusPanelUIController : MonoBehaviour
{
    [SerializeField] private FactionManager targetFaction;
    [SerializeField] private Transform listRoot;
    [SerializeField] private EmployeeListItemUI itemPrefab;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private TMP_Text emptyText;

    private FactionManager subscribedFaction;

    private void OnEnable()
    {
        EnsureTargetFaction();
        SubscribeTargetFaction();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeTargetFaction();
    }

    private void OnDestroy()
    {
        UnsubscribeTargetFaction();
    }

    public void SetFaction(FactionManager _faction)
    {
        if (targetFaction != _faction)
            UnsubscribeTargetFaction();

        targetFaction = _faction;
        SubscribeTargetFaction();
        Refresh();
    }

    public void Refresh()
    {
        ClearItems();

        EnsureTargetFaction();
        IReadOnlyList<EmployeeData> employees = targetFaction != null ? targetFaction.GetEmployees() : null;

        bool hasEmployees = employees != null && employees.Count > 0;
        SetEmptyState(!hasEmployees);

        if (!hasEmployees)
            return;

        if (listRoot == null || itemPrefab == null)
        {
            Debug.LogWarning("[EmployeeStatusPanelUI] List root or item prefab is missing.");
            return;
        }

        for (int i = 0; i < employees.Count; i++)
        {
            if (employees[i] == null)
                continue;

            EmployeeListItemUI item = Instantiate(itemPrefab, listRoot);
            if (item != null)
                item.SetData(employees[i]);
        }
    }

    private void ClearItems()
    {
        if (listRoot == null)
            return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = listRoot.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private void SetEmptyState(bool isEmpty)
    {
        if (emptyRoot != null)
            emptyRoot.SetActive(isEmpty);

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(isEmpty);
            if (isEmpty)
                emptyText.text = "\uC9C1\uC6D0\uC774 \uC5C6\uC2B5\uB2C8\uB2E4.";
        }
    }

    private void SubscribeTargetFaction()
    {
        if (!isActiveAndEnabled || subscribedFaction == targetFaction)
            return;

        if (targetFaction == null)
            return;

        subscribedFaction = targetFaction;
        subscribedFaction.EmployeeAssignmentChanged += OnEmployeeAssignmentChanged;
    }

    private void UnsubscribeTargetFaction()
    {
        if (subscribedFaction == null)
            return;

        subscribedFaction.EmployeeAssignmentChanged -= OnEmployeeAssignmentChanged;
        subscribedFaction = null;
    }

    private void OnEmployeeAssignmentChanged()
    {
        Refresh();
    }

    private void EnsureTargetFaction()
    {
        if (targetFaction != null && targetFaction.IsPlayerFaction)
            return;

        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null && faction.IsPlayerFaction)
            {
                if (targetFaction != faction)
                {
                    UnsubscribeTargetFaction();
                    targetFaction = faction;
                    SubscribeTargetFaction();
                }

                return;
            }
        }
    }

    private string GetFactionName(FactionManager faction)
    {
        return faction != null && !string.IsNullOrWhiteSpace(faction.factionName)
            ? faction.factionName
            : "Unknown";
    }
}
