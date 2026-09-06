using UnityEngine;
using UnityEngine.EventSystems;

public class FactoryProducePopUpButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private FactoryProductionPopUpUIController controller;

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData == null || controller == null)
            return;

        if (_eventData.button == PointerEventData.InputButton.Left)
        {
            controller.OpenCardPackInventoryFromPopup();
            return;
        }

        if (_eventData.button == PointerEventData.InputButton.Right)
            controller.ClosePopup();
    }
}
