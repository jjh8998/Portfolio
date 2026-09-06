using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClosePanelOnAnyClick : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private bool isEnabled = true;
    [SerializeField] private float ignoreClickDuration = 0.1f;

    private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private int openedFrame = -1;
    private float openedTime;
    private bool hasLoggedMissingTarget;

    public bool IsOpen => targetPanel != null && targetPanel.activeInHierarchy;

    private void Awake()
    {
        if (targetPanel == null)
            targetPanel = gameObject;
    }

    private void OnEnable()
    {
        ResetOpenFrame();
    }

    private void Update()
    {
        if (!isEnabled)
            return;

        if (targetPanel == null)
        {
            if (!hasLoggedMissingTarget)
            {
                Debug.LogWarning("[ClosePanelOnAnyClick] Target panel is null.");
                hasLoggedMissingTarget = true;
            }

            return;
        }

        hasLoggedMissingTarget = false;

        if (!targetPanel.activeInHierarchy)
            return;

        if (Time.frameCount <= openedFrame)
            return;

        if (Time.unscaledTime - openedTime < ignoreClickDuration)
            return;

        if (TutorialManager.IsMouseInputBlocked)
            return;

        if (Input.GetMouseButtonDown(0) && !IsPointerInsideTargetPanel(Input.mousePosition))
        {
            targetPanel.SetActive(false);
            return;
        }

        if (TryGetBeganTouchPosition(out Vector2 touchPosition) && !IsPointerInsideTargetPanel(touchPosition))
            targetPanel.SetActive(false);
    }

    public void ResetOpenFrame()
    {
        openedFrame = Time.frameCount;
        openedTime = Time.unscaledTime;
    }

    public void SetEnabled(bool _isEnabled)
    {
        isEnabled = _isEnabled;
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        targetPanel.SetActive(false);
        return true;
    }

    private bool TryGetBeganTouchPosition(out Vector2 position)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                position = Input.GetTouch(i).position;
                return true;
            }
        }

        position = default;
        return false;
    }

    private bool IsPointerInsideTargetPanel(Vector2 screenPosition)
    {
        if (targetPanel == null)
            return false;

        RectTransform panelRect = targetPanel.transform as RectTransform;
        if (panelRect != null && RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, GetEventCamera(panelRect)))
            return true;

        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject target = raycastResults[i].gameObject;
            if (target == null)
                continue;

            if (target == targetPanel || target.transform.IsChildOf(targetPanel.transform))
                return true;
        }

        return false;
    }

    private static Camera GetEventCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }
}
