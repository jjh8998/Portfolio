using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageSelectScript : MonoBehaviour
{
    [SerializeField]
    private string sceneName = null; // 통일 바람

    public void OnClickStageButton()
    {
        // Debug.Log("StageSelectScript");

        if (StageSelectManager.instance.CheckStageClear(sceneName) == true)
        {
            MainMenuManager.instance.OnWordSelectView();
            DatabaseManager.instance.SetSelectedStage(sceneName);
        }
    }

}
