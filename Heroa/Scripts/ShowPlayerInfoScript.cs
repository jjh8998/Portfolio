using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShowPlayerInfoScript : MonoBehaviour
{

    public static ShowPlayerInfoScript instance = null;

    public bool firstShowInfo = false;
    public bool showPlayerInfo = false;
    public bool alwaysShowPlayerInfo = false;
    public bool showPlayerStat = false;

    public TextMeshProUGUI gold = null;
    public TextMeshProUGUI LV = null;
    public Image expImage = null;

    public TextMeshProUGUI hpText = null;
    public TextMeshProUGUI attackDamageText = null;
    public TextMeshProUGUI defenseText = null;
    public TextMeshProUGUI criticalDamageText = null;
    public TextMeshProUGUI criticalRateText = null;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (firstShowInfo == true)
        {
            if (showPlayerInfo == true)
            {
                ShowPlayerInfo();
            }

            if (showPlayerStat == true)
            {
                ShowPlayerStatInfo();
            }
        }
    }
    
    private void Update()
    {
        if (alwaysShowPlayerInfo == true)
            ShowPlayerInfo();
    }

    public void ShowPlayerInfo()
    {
        if (gold != null)
            gold.text = DatabaseManager.instance.GetGold().ToString();

        if (LV != null)
            LV.text = DatabaseManager.instance.GetLV().ToString();
        if (expImage != null)
        {
            float levelUpExp = DatabaseManager.instance.GetLevelUPExp();
            float nowExp = DatabaseManager.instance.GetNowExp();

            expImage.fillAmount = nowExp / levelUpExp;
        }
    }

    public void ShowPlayerStatInfo()
    {
        hpText.text = DatabaseManager.instance.GetPlayer_Hp().ToString();
        attackDamageText.text = DatabaseManager.instance.GetPlayer_AttackDamage().ToString();
        defenseText.text = DatabaseManager.instance.GetPlayer_Defense().ToString();
        criticalDamageText.text = DatabaseManager.instance.GetPlayer_CriticalDamage().ToString();
        criticalRateText.text = DatabaseManager.instance.GetPlayer_CriticalRate().ToString();
    }
}
