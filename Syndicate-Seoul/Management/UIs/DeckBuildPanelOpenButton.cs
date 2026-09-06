using UnityEngine;

public class DeckBuildPanelOpenButton : MonoBehaviour
{
    [SerializeField] private DeckBuilderUI deckBuilderUI;
    [SerializeField] private CardPackInventoryUIController cardPackInventoryUIController;
    [SerializeField] private FactionManager owner;

    public void OpenDeckBuildPanel()
    {
        if (cardPackInventoryUIController != null)
        {
            cardPackInventoryUIController.CloseCardPackInventoryPanel();
        }
        else
        {
            Debug.LogWarning("[DeckBuildPanelOpenButton] cardPackInventoryUIController is not assigned.");
        }

        if (deckBuilderUI == null)
        {
            Debug.LogWarning("[DeckBuildPanelOpenButton] deckBuilderUI is not assigned.");
            return;
        }

        if (owner == null)
        {
            Debug.LogWarning("[DeckBuildPanelOpenButton] owner is not assigned.");
            return;
        }

        deckBuilderUI.Open(owner);
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.OpenDeckBuilder);
    }
}
