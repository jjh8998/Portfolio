using System.Collections.Generic;
using UnityEngine;

public class CitySaveService : MonoBehaviour
{
    private static PowerBuildingDatabaseSO powerDb;
    private static EconomicBuildingDatabaseSO economicDb;
    private static ResearchBuildingDatabaseSO researchDb;
    private static FactoryBuildingDatabaseSO factoryDb;
    private static SupportBuildingDatabaseSO supportDb;

    public static CityScript.CitySaveData ExportSaveData(CityScript _city)
    {
        CityScript.CitySaveData result = new CityScript.CitySaveData();

        if (_city == null || _city.cityData == null)
            return result;

        CityData cityData = _city.cityData;

        result.creditIncome = cityData.creditIncome;
        result.shareData = cityData.shareData != null
            ? cityData.shareData.ExportSaveData()
            : new CityShareData.ShareSaveData();

        if (cityData.buildings == null)
            return result;

        for (int i = 0; i < cityData.buildings.Count; i++)
        {
            BuildingInstance building = cityData.buildings[i];
            int lastProcessedYear = -1;
            int lastProcessedMonth = -1;
            int lastProcessedDay = -1;

            if (building != null)
                building.GetLastProcessedDate(out lastProcessedYear, out lastProcessedMonth, out lastProcessedDay);

            result.buildings.Add(new CityScript.BuildingSlotSaveData
            {
                buildingId = building != null && building.data != null ? building.data.ID : "EMPTY",
                upgradeTargetBuildingId = building != null && building.upgradeTargetData != null ? building.upgradeTargetData.ID : string.Empty,
                remainConstructionDay = building != null ? building.remainConstructionDay : 0,
                remainActivationIntervalDays = building != null ? building.remainActivationIntervalDays : 0,
                factoryProductionProgressBuffer = building != null ? Mathf.Max(0f, building.factoryProductionProgressBuffer) : 0f,
                lastProcessedYear = lastProcessedYear,
                lastProcessedMonth = lastProcessedMonth,
                lastProcessedDay = lastProcessedDay,
                isDisabled = building != null && building.isDisabled,
                scavengerLocked = building != null && building.scavengerLocked
            });
        }

        return result;
    }

    public static void ImportSaveData(CityScript _city, CityScript.CitySaveData _data, IDictionary<string, FactionManager> _factionsByName = null)
    {
        if (_city == null)
            return;

        if (_city.cityData == null)
            _city.cityData = new CityData();

        CityData cityData = _city.cityData;

        if (_data != null)
            cityData.creditIncome = _data.creditIncome;

        if (cityData.shareData == null)
            cityData.shareData = new CityShareData();

        cityData.shareData.ImportSaveData(_data != null ? _data.shareData : null, _factionsByName);

        cityData.buildings = new List<BuildingInstance>();

        if (_data != null && _data.buildings != null)
        {
            for (int i = 0; i < _data.buildings.Count; i++)
            {
                cityData.buildings.Add(CreateBuildingInstance(_data.buildings[i]));
            }
        }

        _city.NormalizeBuildingSlots();
        _city.ApplyStarterBuildingsToEmptySlots();
        _city.SyncWorldBuildings();
    }

    private static BuildingInstance CreateBuildingInstance(CityScript.BuildingSlotSaveData _slot)
    {
        string buildingId = _slot != null ? _slot.buildingId : null;
        BuildingData buildingData = ResolveBuildingData(buildingId);
        BuildingInstance instance = new BuildingInstance(buildingData);
        instance.upgradeTargetData = ResolveBuildingData(_slot != null ? _slot.upgradeTargetBuildingId : null);

        instance.remainConstructionDay = Mathf.Max(0, _slot != null ? _slot.remainConstructionDay : 0);
        instance.remainActivationIntervalDays = Mathf.Max(0, _slot != null ? _slot.remainActivationIntervalDays : 0);
        instance.factoryProductionProgressBuffer = Mathf.Max(0f, _slot != null ? _slot.factoryProductionProgressBuffer : 0f);
        instance.isDisabled = _slot != null && _slot.isDisabled;
        instance.scavengerLocked = _slot != null && _slot.scavengerLocked;
        instance.SetLastProcessedDate(
            _slot != null ? _slot.lastProcessedYear : -1,
            _slot != null ? _slot.lastProcessedMonth : -1,
            _slot != null ? _slot.lastProcessedDay : -1);

        if (instance.upgradeTargetData != null && instance.upgradeTargetData.IsEmptySlot())
            instance.upgradeTargetData = null;

        if (instance.upgradeTargetData != null && instance.remainConstructionDay == 0)
        {
            instance.data = instance.upgradeTargetData;
            instance.upgradeTargetData = null;
            instance.remainActivationIntervalDays = Mathf.Max(1, instance.data.activationIntervalDays);
            instance.factoryProductionProgressBuffer = 0f;
        }

        if (instance.data != null && instance.remainActivationIntervalDays <= 0)
            instance.remainActivationIntervalDays = Mathf.Max(1, instance.data.activationIntervalDays);

        return instance;
    }

    private static BuildingData ResolveBuildingData(string _buildingId)
    {
        if (string.IsNullOrWhiteSpace(_buildingId) || _buildingId == "EMPTY")
            return new EmptySc();

        BuildingData building = GetPowerDatabase().GetBuildingByID(_buildingId);
        if (building != null) return building;

        building = GetEconomicDatabase().GetBuildingByID(_buildingId);
        if (building != null) return building;

        building = GetResearchDatabase().GetBuildingByID(_buildingId);
        if (building != null) return building;

        building = GetFactoryDatabase().GetBuildingByID(_buildingId);
        if (building != null) return building;

        building = GetSupportDatabase().GetBuildingByID(_buildingId);
        if (building != null) return building;

        Debug.LogWarning($"[CitySaveService] Building '{_buildingId}' not found. Empty slot used instead.");
        return new EmptySc();
    }

    private static PowerBuildingDatabaseSO GetPowerDatabase()
    {
        return BuildingDatabaseLoader.GetPowerDatabase(ref powerDb, "[CitySaveService]");
    }

    private static EconomicBuildingDatabaseSO GetEconomicDatabase()
    {
        return BuildingDatabaseLoader.GetEconomicDatabase(ref economicDb, "[CitySaveService]");
    }

    private static ResearchBuildingDatabaseSO GetResearchDatabase()
    {
        return BuildingDatabaseLoader.GetResearchDatabase(ref researchDb, "[CitySaveService]");
    }

    private static FactoryBuildingDatabaseSO GetFactoryDatabase()
    {
        return BuildingDatabaseLoader.GetFactoryDatabase(ref factoryDb, "[CitySaveService]");
    }

    private static SupportBuildingDatabaseSO GetSupportDatabase()
    {
        return BuildingDatabaseLoader.GetSupportDatabase(ref supportDb, "[CitySaveService]");
    }
}
