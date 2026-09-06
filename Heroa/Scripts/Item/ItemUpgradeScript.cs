using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUpgradeScript : MonoBehaviour
{
    public static ItemUpgradeScript instance;

    [SerializeField]
    private GameObject itemupgradeUI;
    [SerializeField]
    private ShowStatScript beforeUpgradeStat, afterUpgradeSta;
    [SerializeField]
    private GameObject itemUpgradeBanPanel;

    private ShowItemInfoScript showItemInfoScript;
    private InventoryScript inventorySc;

    private bool nowUpgrade = false; // 업그레이드중

    public MyItemData selectedMaterialItemDatas; // 재료 아이템

    private Item upgradeItemInfo; // 강화될 아이템 정보
    private MyItem upgradeItemData; // 강화될 아이템
    private float accumulatedItemExp; // 강화에 쓸 축적된 경험치

    private float levelUpExp;

    // 업그레이드 중 레벨업 미리보기 변수들
    private int preLV;
    private float preExp;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        selectedMaterialItemDatas = new MyItemData();
        selectedMaterialItemDatas.myitemData = new List<MyItem>();
        showItemInfoScript = itemupgradeUI.GetComponent<ShowItemInfoScript>();

        inventorySc = InventoryScript.instance;
    }

    // *인벤토리 스크립트에 있어야하는거 아닌가 / 이름 좀 더 명확하게 수정?
    public void OnClickedExitButton()
    {
        if (nowUpgrade == false)
        {
            MainMenuManager.instance.OffAllPanel(); // 인벤토리창 끄기
        }
        else
        {
            // 업그레이드 취소
            accumulatedItemExp = 0f;
            InventoryScript.instance.InitializationLockItem(); // 잠금 아이템 초기화
            InventoryScript.instance.SetIsLock(false);

            selectedMaterialItemDatas.myitemData.Clear();
            InventoryScript.instance.InitializationSlotClicked(); // 클릭된 슬롯 초기화

            inventorySc.equipmentItemPanel.SetActive(true);
            inventorySc.upgradeItemPanel.SetActive(false);
            InventoryScript.instance.OnItemInfoPanel(); // 장비 패널 띄우기
            EquipItemScript.instance.ShowInfoAtItemInfoPanel(); // 아이템 정보 보이기
            InventoryScript.instance.ShowItem(); // 빠진 아이템 빼고 보여주기

            InventoryScript.instance.SetIsUpgrade(false);
            nowUpgrade = false;
        }
    }

    public void OnClickedUpgradeButton()
    {
        nowUpgrade = true;

        upgradeItemInfo = EquipItemScript.instance.GetPreparatoryItem();
        upgradeItemData = EquipItemScript.instance.GetPreparatoryItemData();

        InventoryScript.instance.SetIsLock(true); // 잠금 활성화

        inventorySc.equipmentItemPanel.SetActive(false);
        inventorySc.upgradeItemPanel.SetActive(true);
        
        beforeUpgradeStat.ShowItemStatInfo(upgradeItemInfo, upgradeItemData);
        showItemInfoScript.ShowItemImage(upgradeItemInfo);
        showItemInfoScript.ShowItemLVInfo(upgradeItemInfo, upgradeItemData);

        InventoryScript.instance.OffItemInfoPanel();
        InventoryScript.instance.SetIsUpgrade(true);
    }

    public void UpgradeItem()
    {
        if (selectedMaterialItemDatas.GetMyItemDataCount() <= 0) return;

        /*
        // 재료 제거
        for (int i = 0; i < materialItemDatas.GetMyItemDataCount(); i++)
        {
            InventoryScript.instance.RemoveItem(materialItemDatas.myitemData[i]);
        }
        */

        for (int i = 0; i < selectedMaterialItemDatas.GetMyItemDataCount(); i++)
        {
            InventoryScript.instance.RemoveById(selectedMaterialItemDatas.myitemData[i].uniqueId);
        }

        selectedMaterialItemDatas.myitemData.Clear();
        InventoryScript.instance.SetIsUpgrade(false);

        // 경험치 주기
        upgradeItemData.PlusItemNowExp(accumulatedItemExp);
        accumulatedItemExp = 0f;

        CheckItemLevelUp();

        // 결과/UI 복귀
        EquipItemScript.instance.ReplacePreparatoryItemSet(upgradeItemInfo, upgradeItemData); // 강화된 아이템 전달

        InventoryScript.instance.InitializationLockItem(); // 잠금 아이템 초기화
        InventoryScript.instance.SetIsLock(false);

        inventorySc.equipmentItemPanel.SetActive(true);
        inventorySc.upgradeItemPanel.SetActive(false);
        InventoryScript.instance.OnItemInfoPanel(); // 장비 패널 띄우기
        EquipItemScript.instance.ShowInfoAtItemInfoPanel(); // 아이템 정보 보이기
        InventoryScript.instance.InitializationSlotClicked(); // 클릭된 슬롯 초기화
        InventoryScript.instance.ShowItem(); // 빠진 아이템 빼고 보여주기

        Debug.Log("ItemUpgradeScript : Upgrade Complete");
    }

    public void PreCheckItemLevelUp(int _LV, float _Exp)
    {
        // 업그레이드 중 성장할 경험치와 레벨을 미리 보여주는 기능을 함.
        /* 
         * pre : upgradeItemData.itemLV = preLV, preAccumulatedItemExp = preExp
         * post : 최대 레벨 제한이 안넘는 경우, 경험치와 레벨을 미리 계산(재귀)해서 보여줌.
         */

        preLV = _LV;
        preExp = _Exp;

        int maxLevel = upgradeItemInfo.itemRate * 4;

        while (preLV < maxLevel && preExp >= 100 * preLV)
        {
            preExp -= 100 * preLV;
            preLV++;
        }

        // 최대 레벨 도달 시 경험치 0으로 고정
        if (preLV >= maxLevel)
        {
            preLV = maxLevel;
            preExp = 0f;
        }

        MyItem myItem = new MyItem(upgradeItemData.itemID, 1, preLV, preExp);
        showItemInfoScript.ShowItemLVInfo(upgradeItemInfo, myItem);
        afterUpgradeSta.ShowItemStatInfo(upgradeItemInfo, myItem);
    }

    public void CheckItemLevelUp()
    {
        // 아이템 업그레이드를 하는 함수.

        int maxLevel = upgradeItemInfo.itemRate * 4;

        while (upgradeItemData.itemLV < maxLevel &&
               upgradeItemData.itemNowExp >= 100 * upgradeItemData.itemLV)
        {
            upgradeItemData.itemNowExp -= 100 * upgradeItemData.itemLV;
            upgradeItemData.itemLV++;
        }

        if (upgradeItemData.itemLV >= maxLevel)
        {
            upgradeItemData.itemLV = maxLevel;
            upgradeItemData.itemNowExp = 0f;
        }
    }

    public void AddMaterialItem(MyItem _MyItem)
    {
        selectedMaterialItemDatas.myitemData.Add(_MyItem);
    }

    public void FindRemoveMaterialItem(MyItem _MyItem)
    {
        selectedMaterialItemDatas.FindRemoveMyItemData(_MyItem.itemID, _MyItem.itemCount, _MyItem.itemLV, _MyItem.itemNowExp);
    }

    public void PlusAccumulatedItemExp(float _Plus)
    {
        // 최대 레벨 확인하고 경험치 추가하기
        accumulatedItemExp += _Plus;

        // 경험치 미리 보여주기
        PreCheckItemLevelUp(upgradeItemData.itemLV, upgradeItemData.itemNowExp + accumulatedItemExp); // itemNowExp를 더해서 이전 정보를 불러옴.
    }

}
