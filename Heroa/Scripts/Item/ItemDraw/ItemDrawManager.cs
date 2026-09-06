using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemDrawManager : MonoBehaviour
{
    public static ItemDrawManager instance;

    public GameObject itemImageSet = null;
    public Image itemImage = null;

    public int currentDrawCount;
    public bool isItemImageClosed = false;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void OnItemImageSet()
    {
        itemImageSet.SetActive(true);
    }

    public void OffItemImageSet()
    {
        if (currentDrawCount <= 1)
        {
            itemImageSet.SetActive(false);
        }

        isItemImageClosed = true;
    }
}
