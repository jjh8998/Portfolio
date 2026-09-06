using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowStatScript : MonoBehaviour
{
    public bool firstShowInfo = false;

    public TextMeshProUGUI hpText = null;
    public TextMeshProUGUI attackDamageText = null;
    public TextMeshProUGUI defenseText = null;
    public TextMeshProUGUI criticalDamageText = null;
    public TextMeshProUGUI criticalRateText = null;

    private Item nowItem;

    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        if (firstShowInfo == true)
            ShowPlayerStatInfo();
    }

    public void ShowPlayerStatInfo()
    {
        hpText.text = DatabaseManager.instance.GetPlayer_Hp().ToString();
        attackDamageText.text = DatabaseManager.instance.GetPlayer_AttackDamage().ToString();
        defenseText.text = DatabaseManager.instance.GetPlayer_Defense().ToString();
        criticalDamageText.text = DatabaseManager.instance.GetPlayer_CriticalDamage().ToString();
        criticalRateText.text = DatabaseManager.instance.GetPlayer_CriticalRate().ToString();
    }

    public void ShowItemStatInfo(Item _Item, MyItem _MyItem)
    {
        nowItem = _Item;

        DatabaseManager.instance.CalculateRealItemStat(_Item, _MyItem); // 아이템 스텟 계산

        hpText.text = _Item.equipment.realHp.ToString();
        attackDamageText.text = _Item.equipment.realDamage.ToString();
        defenseText.text = _Item.equipment.realDefense.ToString();
        criticalDamageText.text = _Item.equipment.realCriticalDamage.ToString();
        criticalRateText.text = _Item.equipment.realCriticalRate.ToString();
    }
}
