using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;

    [Header ("Alarm")]
    [SerializeField]
    private GameObject alarmPanel;
    [SerializeField]
    private TextMeshProUGUI alarmText_Text = null;
    [SerializeField]
    private GameObject bossAlarm;

    [Header("Damage FloatingText")]
    [SerializeField]
    private GameObject floatingText = null;
    [SerializeField]
    private TextMeshProUGUI damage_Text = null;
    [SerializeField]
    private GameObject heroDamageFloatingTextPos = null;
    [SerializeField]
    private GameObject monsterDamageFloatingTextPos = null;
    [SerializeField]
    private GameObject monsterHpBar;

    [Header("Panels")]
    [SerializeField]
    private GameObject pausePanel;
    [SerializeField]
    private GameObject heroDeadPanel;
    [SerializeField]
    private GameObject clearPanel;

    private bool canWordChecking = true; // 단어 버튼을 누를 수 있는지 확인하는 변수
    private bool canUseMagic = false;

    private bool isMonsterDead = false; // 몬스터 죽을때 타이머랑 안겹치기 위함

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void EndTurn()
    {
        ConditionManager.instance.EndTurn();

        if (HeroScript.instance.IsCharging())
        {
            HeroScript.instance.SetChargedAttackReady();
        }

        if (canUseMagic == true)
            MagicManager.instance.EndTurn();

        if (MagicManager.instance.pendingNextTurnAction)
        {
            HeroScript.instance.ChangeHero("Hero");
        }
    }

    public void AddConditionObj(string who, ConditionType condition, int turn)
    {
        bool isHero = who == "Hero";
        ConditionManager.instance.AddCondition(isHero, condition, turn);
    }

    #region FloatingText

    public void CreateFloatingText(float _damage, string _damagedTarget)
    {
        StartCoroutine(SetFloatingText(_damage, _damagedTarget));
    }

    IEnumerator SetFloatingText(float _damage, string _damagedTarget)
    {
        if (_damagedTarget == "Hero")
        {
            // 플레이어 캐릭터가 대미지 받을경우
            floatingText.transform.position = heroDamageFloatingTextPos.transform.position;
        }
        else
        {
            floatingText.transform.position = monsterDamageFloatingTextPos.transform.position;
        }

        damage_Text.text = "" + _damage;
        floatingText.SetActive(true);
        yield return new WaitForSeconds(1f);
        floatingText.SetActive(false);
    }

    #endregion

    public void ReturnMainMenuStage()
    {
        Time.timeScale = 1f;
        LoadingSceneManager.LoadScene("MainMenu");
    }

    public void ShowAlarmPanel(string _String)
    {
        StartCoroutine(ShowAlarmText(_String));
    }

    IEnumerator ShowAlarmText(string _String)
    {

        alarmText_Text.text = _String;
        alarmText_Text.gameObject.SetActive(true);
        alarmPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        alarmText_Text.gameObject.SetActive(false);
        alarmPanel.SetActive(false);
    }

    public IEnumerator ShowBossAlarm()
    {
        alarmPanel.SetActive(true);
        bossAlarm.SetActive(true);
        yield return new WaitForSeconds(2f);
        bossAlarm.SetActive(false);
        alarmPanel.SetActive(false);
    }

    #region GameFlow
    public void PauseWordQuiz()
    {
        // stop word quiz
        TimerScript.instance.timerOn = false;
        GameManager.instance.SetCanWordChecking(false);
    }

    public void ResumeWordQuiz()
    {
        // continue word quiz
        TimerScript.instance.ResetTime();
        TimerScript.instance.timerOn = true;
        GameManager.instance.SetCanWordChecking(true);
        WordCollector.instance.SetIsCollecting(false);
    }

    #endregion


    #region On/Off

    public void OnMonsterHpBar()
    {
        monsterHpBar.SetActive(true);
    }

    public void OffMonsterHpBar()
    {
        monsterHpBar.SetActive(false);
    }

    public void OnPausePanel()
    {
        Time.timeScale = 0f; // 버튼 클릭 금지
        pausePanel.SetActive(true);
    }

    public void OffPausePanel()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void OnHeroDeadPanel()
    {
        Time.timeScale = 0f;
        heroDeadPanel.SetActive(true);
    }


    public void OffHeroDeadPanel()
    {
        Time.timeScale = 1f;
        heroDeadPanel.SetActive(false);
    }

    public void OnClearPanel()
    {
        Time.timeScale = 0f; // 버튼 클릭 금지
        clearPanel.SetActive(true);
    }

    public void OffClearPanel()
    {
        Time.timeScale = 1f;
        clearPanel.SetActive(false);
    }

    #endregion

    #region Get

    public bool GetCanWordChecking()
    {
        return canWordChecking;
    }

    public bool GetCanUseMagic()
    {
        return canUseMagic;
    }

    public bool GetIsMonsterDead()
    {
        return isMonsterDead;
    }

    #endregion

    #region Set

    public void SetCanWordChecking(bool _Bool)
    {
        canWordChecking = _Bool;
    }

    public void SetCanUseMagic(bool _Bool)
    {
        canUseMagic = _Bool;
    }

    public void SetIsMonsterDead(bool _Bool)
    {
        isMonsterDead = _Bool;
    }
    #endregion
}
