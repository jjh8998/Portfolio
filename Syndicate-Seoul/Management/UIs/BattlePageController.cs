using System;
using UnityEngine;

public class BattlePageController : MonoBehaviour
{
    public GameObject panel;

    public event Action<CityScript, bool> BattleFinished;

    private CityScript city;
    private FactionManager attacker;
    private FactionManager defender;

    public void StartBattle(CityScript _city, FactionManager _attacker, FactionManager _defender)
    {
        city = _city;
        attacker = _attacker;
        defender = _defender;

        if (panel != null) panel.SetActive(true);

        AttackerWin();
        
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void AttackerWin()
    {
        BattleFinished?.Invoke(city, true);
        Close();
    }

    public void AttackerLose()
    {
        BattleFinished?.Invoke(city, false);
        Close();
    }
}
