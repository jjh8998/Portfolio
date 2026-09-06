using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuAniScript : MonoBehaviour
{

    public static bool canStartAni = true;

    public GameObject UICanvas;
    public GameObject backgroundObj;
    public GameObject candlelight;

    private CanvasGroup UICanvasGroup;
    private SpriteRenderer backrgroundObjSpr;

    // Start is called before the first frame update
    void Start()
    {
        if (canStartAni == true)
        {
            UICanvasGroup = UICanvas.GetComponent<CanvasGroup>();
            backrgroundObjSpr = backgroundObj.GetComponent<SpriteRenderer>();

            ShowStartAni();
        }
        else
        {
            candlelight.SetActive(false);
        }
    }

    public void ShowStartAni()
    {
        StartCoroutine(StartAni());
    }

    IEnumerator StartAni()
    {
        UICanvasGroup.alpha = 0f;

        Color color = backrgroundObjSpr.color;

        color.a = 0f;
        backrgroundObjSpr.color = color;

        yield return new WaitForSeconds(0.8f);

        for (float i = 0.0f; i <= 1.1f; i += 0.1f)
        {
            UICanvasGroup.alpha = i;
            color.a = i;
            backrgroundObjSpr.color = color;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
