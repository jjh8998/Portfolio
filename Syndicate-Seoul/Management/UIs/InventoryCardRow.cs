using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 덱 빌더 좌측 컬렉션 영역의 카드 행 아이템.
/// 카드명 / 코스트 / 보유 장수를 표시하고, 행 전체를 클릭하면 덱에 추가한다.
/// IPointerClickHandler를 직접 구현하여 Button 컴포넌트 없이 클릭을 처리한다.
/// </summary>
public class InventoryCardRow : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text ownedCountText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image typeColorBar;

    private CardData cardData;
    private Action<CardData> onAddClicked;
    private bool isAddable = true;
    private bool isOwned = true;

    // 타입별 색상
    private static readonly Color AttackColor = new Color32(0xFF, 0x4B, 0x63, 0xFF);
    private static readonly Color SkillColor  = new Color32(0x00, 0xBF, 0xE8, 0xFF);
    private static readonly Color CoreColor  = new Color32(0xE8, 0xC3, 0x5A, 0xFF);
    private static readonly Color UnownedGrey = new Color32(0x5D, 0x91, 0xA4, 0x8C);
    private static readonly Color UnownedText = new Color32(0x5D, 0x91, 0xA4, 0xFF);

    /// <summary>
    /// 행 데이터를 설정한다.
    /// </summary>
    /// <param name="data">카드 데이터</param>
    /// <param name="ownedCount">인벤토리 보유 장수. 0이면 미보유로 회색 처리된다.</param>
    /// <param name="onAdd">행 클릭 콜백</param>
    public void Setup(CardData data, int ownedCount, Action<CardData> onAdd)
    {
        cardData = data;
        onAddClicked = onAdd;
        isOwned = ownedCount > 0;

        // 그리드 레이아웃 대응을 위한 크기 고정
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(180, 260);
        }

        if (cardNameText != null)
        {
            cardNameText.text = data.cardName;
            cardNameText.color = CardTierColors.GetNameColor(data.tier);
        }
        if (costText != null)       costText.text       = data.cost.ToString();
        // 미보유 시 보유 장수 텍스트 숨김
        if (ownedCountText != null) ownedCountText.text = isOwned ? $"x{ownedCount}" : "";

        if (rarityText != null)
            rarityText.text = data.rarity.ToString();

        // 미보유 카드는 타입 컬러바도 회색으로
        if (typeColorBar != null)
            typeColorBar.color = isOwned ? GetTypeColor(data.type) : UnownedGrey;

        ApplyVisualState();
    }

    /// <summary>
    /// 행 클릭 가능 여부를 설정한다 (덱 추가 한계 도달 시 반투명 스타일).
    /// 미보유 카드는 이 메서드와 무관하게 항상 비활성 상태다.
    /// </summary>
    public void SetAddable(bool addable)
    {
        isAddable = addable;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (!isOwned)
        {
            // 미보유: 전체 어두운 회색 + 가시성 확보를 위해 투명도 상향 (0.5 -> 0.8)
            SetColor(cardNameText, UnownedText, 0.8f);
            SetColor(costText, UnownedText, 0.8f);
            SetColor(rarityText, UnownedText, 0.8f);
            SetColor(ownedCountText, UnownedText, 0.8f);
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color32(0x03, 0x0F, 0x1C, 0xCC);
            }
        }
        else if (!isAddable)
        {
            // 보유하지만 추가 불가 (인벤 초과 등): 반투명
            SetAlpha(cardNameText, 0.4f);
            SetAlpha(costText, 0.4f);
            SetAlpha(rarityText, 0.4f);
            SetAlpha(ownedCountText, 0.4f);
            if (backgroundImage != null)
            {
                var c = backgroundImage.color;
                backgroundImage.color = new Color(c.r, c.g, c.b, 0.4f);
            }
        }
        else
        {
            // 정상 상태 (보유 및 추가 가능)
            SetAlpha(cardNameText, 1f);
            SetAlpha(costText, 1f);
            SetAlpha(rarityText, 1f);
            SetAlpha(ownedCountText, 1f);
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color32(0x05, 0x1C, 0x2D, 0xF0);
            }
        }
    }

    /// <summary>
    /// 행 클릭 시 EventSystem에서 자동 호출된다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isOwned || !isAddable) return;
        onAddClicked?.Invoke(cardData);
    }

    private static void SetAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        var c = text.color;
        text.color = new Color(c.r, c.g, c.b, alpha);
    }

    private static void SetColor(TMP_Text text, Color color, float alpha)
    {
        if (text == null) return;
        text.color = new Color(color.r, color.g, color.b, alpha);
    }

    private static Color GetTypeColor(CardType type)
    {
        return type switch
        {
            CardType.Attack => AttackColor,
            CardType.Skill  => SkillColor,
            CardType.Core  => CoreColor,
            _               => Color.white,
        };
    }
}
