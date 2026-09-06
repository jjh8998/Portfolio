using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MagicSelectManager : MonoBehaviour
{
    /*
     * 메인 메뉴의 마법 패널에 관한 스크립트
     */

    public static MagicSelectManager instance;

    public Parser parser;
    [Header("Alphabet")]
    [SerializeField]
    private TextMeshProUGUI vowelCount_Text;
    [SerializeField]
    private TextMeshProUGUI consonantCount_Text;

    [Header("SelectMagic")]
    [SerializeField]
    private GameObject magicContent;
    private List<Transform> magicInfo = new List<Transform>();
    [SerializeField]
    private GameObject showSelectedMagicInfoPanel;

    [SerializeField] // 
    private List<Magic> magicList;

    public GameObject blackPanel;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        DatabaseManager.instance.LoadMySelectedMagicDataFromJson();
        magicList = parser.MagicParser("Heroa_MagicList").ToList();

        for (int i = 0; i < magicContent.transform.childCount; i++)
            magicInfo.Add(magicContent.transform.GetChild(i).GetComponent<Transform>());

        ShowMagicInfo();
        ShowAlphabetCount();
    }

    public void ShowMagicInfo()
    {
        for (int i = 0; i < magicInfo.Count; i++)
            magicInfo[i].gameObject.SetActive(true);

        for (int j = 0; j < magicList.Count; j++)
            magicInfo[j].GetComponent<MagicInfoScript>().SetMagicInfo(magicList[j]);

        for (int k = magicList.Count; k < magicInfo.Count; k++)
            magicInfo[k].gameObject.SetActive(false);
    }

    public void ShowAlphabetCount()
    {
        // 알파벳 모음 자음을 보여주는 함수

        int vowelCount = 0;
        int consanant = 0;

        for (int i = 0; i < DatabaseManager.instance.myItemInInventoryData.GetMyItemDataCount(); i++)
        {
            if (DatabaseManager.instance.myItemInInventoryData.myitemData[i].itemID == 1)
            {
                vowelCount = DatabaseManager.instance.myItemInInventoryData.myitemData[i].itemCount;
                vowelCount_Text.text = "" + vowelCount;
                break;
            }
        }

        for (int j = 0; j < DatabaseManager.instance.myItemInInventoryData.GetMyItemDataCount(); j++)
        {
            if (DatabaseManager.instance.myItemInInventoryData.myitemData[j].itemID == 2)
            {
                consanant = DatabaseManager.instance.myItemInInventoryData.myitemData[j].itemCount;
                consonantCount_Text.text = "" + consanant;
                break;
            }
        }

        if (vowelCount == 0)
            vowelCount_Text.text = "0";
        if (consanant == 0)
            consonantCount_Text.text = "0";
    }

    #region On/Off

    public void OnShowSelectedMagicInfoPanel(Magic _Magic)
    {
        showSelectedMagicInfoPanel.SetActive(true);

        showSelectedMagicInfoPanel.GetComponent<ShowSelectedMagicInfoScript>().ShowMagicInfo(_Magic);
    }

    public void OffSelectedMagicInfoPanel()
    {
        MagicManager.instance.SetMagicScriptsIsChangeMagic(false);
        showSelectedMagicInfoPanel.SetActive(false);
    }

    #endregion


}
