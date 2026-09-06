using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterScript : MonoBehaviour
{

    [Header("Basic Stat")]
    [SerializeField]
    protected float hp = 100;
    [SerializeField]
    protected float curHp = 0;
    [SerializeField]
    protected float attackDamage = 10f;
    [SerializeField]
    protected float defense = 0f;
    [SerializeField]
    protected float criticalDamage = 1.5f; // 1.25배
    [SerializeField]
    protected float criticalRate = 10f;

    [SerializeField]
    protected float attackAniTime = 3f;

    protected float realDamage = 0f; // 공식 계산된 데미지

    #region Posion

    [Header ("Poison")]
    [SerializeField]
    protected float probability_poison = 0f; // 독에 중독시킬 확률
    [SerializeField]
    protected int give_poisonTurn = 0; // 중독시킬 턴
    [SerializeField]
    protected float poisonDamagePercent = 0.1f; // 현재 공격력에서 몇퍼센트 독 데미지를 줄건지

    protected int nowPoisoned = 0; // 남은 중독 턴수, 0 이상이면 독에 중독된 상태
    protected int realPoisonDamage = 0; // 실제 독 데미지

    #endregion

    // Start is called before the first frame update
    void Start()
    {

    }

    public int CalculatePoisonDamage()
    {
        realPoisonDamage = Mathf.RoundToInt(attackDamage * poisonDamagePercent);

        return realPoisonDamage;
    }

    #region Get

    public float GetHp()
    {
        return hp;
    }

    public float GetCurHp()
    {
        return curHp;
    }

    public float GetAttackDamage()
    {
        return attackDamage;
    }

    public float GetDefense()
    {
        return defense;
    }

    public float GetCriticalDamage()
    {
        return criticalDamage;
    }

    public float GetCriticalRate()
    {
        return criticalRate;
    }

    public int GetNowPoisoned()
    {
        return nowPoisoned;
    }

    #endregion

    #region Set

    public void SetHp(float _Hp)
    {
        hp = _Hp;
    }

    public void SetAttackDamage(float _AttackDamage)
    {
        attackDamage = _AttackDamage;
    }

    public void SetDefense(float _Defense)
    {
        defense = _Defense;
    }

    public void SetCriticalDamage(float _CriticalDamage)
    {
        criticalDamage = _CriticalDamage;
    }

    public void SetCriticalRate(float _CriticalRate)
    {
        criticalRate = _CriticalRate;
    }

    public void SetNowPoisoned(int _Int)
    {
        nowPoisoned = _Int;
    }

    #endregion

    #region Plus

    public void PlusHp(float _Plus)
    {
        hp += _Plus;
    }

    public virtual void PlusCurHp(float _Plus)
    {
        curHp += _Plus;
    }

    public void PlusAttackDamage(float _Plus)
    {
        attackDamage += _Plus;
    }

    public void PlusCriticalDamage(float _Plus)
    {
        criticalDamage += _Plus;
    }

    public void PlusCriticalRate(float _Plus)
    {
        criticalRate += _Plus;
    }

    public void PlusNowPoisoned(int _Plus)
    {
        nowPoisoned += _Plus;
    }
    #endregion
}
