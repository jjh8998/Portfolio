using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class WordCollector : MonoBehaviour
{
    public static WordCollector instance = null;


    [SerializeField]
    private TextMeshProUGUI question_Text = null;
    [SerializeField]
    private TextMeshProUGUI[] answer_Text = new TextMeshProUGUI[4];

    [SerializeField]
    VocabularyList vocabularyList;

    private int[] id = new int[4];
    private string[] s_Eng = new string[4];
    private string[] s_Kor = new string[4];

    private bool is_English_Korean = true;
    private bool isCollecting = true;

    private int answerID = 0; // 엑셀상 ID
    private int answerIndex = 0; // 0 ~ 3

    private Queue<int> wordDeck = new Queue<int>();

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

// Start is called before the first frame update
    void Start()
    {
        GetVocabularies();
        InitializationWordQuiz();
    }

    // Update is called once per frame
    void Update()
    {
        if (isCollecting == false)
            InitializationWordQuiz();
    }

    public void InitializationWordQuiz()
    {
        // 단어를 랜덤하게 초기화하고 표시하는 함수, 턴종료도 포함함.

        isCollecting = true;

        Collect();
        Setting();

        GameManager.instance.EndTurn();
    }

    private void RefillDeck()
    {
        List<int> allIds = new List<int>();
        for (int i = 1; i < vocabularyList.vocabularies.Length; i++) // 0 제외
        {
            allIds.Add(i);
        }

        // 섞기
        for (int i = 0; i < allIds.Count; i++)
        {
            int rand = Random.Range(i, allIds.Count);
            (allIds[i], allIds[rand]) = (allIds[rand], allIds[i]);
        }

        // 큐에 채우기
        foreach (int id in allIds)
        {
            wordDeck.Enqueue(id);
        }
    }

    private void Collect()
    {
        /*
        HashSet<int> used = new HashSet<int>();

        // 랜덤 단어 넣기
        for (int i = 0; i < s_Eng.Length; i++)
        {
            int randId;
            do
            {
                randId = Random.Range(1, vocabularyList.vocabularies.Length); // 0 제외하고 싶으면 1부터
            } while (used.Add(randId) == false); // 중복이면 다시 뽑음

            id[i] = randId;
            s_Eng[i] = vocabularyList.vocabularies[randId].englishWord;
            s_Kor[i] = vocabularyList.vocabularies[randId].koreanWord;
        }
        */

        // 덱이 비었으면 새로 채움
        if (wordDeck.Count < s_Eng.Length)
        {
            RefillDeck();
        }

        // 덱에서 뽑기
        for (int i = 0; i < s_Eng.Length; i++)
        {
            int randId = wordDeck.Dequeue();
            id[i] = randId;
            s_Eng[i] = vocabularyList.vocabularies[randId].englishWord;
            s_Kor[i] = vocabularyList.vocabularies[randId].koreanWord;
        }
    }

    private void Setting()
    {
        answerIndex = Random.Range(0, 2 + 1);
        answerID = id[answerIndex];

        // 영 - 한
        if (is_English_Korean)
        {
            question_Text.text = s_Eng[answerIndex];

            for (int i = 0; i < s_Kor.Length; i++)
            {
                answer_Text[i].text = s_Kor[i];
            }
        }
        else
        {
            question_Text.text = s_Kor[answerIndex];

            for (int i = 0; i < s_Eng.Length; i++)
            {
                answer_Text[i].text = s_Eng[i];
            }

            HeroScript.instance.SetIsNoramlAttack(false); // 플레이어 공격 마법공격으로
            is_English_Korean = true; // 영 - 한 원상복귀
        }
    }

    #region Check_Answer

    public void Check_Answer_1()
    {
        if (GameManager.instance.GetCanWordChecking() == true)
        {
            if (answerIndex == 0)
            {
                Answer_Coloring(true, 0);
            }
            else
            {
                Answer_Coloring(false, 0);
            }
        }
    }

    public void Check_Answer_2()
    {
        if (GameManager.instance.GetCanWordChecking() == true)
        {
            if (answerIndex == 1)
            {
                Answer_Coloring(true, 0);
            }
            else
            {
                Answer_Coloring(false, 1);
            }
        }
    }

    public void Check_Answer_3()
    {
        if (GameManager.instance.GetCanWordChecking() == true)
        {
            if (answerIndex == 2)
            {
                Answer_Coloring(true, 0);
            }
            else
            {
                Answer_Coloring(false, 2);
            }
        }
    }

    public void Check_Answer_4()
    {
        if (GameManager.instance.GetCanWordChecking() == true)
        {
            if (answerIndex == 3)
            {
                Answer_Coloring(true, 0);
            }
            else
            {
                Answer_Coloring(false, 3);
            }
        }
    }

    #endregion

    public void Answer_Coloring(bool isAnswer, int i)
    {
        answer_Text[answerIndex].color = Color.green;

        // 오답이면
        if (isAnswer == false)
        {
            if (i == 99)
            {
                // 시간초과
                MonsterScript.instance.Monster_Attack();
            }
            else
            {
                answer_Text[i].color = Color.red;
                MonsterScript.instance.Monster_Attack();
            }
        }
        // 정답이면
        else
        {
            DatabaseManager.instance.PlusTodayRightAnswerCount(1);
            DatabaseManager.instance.PlusWeekRightAnswerCount(1);
            DatabaseManager.instance.PlusTotalRightAnwserCount(1);

            HeroScript.instance.Hero_Attack();
        }

    }

    public void Word_Initialization()
    {
        for (int i = 0; i < answer_Text.Length; i++)
        {
            answer_Text[i].color = Color.black;
        }
    }

    public Vocabulary[] GetVocabularies()
    {
        // vocabularies 가져오는 함수
        vocabularyList.vocabularies = DatabaseManager.instance.GetVocabularies();

        if (vocabularyList == null)
        {
            Debug.LogError("vocabularyList is Null");
            return null;

        }

        return vocabularyList.vocabularies;
    }

    #region Set

    public void SetIsEnglish_Korean(bool _Bool)
    {
        is_English_Korean = _Bool;
    }

    public void SetIsCollecting(bool _Bool)
    {
        isCollecting = _Bool;
    }

    #endregion

}
