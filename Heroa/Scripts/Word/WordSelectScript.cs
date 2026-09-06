using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WordSelectScript : MonoBehaviour
{
    /// <summary>
    /// 단어 선택 화면에서의 단어 테마 선택 관련 스크립트
    /// </summary>
    
    [SerializeField]
    private GameObject wordSelectScrollView;
    private ScrollRect scrollRect;
    [SerializeField]
    private GameObject publicOfficialExamWords;
    [SerializeField]
    private GameObject toeicWords;
    [SerializeField]
    private GameObject highSchoolWords;
    [SerializeField]
    private GameObject middleSchoolWords;
    [SerializeField]
    private GameObject elementarySchoolWords;

    // Start is called before the first frame update
    void Start()
    {
        scrollRect = wordSelectScrollView.GetComponent<ScrollRect>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickedPublicOfficialExamWord()
    {
        OffAllWords();
        publicOfficialExamWords.SetActive(true);
        scrollRect.content = publicOfficialExamWords.GetComponent<RectTransform>();
    }

    public void OnClickedToeicWord()
    {
        OffAllWords();
        OnToeicWords();
        scrollRect.content = toeicWords.GetComponent<RectTransform>();
    }

    public void OnClickedHighSchoolWord()
    {
        OffAllWords();
        OnHighSchoolWords();
        scrollRect.content = highSchoolWords.GetComponent<RectTransform>();
    }

    public void OnClickedMiddleSchoolWord()
    {
        OffAllWords();
        OnMiddleSchoolWords();
        scrollRect.content = middleSchoolWords.GetComponent<RectTransform>();
    }

    public void OnClickedElementarySchoolWord()
    {
        OffAllWords();
        OnElementarySchoolWords();
        scrollRect.content = elementarySchoolWords.GetComponent<RectTransform>();
    }

    public void OffAllWords()
    {
        publicOfficialExamWords.SetActive(false);
        toeicWords.SetActive(false);
        highSchoolWords.SetActive(false);
        middleSchoolWords.SetActive(false);
        elementarySchoolWords.SetActive(false);
    }

    public void OnPublicOfficialExamWords()
    {
        publicOfficialExamWords.SetActive(true);
    }

    public void OnToeicWords()
    {
        toeicWords.SetActive(true);
    }

    public void OnHighSchoolWords()
    {
        highSchoolWords.SetActive(true);
    }

    public void OnMiddleSchoolWords()
    {
        middleSchoolWords.SetActive(true);
    }

    public void OnElementarySchoolWords()
    {
        elementarySchoolWords.SetActive(true);
    }
}
