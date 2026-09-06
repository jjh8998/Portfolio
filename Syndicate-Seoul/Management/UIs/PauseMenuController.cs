using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TimeController timeController;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private Selectable firstSelectedOnOpen;
    [SerializeField] private SaveSlotPanel saveSlotPanel;
    [SerializeField] private SaveSlotPanel loadSlotPanel;
    [SerializeField] private Button closeButton;

    [Header("Options")]
    [SerializeField] private bool useTimeScale = false;
    [SerializeField] private bool lockCursorWhenClosed = false;
    [SerializeField] private bool unlockCursorWhenOpened = true;

    private bool isOpen;

    public bool IsOpen => isOpen;

    void Awake()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        if (pausePanel == null)
        {
            Debug.LogError("[PauseMenuController] pausePanel이 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        pausePanel.SetActive(false);
        ApplyCursorState(false);
        BindCloseButton();
    }

    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (isOpen)
            return;

        isOpen = true;
        pausePanel.SetActive(true);

        if (timeController != null)
            timeController.SetPaused(true);

        if (useTimeScale)
            Time.timeScale = 0f;

        ApplyCursorState(true);
        SelectFirstUI();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        pausePanel.SetActive(false);

        if (useTimeScale)
            Time.timeScale = 1f;

        if (timeController != null)
            timeController.SetPaused(false);

        ApplyCursorState(false);
        ClearSelectedUI();
    }

    public void ResumeGame()
    {
        Close();
    }

    public void SaveGame()
    {
        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetMode(SaveSlotPanel.PanelMode.Save);
            saveSlotPanel.Open();
            return;
        }

        // 폴백: 패널 미연결 시 현재 세션 슬롯 또는 빈 슬롯 자동 선택.
        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        if (saveManager == null)
        {
            Debug.LogWarning("[PauseMenuController] SaveManager not found.");
            return;
        }

        int fallbackSlot = ResolveFallbackSaveSlot();
        if (fallbackSlot >= 0)
            saveManager.SaveCurrentGame(fallbackSlot);
        else
            Debug.LogWarning("[PauseMenuController] saveSlotPanel이 연결되지 않았고 빈 슬롯도 없습니다. 저장 취소.");
    }

    public void LoadGame()
    {
        if (loadSlotPanel != null)
        {
            loadSlotPanel.SetMode(SaveSlotPanel.PanelMode.Load);
            loadSlotPanel.Open();
            return;
        }

        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        if (saveManager == null)
        {
            Debug.LogWarning("[PauseMenuController] SaveManager not found.");
            return;
        }

        int slot = SaveManager.CurrentSessionSlot >= 0
            ? SaveManager.CurrentSessionSlot
            : JsonIOScript.AutoSaveSlotIndex;
        saveManager.TryLoadCurrentGame(slot);
    }

    private static int ResolveFallbackSaveSlot()
    {
        if (SaveManager.CurrentSessionSlot >= JsonIOScript.MainSlotMinIndex
            && SaveManager.CurrentSessionSlot <= JsonIOScript.MainSlotMaxIndex)
            return SaveManager.CurrentSessionSlot;

        for (int i = JsonIOScript.MainSlotMinIndex; i <= JsonIOScript.MainSlotMaxIndex; i++)
        {
            if (!JsonIOScript.SaveFileExists(i))
                return i;
        }
        return -1;
    }

    public void ReloadCurrentScene()
    {
        if (useTimeScale)
            Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void ReturnToMainMenu()
    {
        if (useTimeScale)
            Time.timeScale = 1f;

        if (timeController != null)
            timeController.SetPaused(false);

        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        if (useTimeScale)
            Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SelectFirstUI()
    {
        if (EventSystem.current == null || firstSelectedOnOpen == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedOnOpen.gameObject);
    }

    private void ClearSelectedUI()
    {
        if (EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void ApplyCursorState(bool _opened)
    {
        if (_opened)
        {
            if (unlockCursorWhenOpened)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            if (lockCursorWhenClosed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
    }

    private void UnbindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
    }

    void OnDestroy()
    {
        UnbindCloseButton();

        if (useTimeScale)
            Time.timeScale = 1f;
    }
}
