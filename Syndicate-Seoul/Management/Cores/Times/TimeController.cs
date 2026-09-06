using System;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    private static readonly int[] SpeedSteps = { 1, 4, 8, 16, 32 };

    private CalendarScript calendar;

    [SerializeField] private float defaultSecondsPerDay = 2f;

    public event Action<float> SpeedChanged;

    private int speedMultiplier = 1;
    private int lastNonPausedMultiplier = 1;
    private bool inputLocked;

    void Start()
    {
        calendar = FindFirstObjectByType<CalendarScript>();

        if (calendar == null)
        {
            Debug.LogError("[TimeController] CalendarScript가 씬에 없습니다.");
            enabled = false;
            return;
        }

        defaultSecondsPerDay = calendar.realSecondsPerGameDay;
        SpeedChanged?.Invoke(GetCurrentSpeedMultiplier());
    }

    void Update()
    {
        if (inputLocked)
            return;

        if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
        {
            IncreaseSpeed();
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
        {
            DecreaseSpeed();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }
    }

    public void IncreaseSpeed()
    {
        if (!EnsureCalendar())
            return;

        int next = GetNextSpeedMultiplier(speedMultiplier);
        if (next != speedMultiplier)
        {
            speedMultiplier = next;
            lastNonPausedMultiplier = NormalizeSpeedMultiplier(speedMultiplier);

            ApplySpeed();

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.NotifyCondition(TutorialCondition.ClickTimeSpeed);
        }
    }

    public void DecreaseSpeed()
    {
        if (!EnsureCalendar())
            return;

        int next = GetPreviousSpeedMultiplier(speedMultiplier);
        if (next != speedMultiplier)
        {
            speedMultiplier = next;
            if (speedMultiplier > 0)
                lastNonPausedMultiplier = NormalizeSpeedMultiplier(speedMultiplier);

            ApplySpeed();

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.NotifyCondition(TutorialCondition.ClickTimeSpeed);
        }
    }

    public void SetDefaultSpeed()
    {
        SetSpeedMultiplier(1);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.ClickTimeSpeed);
    }

    public void SetSpeedMultiplier(int multiplier)
    {
        if (!EnsureCalendar())
            return;

        int next = NormalizeSpeedMultiplierWithPause(multiplier);
        if (next == speedMultiplier)
        {
            ApplySpeed();
            return;
        }

        speedMultiplier = next;
        if (speedMultiplier > 0)
            lastNonPausedMultiplier = NormalizeSpeedMultiplier(speedMultiplier);

        ApplySpeed();
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused());
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }

    public void SetPaused(bool _paused)
    {
        if (!EnsureCalendar())
            return;

        if (_paused)
        {
            if (speedMultiplier > 0)
                lastNonPausedMultiplier = NormalizeSpeedMultiplier(speedMultiplier);

            speedMultiplier = 0;
        }
        else
        {
            speedMultiplier = lastNonPausedMultiplier > 0 ? NormalizeSpeedMultiplier(lastNonPausedMultiplier) : 1;
        }

        ApplySpeed();
    }

    private void ApplySpeed()
    {
        speedMultiplier = NormalizeSpeedMultiplierWithPause(speedMultiplier);

        if (speedMultiplier <= 0)
        {
            calendar.SetRunning(false);
            Debug.Log("[TimeController] 일시정지");
        }
        else
        {
            calendar.SetRunning(true);
            calendar.realSecondsPerGameDay = defaultSecondsPerDay / speedMultiplier;
            Debug.Log($"[TimeController] 배속: {speedMultiplier}x  (하루당 {calendar.realSecondsPerGameDay:0.###}초)");
        }

        SpeedChanged?.Invoke(speedMultiplier);
    }

    private int GetNextSpeedMultiplier(int currentSpeed)
    {
        if (currentSpeed <= 0)
            return SpeedSteps[0];

        int current = NormalizeSpeedMultiplier(currentSpeed);
        for (int i = 0; i < SpeedSteps.Length; i++)
        {
            if (SpeedSteps[i] > current)
                return SpeedSteps[i];
        }

        return SpeedSteps[SpeedSteps.Length - 1];
    }

    private int GetPreviousSpeedMultiplier(int currentSpeed)
    {
        if (currentSpeed <= 0)
            return 0;

        int current = NormalizeSpeedMultiplier(currentSpeed);
        for (int i = SpeedSteps.Length - 1; i >= 0; i--)
        {
            if (SpeedSteps[i] < current)
                return SpeedSteps[i];
        }

        return 0;
    }

    private int NormalizeSpeedMultiplierWithPause(int multiplier)
    {
        if (multiplier <= 0)
            return 0;

        return NormalizeSpeedMultiplier(multiplier);
    }

    private int NormalizeSpeedMultiplier(int multiplier)
    {
        int bestSpeed = SpeedSteps[0];
        int bestDistance = Mathf.Abs(multiplier - bestSpeed);

        for (int i = 1; i < SpeedSteps.Length; i++)
        {
            int distance = Mathf.Abs(multiplier - SpeedSteps[i]);
            if (distance >= bestDistance)
                continue;

            bestSpeed = SpeedSteps[i];
            bestDistance = distance;
        }

        return bestSpeed;
    }

    public float GetCurrentSpeedMultiplier() => speedMultiplier;
    public bool IsPaused() => speedMultiplier == 0;

    private bool EnsureCalendar()
    {
        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();

        if (calendar != null)
            return true;

        Debug.LogError("[TimeController] CalendarScript가 씬에 없습니다.");
        return false;
    }
}
