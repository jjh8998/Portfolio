using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MagicPanelButtonActivator : MonoBehaviour
{
    public Button magicButton;

    // Start is called before the first frame update
    void Start()
    {
        CheckButton();
    }

    public void CheckButton()
    {
        if (DatabaseManager.instance.myStageProgressData.isStage_1_Clear == true)
        {
            magicButton.image.color = Color.white;
        }
        else
        {
            magicButton.image.color = Color.gray;
        }
    }
}
