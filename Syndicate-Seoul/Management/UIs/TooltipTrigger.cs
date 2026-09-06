using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Controller")]
    [SerializeField] private TooltipUIController tooltipUI;
    [SerializeField] private bool autoFindTooltipUI = true;

    [Header("Content")]
    [SerializeField] private string title;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private bool useObjectNameAsFallbackTitle = true;
    [SerializeField] private bool requireDescription = true;

    private void Awake()
    {
        ResolveTooltipUI();
    }

    private void OnEnable()
    {
        ResolveTooltipUI();
    }

    private void OnDisable()
    {
        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    public void OnPointerEnter(PointerEventData _eventData)
    {
        if (!CanShowTooltip() || _eventData == null)
            return;

        tooltipUI.RequestShow(GetTitle(), description ?? string.Empty, _eventData.position);
    }

    public void OnPointerMove(PointerEventData _eventData)
    {
        if (tooltipUI == null || _eventData == null)
            return;

        tooltipUI.UpdatePointerPosition(_eventData.position);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    public void SetTooltip(string _title, string _description)
    {
        title = _title ?? string.Empty;
        description = _description ?? string.Empty;
    }

    public void ClearTooltip()
    {
        title = string.Empty;
        description = string.Empty;

        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    public void SetTooltipUI(TooltipUIController _tooltipUI)
    {
        tooltipUI = _tooltipUI;
    }

    private bool CanShowTooltip()
    {
        ResolveTooltipUI();

        if (tooltipUI == null)
            return false;

        if (requireDescription && string.IsNullOrWhiteSpace(description))
            return false;

        return !string.IsNullOrWhiteSpace(GetTitle()) || !string.IsNullOrWhiteSpace(description);
    }

    private string GetTitle()
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title;

        return useObjectNameAsFallbackTitle ? gameObject.name : string.Empty;
    }

    private void ResolveTooltipUI()
    {
        if (tooltipUI != null || !autoFindTooltipUI)
            return;

        tooltipUI = FindFirstObjectByType<TooltipUIController>(FindObjectsInactive.Include);
    }
}
