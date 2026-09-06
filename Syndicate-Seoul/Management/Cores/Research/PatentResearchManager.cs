using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PatentResearchSaveData
{
    public string researchId;
    public string ownerFactionKey;
    public bool isLockedForever;
}

[Serializable]
public class PatentLicenseSaveData
{
    public string researchId;
    public string ownerFactionKey;
    public string licenseeFactionKey;
    public int remainingMonths;
    public bool isRenewable;
}

public class PatentResearchManager : MonoBehaviour
{
    public const int OneYearLicenseMonths = 12;
    public const int ThreeYearLicenseMonths = 36;
    public const int FiveYearLicenseMonths = 60;

    private static PatentResearchManager instance;

    [SerializeField] private List<PatentResearchSaveData> claimedPatents = new List<PatentResearchSaveData>();
    [SerializeField] private List<PatentLicenseSaveData> activeLicenses = new List<PatentLicenseSaveData>();
    [SerializeField] private ResearchDatabaseSO researchDatabase;

    private CalendarScript subscribedCalendar;
    private bool warnedMissingCalendar;

    public static PatentResearchManager Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<PatentResearchManager>();
            if (instance != null)
                return instance;

            GameObject managerObject = new GameObject(nameof(PatentResearchManager));
            instance = managerObject.AddComponent<PatentResearchManager>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureCollections();
    }

    private void OnEnable()
    {
        SubscribeCalendar();
    }

    private void OnDestroy()
    {
        UnsubscribeCalendar();

        if (instance == this)
            instance = null;
    }

    public bool IsPatentClaimed(string _researchId)
    {
        return !string.IsNullOrWhiteSpace(GetPatentOwnerKey(_researchId));
    }

    public bool IsPatentClaimedByOtherFaction(string _researchId, FactionManager _faction)
    {
        string ownerKey = GetPatentOwnerKey(_researchId);
        if (string.IsNullOrWhiteSpace(ownerKey))
            return false;

        string factionKey = GetFactionKey(_faction);
        return !string.Equals(ownerKey, factionKey, StringComparison.OrdinalIgnoreCase);
    }

    public bool TryClaimPatent(ResearchData _research, FactionManager _faction)
    {
        if (_research == null || !_research.isPatentResearch)
            return true;

        string factionKey = GetFactionKey(_faction);
        if (string.IsNullOrWhiteSpace(factionKey))
            return false;

        EnsureCollections();

        string researchId = NormalizeResearchId(_research.id);
        if (string.IsNullOrWhiteSpace(researchId))
            return false;

        PatentResearchSaveData claim = FindClaim(researchId);
        if (claim != null)
        {
            if (string.Equals(claim.ownerFactionKey, factionKey, StringComparison.OrdinalIgnoreCase))
                return true;

            Debug.LogWarning($"[PatentResearchManager] Patent claim failed. research={researchId}, owner={claim.ownerFactionKey}, challenger={factionKey}");
            return false;
        }

        claimedPatents.Add(new PatentResearchSaveData
        {
            researchId = researchId,
            ownerFactionKey = factionKey,
            isLockedForever = false
        });
        Debug.Log($"[PatentResearchManager] Patent claimed. research={researchId}, owner={factionKey}");
        return true;
    }

    public string GetPatentOwnerKey(string _researchId)
    {
        PatentResearchSaveData claim = FindClaim(_researchId);
        return claim != null ? claim.ownerFactionKey ?? string.Empty : string.Empty;
    }

    public bool HasPatentAccess(string _researchId, FactionManager _faction)
    {
        ResearchData research = GetResearchData(_researchId);
        if (research == null || !research.isPatentResearch)
            return true;

        string factionKey = GetFactionKey(_faction);
        if (string.IsNullOrWhiteSpace(factionKey))
            return false;

        string ownerKey = GetPatentOwnerKey(_researchId);
        if (string.Equals(ownerKey, factionKey, StringComparison.OrdinalIgnoreCase))
            return true;

        return HasPatentLicense(_researchId, _faction);
    }

    public bool HasPatentLicense(string _researchId, FactionManager _faction)
    {
        string factionKey = GetFactionKey(_faction);
        if (string.IsNullOrWhiteSpace(factionKey))
            return false;

        PatentLicenseSaveData license = FindLicense(_researchId, factionKey);
        return license != null && license.remainingMonths > 0;
    }

    public bool TryGrantPatentLicense(
        string _researchId,
        FactionManager _ownerFaction,
        FactionManager _licenseeFaction,
        int _durationMonths)
    {
        SubscribeCalendar();

        if (!IsValidLicenseDuration(_durationMonths))
            return false;

        ResearchData research = GetResearchData(_researchId);
        if (research == null || !research.isPatentResearch)
            return false;

        string researchId = NormalizeResearchId(_researchId);
        string ownerKey = GetFactionKey(_ownerFaction);
        string licenseeKey = GetFactionKey(_licenseeFaction);
        if (string.IsNullOrWhiteSpace(ownerKey) || string.IsNullOrWhiteSpace(licenseeKey))
            return false;

        PatentResearchSaveData claim = FindClaim(researchId);
        if (claim == null
            || claim.isLockedForever
            || !string.Equals(claim.ownerFactionKey, ownerKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(ownerKey, licenseeKey, StringComparison.OrdinalIgnoreCase))
            return true;

        PatentLicenseSaveData license = FindLicense(researchId, licenseeKey);
        if (license != null)
        {
            if (license.remainingMonths >= _durationMonths)
                return false;

            license.remainingMonths = Mathf.Max(license.remainingMonths, _durationMonths);
            license.ownerFactionKey = ownerKey;
            license.isRenewable = true;
            Debug.Log($"[PatentResearchManager] Patent license extended. research={researchId}, licensee={licenseeKey}, months={license.remainingMonths}");
            return true;
        }

        activeLicenses.Add(new PatentLicenseSaveData
        {
            researchId = researchId,
            ownerFactionKey = ownerKey,
            licenseeFactionKey = licenseeKey,
            remainingMonths = _durationMonths,
            isRenewable = true
        });

        ApplyLicenseEffect(research, _licenseeFaction);
        Debug.Log($"[PatentResearchManager] Patent license granted. research={researchId}, owner={ownerKey}, licensee={licenseeKey}, months={_durationMonths}");
        return true;
    }

    public bool CanGrantPatentLicense(
        string _researchId,
        FactionManager _ownerFaction,
        FactionManager _licenseeFaction,
        int _durationMonths,
        out string _message)
    {
        _message = string.Empty;

        if (!IsValidLicenseDuration(_durationMonths))
        {
            _message = "Patent license duration is invalid.";
            return false;
        }

        ResearchData research = GetResearchData(_researchId);
        if (research == null || !research.isPatentResearch)
        {
            _message = "Patent research is invalid.";
            return false;
        }

        string researchId = NormalizeResearchId(_researchId);
        string ownerKey = GetFactionKey(_ownerFaction);
        string licenseeKey = GetFactionKey(_licenseeFaction);
        if (string.IsNullOrWhiteSpace(ownerKey) || string.IsNullOrWhiteSpace(licenseeKey))
        {
            _message = "Patent trade faction is invalid.";
            return false;
        }

        PatentResearchSaveData claim = FindClaim(researchId);
        if (claim == null || !string.Equals(claim.ownerFactionKey, ownerKey, StringComparison.OrdinalIgnoreCase))
        {
            _message = "Only the patent owner can sell this license.";
            return false;
        }

        if (claim.isLockedForever)
        {
            _message = "This patent is permanently closed.";
            return false;
        }

        if (string.Equals(ownerKey, licenseeKey, StringComparison.OrdinalIgnoreCase))
        {
            _message = "Patent owner does not need a license.";
            return false;
        }

        int remainingMonths = GetRemainingLicenseMonths(researchId, _licenseeFaction);
        if (remainingMonths >= _durationMonths)
        {
            _message = "The buyer already has an equal or longer patent license.";
            return false;
        }

        return true;
    }

    public bool RemovePatentLicense(string _researchId, FactionManager _licenseeFaction)
    {
        string licenseeKey = GetFactionKey(_licenseeFaction);
        if (string.IsNullOrWhiteSpace(licenseeKey))
            return false;

        return RemovePatentLicenseByKey(_researchId, licenseeKey, true);
    }

    public int GetRemainingLicenseMonths(string _researchId, FactionManager _licenseeFaction)
    {
        string licenseeKey = GetFactionKey(_licenseeFaction);
        if (string.IsNullOrWhiteSpace(licenseeKey))
            return 0;

        PatentLicenseSaveData license = FindLicense(_researchId, licenseeKey);
        return license != null ? Mathf.Max(0, license.remainingMonths) : 0;
    }

    public bool TryGetPatentLicenseSnapshot(string _researchId, FactionManager _licenseeFaction, out PatentLicenseSaveData _snapshot)
    {
        _snapshot = null;

        string licenseeKey = GetFactionKey(_licenseeFaction);
        PatentLicenseSaveData license = FindLicense(_researchId, licenseeKey);
        if (license == null)
            return false;

        _snapshot = CopyLicense(license);
        return true;
    }

    public void RestorePatentLicenseForRollback(
        string _researchId,
        FactionManager _licenseeFaction,
        PatentLicenseSaveData _snapshot)
    {
        string licenseeKey = GetFactionKey(_licenseeFaction);
        if (string.IsNullOrWhiteSpace(licenseeKey))
            return;

        PatentLicenseSaveData currentLicense = FindLicense(_researchId, licenseeKey);
        if (_snapshot == null)
        {
            if (currentLicense != null)
                RemovePatentLicenseByKey(_researchId, licenseeKey, true);
            return;
        }

        if (currentLicense == null)
        {
            activeLicenses.Add(CopyLicense(_snapshot));
            ApplyLicenseEffect(GetResearchData(_snapshot.researchId), _licenseeFaction);
            return;
        }

        currentLicense.researchId = NormalizeResearchId(_snapshot.researchId);
        currentLicense.ownerFactionKey = _snapshot.ownerFactionKey;
        currentLicense.licenseeFactionKey = _snapshot.licenseeFactionKey;
        currentLicense.remainingMonths = Mathf.Max(0, _snapshot.remainingMonths);
        currentLicense.isRenewable = _snapshot.isRenewable;
    }

    public void HandleFactionGameOver(FactionManager _faction)
    {
        string factionKey = GetFactionKey(_faction);
        if (string.IsNullOrWhiteSpace(factionKey))
            return;

        EnsureCollections();

        for (int i = 0; i < claimedPatents.Count; i++)
        {
            PatentResearchSaveData claim = claimedPatents[i];
            if (claim == null || !string.Equals(claim.ownerFactionKey, factionKey, StringComparison.OrdinalIgnoreCase))
                continue;

            claim.isLockedForever = true;
            Debug.Log($"[PatentResearchManager] Patent permanently closed. research={claim.researchId}, owner={factionKey}");
        }

        for (int i = 0; i < activeLicenses.Count; i++)
        {
            PatentLicenseSaveData license = activeLicenses[i];
            if (license == null || !string.Equals(license.ownerFactionKey, factionKey, StringComparison.OrdinalIgnoreCase))
                continue;

            license.isRenewable = false;
        }
    }

    public List<PatentResearchSaveData> ExportSaveData()
    {
        EnsureCollections();

        List<PatentResearchSaveData> result = new List<PatentResearchSaveData>();
        for (int i = 0; i < claimedPatents.Count; i++)
        {
            PatentResearchSaveData entry = claimedPatents[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.researchId) || string.IsNullOrWhiteSpace(entry.ownerFactionKey))
                continue;

            result.Add(new PatentResearchSaveData
            {
                researchId = NormalizeResearchId(entry.researchId),
                ownerFactionKey = entry.ownerFactionKey.Trim(),
                isLockedForever = entry.isLockedForever
            });
        }

        return result;
    }

    public void ImportSaveData(List<PatentResearchSaveData> _data)
    {
        claimedPatents = new List<PatentResearchSaveData>();

        if (_data == null)
            return;

        HashSet<string> seenResearchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < _data.Count; i++)
        {
            PatentResearchSaveData entry = _data[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.researchId) || string.IsNullOrWhiteSpace(entry.ownerFactionKey))
                continue;

            string researchId = NormalizeResearchId(entry.researchId);
            if (!seenResearchIds.Add(researchId))
                continue;

            claimedPatents.Add(new PatentResearchSaveData
            {
                researchId = researchId,
                ownerFactionKey = entry.ownerFactionKey.Trim(),
                isLockedForever = entry.isLockedForever
            });
        }
    }

    public List<PatentLicenseSaveData> ExportLicenseSaveData()
    {
        EnsureCollections();

        List<PatentLicenseSaveData> result = new List<PatentLicenseSaveData>();
        for (int i = 0; i < activeLicenses.Count; i++)
        {
            PatentLicenseSaveData license = activeLicenses[i];
            if (!IsValidLicenseEntry(license))
                continue;

            result.Add(CopyLicense(license));
        }

        return result;
    }

    public void ImportLicenseSaveData(List<PatentLicenseSaveData> _data, bool _applyEffects)
    {
        SubscribeCalendar();

        activeLicenses = new List<PatentLicenseSaveData>();

        if (_data == null)
            return;

        HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < _data.Count; i++)
        {
            PatentLicenseSaveData license = _data[i];
            if (!IsValidLicenseEntry(license))
                continue;

            string uniqueKey = $"{NormalizeResearchId(license.researchId)}|{NormalizeFactionKey(license.licenseeFactionKey)}";
            if (!seenKeys.Add(uniqueKey))
                continue;

            PatentLicenseSaveData importedLicense = CopyLicense(license);
            activeLicenses.Add(importedLicense);

            if (_applyEffects)
                ApplyLicenseEffect(GetResearchData(importedLicense.researchId), FindFactionByKey(importedLicense.licenseeFactionKey));
        }
    }

    private void OnMonthChanged(int year, int month)
    {
        ProcessLicenseMonth();
    }

    private void ProcessLicenseMonth()
    {
        EnsureCollections();

        for (int i = activeLicenses.Count - 1; i >= 0; i--)
        {
            PatentLicenseSaveData license = activeLicenses[i];
            if (!IsValidLicenseEntry(license))
            {
                activeLicenses.RemoveAt(i);
                continue;
            }

            license.remainingMonths--;
            if (license.remainingMonths > 0)
                continue;

            FactionManager licenseeFaction = FindFactionByKey(license.licenseeFactionKey);
            ResearchData research = GetResearchData(license.researchId);
            activeLicenses.RemoveAt(i);
            RemoveLicenseEffect(research, licenseeFaction);
            Debug.Log($"[PatentResearchManager] Patent license expired. research={license.researchId}, licensee={license.licenseeFactionKey}");
        }
    }

    private bool RemovePatentLicenseByKey(string _researchId, string _licenseeFactionKey, bool removeEffect)
    {
        string researchId = NormalizeResearchId(_researchId);
        string licenseeKey = NormalizeFactionKey(_licenseeFactionKey);

        for (int i = activeLicenses.Count - 1; i >= 0; i--)
        {
            PatentLicenseSaveData license = activeLicenses[i];
            if (license == null
                || !string.Equals(NormalizeResearchId(license.researchId), researchId, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(NormalizeFactionKey(license.licenseeFactionKey), licenseeKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            activeLicenses.RemoveAt(i);
            if (removeEffect)
                RemoveLicenseEffect(GetResearchData(researchId), FindFactionByKey(licenseeKey));

            return true;
        }

        return false;
    }

    private void ApplyLicenseEffect(ResearchData research, FactionManager faction)
    {
        if (research == null || faction == null)
            return;

        faction.ApplyExternalResearchEffect(research);
    }

    private void RemoveLicenseEffect(ResearchData research, FactionManager faction)
    {
        if (research == null || faction == null)
            return;

        faction.RemoveExternalResearchEffect(research);
    }

    private void SubscribeCalendar()
    {
        if (subscribedCalendar != null)
            return;

        subscribedCalendar = FindFirstObjectByType<CalendarScript>();
        if (subscribedCalendar == null)
        {
            if (!warnedMissingCalendar)
            {
                Debug.LogWarning("[PatentResearchManager] CalendarScript not found. Patent license duration will not tick until a calendar is available.");
                warnedMissingCalendar = true;
            }
            return;
        }

        subscribedCalendar.MonthChanged += OnMonthChanged;
    }

    private void UnsubscribeCalendar()
    {
        if (subscribedCalendar == null)
            return;

        subscribedCalendar.MonthChanged -= OnMonthChanged;
        subscribedCalendar = null;
    }

    private ResearchData GetResearchData(string researchId)
    {
        if (string.IsNullOrWhiteSpace(researchId))
            return null;

        if (researchDatabase == null)
            researchDatabase = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");

        if (researchDatabase == null)
        {
            researchDatabase = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
            researchDatabase.LoadCSV();
        }

        return researchDatabase != null ? researchDatabase.GetResearchById(researchId) : null;
    }

    private FactionManager FindFactionByKey(string factionKey)
    {
        string normalizedKey = NormalizeFactionKey(factionKey);
        if (string.IsNullOrWhiteSpace(normalizedKey))
            return null;

        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null)
                continue;

            if (string.Equals(faction.GetSaveKey(), normalizedKey, StringComparison.OrdinalIgnoreCase))
                return faction;
        }

        return null;
    }

    private PatentResearchSaveData FindClaim(string researchId)
    {
        EnsureCollections();

        string normalizedResearchId = NormalizeResearchId(researchId);
        if (string.IsNullOrWhiteSpace(normalizedResearchId))
            return null;

        for (int i = 0; i < claimedPatents.Count; i++)
        {
            PatentResearchSaveData entry = claimedPatents[i];
            if (entry == null)
                continue;

            if (string.Equals(NormalizeResearchId(entry.researchId), normalizedResearchId, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }

    private PatentLicenseSaveData FindLicense(string researchId, string licenseeFactionKey)
    {
        EnsureCollections();

        string normalizedResearchId = NormalizeResearchId(researchId);
        string normalizedFactionKey = NormalizeFactionKey(licenseeFactionKey);
        if (string.IsNullOrWhiteSpace(normalizedResearchId) || string.IsNullOrWhiteSpace(normalizedFactionKey))
            return null;

        for (int i = 0; i < activeLicenses.Count; i++)
        {
            PatentLicenseSaveData license = activeLicenses[i];
            if (license == null)
                continue;

            if (string.Equals(NormalizeResearchId(license.researchId), normalizedResearchId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(NormalizeFactionKey(license.licenseeFactionKey), normalizedFactionKey, StringComparison.OrdinalIgnoreCase))
            {
                return license;
            }
        }

        return null;
    }

    private bool IsValidLicenseEntry(PatentLicenseSaveData license)
    {
        return license != null
            && !string.IsNullOrWhiteSpace(license.researchId)
            && !string.IsNullOrWhiteSpace(license.ownerFactionKey)
            && !string.IsNullOrWhiteSpace(license.licenseeFactionKey)
            && license.remainingMonths > 0;
    }

    private static PatentLicenseSaveData CopyLicense(PatentLicenseSaveData license)
    {
        if (license == null)
            return null;

        return new PatentLicenseSaveData
        {
            researchId = NormalizeResearchId(license.researchId),
            ownerFactionKey = NormalizeFactionKey(license.ownerFactionKey),
            licenseeFactionKey = NormalizeFactionKey(license.licenseeFactionKey),
            remainingMonths = Mathf.Max(0, license.remainingMonths),
            isRenewable = license.isRenewable
        };
    }

    private void EnsureCollections()
    {
        if (claimedPatents == null)
            claimedPatents = new List<PatentResearchSaveData>();

        if (activeLicenses == null)
            activeLicenses = new List<PatentLicenseSaveData>();
    }

    public static bool IsValidLicenseDuration(int durationMonths)
    {
        return durationMonths == OneYearLicenseMonths
            || durationMonths == ThreeYearLicenseMonths
            || durationMonths == FiveYearLicenseMonths;
    }

    private static string GetFactionKey(FactionManager faction)
    {
        return faction != null ? NormalizeFactionKey(faction.GetSaveKey()) : string.Empty;
    }

    private static string NormalizeResearchId(string researchId)
    {
        return string.IsNullOrWhiteSpace(researchId) ? string.Empty : researchId.Trim();
    }

    private static string NormalizeFactionKey(string factionKey)
    {
        return string.IsNullOrWhiteSpace(factionKey) ? string.Empty : factionKey.Trim();
    }
}
