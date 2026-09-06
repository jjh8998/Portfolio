using UnityEngine;
using UnityEngine.EventSystems;

public class ResearchCompletePopUpButtonScript : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ResearchCompletePopUpUIController controller;

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData == null || controller == null)
            return;

        if (_eventData.button == PointerEventData.InputButton.Left)
        {
            controller.OpenResearchPanelFromPopup();
            return;
        }

        if (_eventData.button == PointerEventData.InputButton.Right)
            controller.ClosePopup();
    }
}
