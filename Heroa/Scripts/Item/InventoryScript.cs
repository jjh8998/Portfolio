using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Xml;

public class InventoryTabGroup
{
    public List<MyItem> inventoryTabList; // 탭으로(16개씩) 나눠진 아이템 리스트

    public InventoryTabGroup()
    {
        inventoryTabList = new List<MyItem>();
    }

    public void AddMyItem(MyItem _MyItem)
    {
        inventoryTabList.Add(_MyItem);
    }

    public int GetCountItem()
    {
        return inventoryTabList.Count;
    }
}



public class InventoryScript : MonoBehaviour
{
    const int MAXITEMPERTAB = 16;

    public static InventoryScript instance;

    public Transform tf; // slot 부모객체
    public GameObject itemInfoPanel = null;
    public GameObject equipBtns;
    public GameObject upgradeBtns;
    public TextMeshProUGUI tabNumberText;
    public GameObject equipmentItemPanel;
    public GameObject upgradeItemPanel;

    private DatabaseManager DB;

    [Header("List")]
    public InventorySlotScript[] slots;

    public List<MyItem> inventoryItemList; // 플레이어가 소지한 아이템 리스트
    private List<InventoryTabGroup> inventoryTabGroupList; // 16개로 나눠진 아이템 리스트
    private List<MyItem> nowInventoryTabList; // 선택한 탭에 따라 보여질 리스트

    private int allSelectTab; // 전체 탭
    private int selectedTab; // 선택된 탭
    private int lastSelectTabItemNum; // 마지막 납는 탭 아이템 숫자

    private int lockPage; // 잠글 페이지 , 배열로 하면 여러개 가능?
    private int lockIndex; // 잠글 슬롯 인덱스

    private bool isUpgrade= false;
    private bool isLock = false; // 인벤토리 잠금 활성화

    private InventorySlotScript currentlyClickedSlot = null;

    /*
     * 원활한 인벤토리 코딩을 위해 잠금 기능은 전부 비활성화 해놨음!!!
     */

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        DB = DatabaseManager.instance;

        inventoryItemList = new List<MyItem>();
        inventoryTabGroupList = new List<InventoryTabGroup>();
        nowInventoryTabList = new List<MyItem>();

        if (tf != null)
            slots = tf.GetComponentsInChildren<InventorySlotScript>();
        else
            Debug.LogWarning("InventoryScript - tf is NULL");

        selectedTab = 1 - 1; // 배열이라 -1

