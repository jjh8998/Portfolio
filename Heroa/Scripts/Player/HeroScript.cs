using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    NormalAttack = 1,
    MagicAttack,
    Heal,
    ChargingAttack

}

public class HeroScript : CharacterScript
{
    public static HeroScript instance = null;

    private DatabaseManager DB = null;

    private Item weaponItem;

    private Attribute weaponAttribute;
    private bool is_NormalAttack = true;

    // 플레이어가 장비한 장비들
    private List<Item> equipedItems;

    private bool isInitialized = false;

    // about Charging
    private bool isCharging = false;
    private bool chargedAttackReady = false;


    public bool nowIAmBoy; // 현재 영웅이 소년인지

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        // DB 할당
        DB = DatabaseManager.instance;
        curHp = hp;

        // DB 로딩이 끝날 때까지 기다림
        while (DB.isFinishedLoading == false)
        {
            yield return null;
        }

        SynchroHeroStat();
        SynchroEquipedItem();
        curHp = hp;

        isInitialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isInitialized == false) return;

        if (curHp <= 0)
        {
            GameManager.instance.OnHeroDeadPanel();
            Destroy(this.gameObject);
        }
    }

    // 플레이어 능력치 동기화 (데이터 베이스에서 중복)
    public void SynchroHeroStat()
    {
        hp = DB.GetPlayer_Hp();
        attackDamage = DB.GetPlayer_AttackDamage();
        defense = DB.GetPlayer_Defense();
        criticalDamage = DB.GetPlayer_CriticalDamage();
        criticalRate = DB.GetPlayer_CriticalRate();
    }

    public void SynchroEquipedItem()
    {
        equipedItems = new List<Item>();

        for (int i = 0; i < 5; i++)
            equipedItems.Add(new Item(0, "빈 장비 슬롯", "리스트 순서 채우기용", "Empty", "None", Item.ItemType.ETC, 0));

        for (int i = 0; i < DB.itemList.Count; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (DB.itemList[i].itemID == DB.myItemInEquipmentData.myitemData[j].itemID)
                    equipedItems[j] = DB.itemList[i];
            }
        }
    }

    public void SetChargedAttackReady()
    {
        if (isCharging)
        {
            isCharging = false;
            GameManager.instance.ShowAlarmPanel("차징 완료! 강력한 공격 준비됨!");
        }
    }
    public void CancelCharging()
    {
        // 차징 중이거나(입력 직후) 차징 완료 대기 상태(EndTurn 후) 둘 다 해제
        if (isCharging || chargedAttackReady)
        {
            isCharging = false;
            chargedAttackReady = false;

            // 차징 아이콘 제거
            ConditionManager.instance.RemoveCondition(true, ConditionType.ChargingAttack);
        }
    }

    public void Hero_Attack()
    {
        if (is_NormalAttack == true)
        {
            if (weaponHasChargeAbility())
            {
                DoChargingAttack();
            }
            else
            {
                DoNormalAttack();
            }
        }
        else
        {
            DoMagicAttack();
        }
    }

    private void DoChargingAttack()
    {
        if (!isCharging && !chargedAttackReady)
        {
            isCharging = true;
            chargedAttackReady = true;
            StartCoroutine(Hero_Attack_Ani(AttackType.ChargingAttack));
            return;
        }
        else if (chargedAttackReady)
        {
            isCharging = false;
            chargedAttackReady = false;
            CalculateRealDamage();
            realDamage *= 2;
            GameManager.instance.ShowAlarmPanel("강력한 공격!");
            StartCoroutine(Hero_Attack_Ani(AttackType.NormalAttack));
            return;
        }
    }

    private void DoNormalAttack()
    {
        CalculateRealDamage();
        StartCoroutine(Hero_Attack_Ani(AttackType.NormalAttack));
    }

    private void DoMagicAttack()
    {
        MagicManager.instance.CastMagic();
        StartCoroutine(Hero_Attack_Ani(AttackType.NormalAttack));
        is_NormalAttack = true;

        Debug.Log("HeroScript : Magic Attack");
    }

    IEnumerator Hero_Attack_Ani(AttackType _attackType)
    {
        // stop word quiz
        GameManager.instance.PauseWordQuiz();

        //공격
        if (_attackType == AttackType.NormalAttack)
        {
            PlayerAttackAniScript.instance.SetIsHeroAttack(true);
            yield return new WaitForSeconds(0.3f);
            PlayerAttackAniScript.instance.SetIsSlashAttack(true);

            GameManager.instance.CreateFloatingText(realDamage, "Monster");
            MonsterScript.instance.PlusCurHp(-realDamage);

            yield return new WaitForSeconds(attackAniTime);

            PlayerAttackAniScript.instance.SetIsHeroAttack(false);
            PlayerAttackAniScript.instance.SetIsSlashAttack(false);

            realDamage = 0;
        }
        else if (_attackType == AttackType.ChargingAttack)
        {
            yield return new WaitForSeconds(1f);
            GameManager.instance.AddConditionObj("Hero", ConditionType.ChargingAttack, 1);
            yield return new WaitForSeconds(attackAniTime);
        }
        else if (_attackType == AttackType.Heal)
        {
            // 힐 애니메이션 만들기
            yield return new WaitForSeconds(0.3f);

            yield return new WaitForSeconds(attackAniTime);
        }

        WordCollector.instance.Word_Initialization();

        if (MagicManager.instance.GetNowCastMagic() == true)
        {
            ChangeHero("Girl");
        }

        // 몬스터 죽을때 타이머랑 안겹치기 위함
        if (GameManager.instance.GetIsMonsterDead() == false)
        {
            // continue word quiz
            GameManager.instance.ResumeWordQuiz();
        }
    }

    #region Damage

    public void CalculateRealDamage()
    {
        // 플레이어가 몬스터에게 줄 데미지 계산

        realDamage = attackDamage - MonsterScript.instance.GetDefense();

        if (realDamage <= 0)
            realDamage = 1;

        weaponAttribute = DB.GetNowWeaponAttribute();

        CalculateAttributeAttack(weaponAttribute);

        // 치명타 계산
        int random = Random.Range(1, 100 + 1); // int가 제외
        if (random < criticalRate)
        {
            realDamage = Mathf.RoundToInt(realDamage * criticalDamage);
        }
    }

    public void CalculateMagicDamage(Magic _CastMagic)
    {

        // 고정 데미지라면
        if (_CastMagic.fixedDamage != 0)
        {
            // 무기 능력 적용
            switch (equipedItems[0].equipment.equipmentAbility)
            {
                case EquipmentAbility.IncreaseMagicDamage:
                    realDamage = (equipedItems[0].equipment.abilityValue * 0.01f) * _CastMagic.fixedDamage + _CastMagic.fixedDamage;
                    break;
                default:
                    realDamage = _CastMagic.fixedDamage;
                    break;
            }
        }
        else
        {
            // 무기 능력 적용
            // 업그레이드 시 value값도 받아와야함. 즉 real값을 value도 만들어야함.
            switch (equipedItems[0].equipment.equipmentAbility)
            {
                case EquipmentAbility.IncreaseMagicDamage:
                    realDamage =  ((_CastMagic.percentDamage + equipedItems[0].equipment.abilityValue) * 0.01f) * attackDamage - MonsterScript.instance.GetDefense();
                    break;
                default:
                    realDamage =  (_CastMagic.percentDamage * 0.01f) * attackDamage - MonsterScript.instance.GetDefense();
                    break;
            }

            CalculateAttributeAttack(_CastMagic.magicAttribute);

            // 치명타?
        }
    }

    private void CalculateAttributeAttack(Attribute _Attribute)
    {
        // 속성 공격 계산
        float bonus = 2f;

        // 물속성이면
        if (_Attribute == Attribute.Water)
        {
            if (MonsterScript.instance.GetMonsterAttribute() == Attribute.Fire)
            {
                realDamage = realDamage * bonus;
            }
            else if (MonsterScript.instance.GetMonsterAttribute() == Attribute.Grass)
            {
                realDamage = realDamage * 0.5f;
            }
        }
        // 불속성이면
        else if (_Attribute == Attribute.Fire)
        {
            if (MonsterScript.instance.GetMonsterAttribute() == Attribute.Grass)
            {
                realDamage = realDamage * bonus;
            }
            else if (MonsterScript.instance.GetMonsterAttribute() == Attribute.Water)
            {
                realDamage = realDamage * 0.5f;
            }
        }
        // 풀속성이면
        else if (_Attribute == Attribute.Grass)
        {
            if (MonsterScript.instance.GetMonsterAttribute() == Attribute.Water)
            {
                realDamage = realDamage * bonus;
            }
            else if (MonsterScript.instance.GetMonsterAttribute() == Attribute.Fire)
            {
                realDamage = realDamage * 0.5f;
            }
        }
    }

    public void Heal(Magic _CastMagic)
    {
        curHp += _CastMagic.abilityAmount;
        GameManager.instance.CreateFloatingText(_CastMagic.abilityAmount, "Hero");

        // 힐 작동안함!!!
        // 수정필요
    }

    public void CalculatePoisonProbability()
    {
        // 플레이어가 몬스터에게 중독을 걸 확률을 계산

        if (probability_poison > 0)
        {
            // 확률에 따라 중독상태로 만들기

            int random = Random.Range(1, 100 + 1); // int가 제외

            if (probability_poison > random)
            {
                // 상태이상 표시 만들기
                GameManager.instance.ShowAlarmPanel("중독!");
                GameManager.instance.AddConditionObj("Monster", ConditionType.Poison, give_poisonTurn);
            }
        }
    }

    public float GetReducedAttributeDamage(float baseDamage, Attribute attackAttribute)
    {
        float reducedDamage = baseDamage;

        foreach (Item item in equipedItems)
        {
            if (item.itemType == Item.ItemType.ETC || item.equipment == null)
                continue;

            if (item.equipment.equipmentAbility == EquipmentAbility.DecreaseAttributeDamage)
            {
                // 예: 불속성 데미지 감소 장비 착용 시
                if (item.equipment.itemAttribute == attackAttribute)
                {
                    float reductionPercent = item.equipment.abilityValue; // 예: 20
                    reducedDamage *= (1f - reductionPercent / 100f);
                }
            }
        }

        return reducedDamage;
    }

    #endregion

    public void ChangeHero(string _changeHeroType)
    {
        if (_changeHeroType == "Hero")
        {
            this.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Images/Hero_Sprite");
            nowIAmBoy = true;
        }
        else
        {
            this.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Images/IngameGirl_Image");
            nowIAmBoy = false;
        }
    }

    private bool weaponHasChargeAbility()
    {
        return equipedItems[0].equipment.equipmentAbility == EquipmentAbility.ChargeAttack;
    }

    public bool IsCharging()
    {
        return isCharging;
    }

    public bool IsChargedAttackReady()
    {
        return chargedAttackReady;
    }

    public void SetIsNoramlAttack(bool _Bool)
    {
        is_NormalAttack = _Bool;
    }
}
