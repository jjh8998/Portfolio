using TMPro;
using UnityEngine;

public class TooltipUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Behavior")]
    [SerializeField] private Vector2 offset = new Vector2(16f, -16f);
    [SerializeField] private float tooltipDelaySeconds = 0.5f;
    [SerializeField] private bool followPointer = true;

    [Header("Boundary")]
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Vector2 screenPadding = new Vector2(12f, 12f);

    private string pendingTitle = string.Empty;
    private string pendingDescription = string.Empty;
    private Vector2 pendingScreenPosition;
    private Vector2 currentScreenPosition;
    private float delayTimer;
    private bool hasPendingRequest;
    private bool isVisible;
    private readonly Vector3[] worldCorners = new Vector3[4];

    private void Awake()
    {
        ResolveCanvas();
        Hide();
    }

    private void OnEnable()
    {
        ResolveCanvas();
        Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void Update()
    {
        if (hasPendingRequest)
        {
            delayTimer += Time.unscaledDeltaTime;
            if (delayTimer >= Mathf.Max(0f, tooltipDelaySeconds))
                ShowPendingRequest();
        }

        if (isVisible && followPointer)
            ApplyPosition(currentScreenPosition);
    }

    public void RequestShow(string _title, string _description, Vector2 _screenPosition)
    {
        pendingTitle = _title ?? string.Empty;
        pendingDescription = _description ?? string.Empty;
        pendingScreenPosition = _screenPosition;
        currentScreenPosition = _screenPosition;
        delayTimer = 0f;
        hasPendingRequest = true;

        SetRootVisible(false);

        if (Mathf.Max(0f, tooltipDelaySeconds) <= 0f)
            ShowPendingRequest();
    }

    public void ShowImmediate(string _title, string _description, Vector2 _screenPosition)
    {
        hasPendingRequest = false;
        currentScreenPosition = _screenPosition;
        SetText(_title, _description);
        SetRootVisible(true);
        Canvas.ForceUpdateCanvases();
        ApplyPosition(_screenPosition);
        isVisible = true;
    }

    public void Hide()
    {
        hasPendingRequest = false;
        delayTimer = 0f;
        isVisible = false;
        SetRootVisible(false);
    }

    public void UpdatePointerPosition(Vector2 _screenPosition)
    {
        currentScreenPosition = _screenPosition;

        if (hasPendingRequest)
            pendingScreenPosition = _screenPosition;

        if (isVisible && followPointer)
            ApplyPosition(_screenPosition);
    }

    private void ShowPendingRequest()
    {
        hasPendingRequest = false;
        ShowImmediate(pendingTitle, pendingDescription, pendingScreenPosition);
    }

    private void SetText(string title, string description)
    {
        if (titleText != null)
            titleText.text = title ?? string.Empty;

        if (descriptionText != null)
            descriptionText.text = description ?? string.Empty;
    }

    private void ApplyPosition(Vector2 screenPosition)
    {
        if (tooltipRect == null)
            return;

        ResolveCanvas();

        Vector2 targetScreenPosition = screenPosition + offset;
        RectTransform parentRect = tooltipRect.parent as RectTransform;
        Camera uiCamera = GetUICamera();

        if (parentRect != null
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPosition, uiCamera, out Vector2 localPosition))
        {
            tooltipRect.anchoredPosition = localPosition;
        }
        else
        {
            tooltipRect.position = targetScreenPosition;
        }

        ClampToScreen();
    }

    private void ClampToScreen()
    {
        if (tooltipRect == null)
            return;

        Camera uiCamera = GetUICamera();
        tooltipRect.GetWorldCorners(worldCorners);

        Vector2 firstCorner = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[0]);
        float minX = firstCorner.x;
        float maxX = firstCorner.x;
        float minY = firstCorner.y;
        float maxY = firstCorner.y;

        for (int i = 1; i < worldCorners.Length; i++)
        {
            Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[i]);
            minX = Mathf.Min(minX, screenCorner.x);
            maxX = Mathf.Max(maxX, screenCorner.x);
            minY = Mathf.Min(minY, screenCorner.y);
            maxY = Mathf.Max(maxY, screenCorner.y);
        }

        float left = Mathf.Max(0f, screenPadding.x);
        float right = Screen.width - Mathf.Max(0f, screenPadding.x);
        float bottom = Mathf.Max(0f, screenPadding.y);
        float top = Screen.height - Mathf.Max(0f, screenPadding.y);

        Vector2 correction = Vector2.zero;
        if (minX < left)
            correction.x = left - minX;
        else if (maxX > right)
            correction.x = right - maxX;

        if (minY < bottom)
            correction.y = bottom - minY;
        else if (maxY > top)
            correction.y = top - maxY;

        if (correction == Vector2.zero)
            return;

        MoveByScreenDelta(correction, uiCamera);
    }

    private void MoveByScreenDelta(Vector2 correction, Camera uiCamera)
    {
        RectTransform parentRect = tooltipRect.parent as RectTransform;
        Vector2 currentScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, tooltipRect.position);
        Vector2 correctedScreenPosition = currentScreenPosition + correction;

        if (parentRect != null
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, correctedScreenPosition, uiCamera, out Vector2 localPosition))
        {
            tooltipRect.anchoredPosition = localPosition;
            return;
        }

        tooltipRect.position += new Vector3(correction.x, correction.y, 0f);
    }

    private Camera GetUICamera()
    {
        ResolveCanvas();

        if (parentCanvas != null)
        {
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (parentCanvas.worldCamera != null)
                return parentCanvas.worldCamera;
        }

        return Camera.main;
    }

    private void ResolveCanvas()
    {
        if (parentCanvas != null)
            return;

        if (tooltipRect != null)
            parentCanvas = tooltipRect.GetComponentInParent<Canvas>();

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
    }

    private void SetRootVisible(bool isActive)
    {
        if (root == null)
            return;

        root.SetActive(isActive);
    }
}
