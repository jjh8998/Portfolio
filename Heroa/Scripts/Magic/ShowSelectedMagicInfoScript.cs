using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ShowSelectedMagicInfoScript : MonoBehaviour
{
    /*
     * Show
     */

    [SerializeField]
    private GameObject magicIdiomPanel;
    [SerializeField]
    private GameObject buyBtn;
    [SerializeField]
    private GameObject activeBtn;

    [Header("Infoes")]
    [SerializeField]
    private Image magicImage;
    [SerializeField]
    private TextMeshProUGUI magicNameText;
    [SerializeField]
    private TextMeshProUGUI magicAbilityText;
    [SerializeField]
    private TextMeshProUGUI magicDescriptionText;

    [Header("IdiomPanelInfoes")]
    [SerializeField]
    private TextMeshProUGUI idiomText;
    [SerializeField]
    private TextMeshProUGUI idiomMeaningText;
    [SerializeField]
    private TextMeshProUGUI needVowerCountText;
    [SerializeField]
    private TextMeshProUGUI needConsanantText;


    private Magic myMagic;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ShowMagicInfo(Magic _Magic)
    {
        // 선택된 마법 상세 정보 보여주기 및 창 설정
        myMagic = _Magic;
        magicImage.sprite = _Magic.magicSprite;
        magicNameText.text = _Magic.name;
        magicAbilityText.text = _Magic.ability;
        magicDescriptionText.text = _Magic.settingDescription;

        if (DatabaseManager.instance.mySelectedMagicData.isHave[_Magic.ID] == true)
        {
            magicIdiomPanel.SetActive(false);
            buyBtn.SetActive(false);
            activeBtn.SetActive(true);
        }
        else
        {
            magicIdiomPanel.SetActive(true);

            idiomText.text = _Magic.idiom;
            idiomMeaningText.text = _Magic.idiomMeaning;
            needVowerCountText.text = "" + _Magic.needVower;
            needConsanantText.text = "" + _Magic.needConsanant;

            buyBtn.SetActive(true);
            activeBtn.SetActive(false);
        }
    }

    public void OnclickBuyMagicButton()
    {
        var inventory = DatabaseManager.instance.myItemInInventoryData.myitemData;

        // 모음과 자음 아이템 찾기
        var vowelItem = inventory.FirstOrDefault(item => item.itemID == 1);
        var consonantItem = inventory.FirstOrDefault(item => item.itemID == 2);

        if (vowelItem == null || consonantItem == null)
        {
            Debug.LogWarning("필요한 아이템이 없습니다.");
            return;
        }

        if (vowelItem.itemCount < myMagic.needVower || consonantItem.itemCount < myMagic.needConsanant)
        {
            Debug.LogWarning("아이템 수량이 부족합니다.");
            return;
        }

        // 마법 구매 처리
        DatabaseManager.instance.mySelectedMagicData.isHave[myMagic.ID] = true;
        DatabaseManager.instance.SaveMySelectedMagicDataToJson();

        InventoryScript.instance.RemoveItem(new MyItem(1, 0, 0, 0), myMagic.needVower);
        InventoryScript.instance.RemoveItem(new MyItem(2, 0, 0, 0), myMagic.needConsanant);

        MagicSelectManager.instance.ShowAlphabetCount();
        MagicSelectManager.instance.OffSelectedMagicInfoPanel();
    }

    public void OnclickActiveMagicButton()
    {
        MagicSelectManager.instance.blackPanel.SetActive(true);
        MagicManager.instance.ChangeMagic_Step1(myMagic);
    }

}
