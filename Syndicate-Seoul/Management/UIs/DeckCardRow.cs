using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 덱 빌더 우측 덱 영역의 카드 행 아이템.
/// 카드명 / 코스트 / 덱 내 수량을 표시하고, 행 전체를 클릭하면 1장 제거한다.
/// </summary>
public class DeckCardRow : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text costText;   // 인스펙터 미연결 시 동적 생성
    [SerializeField] private Image typeColorBar;  // 코스트 배경으로 재사용

    private CardData cardData;
    private Action<CardData> onRemoveClicked;

    private void Awake()
    {
        var img = GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
    }

    /// <summary>
    /// 행 데이터를 설정한다.
    /// </summary>
    public void Setup(CardData data, int count, Action<CardData> onRemove)
    {
        cardData = data;
        onRemoveClicked = onRemove;

        if (cardNameText != null)
        {
            cardNameText.text = data.cardName;
            cardNameText.color = CardTierColors.GetNameColor(data.tier);
        }
        if (costText != null) costText.text = data.cost.ToString();

        UpdateCount(count);
    }

    /// <summary>
    /// 덱 내 수량 텍스트를 갱신한다. 0 이하면 0으로 표시되지 않게 방어한다.
    /// </summary>
    public void UpdateCount(int count)
    {
        if (countText != null)
        {
            if (count <= 0) countText.text = ""; // 0이하인 경우 텍스트 비움
            else countText.text = $"x{count}";
        }
    }

    /// <summary>
    /// 행 클릭 시 카드 제거 콜백을 호출한다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardData != null)
        {
            onRemoveClicked?.Invoke(cardData);
        }
    }

}
