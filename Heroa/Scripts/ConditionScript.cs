using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConditionScript : MonoBehaviour
{

    public ConditionType condition;

    [SerializeField]
    private Image condition_Image = null;
    [SerializeField]
    private TextMeshProUGUI sustainmentTime_Text = null;
    [SerializeField]
    private int sustainmentTIme;

    private bool isFirst = true; // 상태이상을 적용했을때 다음턴부터 효과 작용

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetCondition(ConditionType _condition, int _SustainmentTime)
    {
        condition_Image.gameObject.SetActive(true);
        sustainmentTime_Text.gameObject.SetActive(true);

        condition = _condition;
        sustainmentTIme = _SustainmentTime;
        sustainmentTime_Text.text = _SustainmentTime + "";

        isFirst = true;

        switch (_condition)
        {
            case ConditionType.Poison:
                condition_Image.sprite = Resources.Load<Sprite>("Images/ConditionImages/Poison");
                break;
            case ConditionType.ChargingAttack:
                condition_Image.sprite = Resources.Load<Sprite>("Images/ConditionImages/ChargingIcon");
                break;
            default:
                Debug.LogError("ConditionScript - Nonvalidated AbnormalCondition");
                break;
        }
    }

    public void EndTurn()
    {
        if (isFirst == true)
        {
            isFirst = false;
        }
        else
        {
            Action_Condition();

            sustainmentTIme -= 1;
            sustainmentTime_Text.text = sustainmentTIme + "";
        }

        if (sustainmentTIme <= 0)
            this.gameObject.SetActive(false);
    }

    #region Action_Condition

    public void Action_Condition()
    {
        switch (condition)
        {
            case ConditionType.Poison:
                Action_Poison();
                break;
            default:
                Debug.LogWarning("ConditionScript - Nonvalidated AbnormalCondition name");
                break;
        }
    }
    public void Action_Poison()
    {
        if (this.transform.parent.name == "Hero_AbnormalConditionGrid")
        {
            // 히어로에게 독 데미지주기

            HeroScript.instance.PlusNowPoisoned(-1);
            int realPoisonDamage = MonsterScript.instance.CalculatePoisonDamage();
            HeroScript.instance.SetNowPoisoned(sustainmentTIme);

            GameManager.instance.CreateFloatingText(realPoisonDamage, "Hero");
            HeroScript.instance.PlusCurHp(-realPoisonDamage);
        }
        else if (this.transform.parent.name == "Monster_AbnormalConditionGrid")
        {
            // 몬스터에게 독 데미지주기

            MonsterScript.instance.PlusNowPoisoned(-1);
            int realPoisonDamage = HeroScript.instance.CalculatePoisonDamage();
            MonsterScript.instance.SetNowPoisoned(sustainmentTIme);

            GameManager.instance.CreateFloatingText(realPoisonDamage, "Monster");
            MonsterScript.instance.PlusCurHp(-realPoisonDamage);
        }
        else
            Debug.LogError("ConditionScript - Nonvalidated Position");
    }

    #endregion

}
