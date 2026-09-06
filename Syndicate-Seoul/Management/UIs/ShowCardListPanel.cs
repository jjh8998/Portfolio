using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowCardListPanel : MonoBehaviour, IEscapeClosable
{
    private const float SelectedCardScaleMultiplier = 1.06f;
    private static readonly Color SelectedOutlineColor = new Color(0.2f, 0.85f, 0.8f, 1f);

    [SerializeField] private GameObject panel;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private HandCardUI cardPrefab;
    [SerializeField] private TMP_Text emptyText;
    [Tooltip("카드 셀의 크기 (240x340이 기본)")]
    [SerializeField] private Vector2 cardCellSize = new Vector2(240f, 340f);
    [SerializeField] private Vector2 cardSpacing = new Vector2(80f, 100f);
    [SerializeField] private RectOffset cardPadding;
    [SerializeField] private int cardColumnCount = 4;
    [SerializeField] private bool selectable = false;
    [Header("Reveal VFX")]
    [SerializeField] private bool playRevealVfx = true;
    [SerializeField] private CardDrawAssembleVfx revealVfx;
    [SerializeField, Min(0f)] private float revealStagger = 0.05f;
    [SerializeField, Range(0f, 1f)] private float revealFrameFadeStart = 0.02f;
    [SerializeField, Range(0f, 1f)] private float revealFrameFadeEnd = 0.72f;
    [SerializeField, Min(1f)] private float revealFramesPerSecond = 24f;
    [SerializeField, Min(0.01f)] private float revealFallbackDuration = 0.28f;

    private Action<string> onCardSelected;
    private HandCardUI selectedCardUI;
    private Vector3 selectedCardOriginalScale = Vector3.one;
    private Coroutine revealRoutine;
    private readonly List<Coroutine> revealCardRoutines = new List<Coroutine>();

    public bool IsOpen => panel != null && panel.activeInHierarchy;

    /// <summary>
    /// 카드팩 개봉을 누른 순간부터 개봉 연출 + 카드 등장 연출이 끝날 때까지 true.
    /// 이 동안에는 EscapeUIRouter가 ESC 입력을 무시한다.
    /// </summary>
    public static bool IsEscBlocked { get; private set; }

    public static void SetEscBlocked(bool _blocked)
    {
        IsEscBlocked = _blocked;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetEscBlockedOnLoad()
    {
        IsEscBlocked = false;
    }

    private void Awake()
    {
        if (cardPadding == null)
            cardPadding = new RectOffset(8, 8, 8, 8);

        ResolveCardPrefab();
    }

    public void Open(List<string> _gainedCardIds)
    {
        if (panel != null)
            panel.SetActive(true);
        else
            Debug.LogWarning("[ShowGetCardsPanel] Panel is null.");

        ResetSelection();
        Clear();

        if (_gainedCardIds == null || _gainedCardIds.Count == 0)
        {
            SetEmptyText("카드가 없습니다.", true);
            SetEscBlocked(false);
            return;
        }

        if (contentRoot == null)
        {
            SetEmptyText("카드 결과 영역을 찾을 수 없습니다.", true);
            SetEscBlocked(false);
            return;
        }

        ConfigureGridIfNeeded();

        HandCardUI resolvedCardPrefab = ResolveCardPrefab();
        if (resolvedCardPrefab == null)
        {
            SetEmptyText("카드 결과 프리팹을 찾을 수 없습니다.", true);
            SetEscBlocked(false);
            return;
        }

        SetEmptyText(string.Empty, false);
        int createdCount = 0;
        List<HandCardUI> createdCards = playRevealVfx ? new List<HandCardUI>(_gainedCardIds.Count) : null;

        for (int i = 0; i < _gainedCardIds.Count; i++)
        {
            string cardId = _gainedCardIds[i];
            CardData cardData = CardDatabase.Instance.GetById(cardId);
            if (cardData == null)
            {
                Debug.LogWarning($"[ShowGetCardsPanel] Gained card data not found. cardId={cardId}");
                continue;
            }

            HandCardUI cardUI = Instantiate(resolvedCardPrefab, contentRoot);
            ConfigureDeckBuilderCardLayout(cardUI);
            cardUI.Setup(cardData, -1, 1);
            cardUI.SetGhostMode(false);
            cardUI.SetClickCallback(selectable ? OnClickCard : null);
            cardUI.gameObject.SetActive(true);
            createdCards?.Add(cardUI);
            createdCount++;
        }

        if (createdCount <= 0)
            SetEmptyText("카드가 없습니다.", true);

        Canvas.ForceUpdateCanvases();
        PlayRevealVfx(createdCards);
    }

    public void Open(List<string> _gainedCardIds, bool _selectable, Action<string> _onCardSelected)
    {
        SetSelectable(_selectable);
        SetOnCardSelected(_onCardSelected);
        Open(_gainedCardIds);
    }

    public void Close()
    {
        // 카드 등장 연출이 재생되는 동안은 닫기 동작을 원천 차단하여 비주얼 중첩 충돌을 방지합니다.
        if (revealRoutine != null)
        {
            Debug.Log("[ShowCardListPanel] 카드가 등장하는 연출 중에는 패널을 닫을 수 없습니다.");
            return;
        }

        ResetSelection();

        if (panel != null)
            panel.SetActive(false);
        else
            Debug.LogWarning("[ShowGetCardsPanel] Panel is null.");

        SetEscBlocked(false);
    }

    private void OnDisable()
    {
        // 연출 도중 패널/오브젝트가 비활성화되어도 ESC 차단이 영구히 남지 않도록 안전 해제.
        SetEscBlocked(false);
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        // 카드 등장 연출이 재생되는 동안은 ESC 키를 통한 패널 닫기를 차단합니다.
        if (revealRoutine != null)
            return false;

        Close();
        return true;
    }

    public void SetSelectable(bool _selectable)
    {
        selectable = _selectable;

        if (!selectable)
            ResetSelection();
    }

    public void SetOnCardSelected(Action<string> _onCardSelected)
    {
        onCardSelected = _onCardSelected;
    }

    private void Clear()
    {
        ResetSelection();
        StopRevealVfx();

        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }

    private void PlayRevealVfx(List<HandCardUI> cards)
    {
        StopRevealVfx();

        if (!playRevealVfx || cards == null || cards.Count <= 0)
        {
            SetEscBlocked(false);
            return;
        }

        CardDrawAssembleVfx resolvedVfx = ResolveRevealVfx();
        if (resolvedVfx == null)
        {
            SetEscBlocked(false);
            return;
        }

        resolvedVfx.ConfigureForCardPackReveal(
            revealFrameFadeStart,
            revealFrameFadeEnd,
            revealFramesPerSecond,
            revealFallbackDuration);
        revealRoutine = StartCoroutine(PlayRevealVfxRoutine(cards, resolvedVfx));
    }

    private IEnumerator PlayRevealVfxRoutine(List<HandCardUI> cards, CardDrawAssembleVfx resolvedVfx)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            HandCardUI cardUI = cards[i];
            if (cardUI != null)
                revealCardRoutines.Add(StartCoroutine(resolvedVfx.Play(cardUI, i * revealStagger)));
        }

        float duration = resolvedVfx.EstimatedDuration + Mathf.Max(0, cards.Count - 1) * revealStagger;
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        revealCardRoutines.Clear();
        revealRoutine = null;

        // 카드 등장 연출이 모두 끝났으므로 ESC 입력을 다시 허용한다.
        SetEscBlocked(false);
    }

    private void StopRevealVfx()
    {
        for (int i = 0; i < revealCardRoutines.Count; i++)
        {
            if (revealCardRoutines[i] != null)
                StopCoroutine(revealCardRoutines[i]);
        }
        revealCardRoutines.Clear();

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
    }

    private CardDrawAssembleVfx ResolveRevealVfx()
    {
        if (revealVfx != null)
            return revealVfx;

        revealVfx = GetComponent<CardDrawAssembleVfx>();
        if (revealVfx != null)
            return revealVfx;

        revealVfx = gameObject.AddComponent<CardDrawAssembleVfx>();
        return revealVfx;
    }

    private HandCardUI ResolveCardPrefab()
    {
        if (cardPrefab != null)
            return cardPrefab;

        cardPrefab = Resources.Load<HandCardUI>("Prefabs/card");
        if (cardPrefab != null)
            return cardPrefab;

        GameObject prefabObject = Resources.Load<GameObject>("Prefabs/card");
        if (prefabObject != null)
            cardPrefab = prefabObject.GetComponent<HandCardUI>();

        return cardPrefab;
    }

    private void ConfigureGridIfNeeded()
    {
        if (contentRoot == null)
            return;

        GridLayoutGroup gridLayoutGroup = contentRoot.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup == null)
            gridLayoutGroup = contentRoot.gameObject.AddComponent<GridLayoutGroup>();

        if (cardPadding == null)
            cardPadding = new RectOffset(8, 8, 8, 8);

        gridLayoutGroup.cellSize = cardCellSize;
        gridLayoutGroup.spacing = cardSpacing;
        gridLayoutGroup.padding = cardPadding;
        gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = Mathf.Max(1, cardColumnCount);
    }

    private void ConfigureDeckBuilderCardLayout(HandCardUI cardUI)
    {
        if (cardUI == null)
            return;

        LayoutElement layoutElement = cardUI.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = cardUI.gameObject.AddComponent<LayoutElement>();

        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void OnClickCard(HandCardUI cardUI)
    {
        if (!selectable || cardUI == null || cardUI.CardData == null)
            return;

        SetSelectedCard(cardUI);

        string cardId = cardUI.CardData.id;
        if (string.IsNullOrWhiteSpace(cardId))
            return;

        onCardSelected?.Invoke(cardId);
    }

    private void SetEmptyText(string _message, bool _isVisible)
    {
        if (emptyText == null)
            return;

        emptyText.text = _message;
        emptyText.gameObject.SetActive(_isVisible);
    }

    private void SetSelectedCard(HandCardUI cardUI)
    {
        if (!selectable || cardUI == null)
            return;

        if (ReferenceEquals(selectedCardUI, cardUI))
        {
            ApplySelectedCardVisual(selectedCardUI);
            return;
        }

        ResetSelection();
        selectedCardUI = cardUI;
        selectedCardOriginalScale = cardUI.transform.localScale;
        ApplySelectedCardVisual(cardUI);
    }

    private void ResetSelection()
    {
        if (selectedCardUI == null)
            return;

        ClearSelectedCardVisual(selectedCardUI);
        selectedCardUI = null;
        selectedCardOriginalScale = Vector3.one;
    }

    private void ApplySelectedCardVisual(HandCardUI cardUI)
    {
        if (cardUI == null)
            return;

        cardUI.transform.localScale = selectedCardOriginalScale * SelectedCardScaleMultiplier;

        Graphic highlightGraphic = ResolveHighlightGraphic(cardUI);
        if (highlightGraphic == null)
            return;

        Outline outline = highlightGraphic.GetComponent<Outline>();
        if (outline == null)
            outline = highlightGraphic.gameObject.AddComponent<Outline>();

        outline.effectColor = SelectedOutlineColor;
        outline.effectDistance = new Vector2(6f, 6f);
        outline.useGraphicAlpha = false;
        outline.enabled = true;
    }

    private void ClearSelectedCardVisual(HandCardUI cardUI)
    {
        if (cardUI == null)
            return;

        cardUI.transform.localScale = selectedCardOriginalScale;

        Graphic highlightGraphic = ResolveHighlightGraphic(cardUI);
        if (highlightGraphic == null)
            return;

        Outline outline = highlightGraphic.GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }

    private static Graphic ResolveHighlightGraphic(HandCardUI cardUI)
    {
        if (cardUI == null)
            return null;

        Graphic graphic = cardUI.GetComponent<Graphic>();
        if (graphic != null)
            return graphic;

        Graphic[] graphics = cardUI.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                return graphics[i];
        }

        return null;
    }
}
