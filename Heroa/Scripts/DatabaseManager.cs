
// DEBUG가 정의되어 있으면 (save 경로) Log를 출력합니다.
#define NO_SHOW_DEBUG // DEBUG라 하면 안됌

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System; // 로컬 시간 함수
using System.Linq;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager instance;

    public static string word_CSV_FileName; // 단어 csv파일 정적 변수

    #region 파싱 변수

    [SerializeField] string CSV_FileName; // csv파일  이름 직렬화

    public static bool isFinish = false;

    private Parser parser = null;

    #endregion

    [SerializeField]
    private bool synchroPlayerCharaData = false; // 왜만듬?

    public List<Item> itemList = new List<Item>();
    public MyItemData myItemInInventoryData;
    public MyItemData myItemInEquipmentData;

    public StageProgressData myStageProgressData;

    public MyMagicData mySelectedMagicData;

    public MyPlayerData myPlayerData;

    public bool isFinishedLoading = false; // 데이터 로딩 끝

    #region Items

    public Item nowEquipment_Weapon;
    private Item preEquipment_Weapon;
    private Item nowEquipment_Head;
    private Item preEquipment_Head;
    private Item nowEquipment_Top;
    private Item preEquipment_Top;
    private Item nowEquipment_Bottom;
    private Item preEquipment_Bottom;
    private Item nowEquipment_Shoes;
    private Item preEquipment_Shoes;

    #endregion

    private string selectedStage;

    private bool existEquipmentScript;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // 싱글턴
        }

        parser = GetComponent<Parser>();

        nowEquipment_Weapon = new Item(0, "nowEquipment_Weapon", "현재 무기 장착 아이템 스텟", "현재 장착된 무기 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        preEquipment_Weapon = new Item(0, "preEquipment_Weapon", "지난 무기 장착 아이템 스텟", "이전에 장착된 무기 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        nowEquipment_Head = new Item(0, "nowEquipment_Head", "현재 머리 장착 아이템 스텟", "현재 장착된 머리 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        preEquipment_Head = new Item(0, "preEquipment_Head", "지난 머리 장착 아이템 스텟", "이전에 장착된 머리 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        nowEquipment_Top = new Item(0, "nowEquipment_Top", "현재 상의 장착 아이템 스텟", "현재 장착된 상의 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        preEquipment_Top = new Item(0, "preEquipment_Top", "지난 상의 장착 아이템 스텟", "이전에 장착된 상의 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        nowEquipment_Bottom = new Item(0, "nowEquipment_Bottom", "현재 하의 장착 아이템 스텟", "현재 장착된 하의 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        preEquipment_Bottom = new Item(0, "preEquipment_Bottom", "지난 하의 장착 아이템 스텟", "이전에 장착된 하의 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        nowEquipment_Shoes = new Item(0, "nowEquipment_Shoes", "현재 신발 장착 아이템 스텟", "현재 장착된 신발 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);
        preEquipment_Shoes = new Item(0, "preEquipment_Shoes", "지난 신발 장착 아이템 스텟", "이전에 장착된 신발 아이템의 Item자료형", "None", Item.ItemType.ETC, 0);

        LoadItemData();
        LoadPlayerItemDataFromJson();
        LoadPlayerEquipmentDataFromJson();
        LoadStageProgressDataFromJson();
    }

    private void Start()
    {
        //파일경로
        string strFile = Path.Combine(Application.persistentDataPath, "PlayerData.json"); ;
        FileInfo fileInfo = new FileInfo(strFile);

        //파일이 없으면 (처음이면)
        if (fileInfo.Exists == false)
        {
            SetupInitialGameData();
        }
        else
        {
            LoadData();
        }

        MigrateUniqueIds();
        isFinishedLoading = true;
    }

    public void MigrateUniqueIds()
    {
        bool needSave = false;

        // 인벤토리 아이템 점검
        if (myItemInInventoryData != null && myItemInInventoryData.myitemData != null)
        {
            foreach (var item in myItemInInventoryData.myitemData)
            {
                if (string.IsNullOrEmpty(item.uniqueId))
                {
                    item.uniqueId = System.Guid.NewGuid().ToString();
                    needSave = true;
                }
            }
        }

        // 장비 아이템 점검
        if (myItemInEquipmentData != null && myItemInEquipmentData.myitemData != null)
        {
            foreach (var item in myItemInEquipmentData.myitemData)
            {
                if (string.IsNullOrEmpty(item.uniqueId))
                {
                    item.uniqueId = System.Guid.NewGuid().ToString();
                    needSave = true;
                }
            }
        }

        // 저장
        if (needSave)
        {
            SavePlayerItemDataToJson();
            SavePlayerEquipmentDataToJson();
            Debug.Log("DatabaseManager : UniqueId migration complete");
        }
    }

    public void SetupInitialGameData()
    {
        SetLV(1);
        SetNowExp(0f);
        SetLevelUpExp(100f);
        SetPlayer_Hp(200f);
        SetPlayer_AttackDamage(10);
        SetPlayer_Defense(0f);
        SetPlayer_CriticalDamage(1.5f);
        SetPlayer_CriticalRate(10f);

        myPlayerData.gold = 0f;

        SetIsGetTodayAccessResult(false);
        myPlayerData.lastAccessDay = int.Parse(DateTime.Now.ToString("dd"));

        myPlayerData.totalRightAnswerCount = 0;
        myPlayerData.totalMonsterKillCount = 0;
        myPlayerData.totalItemCount = 0;
        myPlayerData.totalGold = 0f;
        myPlayerData.totalUsedGold = 0f;

        myPlayerData.clearStageTutorial = false;

        // 마법 설정
        List<Magic> magics = parser.MagicParser("Heroa_MagicList").ToList();

        // 계산하기 쉽게 0은 아무것도 아닌데 추가함.
        for (int i = 0; i < magics.Count + 1; i++)
            mySelectedMagicData.isHave.Add(false);

        mySelectedMagicData.selectedMagicID[0] = 1;
        mySelectedMagicData.isHave[1] = true;
        mySelectedMagicData.selectedMagicID[1] = 2;
        mySelectedMagicData.isHave[2] = true;
        mySelectedMagicData.selectedMagicID[2] = 3;
        mySelectedMagicData.isHave[3] = true;

        // 스토리 진행도
        myPlayerData.chpater1_StoryReader = 1;

        // 장비창 리셋
        for (int i = 0; i < 5; i++)
        {
            myItemInEquipmentData.AddMyItemData(new MyItem(0, 0, 0, 0));
        }

        SaveMySelectedMagicDataToJson();
        SaveMyPlayerDataToJson();
        SavePlayerEquipmentDataToJson();
        SaveStageProgressDataToJson();

        Debug.Log("DatabaseManager : BeginningSetting");

        DialogueManager.StartDialogue("Heroa_Prologue", "MainMenu");
    }


    public void SynchroEquipment()
    {
        // EquipmentScript 존재 유뮤 판단 후, 존재하면 장비창까지 싱크로나이즈, 아니면 데이터베이스만
        if (EquipItemScript.instance != null)
            existEquipmentScript = true;

        // setNow때문에 여기서 equipment 유무 확인하면 안됨
        Debug.Log("DatabaseManager : operate SynchroEquipment");

        for (int i = 0; i < myItemInEquipmentData.GetMyItemDataCount(); i++)
        {
            for (int j = 0; j < itemList.Count; j++)
            {
                if (myItemInEquipmentData.GetItemID(i) == itemList[j].itemID)
                {
                    if (existEquipmentScript == true)
                        EquipItemScript.instance.slots[i].AddItem(myItemInEquipmentData.myitemData[i], i);

                    switch (i)
                    {
                        case 0:
                            if (existEquipmentScript == true)
                                EquipItemScript.instance.SetEquipedWeaponItem(myItemInEquipmentData.myitemData[i]);
                            // 데이터베이스 아이템 설정
                            SetNowEquipment_Weapon(itemList[j], myItemInEquipmentData.myitemData[i]);
                            break;
                        case 1:
                            if (existEquipmentScript == true)
                                EquipItemScript.instance.SetEquipedHeadItemID(myItemInEquipmentData.GetItemID(i));
                            // 데이터베이스 아이템 설정
                            SetNowEquipment_Head(itemList[j], myItemInEquipmentData.myitemData[i]);
                            break;
                        case 2:
                            if (existEquipmentScript == true)
                                EquipItemScript.instance.SetEquipedTopItemID(myItemInEquipmentData.GetItemID(i));
                            // 데이터베이스 아이템 설정
                            SetNowEquipment_Top(itemList[j], myItemInEquipmentData.myitemData[i]);
                            break;
                        case 3:
                            if (existEquipmentScript == true)
                                EquipItemScript.instance.SetEquipedBottomItemID(myItemInEquipmentData.GetItemID(i));
                            // 데이터베이스 아이템 설정
                            SetNowEquipment_Bottom(itemList[j], myItemInEquipmentData.myitemData[i]);
                            break;
                        case 4:
                            if (existEquipmentScript == true)
                                EquipItemScript.instance.SetEquipedShoesItemID(myItemInEquipmentData.GetItemID(i));
                            // 데이터베이스 아이템 설정
                            SetNowEquipment_Shoes(itemList[j], myItemInEquipmentData.myitemData[i]);
                            break;
                    }
                }
            }

        }
    }


    #region Load

    public void LoadData()
    {
        LoadPlayerDataFromJson();
        LoadMySelectedMagicDataFromJson();

#if SHOW_DEBUG
        Debug.Log("DB : Finish LoadData");
#endif
        // SynchroEquipment();

        if (synchroPlayerCharaData == true) // 히어로스크립트에 데이터 동기화
        {
            UpdatePlayerData();
        }

        // Debug.Log("DatabaseManager : LoadData");
    }

    public void LoadItemData()
    {
        if (parser != null)
            parser.ItemListParser("Heroa_ItemList");
    }

    #endregion

    #region Json

    // * 세이브 폴더 만들어서 거기다 저장하기.
    // * 암호화

    public void SavePlayerItemDataToJson()
    {
        string jsonData = JsonUtility.ToJson(myItemInInventoryData, true); ;
        string path = Path.Combine(Application.persistentDataPath, "ItemData.json");
        File.WriteAllText(path, jsonData);

#if SHOW_DEBUG
        Debug.Log("Saving Item Data to Json is finished in " + path);
#endif
    }

    public void SavePlayerEquipmentDataToJson()
    {
        string jsonData = JsonUtility.ToJson(myItemInEquipmentData, true);
        string path = Path.Combine(Application.persistentDataPath, "EquipmentData.json"); ;
        File.WriteAllText(path, jsonData);

#if SHOW_DEBUG
        Debug.Log("Saving Equipment Data to Json is finished in " + path);
#endif
    }

    public void SaveStageProgressDataToJson()
    {
        string jsonData = JsonUtility.ToJson(myStageProgressData, true);
        string path = Path.Combine(Application.persistentDataPath, "StageProgressData.json");
        File.WriteAllText(path, jsonData);

#if SHOW_DEBUG
        Debug.Log("Saving StageProgress Data to Json is finished in " + path);
#endif
    }

    public void SaveMyPlayerDataToJson()
    {
        string jsonData = JsonUtility.ToJson(myPlayerData, true); ;
        string path = Path.Combine(Application.persistentDataPath, "PlayerData.json");
        File.WriteAllText(path, jsonData);

#if SHOW_DEBUG
        Debug.Log("Saving Item Data to Json is finished in " + path);
#endif
    }

    public void SaveMySelectedMagicDataToJson()
    {
        string jsonData = JsonUtility.ToJson(mySelectedMagicData, true); ;
        string path = Path.Combine(Application.persistentDataPath, "MyMagicData.json");
        File.WriteAllText(path, jsonData);

#if SHOW_DEBUG
        Debug.Log("Saving Item Data to Json is finished in " + path);
#endif
    }

    public void LoadPlayerItemDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "ItemData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            myItemInInventoryData = JsonUtility.FromJson<MyItemData>(jsonData);

#if SHOW_DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("DatabaseManager : No Path to load PlayerItemData");
        }
    }
    
    public void LoadPlayerEquipmentDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "EquipmentData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            myItemInEquipmentData = JsonUtility.FromJson<MyItemData>(jsonData);

#if SHOW_DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("DatabaseManager : No Path to load PlayerEquipmentData");
        }
    }

    public void LoadStageProgressDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "StageProgressData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            myStageProgressData = JsonUtility.FromJson<StageProgressData>(jsonData);

