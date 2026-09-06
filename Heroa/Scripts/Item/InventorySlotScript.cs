using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotScript : MonoBehaviour
{

    // 아이템 없는데 버튼 눌리는거 해결
    // 버튼 복수개 눌리는거 해결

    public Image icon;

    [SerializeField]
    private GameObject bg_Image = null; // backgroubd Image
    [SerializeField]
    private TextMeshProUGUI LVText;

    private Item itemInSlot;
    private MyItem myItemDataInSlot;
    private int slotIndex; // 몇번째 슬롯인지
    private bool isClicked = false;
    private bool isLockSlot = false;

    // * 장비창 슬롯은 인덱스 안받아오 되지 않음?
    public void AddItem(MyItem _MyItem, int _Index)
    {
        for (int i = 0; i < DatabaseManager.instance.itemList.Count; i++)
        {
            if (_MyItem.itemID == DatabaseManager.instance.itemList[i].itemID)
            {
                Item item = DatabaseManager.instance.itemList[i];
                myItemDataInSlot = _MyItem;
                itemInSlot = item;
                icon.sprite = item.itemSprite;
                LVText.text = "LV. " + _MyItem.itemLV.ToString();
                slotIndex = _Index;

                SetBackgroundColorFromRate(item.itemRate);
            }
        }
    }

    public void RemoveItemInSlot()
    {
        icon.sprite = null;
        itemInSlot = null;
        myItemDataInSlot = null;
        this.gameObject.SetActive(false);
    }

    public void RemoveItemInEquipment()
    {
        icon.sprite = null;
        itemInSlot = null;
        myItemDataInSlot = null;
    }

    public void ClickItemInInventory()
    {
        /*
         * 만약 업그레이드 중이면, 최초 선택시 재료 아이템 & (등급에 따라 지정된) 강화 경험치로 추가 / 다시 선택시 강화 재료에서 뺀다.
         * 아무 상태도 아니라면 equipmentScript에 장비 정보를 넘기고 장비 정보 패널을 띄운다.
         */

        if (myItemDataInSlot == null || myItemDataInSlot.itemID == 0) return;

        // 업그레이드 재료 선택
        if (InventoryScript.instance.GetIsUpgrade() == true)
        {
            if (itemInSlot.itemID == 3) return;

            // 강화 대상 아이템은 재료로 사용할 수 없도록 차단
            var upgradeTarget = EquipItemScript.instance.GetPreparatoryItemData();

            if (upgradeTarget != null)
            {
                bool isSameRef = object.ReferenceEquals(myItemDataInSlot, upgradeTarget);
                if (isSameRef)
                {
                    return;
                }
            }

            // 잠긴 슬롯이 아니라면
            if (isLockSlot == false)
            {
                float exp = GetExpFromRate(itemInSlot.itemRate);

                // 한번 누른 상태면
                if (isClicked == true)
                {
                    // 업그레이드 재료에서 빼기
                    ItemUpgradeScript.instance.FindRemoveMaterialItem(myItemDataInSlot);
                    ItemUpgradeScript.instance.PlusAccumulatedItemExp(-exp);

                    isClicked = false;
                    bg_Image.SetActive(false);
                }
                else
                {
                    // 업그레이드 재료로 추가
                    ItemUpgradeScript.instance.AddMaterialItem(myItemDataInSlot);
                    ItemUpgradeScript.instance.PlusAccumulatedItemExp(exp);

                    isClicked = true;
                    bg_Image.SetActive(true);
                }
            }
        }
        // 그냥 선택
        else
        {
            // 한번 누른 상태면
            if (isClicked == true)
            {
                InventoryScript.instance.ClearCurrentlyClickedSlot();

                // 슬롯에 있는 아이템 전달
                EquipItemScript.instance.SetPreparatoryItem(itemInSlot);
                EquipItemScript.instance.SetPreparatoryItemData(myItemDataInSlot);

                // 만약 인벤토리에 들어간 아이템이라면 추가해야함

                // InventoryScript.instance.SetLockItem(slotIndex); // 이 아이템 잠금
                Debug.Log("InventorySlotScript : 현재 잠금 관련 스크립트는 전부 주석 처리");

                InventoryScript.instance.OnItemInfoPanel(); // 장비 패널 띄우기
                // 업글 아이템일때
                InventoryScript.instance.OnUpgradeBtns();
                InventoryScript.instance.OffEquipBtns();
                EquipItemScript.instance.ShowInfoAtItemInfoPanel(); // 아이템 정보 보이기
                SetClickedSet(false);
            }
            else
            {
                InventoryScript.instance.SetCurrentlyClickedSlot(this);
                SetClickedSet(true);
            }
        }
    }

    // * 장비창아이템 전체잠그기
    public void ClickItemInEquipment()
    {
        if (myItemDataInSlot == null || myItemDataInSlot.itemID == 0) return;

        // 한번 누른 상태면
        if (isClicked == true)
        {
            // 슬롯에 있는 아이템 전달
            EquipItemScript.instance.SetPreparatoryItem(itemInSlot);
            EquipItemScript.instance.SetPreparatoryItemData(myItemDataInSlot);

            // InventoryScript.instance.SetLockItem(slotIndex); // 이 아이템 잠금
            Debug.Log("ItemUpgardeScript : 현재 잠금 관련 스크립트는 전부 주석 처리");

            InventoryScript.instance.OnItemInfoPanel(); // 장비 패널 띄우기
            // 장비 아이템일때
            InventoryScript.instance.OnEquipBtns();
            InventoryScript.instance.OffUpgradeBtns();
            EquipItemScript.instance.ShowInfoAtItemInfoPanel(); // 아이템 정보 보이기
            SetClickedSet(false);
        }
        else
        {
            SetClickedSet(true);
        }
    }

    public void LockSlot()
    {
        bg_Image.SetActive(true);
        bg_Image.GetComponent<Image>().color = Color.yellow;
        isLockSlot = true;
    }

    public void UnlockSlot()
    {
        bg_Image.SetActive(true);
        bg_Image.GetComponent<Image>().color = Color.green;
        bg_Image.SetActive(false);
        isLockSlot = false;
    }

    public Item GetItemInSlot()
    {
        return itemInSlot;
    }

    public MyItem GetMyItemData()
    {
        return myItemDataInSlot;
    }

    private static float GetExpFromRate(int rate)
    {
        switch (rate)
        {
            case 1: return 10f;
            case 2: return 25f;
            case 3: return 50f;
            case 4: return 100f;
            case 5: return 200f;
            default: return 0f;
        }
    }

    public void SetClickedSet(bool _Bool)
    {
        isClicked = _Bool;
        bg_Image.SetActive(_Bool);
    }

    private void SetBackgroundColorFromRate(int rate)
    {
        Color color;

        switch (rate)
        {
            case 1: color = Color.white; break;
            case 2: color = Color.green; break;
            case 3: color = Color.blue; break;
            case 4: color = Color.magenta; break;
            case 5: color = Color.yellow; break;
            default: color = Color.white; break;
        }

        GetComponent<Image>().color = color;
    }
}
