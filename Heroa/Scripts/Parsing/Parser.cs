using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parser : MonoBehaviour
{

    // 아이템 파서
    private Item.ItemType type;
    private Attribute itemAttribute;

    //단어장 파싱
    public Vocabulary[] WordParse(string _CSVFileName)
    {
        List<Vocabulary> vocabularyList = new List<Vocabulary>(); // 대사리스트 생성
        TextAsset csvData = Resources.Load<TextAsset>("Words/"+ _CSVFileName); // csv파일 가져와서 textAsset으로 저장

        string[] data = csvData.text.Split(new char[] { '\n' }); // 엔터기준으로 열을 나눠줌

        for (int i = 1; i < data.Length + 1;) // 1열은 정리용 정보니까 생략할려고 i = 1 부터, 마지막 안짤리게 +1
        {
            string[] row = data[i].Split(new char[] { ',' }); // csv파일이니 , 로 행 구분

            Vocabulary vocabulary = new Vocabulary();

            vocabulary.ID = int.Parse(row[0]);
            vocabulary.englishWord = row[1]; // i=1인거 명심!!
            vocabulary.koreanWord = row[2];

            if (++i < data.Length)
            {
                row = data[i].Split(new char[] { ',' }); // 앞에 인덱스가 더있으면 짤라줌
            }
            else
            {
                break;
            }

            vocabularyList.Add(vocabulary); // 리스트에 넣어줌
        }

        return vocabularyList.ToArray(); // 배열 형태로 반환
    }

    public Dialogue[] DialogueParser(string _CSVFileName)
    {
        // 컷신 대화 파싱
        /*
         * pre : csv파일 이름
         * post : csv파일의 내용을 dialogue형으로 파싱해 dailogueList에 넣고 배열 형태로 리턴
         */

        List<Dialogue> dialogueList = new List<Dialogue>();
        TextAsset csvData = Resources.Load<TextAsset>("Dialogues/" + _CSVFileName); // csv파일 가져와서 textAsset으로 저장

        string[] data = csvData.text.Split(new char[] { '\n' }); // 엔터기준으로 열을 나눠줌

        for (int i = 1; i < data.Length; i++) // 1열은 정리용 정보니까 생략할려고 i = 1 부터
        {
            string[] row = data[i].Split(new char[] { ',' }); // csv파일이니 , 로 행 구분

            int ID = int.Parse(row[0]);
            row[2] = row[2].Replace("$", ","); // $ -> , 변환
            float contextDelay = float.Parse(row[3]);
            string spriteName = row[4].TrimEnd('\n', '\r'); // 마지막이라 개행문자가 들어가서 지워줌
            float effectTime = float.Parse(row[6]);

            Dialogue dialgoue = new Dialogue(ID, row[1], row[2], contextDelay, spriteName, row[5], effectTime);

            dialogueList.Add(dialgoue);
        }

        return dialogueList.ToArray(); // 배열 형태로 반환
    }

    // 마지막 열은 문자열 == 이 안되드라
    public void ItemListParser(string _CSVFileName)
    {
        TextAsset csvData = Resources.Load<TextAsset>("CSVs/" + _CSVFileName); // csv파일 가져와서 textAsset으로 저장

        string[] data = csvData.text.Split(new char[] { '\n' }); // 엔터기준으로 열을 나눠줌

        // 시트 완성되면 data.Length로
        for (int i = 1; i < 67 + 1; i++)
        {
            string[] row = data[i].Split(new char[] { ',' }); // csv파일이니 , 로 행 구분

            int ID = int.Parse(row[0]);

            row[2] = row[2].Replace("$", ","); // $ -> , 변환
            row[3] = row[3].Replace("$", ","); // $ -> , 변환

            if (row[5] == "Weapon")
            {
                type = Item.ItemType.Weapon;
            }
            else if (row[5] == "Head")
            {
                type = Item.ItemType.Head;
            }
            else if (row[5] == "Top")
            {
                type = Item.ItemType.Top;
            }
            else if (row[5] == "Bottom")
            {
                type = Item.ItemType.Bottom;
            }
            else if (row[5] == "Shoes")
            {
                type = Item.ItemType.Shoes;
            }
            else if (row[5] == "ETC")
            {
                type = Item.ItemType.ETC;
            }
            else
            {
                Debug.LogError("아이템 타입 없음! - " + row[1]);
            }

            int rate = int.Parse(row[6]);

            bool countable;
            if (row[7] == "TRUE")
                countable = true;
            else
                countable = false;
            int count = int.Parse(row[8]);

            float hp = float.Parse(row[9]);
            float damage = float.Parse(row[10]);
            float defense = float.Parse(row[11]);
            float CD = float.Parse(row[12]);
            float CR = float.Parse(row[13]);

            switch (row[14])
            {
                case "Nothing":
                    itemAttribute = Attribute.Nothing;
                        break;
                case "Fire":
                    itemAttribute = Attribute.Fire;
                    break;
                case "Grass":
                    itemAttribute = Attribute.Grass;
                    break;
                case "Water":
                    itemAttribute = Attribute.Water;
                    break;
                default:
                    Debug.Log(row[0]);
                    Debug.LogError("Parser : no impormation about attribute - " + row[12]);
                    break;
            }

            float upHp = float.Parse(row[15]);
            float upDamage = float.Parse(row[16]);
            float upDefense = float.Parse(row[17]);
            float upCD = float.Parse(row[18]);
            float upCR = float.Parse(row[19]);
            
            float abilityValue = float.Parse(row[21]);
            int abilityTurn = int.Parse(row[22]);
            float upAbilityValue = float.Parse(row[23]);

            DatabaseManager.instance.itemList.Add(new Item(ID, row[1], row[2], row[3], row[4], type, rate, countable, count,
               hp, damage, defense, CD, CR, itemAttribute,
               upHp, upDamage, upDefense, upCD, upCR, row[20], abilityValue, abilityTurn, upAbilityValue));
        }
    }

    public void QuestParser(string _CSVFileName)
    {
        /*
         * pre : csv파일 이름
         * post : questList를 초기화 하고 csv파일의 내용을 QuestScript 의 questList 목록에 추가 ( 현재 5줄까지밖에 안됌)
         */

        QuestManager.instance.questList.Clear();

        TextAsset csvData = Resources.Load<TextAsset>("Quests/" + _CSVFileName); // csv파일 가져와서 textAsset으로 저장

        string[] data = csvData.text.Split(new char[] { '\n' }); // 엔터기준으로 열을 나눠줌

        // 시트 완성되면 data.Length로
        for (int i = 1; i < 5 + 1; i++)
        {
            string[] row = data[i].Split(new char[] { ',' }); // csv파일이니 , 로 행 구분

            int sortID = int.Parse(row[0]);
            int ID = int.Parse(row[1]);
            string name = row[2];
            row[3] = row[3].Replace("$", ","); // $ -> , 변환
            string explanation = row[3];
            int gold = int.Parse(row[4]);
            int exp = int.Parse(row[5]);

            Quest.Condition condition;

            switch (row[6])
            {
                case "TodayAcess":
                    condition = Quest.Condition.TodayAccess;
                    break;
                case "TodayRightAnswerCount":
                    condition = Quest.Condition.TodayRightAnswerCount;
                    break;
                case "TodayMonsterKill":
                    condition = Quest.Condition.TodayMonsterKill;
                    break;
                case "TodayUseMagic":
                    condition = Quest.Condition.TodayUseMagic;
                    break;
                case "TodayUseGold":
                    condition = Quest.Condition.TodayUseGold;
                    break;

                case "WeekAccessDayCount":
                    condition = Quest.Condition.WeekAccessDayCount;
                    break;
                case "WeekRightAnswerCount":
                    condition = Quest.Condition.WeekRightAnswerCount;
                    break;
                case "WeekMonsterKill":
                    condition = Quest.Condition.WeekMonsterKill;
                    break;
                case "WeekUseMagic":
                    condition = Quest.Condition.WeekUseMagic;
                    break;
                case "WeekUseGold":
                    condition = Quest.Condition.WeekUseGold;
                    break;

                case "Word":
                    condition = Quest.Condition.Word;
                    break;
                case "Monster":
                    condition = Quest.Condition.Monster;
                    break;
                default:
                    Debug.LogError("Parser : no impormation about condition in row " + row[1]);
                    condition = Quest.Condition.ETC;
                    break;
            }

            int num = int.Parse(row[7]);

            QuestManager.instance.questList.Add(new Quest(sortID, ID, name, explanation, gold, exp, condition, num));
        }
    }

    public Magic[] MagicParser(string _CSVFileName)
    {
        TextAsset csvData = Resources.Load<TextAsset>("CSVs/" + _CSVFileName); // csv파일 가져와서 textAsset으로 저장

        string[] data = csvData.text.Split(new char[] { '\n' }); // 엔터기준으로 열을 나눠줌

        List<Magic> magics = new List<Magic>();

        // 시트 완성되면 data.Length로
        for (int i = 1; i < 4 + 1; i++)
        {
            string[] row = data[i].Split(new char[] { ',' }); // csv파일이니 , 로 행 구분

            int ID = int.Parse(row[0]);

            row[2] = row[2].Replace("$", ","); // $ -> , 변환
            row[3] = row[3].Replace("$", ","); // $ -> , 변환
            int cooldown = int.Parse(row[7]);
            float fixedDamage = float.Parse(row[8]);
            float percentDamage = float.Parse(row[9]);
            float criticalDamage = float.Parse(row[10]);
            float criticalRate = float.Parse(row[11]);

            string ability = row[12];
            int abilityAmount = int.Parse(row[13]);
            float abilityPercent = float.Parse(row[14]);
            int abilityMaintainTurn = int.Parse(row[15]);

            int needVower = int.Parse(row[18]);
            int neewConsanant = int.Parse(row[19]);
            row[20] = row[20].Replace("$", ","); // $ -> , 변환
            row[21] = row[21].Replace("$", ","); // $ -> , 변환

            Magic magic = new Magic(ID, row[1], row[2], row[3], row[4], row[5], row[6], cooldown, fixedDamage, percentDamage, criticalDamage, criticalRate, ability, abilityAmount, abilityPercent, abilityMaintainTurn, 
                row[16], row[17], needVower, neewConsanant, row[20], row[21]);

            magics.Add(magic);
        }

        return magics.ToArray();
    }

}
