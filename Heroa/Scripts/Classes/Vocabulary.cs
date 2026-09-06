using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // 직렬화
public class Vocabulary
{
    public int ID;
    public string englishWord;
    public string koreanWord;
}

[System.Serializable]
public class VocabularyList
{
    public string listName;
    public Vocabulary[] vocabularies;
}
