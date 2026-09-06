using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipItemScript : MonoBehaviour
{

    public static EquipItemScript instance;

    private const int WEAPON = 0, HEAD = 1, TOP = 2, BOTTOM = 3, SHOES = 4;

    public InventorySlotScript[] slots;

    [SerializeField]
    private InventoryScript inventoryScript;
    [SerializeField]
    private GameObject itemInfoPanel;
    [SerializeField]
    private GameObject equipmentGridObj;
    [SerializeField]
    private GameObject equipmentBackgroundObj;
    public ShowStatScript showStatScriptInEquipment;

    private List<Item> equipmentItemList;

    private DatabaseManager DB;
    private ShowStatScript showStatScriptInItemInfo;
    private ShowItemInfoScript showItemInfoScript;

    private Item preparatoryItem; // 장착 예비 아이템
    private MyItem preparatoryItemData; // 장착 예비 아이템 데이터

    #region equiped items

    private MyItem equipedWeaponItem;
    private MyItem equipedHeadItem;
    private MyItem equipedTopItem;
    private MyItem equipedBottomItem;
    private MyItem equipedShoesItem;

    #endregion

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        equipmentItemList = new List<Item>();
        slots = equipmentGridObj.GetComponentsInChildren<InventorySlotScript>();
        DB = DatabaseManager.instance;

        showStatScriptInItemInfo = itemInfoPanel.GetComponent<ShowStatScript>();
        showItemInfoScript = itemInfoPanel.GetComponent<ShowItemInfoScript>();

        equipedWeaponItem = new MyItem(0);
        equipedHeadItem = new MyItem(0);
        equipedTopItem = new MyItem(0);
        equipedBottomItem = new MyItem(0);
        equipedShoesItem = new MyItem(0);

        DB.SynchroEquipment();
    }

    public void ShowInfoAtItemInfoPanel()
    {
        // 아이템 클릭시 정보창에서 정보보이기

        showStatScriptInItemInfo.ShowItemStatInfo(preparatoryItem, preparatoryItemData);
        showItemInfoScript.ShowItemImage(preparatoryItem);
        showItemInfoScript.ShowItemRate(preparatoryItem, false);
        showItemInfoScript.ShowItemStringInfo(preparatoryItem);
        showItemInfoScript.ShowItemLVInfo(preparatoryItem, preparatoryItemData);
    }

    public void EquipAnItem()
    {
        SynchroEquipmentItem();

        switch (preparatoryItem.itemType)
        {
            case Item.ItemType.Weapon:
                EquipToSlot(WEAPON);
                break;

            case Item.ItemType.Head:
                EquipToSlot(HEAD);
                break;

            case Item.ItemType.Top:
                EquipToSlot(TOP);
                break;

            case Item.ItemType.Bottom:
                EquipToSlot(BOTTOM);
                break;

            case Item.ItemType.Shoes:
                EquipToSlot(SHOES);
                break;
        }

        DB.SwitchItem(preparatoryItemData);


        // 작업 종료 후에는 재사용 방지를 위해 반드시 리셋
        preparatoryItem = null;
        preparatoryItemData = default;

        DatabaseManager.instance.SavePlayerEquipmentDataToJson();
        InventoryScript.instance.ShowItem();
        showStatScriptInEquipment.ShowPlayerStatInfo();
        InventoryScript.instance.OffItemInfoPanel();
    }

    private void EquipToSlot(int _SlotIndex)
    {
        ref MyItem equipped = ref GetRefEquipped(_SlotIndex);

        if (equipped.itemID == 0)
        {
            // 슬롯 비어있으면
            DB.myItemInEquipmentData.RemoveAtMyItemData(_SlotIndex); // 장비창 아이템 삭제
            equipped = preparatoryItemData;
            slots[_SlotIndex].AddItem(equipped, 0);
            DB.myItemInEquipmentData.InsertMyItemData(_SlotIndex, equipped);
            InventoryScript.instance.RemoveItem(equipped);
        }
        else
        {
            // 아이템 교체
            // 아이템 해제
            DB.myItemInEquipmentData.RemoveAtMyItemData(_SlotIndex); // 장비창 아이템 삭제
            inventoryScript.GetAnItem(equipped); // 인벤토리에 아이템 추가

            // 아이템 장착
            equipped = preparatoryItemData; // 선택된 아이템으로 교체
            slots[_SlotIndex].AddItem(equipped, 0); // 장비창에 아이템 추가
            DB.myItemInEquipmentData.InsertMyItemData(_SlotIndex, equipped);
            InventoryScript.instance.RemoveItem(equipped);
        }
    }

    public void UnequipAnItem()
    {
        int index = -1;
        switch (preparatoryItem.itemType)
        {
            case Item.ItemType.Weapon:
                index = 0;
                DatabaseManager.instance.UnequipWeapon();
                break;
            case Item.ItemType.Head:
                index = 1;
                DatabaseManager.instance.UnequipHead();
                break;
            case Item.ItemType.Top:
                index = 2;
                DatabaseManager.instance.UnequipTop();
                break;
            case Item.ItemType.Bottom:
                index = 3;
                DatabaseManager.instance.UnequipBottom();
                break;
            case Item.ItemType.Shoes:
                index = 4;
                DatabaseManager.instance.UnequipShoes();
                break;
        }

        // 장비창 아이템 삭제
        DB.myItemInEquipmentData.RemoveAtMyItemData(index);
        slots[index].RemoveItemInEquipment();
        DB.myItemInEquipmentData.InsertMyItemData(index, new MyItem(0, 0, 0, 0)); // 빈칸 추가

        InventoryScript.instance.GetAnItem(preparatoryItemData); // 인벤토리에 장비 아이템 추가

        MyItem myItem = new MyItem(0, 0, 0, 0);
        DatabaseManager.instance.SwitchItem(myItem);

        DatabaseManager.instance.SavePlayerEquipmentDataToJson();
        InventoryScript.instance.ShowItem();
        showStatScriptInEquipment.ShowPlayerStatInfo();
        InventoryScript.instance.OffItemInfoPanel();
    }

    public void ReplacePreparatoryItemSet(Item _ReplaceItem, MyItem _ReplaceMyItem)
    {
        InventoryScript.instance.ReplaceItemInInventory(preparatoryItemData, _ReplaceMyItem); // 기존 아이템 교체

        // 아이템 교체
        preparatoryItem = _ReplaceItem;
        preparatoryItemData = _ReplaceMyItem;
    }

    public void SynchroEquipmentItem()
    {
        // Database에 있는 equipment데이터와 이 스크립트의 equipment 데이터를 동기화

        var data = DB.myItemInEquipmentData.myitemData;

        equipedWeaponItem = data[0];
        equipedHeadItem = data[1];
        equipedTopItem = data[2];
        equipedBottomItem = data[3];
        equipedShoesItem = data[4];
    }

    #region On/Off

    public void OnEquipmentGridObj()
    {
        equipmentGridObj.SetActive(true);
    }
    public void OffEquipmentGridObj()
    {
        equipmentGridObj.SetActive(false);
    }

    public void OnEquipmentBackgroundObj()
    {
        equipmentBackgroundObj.SetActive(true);
    }

    public void OffEquipmentBackgroundObj()
    {
        equipmentBackgroundObj.SetActive(false);
    }

    public void OnEquipmentObjs()
    {
        // 장비창에 관련된 모든 obj를 활성화

        equipmentBackgroundObj.SetActive(true);
        equipmentGridObj.SetActive(true);
    }

    public void OffEquipmentObjs()
    {
        // 장비창에 관련된 모든 obj를 비활성화

        equipmentBackgroundObj.SetActive(false);
        equipmentGridObj.SetActive(false);
    }

    #endregion

    #region Get

    private ref MyItem GetRefEquipped(int _Index)
    {
        // ref return: 각 필드를 직접 반환 (C# 7+)
        switch (_Index)
        {
            case 0: return ref equipedWeaponItem;
            case 1: return ref equipedHeadItem;
            case 2: return ref equipedTopItem;
            case 3: return ref equipedBottomItem;
            case 4: return ref equipedShoesItem;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(_Index), _Index, null);
        }
    }

    public Item GetPreparatoryItem()
    {
        return preparatoryItem;
    }

    public MyItem GetPreparatoryItemData()
    {
        return preparatoryItemData;
    }

    #endregion

    #region Set

    public void SetPreparatoryItem(Item _Item)
    {
        preparatoryItem = _Item;
    }

    public void SetPreparatoryItemData(MyItem _MyItem)
    {
        preparatoryItemData = _MyItem;
    }

    public void SetEquipedWeaponItem(MyItem _MyItem)
    {
        equipedWeaponItem = _MyItem;
    }

    public void SetEquipedHeadItemID(int _ID)
    {
        equipedHeadItem.itemID = _ID;
    }
    public void SetEquipedTopItemID(int _ID)
    {
        equipedTopItem.itemID = _ID;
    }
    public void SetEquipedBottomItemID(int _ID)
    {
        equipedBottomItem.itemID = _ID;
    }
    public void SetEquipedShoesItemID(int _ID)
    {
        equipedShoesItem.itemID = _ID;
    }

    #endregion
}
