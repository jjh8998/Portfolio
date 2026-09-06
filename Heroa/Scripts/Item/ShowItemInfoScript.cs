using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class Coordinate
{
    public float x;
    public float y;
}

public class ShowItemInfoScript : MonoBehaviour
{

    [SerializeField]
    private GameObject starPos = null;
    [SerializeField]
    private GameObject[] star = null;
    [SerializeField]
    private Coordinate rate_1_Co;
    [SerializeField]
    private Coordinate rate_2_Co;
    [SerializeField]
    private Coordinate rate_3_Co;
    [SerializeField]
    private Coordinate rate_4_Co;
    [SerializeField]
    private Coordinate rate_5_Co;

    [SerializeField]
    private Image itemImage = null;
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI descriptionText;
    [SerializeField]
    private TextMeshProUGUI LVText;
    [SerializeField]
    private Image expImage;

    private Item nowItem;

    private Coordinate pos;
    private int starNum = 0;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void ShowItemRate(Item _Item, bool _DoAni)
    {

        switch (_Item.itemRate)
        {
            case 1 :
                pos = rate_1_Co;
                starNum = 1;
                break;

            case 2 :
                pos = rate_2_Co;
                starNum = 2;
                break;

            case 3 :
                pos = rate_3_Co;
                starNum = 3;
                break;

            case 4 :
                pos = rate_4_Co;
                starNum = 4;
                break;

            case 5 :
                pos = rate_5_Co;
                starNum = 5;
                break;
            default:
                Debug.Log("ShowItemInfoScript : NO impormation for rate");
                return;
        }

        starPos.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x, pos.y);

        // 별 초기화
        for (int i = 0; i < 5; i++)
        {
            star[i].SetActive(false);
        }

        if (_DoAni == true)
        {
            StartCoroutine(star_Ani());
        }
        else
        {
            for (int i = 0; i < starNum; i++)
            {
                star[i].SetActive(true);
            }
            /*
            for (int j = 0; j < 5 - starNum; j++)
            {
                star[4 - j].SetActive(false);
            }
            */
        }
    }

    IEnumerator star_Ani()
    {
        for (int i = 0; i < starNum; i++)
        {
            yield return new WaitForSeconds(0.15f);
            star[i].SetActive(true);
        }
    }

    public void ShowItemStringInfo(Item _Item)
    {
        if (nameText != null)
            nameText.text = _Item.itemName;
        if (descriptionText != null)
        {
            string str = _Item.settingDescription + "\n\n" + _Item.abilityDescription;
            descriptionText.text = str;
        }
    }

    public void ShowItemImage(Item _Item)
    {
        itemImage.sprite = _Item.itemSprite;
    }

    public void ShowItemLVInfo(Item _Item, MyItem _MyItem)
    {
        if (_MyItem.itemLV == _Item.itemRate * 4)
        {
            LVText.text = "LV. " + _MyItem.itemLV + " (max)";
        }
        else
        {
            LVText.text = "LV. " + _MyItem.itemLV;
        }
        int itemExp = _MyItem.itemLV * 100;
        expImage.fillAmount = _MyItem.itemNowExp / itemExp;
    }
}
