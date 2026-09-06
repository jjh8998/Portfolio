using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawTutorialScript : MonoBehaviour
{
    public ItemDrawScript idScript;

    public void OnClickedDrawTutorialButton()
    {
        idScript.ShakeAndDrawCoroutine();

        MyItem myItem = new MyItem(3);
        InventoryScript.instance.GetAnItem(myItem);
        idScript.ShowItemImage(3);
    }
}