#if SHOW_DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("DatabaseManager : No Path to load StageProgressData");
        }
    }

    public void LoadPlayerDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "PlayerData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            myPlayerData = JsonUtility.FromJson<MyPlayerData>(jsonData);

#if SHOW_DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("DatabaseManager : No Path to load PlayerData");
        }
    }

    public void LoadMySelectedMagicDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "MyMagicData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            mySelectedMagicData = JsonUtility.FromJson<MyMagicData>(jsonData);

#if SHOW_DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("DatabaseManager : No Path to load MagicData");
        }
    }

    #endregion

    // 중복
    public void UpdatePlayerData()
    {
        HeroScript.instance.SetHp(myPlayerData.hp);
        HeroScript.instance.SetAttackDamage(myPlayerData.player_AttackDamage);
        HeroScript.instance.SetDefense(myPlayerData.player_Defense);
        HeroScript.instance.SetCriticalDamage(myPlayerData.player_CriticalDamage);
        HeroScript.instance.SetCriticalRate(myPlayerData.player_CriticalRate);
    }

#region About_Item

    public void CalculateRealItemStat(Item _Item, MyItem _MyItem)
    {

        float hp = _Item.equipment.equipment_Hp + Mathf.Round(_MyItem.itemLV * _Item.equipment.upHp);
        float damage = _Item.equipment.equipment_Damage + Mathf.Round(_MyItem.itemLV * _Item.equipment.upDamage);
        float defense = _Item.equipment.equipment_Defense + Mathf.Round(_MyItem.itemLV * _Item.equipment.upDefense);
        float criticalDamage = _Item.equipment.equipment_CriticalDamage + Mathf.Round(_MyItem.itemLV * _Item.equipment.upCD);
        float criticalRate = _Item.equipment.equipment_CriticalRate + Mathf.Round(_MyItem.itemLV * _Item.equipment.upCR);

        _Item.equipment.realHp = hp;
        _Item.equipment.realDamage = damage;
        _Item.equipment.realDefense = defense;
        _Item.equipment.realCriticalDamage = criticalDamage;
        _Item.equipment.realCriticalRate = criticalRate;
    }

    public void SwitchItem(MyItem _MyItem)
    {
        for (int i = 0; i < itemList.Count; i++)
        {
            if (_MyItem.itemID == itemList[i].itemID)
            {
                Item item = itemList[i];

                switch (item.itemType)
                {
                    case Item.ItemType.Weapon:
                        CalculateRealItemStat(itemList[i], _MyItem);
                        Debug.Log("databaseManager : switchItem realDamage - " + item.equipment.realDamage);
                        // 현재 무기 전무기로 설정
                        MyItem preMyItem_Weapon = new MyItem(nowEquipment_Weapon.itemID, nowEquipment_Weapon.itemCount, nowEquipment_Weapon.equipment.itemLV, nowEquipment_Weapon.equipment.itemNowExp);
                        SetPreEquipment_Weapon(nowEquipment_Weapon, preMyItem_Weapon);
                        // 전 무기 스텟 빼기
                        PlusPlayerHp(-preEquipment_Weapon.equipment.realHp);
                        PlusPlayerAttackDamage(-preEquipment_Weapon.equipment.realDamage);
                        PlusPlayerDefense(-preEquipment_Weapon.equipment.realDefense);
                        PlusPlayerCriticalDamage(-preEquipment_Weapon.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(-preEquipment_Weapon.equipment.realCriticalRate);
                        // 바꾼 무기 현재 무기로 설정
                        SetNowEquipment_Weapon(item, _MyItem);

                        // 아이템에 따른 스텟 증감
                        PlusPlayerHp(nowEquipment_Weapon.equipment.realHp);
                        PlusPlayerAttackDamage(nowEquipment_Weapon.equipment.realDamage);
                        PlusPlayerDefense(nowEquipment_Weapon.equipment.realDefense);
                        PlusPlayerCriticalDamage(nowEquipment_Weapon.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(nowEquipment_Weapon.equipment.realCriticalRate);
                        break;

                    case Item.ItemType.Head:
                        // 현재 머리 전머리로 설정
                        MyItem preMyItem_Head = new MyItem(nowEquipment_Head.itemID, nowEquipment_Head.itemCount, nowEquipment_Head.equipment.itemLV, nowEquipment_Head.equipment.itemNowExp);
                        SetPreEquipment_Head(nowEquipment_Head, preMyItem_Head);
                        // 전 헬멧 스텟 빼기
                        PlusPlayerHp(-preEquipment_Head.equipment.realHp);
                        PlusPlayerAttackDamage(-preEquipment_Head.equipment.realDamage);
                        PlusPlayerDefense(-preEquipment_Head.equipment.realDefense);
                        PlusPlayerCriticalDamage(-preEquipment_Head.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(-preEquipment_Head.equipment.realCriticalRate);
                        // 바꾼 헬멧 현재 무기로 설정
                        SetNowEquipment_Head(item, _MyItem);

                        // 아이템에 따른 스텟 증감
                        PlusPlayerHp(nowEquipment_Head.equipment.realHp);
                        PlusPlayerAttackDamage(nowEquipment_Head.equipment.realDamage);
                        PlusPlayerDefense(nowEquipment_Head.equipment.realDefense);
                        PlusPlayerCriticalDamage(nowEquipment_Head.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(nowEquipment_Head.equipment.realCriticalRate);
                        break;

                    case Item.ItemType.Top:
                        // 현재 상의 전상의로 설정
                        MyItem preMyItem_Top = new MyItem(nowEquipment_Top.itemID, nowEquipment_Top.itemCount, nowEquipment_Top.equipment.itemLV, nowEquipment_Top.equipment.itemNowExp);
                        SetPreEquipment_Top(nowEquipment_Top, preMyItem_Top);
                        // 전 상의 스텟 빼기
                        PlusPlayerHp(-preEquipment_Top.equipment.realHp);
                        PlusPlayerAttackDamage(-preEquipment_Top.equipment.realDamage);
                        PlusPlayerDefense(-preEquipment_Top.equipment.realDefense);
                        PlusPlayerCriticalDamage(-preEquipment_Top.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(-preEquipment_Top.equipment.realCriticalRate);
                        // 바꾼 상의 현재 상의로 설정
                        SetNowEquipment_Top(item, _MyItem);

                        // 아이템에 따른 스텟 증감
                        PlusPlayerHp(nowEquipment_Top.equipment.realHp);
                        PlusPlayerAttackDamage(nowEquipment_Top.equipment.realDamage);
                        PlusPlayerDefense(nowEquipment_Top.equipment.realDefense);
                        PlusPlayerCriticalDamage(nowEquipment_Top.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(nowEquipment_Top.equipment.realCriticalRate);
                        break;

                    case Item.ItemType.Bottom:
                        // 현재 하의 전하의로 설정
                        MyItem preMyItem_Bottom = new MyItem(nowEquipment_Bottom.itemID, nowEquipment_Bottom.itemCount, nowEquipment_Bottom.equipment.itemLV, nowEquipment_Bottom.equipment.itemNowExp);
                        SetPreEquipment_Bottom(nowEquipment_Bottom, preMyItem_Bottom);
                        // 전 하의 스텟 빼기
                        PlusPlayerHp(-preEquipment_Bottom.equipment.realHp);
                        PlusPlayerAttackDamage(-preEquipment_Bottom.equipment.realDamage);
                        PlusPlayerDefense(-preEquipment_Bottom.equipment.realDefense);
                        PlusPlayerCriticalDamage(-preEquipment_Bottom.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(-preEquipment_Bottom.equipment.realCriticalRate);
                        // 바꾼 하의 현재 상의로 설정
                        SetNowEquipment_Bottom(item, _MyItem);

                        // 아이템에 따른 스텟 증감
                        PlusPlayerHp(nowEquipment_Bottom.equipment.realHp);
                        PlusPlayerAttackDamage(nowEquipment_Bottom.equipment.realDamage);
                        PlusPlayerDefense(nowEquipment_Bottom.equipment.realDefense);
                        PlusPlayerCriticalDamage(nowEquipment_Bottom.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(nowEquipment_Bottom.equipment.realCriticalRate);
                        break;

                    case Item.ItemType.Shoes:
                        // 현재 신발 전신발로 설정
                        MyItem preMyItem_Shoes = new MyItem(nowEquipment_Shoes.itemID, nowEquipment_Shoes.itemCount, nowEquipment_Shoes.equipment.itemLV, nowEquipment_Shoes.equipment.itemNowExp);
                        SetPreEquipment_Shoes(nowEquipment_Shoes, preMyItem_Shoes);
                        // 전 신발 스텟 빼기
                        PlusPlayerHp(-preEquipment_Shoes.equipment.realHp);
                        PlusPlayerAttackDamage(-preEquipment_Shoes.equipment.realDamage);
                        PlusPlayerDefense(-preEquipment_Shoes.equipment.realDefense);
                        PlusPlayerCriticalDamage(-preEquipment_Shoes.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(-preEquipment_Shoes.equipment.realCriticalRate);
                        // 바꾼 신발 현재 상의로 설정
                        SetNowEquipment_Shoes(item, _MyItem);

                        // 아이템에 따른 스텟 증감
                        PlusPlayerHp(nowEquipment_Shoes.equipment.realHp);
                        PlusPlayerAttackDamage(nowEquipment_Shoes.equipment.realDamage);
                        PlusPlayerDefense(nowEquipment_Shoes.equipment.realDefense);
                        PlusPlayerCriticalDamage(nowEquipment_Shoes.equipment.realCriticalDamage);
                        PlusPlayerCriticalRate(nowEquipment_Shoes.equipment.realCriticalRate);
                        break;
                }
            }
        }
    }

    #region UnequipItems

    public void UnequipWeapon()
    {
        /*
         * Precondition
         * Null
         * 
         * Postcondition
         * 전 장착 아이템 스텟 빼기
         * 현재 장착 아이템 ID : 0
         * 저장
         */

        // 현재 무기 전무기로 설정
        MyItem preMyItem_Weapon = new MyItem(nowEquipment_Weapon.itemID, nowEquipment_Weapon.itemCount, nowEquipment_Weapon.equipment.itemLV, nowEquipment_Weapon.equipment.itemNowExp);
        SetPreEquipment_Weapon(nowEquipment_Weapon, preMyItem_Weapon);
        // 전 무기 스텟 빼기
        PlusPlayerHp(-preEquipment_Weapon.equipment.realHp);
        PlusPlayerAttackDamage(-preEquipment_Weapon.equipment.realDamage);
        PlusPlayerDefense(-preEquipment_Weapon.equipment.realDefense);
        PlusPlayerCriticalDamage(-preEquipment_Weapon.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(-preEquipment_Weapon.equipment.realCriticalRate);

        //현재 무기 초기화
        Item item = new Item(0, "None", "빈 장착 슬롯", "초기화를 위한 비어있는 무기 아이템자료형입니다.", "None", 0, 0);
        MyItem myItem = new MyItem(0, 0, 0, 0);
        SetNowEquipment_Weapon(item,myItem);

        // 아이템에 따른 스텟 증감
        PlusPlayerHp(nowEquipment_Weapon.equipment.realHp);
        PlusPlayerAttackDamage(nowEquipment_Weapon.equipment.realDamage);
        PlusPlayerDefense(nowEquipment_Weapon.equipment.realDefense);
        PlusPlayerCriticalDamage(nowEquipment_Weapon.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(nowEquipment_Weapon.equipment.realCriticalRate);

        // SavePlayerEquipmentDataToJson();
    }

    public void UnequipHead()
    {
        /*
         * UnequipWeapon 참고
         */

        // 현재 머리 전머리로 설정
        MyItem preMyItem_Head = new MyItem(nowEquipment_Head.itemID, nowEquipment_Head.itemCount, nowEquipment_Head.equipment.itemLV, nowEquipment_Head.equipment.itemNowExp);
        SetPreEquipment_Head(nowEquipment_Head, preMyItem_Head);
        // 전 머리 스텟 빼기
        PlusPlayerHp(-preEquipment_Head.equipment.realHp);
        PlusPlayerAttackDamage(-preEquipment_Head.equipment.realDamage);
        PlusPlayerDefense(-preEquipment_Head.equipment.realDefense);
        PlusPlayerCriticalDamage(-preEquipment_Head.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(-preEquipment_Head.equipment.realCriticalRate);

        //현재 머리 초기화
        Item item = new Item(0, "None", "빈 장착 슬롯", "초기화를 위한 비어있는 머리 아이템 자료형입니다.", "None", 0, 0);
        MyItem myItem = new MyItem(0, 0, 0, 0);
        SetNowEquipment_Head(item, myItem);

        // 아이템에 따른 스텟 증감
        PlusPlayerHp(nowEquipment_Head.equipment.realHp);
        PlusPlayerAttackDamage(nowEquipment_Head.equipment.realDamage);
        PlusPlayerDefense(nowEquipment_Head.equipment.realDefense);
        PlusPlayerCriticalDamage(nowEquipment_Head.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(nowEquipment_Head.equipment.realCriticalRate);
    }

    public void UnequipTop()
    {
        /*
         * UnequipWeapon 참고
         */

        // 현재 상의 전상의로 설정
        MyItem preMyItem_Top = new MyItem(nowEquipment_Top.itemID, nowEquipment_Top.itemCount, nowEquipment_Top.equipment.itemLV, nowEquipment_Top.equipment.itemNowExp);
        SetPreEquipment_Top(nowEquipment_Top, preMyItem_Top);
        // 전 상의 스텟 빼기
        PlusPlayerHp(-preEquipment_Top.equipment.realHp);
        PlusPlayerAttackDamage(-preEquipment_Top.equipment.realDamage);
        PlusPlayerDefense(-preEquipment_Top.equipment.realDefense);
        PlusPlayerCriticalDamage(-preEquipment_Top.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(-preEquipment_Top.equipment.realCriticalRate);

        //현재 상의 초기화
        Item item = new Item(0, "None", "빈 장착 슬롯", "초기화를 위한 비어있는 상의 아이템 자료형입니다.", "None", 0, 0);
        MyItem myItem = new MyItem(0, 0, 0, 0);
        SetNowEquipment_Top(item, myItem);

        // 아이템에 따른 스텟 증감
        PlusPlayerHp(nowEquipment_Top.equipment.realHp);
        PlusPlayerAttackDamage(nowEquipment_Top.equipment.realDamage);
        PlusPlayerDefense(nowEquipment_Top.equipment.realDefense);
        PlusPlayerCriticalDamage(nowEquipment_Top.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(nowEquipment_Top.equipment.realCriticalRate);
    }

    public void UnequipBottom()
    {
        /*
         * UnequipWeapon 참고
         */

        // 현재 하의 전하의로 설정
        MyItem preMyItem_Bottom = new MyItem(nowEquipment_Bottom.itemID, nowEquipment_Bottom.itemCount, nowEquipment_Bottom.equipment.itemLV, nowEquipment_Bottom.equipment.itemNowExp);
        SetPreEquipment_Bottom(nowEquipment_Bottom, preMyItem_Bottom);
        // 전 하의 스텟 빼기
        PlusPlayerHp(-preEquipment_Bottom.equipment.realHp);
        PlusPlayerAttackDamage(-preEquipment_Bottom.equipment.realDamage);
        PlusPlayerDefense(-preEquipment_Bottom.equipment.realDefense);
        PlusPlayerCriticalDamage(-preEquipment_Bottom.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(-preEquipment_Bottom.equipment.realCriticalRate);

        //현재 하의 초기화
        Item item = new Item(0, "None", "빈 장착 슬롯", "초기화를 위한 비어있는 하의 아이템 자료형입니다.", "None", 0, 0);
        MyItem myItem = new MyItem(0, 0, 0, 0);
        SetNowEquipment_Bottom(item, myItem);

        // 아이템에 따른 스텟 증감
        PlusPlayerHp(nowEquipment_Bottom.equipment.realHp);
        PlusPlayerAttackDamage(nowEquipment_Bottom.equipment.realDamage);
        PlusPlayerDefense(nowEquipment_Bottom.equipment.realDefense);
        PlusPlayerCriticalDamage(nowEquipment_Bottom.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(nowEquipment_Bottom.equipment.realCriticalRate);
    }

    public void UnequipShoes()
    {
        /*
         * UnequipWeapon 참고
         */

        // 현재 신발 전신발로 설정
        MyItem preMyItem_Shoes = new MyItem(nowEquipment_Shoes.itemID, nowEquipment_Shoes.itemCount, nowEquipment_Shoes.equipment.itemLV, nowEquipment_Shoes.equipment.itemNowExp);
        SetPreEquipment_Shoes(nowEquipment_Shoes, preMyItem_Shoes);
        // 전 신발 스텟 빼기
        PlusPlayerHp(-preEquipment_Shoes.equipment.realHp);
        PlusPlayerAttackDamage(-preEquipment_Shoes.equipment.realDamage);
        PlusPlayerDefense(-preEquipment_Shoes.equipment.realDefense);
        PlusPlayerCriticalDamage(-preEquipment_Shoes.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(-preEquipment_Shoes.equipment.realCriticalRate);

        //현재 신발 초기화
        Item item = new Item(0, "None", "빈 장착 슬롯", "초기화를 위한 비어있는 신발 아이템 자료형입니다.", "None", 0, 0);
        MyItem myItem = new MyItem(0, 0, 0, 0);
        SetNowEquipment_Shoes(item, myItem);

        // 아이템에 따른 스텟 증감
        PlusPlayerHp(nowEquipment_Shoes.equipment.realHp);
        PlusPlayerAttackDamage(nowEquipment_Shoes.equipment.realDamage);
        PlusPlayerDefense(nowEquipment_Shoes.equipment.realDefense);
        PlusPlayerCriticalDamage(nowEquipment_Shoes.equipment.realCriticalDamage);
        PlusPlayerCriticalRate(nowEquipment_Shoes.equipment.realCriticalRate);
    }

    #endregion

#endregion

    //단어장 가져오기
    public Vocabulary[] GetVocabularies()
    {
        Vocabulary[] vocabularies = parser.WordParse(word_CSV_FileName); // 단어장 파싱

        return vocabularies;
    }

    //단어장 가져오기
    public Dialogue[] GetDialogues(string _CSV_FileName)
    {
        Dialogue[] dialogues = parser.DialogueParser(_CSV_FileName); // 단어장 파싱

        return dialogues;
    }

#region Get

    public float GetGold()
    {
        return myPlayerData.gold;
    }

    public int GetLV()
    {
        return myPlayerData.LV;
    }

    public float GetLevelUPExp()
    {
        return myPlayerData.LevelUpExp;
    }

    public float GetNowExp()
    {
        return myPlayerData.nowExp;
    }

    public float GetPlayer_Hp()
    {
        return myPlayerData.hp;
    }

    public float GetPlayer_AttackDamage()
    {
        return myPlayerData.player_AttackDamage;
    }

    public float GetPlayer_Defense()
    {
        return myPlayerData.player_Defense;
    }

    public float GetPlayer_CriticalDamage()
    {
        return myPlayerData.player_CriticalDamage;
    }

    public float GetPlayer_CriticalRate()
    {
        return myPlayerData.player_CriticalRate;
    }
    
    public string GetSelectedStageName()
    {
        return selectedStage;
    }

    public Attribute GetNowWeaponAttribute()
    {
        return nowEquipment_Weapon.equipment.itemAttribute;
    }

#endregion

#region Set

    public void SetGold(float _Gold)
    {
        myPlayerData.gold = _Gold;
        myPlayerData.totalGold = _Gold;

        SaveMyPlayerDataToJson();
    }

    public void SetLV(int _LV)
    {
        myPlayerData.LV = _LV;
        SaveMyPlayerDataToJson();
    }

    public void SetLevelUpExp(float _LevelUpExp)
    {
        myPlayerData.LevelUpExp = _LevelUpExp;
        SaveMyPlayerDataToJson();
    }

    public void SetNowExp(float _NowExp)
    {
        myPlayerData.nowExp = _NowExp;

        SaveMyPlayerDataToJson();
    }

    public void SetPlayer_Hp(float _Hp)
    {
        myPlayerData.hp = _Hp;

        SaveMyPlayerDataToJson();
    }

    public void SetPlayer_AttackDamage(float _AttackDamage)
    {
        myPlayerData.player_AttackDamage = _AttackDamage;

        SaveMyPlayerDataToJson();
    }

    public void SetPlayer_Defense(float _Defense)
    {
        myPlayerData.player_Defense = _Defense;

        SaveMyPlayerDataToJson();
    }

    public void SetPlayer_CriticalDamage(float _CriticalDamage)
    {
        myPlayerData.player_CriticalDamage = _CriticalDamage;

        SaveMyPlayerDataToJson();
    }

    public void SetPlayer_CriticalRate(float _CriticalRate)
    {
        myPlayerData.player_CriticalRate = _CriticalRate;

        SaveMyPlayerDataToJson();
    }

    public void SetMySelectedMagicData(int _Index, int _ChangeID)
    {
        mySelectedMagicData.selectedMagicID[_Index] = _ChangeID;

        SaveMySelectedMagicDataToJson();
    }

#region Set_Quest

    public void SetLastAccessDay(int _LastAccessDay)
    {
        myPlayerData.lastAccessDay = _LastAccessDay;

        SaveMyPlayerDataToJson();
    }

    public void SetIsGetTodayAccessResult(bool _Bool)
    {
        myPlayerData.isGetTodayAccessResult = _Bool;

        SaveMyPlayerDataToJson();
    }

    public void SetTodayRightAnswerCount(int _TodayWord)
    {
        myPlayerData.todayRightAnswerCount = _TodayWord;

        SaveMyPlayerDataToJson();
    }

    public void SetTodayMonsterKill(int _TodayMonsterKill)
    {
        myPlayerData.todayMonsterKill = _TodayMonsterKill;

        SaveMyPlayerDataToJson();
    }

    public void SetTodayUseMagic(int _TodayUseMagic)
    {
        myPlayerData.todayUseMagic = _TodayUseMagic;

        SaveMyPlayerDataToJson();
    }

    public void SetTodayUseGold(float _TodayUseGold)
    {
        myPlayerData.todayUseGold = _TodayUseGold;

        SaveMyPlayerDataToJson();
    }

    public void SetWeekAccessDayCount(int _WeekAccessDayCount)
    {
        myPlayerData.weekAccessDayCount = _WeekAccessDayCount;

        SaveMyPlayerDataToJson();
    }

    public void SetWeekRightAnswerCount(int _WeekRightAnswerCount)
    {
        myPlayerData.weekRightAnswerCount = _WeekRightAnswerCount;

        SaveMyPlayerDataToJson();
    }

    public void SetWeekMonsterKill(int _WeekMosterKill)
    {
        myPlayerData.weekMonsterKill = _WeekMosterKill;

        SaveMyPlayerDataToJson();
    }

    public void SetWeekUseMagic(int _WeekUseMagic)
    {
        myPlayerData.weekUseMagic = _WeekUseMagic;

        SaveMyPlayerDataToJson();
    }

    public void SetWeekUseGold(float _WeekUseGold)
    {
        myPlayerData.weekUseGold = _WeekUseGold;

        SaveMyPlayerDataToJson();
    }

#endregion

    public void SetSelectedStage(string _StageName)
    {
        selectedStage = _StageName;
    }

    // 장비 관련 Set

    public void SetNowEquipment_Weapon(Item _Item, MyItem _MyItem)
    {
        nowEquipment_Weapon = _Item;
        nowEquipment_Weapon.equipment.itemLV = _MyItem.itemLV;
        nowEquipment_Weapon.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetPreEquipment_Weapon(Item _Item, MyItem _MyItem)
    {
        preEquipment_Weapon = _Item;
        preEquipment_Weapon.equipment.itemLV = _MyItem.itemLV;
        preEquipment_Weapon.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetNowEquipment_Head(Item _Item, MyItem _MyItem)
    {
        nowEquipment_Head = _Item;
        nowEquipment_Head.equipment.itemLV = _MyItem.itemLV;
        nowEquipment_Head.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetPreEquipment_Head(Item _Item, MyItem _MyItem)
    {
        preEquipment_Head = _Item;
        preEquipment_Head.equipment.itemLV = _MyItem.itemLV;
        preEquipment_Head.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetNowEquipment_Top(Item _Item, MyItem _MyItem)
    {
        nowEquipment_Top = _Item;
        nowEquipment_Top.equipment.itemLV = _MyItem.itemLV;
        nowEquipment_Top.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetPreEquipment_Top(Item _Item, MyItem _MyItem)
    {
        preEquipment_Top = _Item;
        preEquipment_Top.equipment.itemLV = _MyItem.itemLV;
        preEquipment_Top.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetNowEquipment_Bottom(Item _Item, MyItem _MyItem)
    {
        nowEquipment_Bottom = _Item;
        nowEquipment_Bottom.equipment.itemLV = _MyItem.itemLV;
        nowEquipment_Bottom.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetPreEquipment_Bottom(Item _Item, MyItem _MyItem)
    {
        preEquipment_Bottom = _Item;
        preEquipment_Bottom.equipment.itemLV = _MyItem.itemLV;
        preEquipment_Bottom.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetNowEquipment_Shoes(Item _Item, MyItem _MyItem)
    {
        nowEquipment_Shoes = _Item;
        nowEquipment_Shoes.equipment.itemLV = _MyItem.itemLV;
        nowEquipment_Shoes.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

    public void SetPreEquipment_Shoes(Item _Item, MyItem _MyItem)
    {
        preEquipment_Shoes = _Item;
        preEquipment_Shoes.equipment.itemLV = _MyItem.itemLV;
        preEquipment_Shoes.equipment.itemNowExp = _MyItem.itemNowExp;
        CalculateRealItemStat(_Item, _MyItem);
    }

#endregion

#region Plus

    public void PlusGold(float _Plus)
    {
        myPlayerData.gold += _Plus;
        if (_Plus >= 0)
        {
            myPlayerData.totalGold += _Plus;
        }
        else
        {
            PlusTodayUseGold(-_Plus);
            PlusWeekUseGold(-_Plus);
            myPlayerData.totalUsedGold += -_Plus;
        }

        SaveMyPlayerDataToJson();
    }

    public void PlusNowExp(float _Plus)
    {
        myPlayerData.nowExp += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusPlayerHp(float _Plus)
    {
        myPlayerData.hp += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusPlayerAttackDamage(float _Plus)
    {
        myPlayerData.player_AttackDamage += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusPlayerDefense(float _Plus)
    {
        myPlayerData.player_Defense += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusPlayerCriticalDamage(float _Plus)
    {
        myPlayerData.player_CriticalDamage += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusPlayerCriticalRate(float _Plus)
    {
        myPlayerData.player_CriticalRate += _Plus;

        SaveMyPlayerDataToJson();
    }

#region Plus_Quest

    public void PlusTodayRightAnswerCount(int _Plus)
    {
        myPlayerData.todayRightAnswerCount += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusTodayMonsterKill(int _Plus)
    {
        myPlayerData.todayMonsterKill += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusTodayUseMagic(int _Plus)
    {
        myPlayerData.todayUseMagic += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusTodayUseGold(float _Plus)
    {
        myPlayerData.todayUseGold += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusWeekAccessDayCount(int _Plus)
    {
        myPlayerData.weekAccessDayCount += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusWeekRightAnswerCount(int _Plus)
    {
        myPlayerData.weekRightAnswerCount += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusWeekMonsterKill(int _Plus)
    {
        myPlayerData.weekMonsterKill += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusWeekUseMagic(int _Plus)
    {
        myPlayerData.weekUseMagic += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusWeekUseGold(float _Plus)
    {
        myPlayerData.weekUseGold += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusTotalRightAnwserCount(int _Plus)
    {
        myPlayerData.totalRightAnswerCount += _Plus;

        SaveMyPlayerDataToJson();
    }

    public void PlusTotalMonsterKillCount(int _Plus)
    {
        myPlayerData.totalMonsterKillCount += _Plus;

        SaveMyPlayerDataToJson();
    }

#endregion

#endregion
}
