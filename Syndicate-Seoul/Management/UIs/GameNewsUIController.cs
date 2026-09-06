using System.Collections;
using TMPro;
using UnityEngine;

public class GameNewsUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text newsText;
    [SerializeField] private float displayDuration = 5f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        HideNews();
    }

    public void ShowNews(string message)
    {
        if (newsText == null)
            return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            HideNews();
            return;
        }

        newsText.text = message;
        newsText.gameObject.SetActive(true);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    public void ShowAIWarResult(FactionManager attacker, FactionManager defender, CityScript targetCity, bool attackerWon)
    {
        string attackerName = GetFactionName(attacker);
        string defenderName = GetFactionName(defender);
        string winnerName = attackerWon ? attackerName : defenderName;
        string cityName = GetCityName(targetCity);

        string message = string.IsNullOrWhiteSpace(cityName)
            ? $"[NEWS] {attackerName}가 {defenderName}에 대한 인수합병을 시도했습니다. 승자: {winnerName}"
            : $"[NEWS] {attackerName}가 {defenderName}의 {cityName}에 대한 인수합병을 시도했습니다. 승자: {winnerName}";

        ShowNews(message);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, displayDuration));

        hideCoroutine = null;
        HideNews();
    }

    private void HideNews()
    {
        if (newsText == null)
            return;

        newsText.text = string.Empty;
        newsText.gameObject.SetActive(false);
    }

    private string GetFactionName(FactionManager faction)
    {
        return faction != null && !string.IsNullOrWhiteSpace(faction.factionName)
            ? faction.factionName
            : "Unknown";
    }

    private string GetCityName(CityScript city)
    {
        return city != null && city.cityData != null && !string.IsNullOrWhiteSpace(city.cityData.cityName)
            ? city.cityData.cityName
            : string.Empty;
    }
}
