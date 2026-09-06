using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpScript : MonoBehaviour
{
    public static ExpScript instance;

    [SerializeField]
    private GameObject levelUpObj;

    private int LV = 0;
    private float levelUpExp = 0;
    private float nowExp = 0;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void LoadLevelData()
    {
        LV = DatabaseManager.instance.GetLV();
        nowExp = DatabaseManager.instance.GetNowExp();
        levelUpExp = DatabaseManager.instance.GetLevelUPExp();
        // Debug.Log(levelUpExp);
    }

    public void CheckLevelUp()
    {
        LoadLevelData();

        if (nowExp >= levelUpExp)
        {
            StartCoroutine(ShowLevelUp());

            nowExp = nowExp - levelUpExp;
            DatabaseManager.instance.SetNowExp(nowExp);
            levelUpExp = levelUpExp * 1.5f;
            DatabaseManager.instance.SetLevelUpExp(levelUpExp);
            LV++;
            DatabaseManager.instance.SetLV(LV);

            // 능력치 상승
            DatabaseManager.instance.PlusPlayerHp(10f);
            HeroScript.instance.PlusCurHp(10f);
            DatabaseManager.instance.PlusPlayerAttackDamage(1f);
            HeroScript.instance.SynchroHeroStat();

            Debug.Log("ExpScript : Level Up!");

            CheckLevelUp(); // 남은 경험치로 레벨업 확인
        }
    }

    IEnumerator ShowLevelUp()
    {
        levelUpObj.SetActive(true);
        yield return new WaitForSeconds(3f);
        levelUpObj.SetActive(false);
    }
}
