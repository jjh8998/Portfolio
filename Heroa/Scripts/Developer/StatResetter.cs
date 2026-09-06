using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatResetter : MonoBehaviour
{
    [Header("Base Stat @ LV1 (장비 미착용)")]
    [SerializeField] private float baseHp_Lv1 = 200f;
    [SerializeField] private float baseAtk_Lv1 = 10f;
    [SerializeField] private float baseDef_Lv1 = 0f;
    [SerializeField] private float baseCritDmg_Lv1 = 1.5f; // 배수
    [SerializeField] private float baseCritRate_Lv1 = 10f; // %

    [Header("Per Level Growth (장비 미착용 성장량)")]
    [SerializeField] private float hpPerLevel = 10f;
    [SerializeField] private float atkPerLevel = 1f;
    [SerializeField] private float defPerLevel = 0f;
    [SerializeField] private float cdPerLevel = 0.0f; // 보통 고정치(배수)는 성장X, 필요시 조정
    [SerializeField] private float crPerLevel = 0.0f; // 필요시 조정

    [Header("Options")]
    [Tooltip("재계산 후 HeroScript로 즉시 반영할지")]
    [SerializeField] private bool pushToHeroScript = true;

    private DatabaseManager DB => DatabaseManager.instance;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DB != null && DB.isFinishedLoading);

        ResetPlayerStats();
    }

    [ContextMenu("Reset Player Stats Now")]
    public void ResetPlayerStats()
    {
        if (DB == null)
        {
            Debug.LogError("[StatResetter] DatabaseManager.instance가 없습니다.");
            return;
        }

        // 1) 레벨 기반 '기본 스탯(장비 미착용 가정)' 계산
        int lv = Mathf.Max(1, DB.GetLV());
        int deltaLv = lv - 1;

        float baseHp = baseHp_Lv1 + hpPerLevel * deltaLv;
        float baseAtk = baseAtk_Lv1 + atkPerLevel * deltaLv;
        float baseDef = baseDef_Lv1 + defPerLevel * deltaLv;
        float baseCD = baseCritDmg_Lv1 + cdPerLevel * deltaLv;
        float baseCR = baseCritRate_Lv1 + crPerLevel * deltaLv;

        // 2) 현재 장비들의 '실제(강화 포함) 스탯' 합산
        EquipSum equip = SumEquippedStats();

        // 3) 최종 스탯 = 기본 + 장비합
        float finalHp = baseHp + equip.hp;
        float finalAtk = baseAtk + equip.atk;
        float finalDef = baseDef + equip.def;
        float finalCD = baseCD + equip.cd;
        float finalCR = baseCR + equip.cr;

        finalHp = Sanitize(finalHp, 0f, 999999f);
        finalAtk = Sanitize(finalAtk, 0f, 99999f);
        finalDef = Sanitize(finalDef, 0f, 99999f);
        finalCD = Sanitize(finalCD, 0f, 1000f);
        finalCR = Mathf.Clamp(finalCR, 0f, 100f);        // 반드시 0~100%

        // 4) DB 에 덮어쓰기(세이브 포함)
        DB.SetPlayer_Hp(finalHp);
        DB.SetPlayer_AttackDamage(finalAtk);
        DB.SetPlayer_Defense(finalDef);
        DB.SetPlayer_CriticalDamage(finalCD);
        DB.SetPlayer_CriticalRate(finalCR);

        // 5) (옵션) HeroScript로 즉시 반영
        if (pushToHeroScript)
        {
            DB.UpdatePlayerData(); // HeroScript.instance 에 세팅
        }

        Debug.Log($"[StatResetter] 스탯 초기화 완료 (LV {lv})  " +
                  $"Final => HP:{finalHp}, ATK:{finalAtk}, DEF:{finalDef}, CD:{finalCD}, CR:{finalCR}");
    }

    private struct EquipSum { public float hp, atk, def, cd, cr; }

    /// <summary>
    /// 현재 장착중인 각 슬롯의 MyItem과 Item을 매칭해
    /// DatabaseManager.CalculateRealItemStat(...)로 실스탯을 갱신한 뒤 합산.
    /// </summary>
    /// 
    private EquipSum SumEquippedStats()
    {

        EquipSum sum = new EquipSum();
        var equipData = DB.myItemInEquipmentData?.myitemData;
        var itemDB = DB.itemList;

        if (equipData == null || itemDB == null)
            return sum;

        // 슬롯 순회 (무기/머리/상의/하의/신발)
        for (int i = 0; i < equipData.Count; i++)
        {
            MyItem eq = equipData[i];
            if (eq == null || eq.itemID == 0) continue;

            // itemID로 Item 찾기
            Item refItem = null;
            for (int j = 0; j < itemDB.Count; j++)
            {
                if (itemDB[j].itemID == eq.itemID)
                {
                    refItem = itemDB[j];
                    break;
                }
            }
            if (refItem == null) continue;

            // 장비 실스탯(강화 반영) 갱신
            DB.CalculateRealItemStat(refItem, eq);

            // 합산
            var eqp = refItem.equipment;
            sum.hp += Safe(eqp.realHp);
            sum.atk += Safe(eqp.realDamage);
            sum.def += Safe(eqp.realDefense);
            sum.cd += Safe(eqp.realCriticalDamage);
            sum.cr += Safe(eqp.realCriticalRate);
        }

        return sum;
    }
    static float Safe(float v) => (float.IsNaN(v) || float.IsInfinity(v)) ? 0f : v;

    private static float Sanitize(float v, float min, float max)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return min;
        return Mathf.Clamp(v, min, max);
    }
}
