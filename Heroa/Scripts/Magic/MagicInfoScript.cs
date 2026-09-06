using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MagicInfoScript : MonoBehaviour
{
    [Header ("Infoes")]
    [SerializeField]
    private Image magicImage;
    [SerializeField]
    private TextMeshProUGUI magicNameText;
    [SerializeField]
    private TextMeshProUGUI magicDescriptionText;

    private Magic myMagic;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetMagicInfo(Magic _Magic)
    {
        // 선택된 마법 정보 설정
        myMagic = _Magic;
        magicImage.sprite = _Magic.magicSprite;
        magicNameText.text = _Magic.name;
        magicDescriptionText.text = _Magic.settingDescription;
    }

    public void OnclickShowSelectMagicButton()
    {
        MagicSelectManager.instance.OnShowSelectedMagicInfoPanel(myMagic);
    }
}
