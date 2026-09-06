using UnityEngine;
using UnityEngine.EventSystems;

public class WarDeclarationWarningPopUpButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CitySharePopUpUIController controller;

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData == null || controller == null)
            return;

        if (_eventData.button == PointerEventData.InputButton.Left)
        {
            controller.OpenTargetCityFromPopup();
            return;
        }

        if (_eventData.button == PointerEventData.InputButton.Right)
            controller.ClosePopup();
    }
}
