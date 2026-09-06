using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WordSelectButtonScript : MonoBehaviour
{

    [SerializeField]
    private string wordFileName = null;
    [SerializeField]
    private Transform tf; // 텍스트들의 부모 객체

    // 직렬화 안하면 에러남
    [SerializeField]
    private VocabularyList vocabularyList;
    private TextMeshProUGUI[] wordTexts;

    public void ShowSelectedWord()
    {
        SetCSVFile();
        vocabularyList.vocabularies = DatabaseManager.instance.GetVocabularies();

        wordTexts = tf.GetComponentsInChildren<TextMeshProUGUI>();

        for (int i = 0; i < vocabularyList.vocabularies.Length; i++)
        {
            wordTexts[2 * i].text = vocabularyList.vocabularies[i].englishWord; // 짝수는 영단어
            wordTexts[2 * i + 1].text = vocabularyList.vocabularies[i].koreanWord; // 홀수는 한국어
        }
        if (vocabularyList.vocabularies.Length < 55)
        {
            for (int j = 0; j < 55 - vocabularyList.vocabularies.Length; j++)
            {
                wordTexts[109 - 2 * j].text = "";
                wordTexts[109 - 2 * j -1].text = "";
            }
        }

        StageSelectManager.instance.OnShowWordPanel();
    }

    public void LoadStage()
    {
        MainMenuAniScript.canStartAni = false;
        SetCSVFile();
        LoadingSceneManager.LoadScene(DatabaseManager.instance.GetSelectedStageName());
    }

    public void SetCSVFile()
    {
        DatabaseManager.word_CSV_FileName = wordFileName; // 단어장 파일
    }
}
