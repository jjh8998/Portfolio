using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MagicManager : MonoBehaviour
{

    public static MagicManager instance;

    public GameObject magicGrid;

    public Parser parser;

    public bool pendingNextTurnAction; // 마법 사용 후 다음턴 행동 예약을 위한 변수

    private DatabaseManager DB;
    private List<Magic> magicList;
    [SerializeField]
    private List<MagicScript> magicScripts;

    private GameObject prefab_Magic;

    private bool nowCastMagic = false; // 지금 마법 캐스팅중
    private Magic nowMagic;

    [SerializeField]
    private bool isMainMenu;

    private Magic changeMagic;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        DB = DatabaseManager.instance;

        if (isMainMenu == true)
        {
            BasicSetting(isMainMenu);
        }
        else if (DB.myStageProgressData.isStage_1_Clear == true)
        {
            BasicSetting(isMainMenu);
        }
        else
        {
            GameManager.instance.SetCanUseMagic(false);
            this.gameObject.SetActive(false);
        }
    }

    public void BasicSetting(bool _IsMainMenu)
    {
        // 파싱하고 저장되 있는 마법들을 만들어서 보여주는 함수

        GameManager.instance.SetCanUseMagic(true);

        // 기본 설정
        magicList = parser.MagicParser("Heroa_MagicList").ToList();

        for (int i = 0; i < magicGrid.transform.childCount; i++)
            magicScripts.Add(magicGrid.transform.GetChild(i).GetComponent<MagicScript>());

        ShowMagicList(_IsMainMenu);
    }

    public void ShowMagicList(bool _IsMainMenu)
    {
        // 마법 생성
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < magicList.Count; j++)
            {
                if (DB.mySelectedMagicData.selectedMagicID[i] == magicList[j].ID)
                {
                    magicScripts[i].SetMagicScript(magicList[j]);
                    magicScripts[i].SetIsMainMenu(_IsMainMenu);
                    break;
                }
            }
        }
    }

    public void ChangeMagic_Step1(Magic _Magic)
    {
        changeMagic = _Magic;
        SetMagicScriptsIsChangeMagic(true);
    }

    public void ChangeMagic_Step2(int _ChangeIndex, int overlapIndex, int overlapMagicID)
    {
        // 중복일 경우 자리에 changeMagic의 원래 자리에 바뀔 인덱스에 있는 마법 넣기
        if (overlapIndex != -1)
            DatabaseManager.instance.SetMySelectedMagicData(overlapIndex, overlapMagicID);

        DatabaseManager.instance.SetMySelectedMagicData(_ChangeIndex, changeMagic.ID);

        ShowMagicList(true);
        MagicSelectManager.instance.blackPanel.SetActive(false);
        SetMagicScriptsIsChangeMagic(false);
    }

    public void ChangeMagic_Overlap_Step2(int _ChangeIndex)
    {
        DatabaseManager.instance.SetMySelectedMagicData(_ChangeIndex, changeMagic.ID);

        BasicSetting(true);
        MagicSelectManager.instance.blackPanel.SetActive(false);
        SetMagicScriptsIsChangeMagic(false);
    }

    public void SetMagicScriptsIsChangeMagic(bool _Bool)
    {
        for (int i = 0; i < 3; i++)
        {
            magicScripts[i].SetIsChangeMagic(_Bool);
        }
    }

    public void CastMagic()
    {
        // magic 정보를 받기위해 필요함

        pendingNextTurnAction = true;

        switch (nowMagic.magicTpye)
        {
            case Magic.MagicType.Attack:
                HeroScript.instance.CalculateMagicDamage(nowMagic);
                break;

            case Magic.MagicType.Heal:
                HeroScript.instance.Heal(nowMagic);
                break;

            case Magic.MagicType.ETC:
                break;

            default:
                Debug.LogError("MagicManager : No Magic type at castMagic");
                break;
        }
    }

    public void EndTurn()
    {
        // 마법의 턴을 까는 함수
        for (int i = 0; i < 3; i++)
        {
            magicScripts[i].CheckCooldown();
        }
    }

    #region Get

    public bool GetNowCastMagic()
    {
        return nowCastMagic;
    }

    public Magic GetChangeMagic()
    {
        return changeMagic;
    }

    #endregion


    #region Set

    public void SetNowCastMagic(bool _Bool)
    {
        nowCastMagic = _Bool;
    }

    public void SetNowMagicStat(Magic _NowMagic)
    {
        nowMagic =  _NowMagic;
    }

    #endregion

}
