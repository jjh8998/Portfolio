using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MonsterScript : CharacterScript
{

    public static MonsterScript instance = null;

    [SerializeField]
    private Animator monsterAnim;

    [Header("Monster Stat")]
    [SerializeField]
    private Attribute monsterAttribute = Attribute.Nothing;
    [SerializeField]
    private float dropMoney = 0;
    [SerializeField]
    private float dropExp = 0;
    [SerializeField]
    private bool isBoss = false;

    // 모음 드랍 변수들
    [Header ("DropVower")]
    [SerializeField]
    private float probability_DropVower = 0f;
    [SerializeField]
    private int minNumber_DropVower = 0;
    [SerializeField]
    private int maxNumber_DropVower = 0;

    // 자음 드랍 변수들
    [Header ("DropConsonant")]
    [SerializeField]
    private float probability_DropConsonant = 0f;
    [SerializeField]
    private int minNumber_DropConsonant = 0;
    [SerializeField]
    private int maxNumber_DropConsonant = 0;

    private HpBarScript hpBar;

    // Start is called before the first frame update
    void Start()
    {
        /*
        if (instance == null)
        {
            instance = this;
        }
        으로 하면 리스폰시 본 스크립트 인식 안됌.
        */

        instance = this;

        curHp = hp;

        hpBar = GetComponent<HpBarScript>();
        monsterAnim = GetComponent<Animator>();

        monsterAnim.SetTrigger("nowSpawn");
    }

    // Update is called once per frame
    void Update()
    {
        if (curHp <= 0)
        {
            StartCoroutine(Monster_Dead_Ani());
        }

        GameManager.instance.OnMonsterHpBar();
    }

    public void Monster_Attack()
    {
        StartCoroutine(Monster_Attack_Ani());
    }

    IEnumerator Monster_Attack_Ani()
    {
        GameManager.instance.PauseWordQuiz();

        monsterAnim.SetTrigger("isMonsterAttack");

        yield return new WaitForSeconds(attackAniTime);

        // 공격 데미지 계산
        CalculateRealDamage();
        GameManager.instance.CreateFloatingText(realDamage, "Hero");
        float finalDamage = HeroScript.instance.GetReducedAttributeDamage(realDamage, GetMonsterAttribute());
        HeroScript.instance.PlusCurHp(-finalDamage);

        yield return new WaitForSeconds(1f);


        // 독데미지 계산
        CalculatePoisonProbability();

        WordCollector.instance.Word_Initialization();

        GameManager.instance.ResumeWordQuiz();
    }

    IEnumerator Monster_Dead_Ani()
    {
        GameManager.instance.SetIsMonsterDead(true);

        TimerScript.instance.timerOn = false;
        GameManager.instance.SetCanWordChecking(false);
        // 애니메이션
        yield return new WaitForSeconds(2f);
        DatabaseManager.instance.PlusGold(dropMoney); // money drop
        DatabaseManager.instance.PlusNowExp(dropExp); // exp drop
        ExpScript.instance.CheckLevelUp();

        // 몬스터 1킬 추가
        DatabaseManager.instance.PlusTodayMonsterKill(1);
        DatabaseManager.instance.PlusWeekMonsterKill(1);
        DatabaseManager.instance.PlusTotalMonsterKillCount(1);

        StageManager.instance.PlusNowStageProcessivity(1);
        StageManager.instance.CheckProcssivity(); // 진행도 확인 및 몬스터 소환
        // WordCollector.instance.SetIsCollecting(false); // 단어가 바뀜

        GameManager.instance.OffMonsterHpBar();

        if (isBoss == true)
        {
            DropAlphabet();

            StageManager.instance.ClearBoss();

            // 보스 1킬 추가 추가
        }

        Destroy(this.gameObject);
    }

    #region Damage

    //, 부모 스크립트에서 써도 되지않음?

    // 속성 공격 추가 요망 , 4부위 데미지 계산해서 평균으로?
    public void CalculateRealDamage()
    {
        // 몬스터가 플레이어에게 줄 실제 공격력 계산

        realDamage = attackDamage - HeroScript.instance.GetDefense();

        if (realDamage <= 0)
            realDamage = 1;

        if (HeroScript.instance.IsCharging() || HeroScript.instance.IsChargedAttackReady())
        {
            GameManager.instance.ShowAlarmPanel("차징 중 피격! 피해 2배, 차징 해제");
            HeroScript.instance.CancelCharging();
            realDamage *= 2;
        }


        // 치명타 계산
        int random = Random.Range(1, 100 + 1); // int가 제외
        if (random < criticalRate)
        {
            realDamage = Mathf.RoundToInt(realDamage * criticalDamage);
        }
    }

    public void CalculatePoisonProbability()
    {
        // 몬스터가 플레이어에게 중독을 걸 확률을 계산

        if (probability_poison > 0)
        {
            // 확률에 따라 중독상태로 만들기

            int random = Random.Range(1, 100 + 1); // int가 제외

            if (probability_poison > random)
            {
                // 상태이상 표시 만들기
                GameManager.instance.ShowAlarmPanel("중독!");
                GameManager.instance.AddConditionObj("Hero", ConditionType.Poison, give_poisonTurn);
            }
        }
    }

    #endregion

    // 드랍 애니메이션 추가 요망
    public void DropAlphabet()
    {
        // 알파벳 드랍 확률을 계산해서 알파벳을 드랍함

        int randomVowelDropCount = 0;
        int randomConsonantDropCount = 0;

        int random = Random.Range(1, 100 + 1); // int가 제외
        if (random <= probability_DropVower)
        {
            randomVowelDropCount = Random.Range(minNumber_DropVower, maxNumber_DropVower + 1); // int가 제외

            MyItem myItem = new MyItem(1);
            InventoryScript.instance.GetAnItem(myItem, randomVowelDropCount); // 인벤토리 스크립트에서 수정필요

            // 드랍 애니메이션 추가 요망
        }

        random = Random.Range(1, 100 + 1); // int가 제외
        if (random <= probability_DropConsonant)
        {
            randomConsonantDropCount = Random.Range(minNumber_DropConsonant, maxNumber_DropConsonant + 1); // int가 제외

            MyItem myItem = new MyItem(2);
            InventoryScript.instance.GetAnItem(myItem, randomConsonantDropCount);

            // 드랍 애니메이션 추가 요망
        }

        // 알파벳 몇개 추가했는지 알려주기
        if (randomVowelDropCount != 0 && randomConsonantDropCount == 0)
            GameManager.instance.ShowAlarmPanel("모음 : " + randomVowelDropCount.ToString() + "개 획득! \n");
        else if (randomVowelDropCount == 0 && randomConsonantDropCount != 0)
            GameManager.instance.ShowAlarmPanel("자음 : " + randomConsonantDropCount.ToString() + "개 획득! \n");
        else
            GameManager.instance.ShowAlarmPanel("모음 : " + randomVowelDropCount.ToString() + "개,  자음 : " + randomConsonantDropCount + "개 획득! \n");

        // 에러나면 GameManager.instance.ShowAlarmPanel("모음 : " + randomVowelDropCount.ToString() + "개, " + randomConsonantDropCount.ToString() + "개 획득! \n"); 로 교체

    }

    #region Get

    public Attribute GetMonsterAttribute()
    {
        return monsterAttribute;
    }

    public bool GetIsBoss()
    {
        return isBoss;
    }

    #endregion
}
