using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpBarScript : MonoBehaviour
{

    [SerializeField]
    private bool isHeros = false;
    [SerializeField]
    private bool isMonsters = false;

    [SerializeField]
    private Image hpBar;
    private Image attributeIcon;
    private GameObject hpBarObj;

    private HeroScript hero = null;
    private MonsterScript monster = null;

    // Start is called before the first frame update
    void Start()
    {
        if (isHeros == true)
        {
            hero = GetComponent<HeroScript>();
            hpBarObj = GameObject.Find("Canvas(Stage)").transform.Find("BattlePanel").gameObject.transform.Find("HeroPos").gameObject;
            hpBar = hpBarObj.transform.Find("HpBarBackground").transform.Find("HpBar").GetComponent<Image>();
            attributeIcon = hpBarObj.transform.Find("HpBarBackground").transform.Find("Attribute_Image").GetComponent<Image>();

            // 장착 데이터 불러오기
            DatabaseManager.instance.LoadPlayerEquipmentDataFromJson();
            DatabaseManager.instance.SynchroEquipment();

            // 속성 표시
            Attribute attribute = DatabaseManager.instance.GetNowWeaponAttribute();
            switch (attribute)
            {
                case Attribute.ETC:
                case Attribute.Nothing:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/NothingAttribute_Image");
                    break;

                case Attribute.Fire:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/FireAttribute_Image");
                    break;

                case Attribute.Water:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/WaterAttribute_Image");
                    break;

                case Attribute.Grass:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/GrassAttribute_Image");
                    break;
            }
        }

        else if (isMonsters == true)
        {
            monster = GetComponent<MonsterScript>();
            hpBarObj = GameObject.Find("Canvas(Stage)").transform.Find("BattlePanel").gameObject.transform.Find("MonsterPos").gameObject;
            hpBar = hpBarObj.transform.Find("HpBarBackground").transform.Find("HpBar").GetComponent<Image>();
            attributeIcon = hpBarObj.transform.Find("HpBarBackground").transform.Find("Attribute_Image").GetComponent<Image>();

            // null 체크
            if (hpBar == null)
                Debug.LogError("HpBarScript : no monster's hpbar");

            // 속성 표시
            Attribute attribute = monster.GetMonsterAttribute();
            switch (attribute)
            {
                case Attribute.Nothing:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/NothingAttribute_Image");
                    //attributeIcon.sprite = Resources.Load("Images/NothingAttribute_Image", typeof(Sprite)) as Sprite;
                    break;

                case Attribute.Fire:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/FireAttribute_Image");
                    break;

                case Attribute.Water:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/WaterAttribute_Image");
                    break;

                case Attribute.Grass:
                    attributeIcon.sprite = Resources.Load<Sprite>("Images/GrassAttribute_Image");
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isHeros == true)
        {
            hpBar.fillAmount = hero.GetCurHp() / hero.GetHp();
        }
        else if (isMonsters == true)
        {
            hpBar.fillAmount = monster.GetCurHp() / monster.GetHp();
        }
    }
}
