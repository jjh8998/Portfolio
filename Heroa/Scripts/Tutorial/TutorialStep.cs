using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TutorialStep
{
    [Header ("Next Type")]
    public bool isClickToContinue = true; // true면 화면 터치, false면 버튼 클릭
    public List<Button> nextButton = new List<Button>();

    [Header ("Tutorial")]
    [TextArea]
    public string message;          // 안내 텍스트
    public Vector2 messagePos;
    public GameObject obj;
}
