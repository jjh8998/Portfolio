using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{

    public GameObject UICanvas;
    public GameObject backgroundObj;

    private CanvasGroup UICanvasGroup;
    private SpriteRenderer backrgroundObjSpr;

    public static string nextSceneName;
    public static string dialogueCSVFileName;

    public GameObject dialoguePanel;
    public GameObject charaNameObj;

    [Header("Text")]
    public TextMeshProUGUI charaNameText = null;
    public TextMeshProUGUI contextText = null;
    public Image backgroundImage_Backup = null; // 페이드 인 효과를 위해서
    public Image backgroundImage_Main = null; 

    private Dialogue[] nowDialogue; // 현재 대화 데이터
    private int nowDialogueCount; // 현재 대화 내용이 몇번째인지 카운트 하는 변수
    private bool isDialogue = false; // 현재 대화중인지 판단하는 변수
    private bool isNext; // 다음 대화창으로 넘어가도 되는지 확인하는 변수

    // Start is called before the first frame update
    void Start()
    {
        UICanvasGroup = UICanvas.GetComponent<CanvasGroup>();

        StartCoroutine(StartAni());
        ShowDialogue(dialogueCSVFileName);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDialogue == true)
        {
            if (isNext == true)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.touchCount > 0)
                {
                    if (++nowDialogueCount < nowDialogue.Length)
                    {
                        isNext = false;
                        StartCoroutine(DialogueWriter(nowDialogueCount));
                    }
                    else
                    {
                        EndDialogue();
                    }
                }
            }
        }
    }

    public static void StartDialogue(string _DialogueCSVFileName, string _NextSceneName)
    {
        dialogueCSVFileName = _DialogueCSVFileName;
        nextSceneName = _NextSceneName;
        SceneManager.LoadScene("DialogueScene");
    }

    public void ShowDialogue(string _DialogueCSVFileName)
    {
        // CSV파일을 파싱해 대화 컷신을 보여주는 함수
        // 타임 0? - 인게임 컷신이면 시간 멈추는거랑 체력 저장 등등이 필요함
        /*
         * pre : csv파일 이름
         * post : isDialogue = true, 대화 패널 on 후 파싱된 내용 보여주기
         */

        // nowDialogue = parser.DialogueParser(_DialogueCSVFileName);
        nowDialogue = DatabaseManager.instance.GetDialogues(_DialogueCSVFileName);

        isDialogue = true;
        dialoguePanel.SetActive(true);

        StartCoroutine(DialogueWriter(nowDialogueCount));
    }

    public void EndDialogue()
    {
        // 대화 컷신을 끝내는 함수
        // 시간 시작해야하는지?

        isDialogue = false;
        dialoguePanel.SetActive(false);

        nowDialogueCount = 0;

        if (nextSceneName == null)
            nextSceneName = "MainMenu";

        LoadingSceneManager.LoadScene(nextSceneName);
    }

    IEnumerator DialogueWriter(int _Index)
    {
        // 대화를 출력하는 함수

        charaNameText.text = nowDialogue[_Index].charaName;

        switch (nowDialogue[_Index].sceneEffect)
        {
            case "FadeIn" :
                StartCoroutine(StartFadeIn(_Index, nowDialogue[_Index].effectTime));
                break;
            default :
                backgroundImage_Main.sprite = nowDialogue[_Index].backgroundSprite;
                break;
        }

        contextText.text = "";

        if (nowDialogue[_Index].charaName == "None")
            charaNameObj.SetActive(false);
        else
            charaNameObj.SetActive(true);


        char[] split_contexts = nowDialogue[_Index].context.ToCharArray();

        for (int i = 0; i < split_contexts.Length; i++)
        {
            contextText.text += split_contexts[i];
            yield return new WaitForSeconds(nowDialogue[_Index].contextDelay);
        }

        isNext = true;
    }


    IEnumerator StartFadeIn(int _Index, float _FadeTime)
    {
        backgroundImage_Backup.sprite = nowDialogue[_Index].backgroundSprite;

        Color color = backgroundImage_Main.color;

        float i = 1f;

        for (i = 1f; i >= 0f; i -= 0.1f)
        {
            color.a = i;
            backgroundImage_Main.color = color;
            yield return new WaitForSeconds(_FadeTime / 10); // 10번에 걸처서 밝아지니까
        }

        backgroundImage_Main.sprite = backgroundImage_Backup.sprite;
        color.a = 1f;
        backgroundImage_Main.color = color;
    }

    IEnumerator StartAni()
    {
        //처음 시작할때 페이드 인
        UICanvasGroup.alpha = 0f;

        yield return new WaitForSeconds(0.8f);

        for (float i = 0.0f; i <= 1.1f; i += 0.1f)
        {
            UICanvasGroup.alpha = i;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
