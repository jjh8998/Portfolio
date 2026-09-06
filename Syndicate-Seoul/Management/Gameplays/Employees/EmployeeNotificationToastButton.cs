using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EmployeeNotificationToastButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button openCityButton;

    private Action closeAction;
    private Action openCityAction;

    public bool IsOpen => popupRoot != null ? popupRoot.activeInHierarchy : gameObject.activeInHierarchy;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData == null)
            return;

        if (_eventData.button == PointerEventData.InputButton.Left)
        {
            openCityAction?.Invoke();
            return;
        }

        if (_eventData.button == PointerEventData.InputButton.Right)
            closeAction?.Invoke();
    }

    public void SetContent(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;
    }

    public void Show()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void Bind(Action closeAction, Action openCityAction)
    {
        Unbind();

        this.closeAction = closeAction;
        this.openCityAction = openCityAction;

        if (closeButton != null && this.closeAction != null)
            closeButton.onClick.AddListener(OnClickClose);

        if (openCityButton != null && this.openCityAction != null)
            openCityButton.onClick.AddListener(OnClickOpenCity);
    }

    public void Unbind()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);

        if (openCityButton != null)
            openCityButton.onClick.RemoveListener(OnClickOpenCity);

        closeAction = null;
        openCityAction = null;
    }

    private void OnClickClose()
    {
        closeAction?.Invoke();
    }

    private void OnClickOpenCity()
    {
        openCityAction?.Invoke();
    }
}
