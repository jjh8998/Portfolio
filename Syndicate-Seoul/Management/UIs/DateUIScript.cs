using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DateUIScript : MonoBehaviour
{
    public CalendarScript calendar;
    public TMP_Text tmpText;   // UI
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TimeController timeController;
    [SerializeField] private Button pauseButton;

    private Action<int, int> monthChangedHandler;
    private Action<int> yearChangedHandler;
    private Action<float> speedChangedHandler;

    void OnEnable()
    {
        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();

        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        BindPauseButton();

        if (calendar == null)
        {
            RefreshSpeedText();
            return;
        }

        if (monthChangedHandler == null)
            monthChangedHandler = (int year, int month) => UpdateText(calendar.CurrentDate);

        if (yearChangedHandler == null)
            yearChangedHandler = (int year) => UpdateText(calendar.CurrentDate);

        if (speedChangedHandler == null)
            speedChangedHandler = (float speedMultiplier) => RefreshSpeedText(speedMultiplier);

        calendar.DayChanged += UpdateText;
        calendar.MonthChanged += monthChangedHandler;
        calendar.YearChanged += yearChangedHandler;

        if (timeController != null)
        {
            timeController.SpeedChanged -= speedChangedHandler;
            timeController.SpeedChanged += speedChangedHandler;
        }

        UpdateText(calendar.CurrentDate);
        RefreshSpeedText();
    }

    void OnDisable()
    {
        UnbindPauseButton();

        if (calendar != null)
        {
            calendar.DayChanged -= UpdateText;

            if (monthChangedHandler != null)
                calendar.MonthChanged -= monthChangedHandler;

            if (yearChangedHandler != null)
                calendar.YearChanged -= yearChangedHandler;
        }

        if (timeController != null && speedChangedHandler != null)
            timeController.SpeedChanged -= speedChangedHandler;
    }

    public void RefreshNow()
    {
        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();

        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (calendar == null || calendar.CurrentDate == null)
        {
            RefreshSpeedText();
            return;
        }

        UpdateText(calendar.CurrentDate);
        RefreshSpeedText();
    }

    public void TogglePause()
    {
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();

        if (timeController == null)
            return;

        timeController.TogglePause();
        RefreshSpeedText();
    }

    void UpdateText(GameDate date)
    {
        string s = $"Date: {date}";
        if (tmpText != null) tmpText.text = s;
    }

    void RefreshSpeedText(float? speedMultiplier = null)
    {
        if (speedText == null)
            return;

        float currentSpeedMultiplier = speedMultiplier ?? (timeController != null ? timeController.GetCurrentSpeedMultiplier() : 1f);
        speedText.text = currentSpeedMultiplier <= 0f ? "x0" : $"x{currentSpeedMultiplier:0}";
    }

    private void BindPauseButton()
    {
        if (pauseButton == null)
            return;

        pauseButton.onClick.RemoveListener(TogglePause);
        pauseButton.onClick.AddListener(TogglePause);
    }

    private void UnbindPauseButton()
    {
        if (pauseButton == null)
            return;

        pauseButton.onClick.RemoveListener(TogglePause);
    }
}