        ShowItem();
    }

    public void ShowItem()
    {
        // 초기화
        initializationInventory();
        LoadInventoryItem();

        DivideItemsIntoTabs();
        ShowTabNumber();
        SetCurrentTabItems();
        DisplayTabItems();
    }

    private bool IsFilteredItem(MyItem item)
    {
        return item.itemID == 1 || item.itemID == 2;
    }

    private void DivideItemsIntoTabs()
    {
        InventoryTabGroup currentGroup = new InventoryTabGroup();

        for (int i = 0; i < inventoryItemList.Count; i++)
        {
            MyItem item = inventoryItemList[i];

            if (IsFilteredItem(item)) continue;

            currentGroup.AddMyItem(item);

            if (currentGroup.GetCountItem() == MAXITEMPERTAB)
            {
                inventoryTabGroupList.Add(currentGroup);
                currentGroup = new InventoryTabGroup();
            }
        }

        if (currentGroup.GetCountItem() > 0)
        {
            inventoryTabGroupList.Add(currentGroup);
        }

        allSelectTab = inventoryTabGroupList.Count - 1;
    }

    private void SetCurrentTabItems()
    {
        if (inventoryTabGroupList.Count == 0)
        {
            nowInventoryTabList.Clear();
            lastSelectTabItemNum = 0;
            return;
        }

        nowInventoryTabList = inventoryTabGroupList[selectedTab].inventoryTabList;
        lastSelectTabItemNum = nowInventoryTabList.Count;
    }

    private void DisplayTabItems()
    {
        // 인벤토리 탭 리스트를 인벤토리에 추가
        RemoveSlots();

        for (int i = 0; i < nowInventoryTabList.Count; i++)
        {
            slots[i].gameObject.SetActive(true);
            slots[i].AddItem(nowInventoryTabList[i], i);

            if (ItemUpgradeScript.instance != null && ItemUpgradeScript.instance.selectedMaterialItemDatas.myitemData
                    .Exists(m => m != null && m.uniqueId == nowInventoryTabList[i].uniqueId))
            {
                slots[i].SetClickedSet(true);
            }
        }
    }

    public void ShowAlphabetCount()
    {
        // show alphabet count
    }

    public void initializationInventory()
    {
        //인벤토리 초기화

        nowInventoryTabList.Clear();
        inventoryTabGroupList.Clear();
        RemoveSlots();

        InitializationSlotClicked();
    }

    public void RemoveSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].RemoveItemInSlot();
            slots[i].gameObject.SetActive(false);
        }
    }

    // 저장된 아이템 불러오기
    public void LoadInventoryItem()
    {
        inventoryItemList.Clear();

        for (int i = 0; i <DB.myItemInInventoryData.GetMyItemDataCount(); i++)
        {
            inventoryItemList.Add(DB.myItemInInventoryData.myitemData[i]);

            /*
            for (int j = 0; j < databaseManager.itemList.Count; j++)
            {
                if (databaseManager.myItemInInventoryData.GetItemID(i) == databaseManager.itemList[j].itemID)
                {
                    inventoryItemList.Add(databaseManager.itemList[j]);
                }
            }
            */
        }
    }

    public void GetAnItem(MyItem _MyItem, int _Count = 1)
    {
        /*
         * Precondition : MyItem, 아이템 갯수
         * 
         * Postcondition
         * 중복 가능여부를 따져 inventoryItemList에 아이템 추가.
         * 데이터베이스 MyItemData에 아이템 저장.
         * 아이템데이터 세이브.
         */

        if (_MyItem != null)
        {
            Debug.Log("get Item" + _MyItem.itemID);
            for (int i = 0; i < DatabaseManager.instance.itemList.Count; i++)
            {
                if (_MyItem.itemID == DatabaseManager.instance.itemList[i].itemID)
                {
                    // 중복 불가능한 아이템 흭득
                    if (DatabaseManager.instance.itemList[i].countable == false)
                    {
                        // inventoryItemList.Add(_MyItem);

                        DatabaseManager.instance.myItemInInventoryData.AddMyItemData(_MyItem); // 아이템 저장
                        DatabaseManager.instance.SavePlayerItemDataToJson();

                        return;
                    }
                    else
                    {
                        // 중복가능한 아이템은 중복해서 아이템 주기

                        // for (int j = 0; j < inventoryItemList.Count; j++)
                        int j;
                        for (j = 0; j < DatabaseManager.instance.myItemInInventoryData.GetMyItemDataCount() ; j++)
                        {
                            // Debug.Log("get Countable Item");
                            // 아이템이 이미 있다면
                            if (_MyItem.itemID == DatabaseManager.instance.myItemInInventoryData.GetItemID(j))
                            {
                                // inventoryItemList[j].itemCount += _Count; << inventoryitemlist를 왜 여기서 쓰는지 모르겠음; 그리고 이게 왜 데이터에 영향이 가는지도
                                DatabaseManager.instance.myItemInInventoryData.PlusMyItemDataCount(_MyItem.itemID, _Count);
                                DatabaseManager.instance.SavePlayerItemDataToJson();

                                Debug.Log("InventoryScript : " + _MyItem.itemID + "아이템 " + _Count + "추가 후 저장");
                                return;
                            }
                        }

                        // 아이템이 없다면
                        // inventoryItemList.Add(_MyItem);
                        // inventoryItemList[j].itemCount = _Count;

                        DatabaseManager.instance.myItemInInventoryData.AddMyItemData(_MyItem); // 아이템 저장
                        DatabaseManager.instance.myItemInInventoryData.PlusMyItemDataCount(_MyItem.itemID, _Count - 1); // 이미 하나 생성했기에 -1
                        DatabaseManager.instance.SavePlayerItemDataToJson();

                        Debug.Log("InventoryScript : " + _MyItem.itemID + "아이템 생성 후 " + _Count + "추가 후 저장");

                        return;
                    }
                }
            }
            Debug.LogError("InventoryScript : 데이터 베이스에 아이템 없음!");
        }
        else
        {
            Debug.LogError("InventoryScript : GetAnItem - MyItem is NULL");
        }
    }

    public void RemoveItem(MyItem _MyItem, int _Count = 1)
    {
        /*
         * Precondition
         * MyItem
         * 아이템 갯수
         * 
         * Postcondition
         * inventoryItemList에서 아이템 삭제.
         * 데이터베이스 MyItemData에서 아이템 삭제.
         * 아이템데이터 세이브.
         */

        for (int i = 0; i < DatabaseManager.instance.itemList.Count; i++)
        {
            if (_MyItem.itemID == DatabaseManager.instance.itemList[i].itemID)
            {
                // 중복가능
                if (DatabaseManager.instance.itemList[i].countable == true)
                {
                    for (int j = 0; j < inventoryItemList.Count; j++)
                    {
                        // 아이템이 이미 있다면
                        if (_MyItem.itemID == DatabaseManager.instance.myItemInInventoryData.GetItemID(j))
                        {
                            // inventoryItemList[j].itemCount -= _Count;
                            DatabaseManager.instance.myItemInInventoryData.PlusMyItemDataCount(_MyItem.itemID, -_Count);
                            DatabaseManager.instance.SavePlayerItemDataToJson();

                            Debug.Log("InventoryScript : " + _MyItem.itemID + "아이템 " + _Count + "제거 후 저장");
                            return;
                        }
                    }
                }
                else
                {
                    // 중복 불가능
                    FindRemoveItemInInventory(_MyItem);
                    DatabaseManager.instance.myItemInInventoryData.FindRemoveMyItemData(_MyItem.itemID, _MyItem.itemCount, _MyItem.itemLV, _MyItem.itemNowExp); // 아이템 삭제

                    DatabaseManager.instance.SavePlayerItemDataToJson();
                    return;
                }
            }
        }
        Debug.LogError("InventoryScript : 데이터 베이스에 아이템 없음!");
    }

    public void FindRemoveItemInInventory(MyItem _MyItem, int _Count = 1)
    {
        for (int i = 0; i < inventoryItemList.Count; i++)
        {
            if(inventoryItemList[i] == _MyItem)
            {
                inventoryItemList.RemoveAt(i);
                return; // 동일한 거 여러개 안지우고 바로 함수 종료
            }
        }
    }

    public void ReplaceItemInInventory(MyItem _OriginalMyItem, MyItem _ReplaceMyItem)
    {
        for (int i = 0; i < inventoryItemList.Count; i++)
        {
            if (inventoryItemList[i] == _OriginalMyItem)
            {
                inventoryItemList[i] = _ReplaceMyItem;
                DB.SavePlayerItemDataToJson();
                return; // 동일한 거 여러개 안지우고 바로 함수 종료
            }
        }
    }

    public void InitializationSlotClicked()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetClickedSet(false);
        }
    }

    public void InitializationLockItem()
    {
        // * 페이지 확인 필요?
        /*
        slots[lockIndex].UnlockSlot();

        lockPage = -1;
        lockIndex = -1;

        */

        Debug.Log("inventoryScript : 현재 잠금 관련 스크립트는 전부 주석 처리");
    }

    public void CheckLockSlot(int _Page, int _Index)
    {
        /*
        if (isLock == true)
        {
            if (_Page == lockPage)
            {
                if (_Index == lockIndex)
                {
                    slots[_Index].LockSlot();
                    Debug.Log("InventoryScript : LockSlot Index - " + _Index);
                    return;
                }
            }

            slots[_Index].UnlockSlot();
        }

        */

        Debug.Log("inventoryScript : 현재 잠금 관련 스크립트는 전부 주석 처리");
    }

    public void ShowTabNumber()
    {
        tabNumberText.text = (selectedTab + 1) + "/" + (allSelectTab + 1);
    }

    public void SetCurrentlyClickedSlot(InventorySlotScript slot)
    {
        // 이전 선택된 슬롯이 있다면 해제
        if (currentlyClickedSlot != null && currentlyClickedSlot != slot)
        {
            currentlyClickedSlot.SetClickedSet(false);
        }

        currentlyClickedSlot = slot;
    }

    public void ClearCurrentlyClickedSlot()
    {
        if (currentlyClickedSlot != null)
        {
            currentlyClickedSlot.SetClickedSet(false);
            currentlyClickedSlot = null;
        }
    }

    public bool RemoveById(string id)
    {
        // 1) 인벤토리 아이템 리스트에서 검색
        for (int i = 0; i < inventoryItemList.Count; i++)
        {
            if (inventoryItemList[i].uniqueId == id)
            {
                MyItem target = inventoryItemList[i];

                // DatabaseManager의 myItemInInventoryData에서도 동일한 아이템 삭제
                DatabaseManager.instance.myItemInInventoryData.FindRemoveMyItemData(
                    target.itemID, target.itemCount, target.itemLV, target.itemNowExp);

                // 인벤토리 리스트에서도 삭제
                inventoryItemList.RemoveAt(i);

                // 세이브 반영
                DatabaseManager.instance.SavePlayerItemDataToJson();

                Debug.Log($"RemoveByUniqueId : 아이템 {id} 삭제 성공");
                return true;
            }
        }

        Debug.LogWarning($"RemoveByUniqueId : uniqueId {id} 를 찾을 수 없음");
        return false;
    }

    #region Button

    public void LeftButton()
    {
        if (selectedTab != 0)
        {
            selectedTab -= 1;

            InitializationSlotClicked();
            ShowItem();
            ShowTabNumber();
        }
    }

    public void RightButton()
    {
        if (selectedTab < allSelectTab)
        {
            selectedTab += 1;

            InitializationSlotClicked();
            ShowItem();
            ShowTabNumber();
        }
    }

    #endregion

    #region On/Off

    public void OnItemInfoPanel()
    {
        itemInfoPanel.SetActive(true);
    }

    public void OffItemInfoPanel()
    {
        for (int i = 0; i < nowInventoryTabList.Count; i++)
        {
            CheckLockSlot(selectedTab, i);
        }

        itemInfoPanel.SetActive(false);
    }

    public void OnEquipBtns()
    {
        equipBtns.SetActive(true);
    }

    public void OffEquipBtns()
    {
        equipBtns.SetActive(false);
    }

    public void OnUpgradeBtns()
    {
        upgradeBtns.SetActive(true);
    }
    public void OffUpgradeBtns()
    {
        upgradeBtns.SetActive(false);
    }

    #endregion

    #region Get

    public bool GetIsUpgrade()
    {
        return isUpgrade;
    }

    public int GetSelectedTab()
    {
        return selectedTab;
    }

    public int GetLockPage()
    {
        return lockPage;
    }

    public int GetLockIndex()
    {
        // Debug.Log("InventoryScript : lockIndex : " + lockIndex);
        return lockIndex;
    }

    #endregion

    #region Set

    public void SetIsUpgrade(bool _IsUpgrade)
    {
        isUpgrade = _IsUpgrade;
    }
    
    public void SetIsLock(bool _Bool)
    {
        isLock = _Bool;
    }

    public void SetLockIte(int _Index)
    {
        lockPage = selectedTab;
        lockIndex = _Index;
    }

    #endregion
}
