using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManagementPopupUIController : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button closeButton;

    public bool IsOpen => popupRoot != null && popupRoot.activeInHierarchy;

    private void Awake()
    {
        ClosePopup();
    }

    private void OnEnable()
    {
        BindCloseButton();
    }

    private void OnDisable()
    {
        UnbindCloseButton();
    }

    private void OnDestroy()
    {
        UnbindCloseButton();
    }

    public void Open(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        ClosePopup();
        return true;
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(ClosePopup);
        closeButton.onClick.AddListener(ClosePopup);
    }

    private void UnbindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(ClosePopup);
    }
}
