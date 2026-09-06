using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    public List<TutorialScript> tutorials = new List<TutorialScript>();

    private DatabaseManager DB;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    IEnumerator Start()
    {
        DB = DatabaseManager.instance;

        yield return new WaitUntil(() => DB.isFinishedLoading == true);

        if (DB.myPlayerData.clearStageTutorial == false)
        {
            StartStageTutorial();
        }
        if (DB.myPlayerData.clearStageTutorial && DB.myPlayerData.clearDrawTutorial == false)
        {
            StartDrawTutorial();
        }
    }

    public void StartStageTutorial()
    {
        tutorials[0].StartTutorial();
    }
    
    public void StartBattleTutorial()
    {

    }

    public void StartDrawTutorial()
    {
        tutorials[1].StartTutorial();
    }

    public void StartItemTutorial()
    {
        tutorials[2].StartTutorial();
    }
}
