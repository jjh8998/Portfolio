using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryDeveloperScript : MonoBehaviour
{
    public string dialogueName;

    public void StartStory()
    {
        DialogueManager.StartDialogue(dialogueName, "MainMenu");
    }
}
