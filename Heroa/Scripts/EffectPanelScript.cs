using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectPanelScript : MonoBehaviour
{
    public DrawSelectScript dsScript;

    public GameObject woodBox;
    public GameObject woodChest;
    public GameObject silverChest;
    public GameObject goldChest;

    public string nowActive;

    public void SetAllChestActive(bool _bool)
    {
        woodBox.SetActive(_bool);
        woodChest.SetActive(_bool);
        silverChest.SetActive(_bool);
        goldChest.SetActive(_bool);
    }

    public void SetChest()
    {
        if (dsScript == null)
            return;

        SetAllChestActive(false);

        nowActive = dsScript.GetNowActivePanelToString();

        switch (nowActive)
        {
            case "woodBoxDrawPanel":
                woodBox.SetActive(true);
                break;
            case "woodChestDrawPanel":
                woodChest.SetActive(true);
                break;
            case "silverChestDrawPanel":
                silverChest.SetActive(true);
                break;
            case "goldChestDrawPanel":
                goldChest.SetActive(true);
                break;
        }
    }
    
    public Animator GetNowChestAnimator()
    {
        switch (nowActive)
        {
            case "woodBoxDrawPanel":
                return woodBox.GetComponent<Animator>();
            case "woodChestDrawPanel":
                return woodChest.GetComponent<Animator>();
            case "silverChestDrawPanel":
                return silverChest.GetComponent<Animator>();
            case "goldChestDrawPanel":
                return goldChest.GetComponent<Animator>();

                default: return null;
        }
    }
}
