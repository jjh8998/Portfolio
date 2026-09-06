using UnityEngine;

public class FactionAIBuildingRestrictionService : MonoBehaviour
{
    public static FactionAIBuildingRestrictionService Instance { get; private set; }

    [SerializeField] private FactionAIBuildingRestrictionConfig config;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool IsDisabledForAI(BuildingData building)
    {
        if (building == null)
            return false;

        FactionAIBuildingRestrictionService service = Instance;
        if (service == null || service.config == null)
            return false;

        return service.config.IsDisabled(building);
    }
}
