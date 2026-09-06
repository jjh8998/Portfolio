using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DrawItemGroup
{
    public int groupProbability;
    public List<int> itemIds = new List<int>();
}

public class ItemDrawScript : MonoBehaviour
{
    [SerializeField]
    private float drawPrice = 10f;
    [SerializeField]
    private int maxProbability = 100; // 확률
    [SerializeField]
    private GameObject drawPanel;


    public GameObject effectPanel;

    [Header("DrawInfoes")]
    public List<DrawItemGroup> drawItemGroups;

    private bool isDrawing = false;
    private int drawCount = 1;
    private int probability = 0;

    private ShowItemInfoScript showItemInfoScript;
    private ShowPlayerInfoScript showPlayerInfoScriptInDrawPanel;



    // Start is called before the first frame update
    void Start()
    {
        showPlayerInfoScriptInDrawPanel = drawPanel.GetComponent<ShowPlayerInfoScript>();

        CheckIntergrity();
    }

    public bool CheckIntergrity()
    {
        if (drawItemGroups == null || drawItemGroups.Count == 0) return false;

        int total = 0;
        foreach (var g in drawItemGroups)
        {
            if (g.groupProbability <= 0) return false;
            if (g.itemIds == null || g.itemIds.Count == 0) return false;
            total += g.groupProbability;
        }

        return total == maxProbability;
    }

    public IEnumerator RandomItemDraw()
    {
        for (int draw = drawCount; draw > 0; draw--)
        {
            probability = Random.Range(1, maxProbability + 1); // float은 최댓값 포함
            int nowGroupProbability = 0;
            Debug.Log("ItemDrawScript : probability - " + probability);

            for (int i = 0; i < drawItemGroups.Count; i++)
            {
                ItemDrawManager.instance.isItemImageClosed = false;

                nowGroupProbability += drawItemGroups[i].groupProbability;
                if (probability <= nowGroupProbability)
                {
                    int random = Random.Range(0, drawItemGroups[i].itemIds.Count);

                    MyItem myItem = new MyItem(drawItemGroups[i].itemIds[random]);
                    InventoryScript.instance.GetAnItem(myItem);
                    ShowItemImage(myItem.itemID);

                    showPlayerInfoScriptInDrawPanel.ShowPlayerInfo(); // 하드코딩 수정 필요

                    ItemDrawManager.instance.currentDrawCount = draw;
                    yield return new WaitUntil(() => ItemDrawManager.instance.isItemImageClosed == true);

                    break;
                }
            }
        }
    }

    public void OnClicked_1_ItemDraw()
    {
        if (isDrawing) return;

        if (DatabaseManager.instance.GetGold() >= drawPrice)
        {
            if (CheckIntergrity() == false)
            {
                Debug.LogError("ItemDrawScript : Intergrity Error");
                return;
            }

            isDrawing = true;
            drawCount = 1;
            DatabaseManager.instance.PlusGold(-drawPrice);
            StartCoroutine(ShakeAndDrawCoroutine());
        }
    }

    public void OnClicked_10_ItemDraw()
    {
        if (isDrawing) return;

        if (DatabaseManager.instance.GetGold() >= drawPrice * 10)
        {
            if (CheckIntergrity() == false)
            {
                Debug.LogError("ItemDrawScript : Intergrity Error");
                return;
            }

            isDrawing = true;
            drawCount = 10;
            DatabaseManager.instance.PlusGold(-drawPrice * 10);
            StartCoroutine(ShakeAndDrawCoroutine());
        }
    }

    public IEnumerator ShakeAndDrawCoroutine()
    {
        Debug.Log("ItemDrawScript : drawCount - " + drawCount);

        effectPanel.SetActive(true);
        effectPanel.GetComponent<EffectPanelScript>().SetChest();
        Animator boxAnimator = effectPanel.GetComponent<EffectPanelScript>().GetNowChestAnimator();

        boxAnimator.SetTrigger("isShaking");

        yield return null; // 다음 프레임까지 대기

        // 현재 애니메이션 상태가 ChestShakeEffect_Ani가 될 때까지 대기
        yield return new WaitUntil(() =>
            boxAnimator.GetCurrentAnimatorStateInfo(0).IsName("ChestShakeEffect_Ani"));

        yield return null; // 다음 프레임까지 대기

        // 애니메이션이 끝날 때까지 대기
        yield return new WaitUntil(() =>
            boxAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));

        effectPanel.SetActive(false);
        isDrawing = false;
        StartCoroutine(RandomItemDraw()); // 애니 끝나고 실행
    }

    public void ShowItemImage(int _itemId)
    {
        for (int i = 0; i < DatabaseManager.instance.itemList.Count; i++)
        {
            if (_itemId == DatabaseManager.instance.itemList[i].itemID)
            {
                ItemDrawManager.instance.itemImage.sprite = DatabaseManager.instance.itemList[i].itemSprite;

                ItemDrawManager.instance.OnItemImageSet();

                showItemInfoScript = ItemDrawManager.instance.itemImageSet.GetComponent<ShowItemInfoScript>();

                if (showItemInfoScript == null)
                    Debug.LogError("ShowItemInfoScript is NULL");

                showItemInfoScript.ShowItemStringInfo(DatabaseManager.instance.itemList[i]);
                showItemInfoScript.ShowItemRate(DatabaseManager.instance.itemList[i], true); // 등급 나타내기
            }
        }
    }
}
