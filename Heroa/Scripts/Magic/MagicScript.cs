using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MagicScript : MonoBehaviour
{
    /*
     * 플레이어가 선택한 마법에 대해 다루는 스크립트
     */

    [Header("Magic Info")]
    private Magic myMagic;
    [SerializeField]
    private TextMeshProUGUI magicName;
    [SerializeField]
    private Image magicImage;
    [SerializeField]
    private GameObject selectedBackgroundImage;

    [SerializeField]
    private GameObject cooldownObj;
    private TextMeshProUGUI cooldownText;

    private int cooldown = 0;
    private int nowCooldown = 0;

    private bool isMainMenu;
    private bool isChangeMagic; // 마법 선택 패널에서 마법을 바꿀 상태일때 true

    private bool canCast = true;

    // private Image btnImage;

    // Start is called before the first frame update
    void Start()
    {
        cooldownText = cooldownObj.GetComponent<TextMeshProUGUI>();
        // btnImage = GetComponent<Image>();

        cooldownObj.SetActive(false);
    }

    public void OnClickedMagicButton()
    {
        // 전투시에는 마법 사용, 메인로비에서는 마법 교체

        if (isMainMenu == false)
        {
            if (canCast == true)
            {
                if (MagicManager.instance.GetNowCastMagic() == false)
                {
                    MagicManager.instance.SetNowCastMagic(true);
                    MagicManager.instance.SetNowMagicStat(myMagic);

                    WordCollector.instance.SetIsEnglish_Korean(false); // 영단어 한-영으로 바꾸기

                    selectedBackgroundImage.SetActive(true);

                    nowCooldown = cooldown;
                    canCast = false;
                }
            }
        }
        else
        {
            if (isChangeMagic == true)
            {
                int overlapIndex = -1;

                // 중복 확인
                for (int i = 0; i < 3; i++)
                {
                    if (MagicManager.instance.GetChangeMagic().ID == DatabaseManager.instance.mySelectedMagicData.selectedMagicID[i])
                    {
                        overlapIndex = i;
                        break;
                    }
                }

                for (int j = 0; j < 3; j++)
                {
                    if (myMagic.ID == DatabaseManager.instance.mySelectedMagicData.selectedMagicID[j])
                    {
                        MagicManager.instance.ChangeMagic_Step2(j, overlapIndex, myMagic.ID);
                        break;
                    }
                }
            }
        }
    }

    public void CheckCooldown()
    {
        if (nowCooldown <= 1)
        {
            SetImageColor("white");
            canCast = true;
            cooldownObj.SetActive(false);
        }
        else
        {
            MagicManager.instance.SetNowCastMagic(false);
            SetImageColor("gray");
            selectedBackgroundImage.SetActive(false);

            nowCooldown -= 1;
            cooldownObj.SetActive(true);
            cooldownText.text = "쿨타임 : " + nowCooldown.ToString();

            // Debug.Log("MagicScript : Cooldown");
        }
    }

    private void SetImageColor(string _Color)
    {
        try
        {
            switch (_Color)
            {
                case "white":
                    magicImage.color = Color.white;
                    break;

                case "gray":
                    magicImage.color = Color.gray;
                    break;
            }
        }
        catch
        {
            Debug.LogError("MagicScript : btnImage is NULL!");
        }
    }

    public void SetMagicScript(Magic _Magic)
    {
        myMagic = _Magic;

        magicName.text = _Magic.name;
        magicImage.sprite = _Magic.magicSprite;

        cooldown = _Magic.cooldown;
    }

    public void SetIsMainMenu(bool _Bool)
    {
        isMainMenu = _Bool;
    }

    public void SetIsChangeMagic(bool _Bool)
    {
        isChangeMagic = _Bool;
    }
}
