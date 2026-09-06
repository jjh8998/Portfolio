using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class EscapeUIRouter : MonoBehaviour
{
    private static readonly List<MonoBehaviour> closableBuffer = new List<MonoBehaviour>();
    private const BindingFlags PopUpUIsReferenceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Tooltip("ESC close priority. Recommended order: DeckBuilderUI, ResearchPanelController, CityUIController.")]
    [SerializeField] private MonoBehaviour[] closableUis;
    [SerializeField] private PauseMenuController pauseMenuController;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
            return;

        // 카드팩 개봉 연출(개봉 VFX + 카드 등장)이 재생되는 동안에는 ESC 입력을 무시한다.
        if (ShowCardListPanel.IsEscBlocked)
            return;

        if (TryCloseUnregisteredClosable())
            return;

        if (TryCloseConfiguredClosable())
            return;

        if (HasOpenPopUpUIs())
            return;

        if (pauseMenuController != null)
            pauseMenuController.Toggle();
    }

    private bool TryCloseConfiguredClosable()
    {
        if (closableUis == null)
            return false;

        for (int i = 0; i < closableUis.Length; i++)
        {
            MonoBehaviour closableBehaviour = closableUis[i];
            if (closableBehaviour == null)
                continue;

            IEscapeClosable closable = closableBehaviour as IEscapeClosable;
            if (closable == null)
            {
                Debug.LogWarning($"[EscapeUIRouter] {closableBehaviour.name} does not implement IEscapeClosable.");
                continue;
            }

            if (!closable.IsOpen)
                continue;

            if (closable.CloseByEscape())
                return true;
        }

        return false;
    }

    private bool TryCloseUnregisteredClosable()
    {
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        closableBuffer.Clear();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this || IsConfiguredClosable(behaviour))
                continue;

            IEscapeClosable closable = behaviour as IEscapeClosable;
            if (closable == null || !closable.IsOpen)
                continue;

            closableBuffer.Add(behaviour);
        }

        MonoBehaviour topmost = null;
        int topmostDepth = -1;
        for (int i = 0; i < closableBuffer.Count; i++)
        {
            MonoBehaviour behaviour = closableBuffer[i];
            int depth = GetHierarchyDepth(behaviour.transform);
            if (depth <= topmostDepth)
                continue;

            topmost = behaviour;
            topmostDepth = depth;
        }

        closableBuffer.Clear();

        IEscapeClosable topmostClosable = topmost as IEscapeClosable;
        return topmostClosable != null && topmostClosable.CloseByEscape();
    }

    private static bool HasOpenPopUpUIs()
    {
        GameObject popUpUIs = GameObject.Find("PopUpUIs");
        if (popUpUIs == null)
            return false;

        for (int i = 0; i < popUpUIs.transform.childCount; i++)
        {
            Transform child = popUpUIs.transform.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private static bool IsPopUpUIsClosable(MonoBehaviour behaviour)
    {
        return IsUnderPopUpUIs(behaviour.transform) || ReferencesPopUpUIs(behaviour);
    }

    private static bool ReferencesPopUpUIs(MonoBehaviour behaviour)
    {
        FieldInfo[] fields = behaviour.GetType().GetFields(PopUpUIsReferenceBindingFlags);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (typeof(GameObject).IsAssignableFrom(field.FieldType))
            {
                GameObject referencedObject = field.GetValue(behaviour) as GameObject;
                if (referencedObject != null && IsUnderPopUpUIs(referencedObject.transform))
                    return true;
            }
            else if (typeof(Component).IsAssignableFrom(field.FieldType))
            {
                Component referencedComponent = field.GetValue(behaviour) as Component;
                if (referencedComponent != null && IsUnderPopUpUIs(referencedComponent.transform))
                    return true;
            }
        }

        return false;
    }

    private static bool IsUnderPopUpUIs(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "PopUpUIs")
                return true;

            current = current.parent;
        }

        return false;
    }

    private bool IsConfiguredClosable(MonoBehaviour behaviour)
    {
        if (closableUis == null)
            return false;

        for (int i = 0; i < closableUis.Length; i++)
        {
            if (closableUis[i] == behaviour)
                return true;
        }

        return false;
    }

    private static int GetHierarchyDepth(Transform transform)
    {
        int depth = 0;
        while (transform != null)
        {
            depth++;
            transform = transform.parent;
        }

        return depth;
    }
}
