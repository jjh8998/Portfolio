using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CityPopUpUIController : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private FactionManager playerFac;
    [SerializeField] private CityUIController cityUIController;
    [SerializeField] private GameObject popupRoot;

    private CityScript targetCity;
    private bool ownershipEventsBound;
    private bool factionCitiesEventsBound;

    public bool IsOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
        ClosePopup();
    }

    private void OnEnable()
    {
        SubscribeCityOwnershipEvents();
        SubscribeFactionCitiesEvents();
    }

    private void OnDisable()
    {
        UnsubscribeCityOwnershipEvents();
        UnsubscribeFactionCitiesEvents();
    }

    private void Start()
    {
        SubscribeCityOwnershipEvents();
        SubscribeFactionCitiesEvents();
        RefreshEmptySlotPopup();
    }

    private void OnDestroy()
    {
        UnsubscribeCityOwnershipEvents();
        UnsubscribeFactionCitiesEvents();
    }

    public void RefreshEmptySlotPopup()
    {
        CityScript firstCity = null;
        int cityCount = CountCitiesWithEmptySlots(out firstCity);

        if (cityCount <= 0)
        {
            targetCity = null;
            ClosePopup();
            return;
        }

        targetCity = firstCity;

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }

        string cityName = GetCityName(firstCity);

    }

    public void OpenTargetCityFromPopup()
    {
        if (targetCity == null || targetCity.cityData == null || cityUIController == null)
        {
            return;
        }

        ClosePopup();
        cityUIController.OpenCityPanel(targetCity);
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        ClosePopup();
        return true;
    }

    private void SubscribeCityOwnershipEvents()
    {
        if (ownershipEventsBound || CityOwnershipManager.instance == null)
        {
            return;
        }

        CityOwnershipManager.instance.AnyCityOwnerChanged += OnCityOwnerChanged;
        ownershipEventsBound = true;
    }

    private void UnsubscribeCityOwnershipEvents()
    {
        if (!ownershipEventsBound || CityOwnershipManager.instance == null)
        {
            ownershipEventsBound = false;
            return;
        }

        CityOwnershipManager.instance.AnyCityOwnerChanged -= OnCityOwnerChanged;
        ownershipEventsBound = false;
    }

    private void SubscribeFactionCitiesEvents()
    {
        if (factionCitiesEventsBound || playerFac == null)
        {
            return;
        }

        playerFac.CitiesChanged += OnPlayerCitiesChanged;
        factionCitiesEventsBound = true;
    }

    private void UnsubscribeFactionCitiesEvents()
    {
        if (!factionCitiesEventsBound || playerFac == null)
        {
            factionCitiesEventsBound = false;
            return;
        }

        playerFac.CitiesChanged -= OnPlayerCitiesChanged;
        factionCitiesEventsBound = false;
    }

    private void OnCityOwnerChanged(CityScript _city, FactionManager _oldOwner, FactionManager _newOwner)
    {
        if (playerFac == null)
        {
            return;
        }

        if (_oldOwner != playerFac && _newOwner != playerFac)
        {
            return;
        }

        RefreshEmptySlotPopup();
    }

    private void OnPlayerCitiesChanged()
    {
        RefreshEmptySlotPopup();
    }

    private int CountCitiesWithEmptySlots(out CityScript _firstCity)
    {
        _firstCity = null;

        if (playerFac == null || playerFac.ownedCities == null)
        {
            return 0;
        }

        int count = 0;
        List<CityScript> ownedCities = playerFac.ownedCities;

        for (int i = 0; i < ownedCities.Count; i++)
        {
            CityScript city = ownedCities[i];
            if (!HasEmptyBuildingSlot(city))
            {
                continue;
            }

            if (_firstCity == null)
            {
                _firstCity = city;
            }

            count++;
        }

        return count;
    }

    private bool HasEmptyBuildingSlot(CityScript _city)
    {
        if (_city == null || _city.cityData == null || _city.cityData.buildings == null)
        {
            return false;
        }

        List<BuildingInstance> buildings = _city.cityData.buildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingInstance building = buildings[i];
            if (building != null && building.IsEmptySlot())
            {
                return true;
            }
        }

        return false;
    }

    private string GetCityName(CityScript _city)
    {
        if (_city != null && _city.cityData != null && !string.IsNullOrEmpty(_city.cityData.cityName))
        {
            return CityDisplayNameUtility.ToKoreanDisplayName(_city.cityData.cityName);
        }

        return "A city";
    }

    private void BindCloseButton(Button _button)
    {
        if (_button == null)
        {
            return;
        }

        _button.onClick.RemoveListener(ClosePopup);
        _button.onClick.AddListener(ClosePopup);
    }

    private void UnbindCloseButton(Button _button)
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(ClosePopup);
        }
    }
}
