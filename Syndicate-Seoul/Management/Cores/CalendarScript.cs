using System;
using UnityEngine;
using UnityEngine.Events;

// --- UnityEvents (�ν����Ϳ��� ���ε� ����) ---
[Serializable] public class DateEvent : UnityEvent<GameDate> { }
[Serializable] public class MonthEvent : UnityEvent<int, int> { } // (year, month)
[Serializable] public class YearEvent : UnityEvent<int> { }      // (year)

public class CalendarScript : MonoBehaviour
{
    public float realSecondsPerGameDay = 2f;
    public bool useUnscaledTime = true;

    public bool autoStart = true;

    public bool useGregorianLeapYear = false;
    public int[] daysInMonths = new int[12] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };


    public GameDate startDate = new GameDate(1, 1, 1);

    public DateEvent OnDayPassed;
    public MonthEvent OnMonthPassed;
    public YearEvent OnYearPassed;

    public GameDate CurrentDate { get; private set; }

    float _accumulator;
    bool _running;

    public event Action<GameDate> DayChanged;
    public event Action<int, int> MonthChanged;
    public event Action<int> YearChanged;

    void Awake()
    {
        if (daysInMonths == null || daysInMonths.Length != 12)
            daysInMonths = new int[12] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        CurrentDate = ClampDate(startDate);
    }

    void OnEnable()
    {
        _running = autoStart;
    }

    void Update()
    {
        if (!_running || realSecondsPerGameDay <= 0f) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _accumulator += dt;

        while (_accumulator >= realSecondsPerGameDay)
        {
            _accumulator -= realSecondsPerGameDay;
            AdvanceOneDay();
        }
    }

    // ���� API

    /// <summary>����/�Ͻ�����</summary>
    public void SetRunning(bool run)
    {
        _running = run;
    }

    /// <summary>���� ��¥�� �� ������ ����</summary>
    public void SetDate(GameDate date)
    {
        CurrentDate = ClampDate(date);
        // ���ϴ� ��� ���⼭ ��� �̺�Ʈ ���� ����(�⺻�� ������)
    }

    /// <summary>n�� ������ ����(������ �ǵ����� ������)</summary>
    public void AddDays(int days)
    {
        for (int i = 0; i < days; i++) AdvanceOneDay();
    }

    /// <summary>n���� ����</summary>
    public void AddMonths(int months)
    {
        for (int i = 0; i < months; i++) AdvanceToNextMonth();
    }

    /// <summary>n�� ����</summary>
    public void AddYears(int years)
    {
        for (int i = 0; i < years; i++) AdvanceToNextYear();
    }

    // --- ���� ���� ---

    void AdvanceOneDay()
    {
        int dim = DaysInMonth(CurrentDate.year, CurrentDate.month);

        CurrentDate.day++;
        bool monthRolled = false;
        bool yearRolled = false;

        if (CurrentDate.day > dim)
        {
            CurrentDate.day = 1;
            CurrentDate.month++;
            monthRolled = true;

            if (CurrentDate.month > 12)
            {
                CurrentDate.month = 1;
                CurrentDate.year++;
                yearRolled = true;
            }
        }

        // �̺�Ʈ ����(����: �� �� �� �� ��)
        OnDayPassed?.Invoke(CurrentDate);
        DayChanged?.Invoke(CurrentDate);

        if (monthRolled)
        {
            OnMonthPassed?.Invoke(CurrentDate.year, CurrentDate.month);
            MonthChanged?.Invoke(CurrentDate.year, CurrentDate.month);
        }

        if (yearRolled)
        {
            OnYearPassed?.Invoke(CurrentDate.year);
            YearChanged?.Invoke(CurrentDate.year);
        }
    }

    void AdvanceToNextMonth()
    {
        // ���� �� 1�Ϸ� �̵�
        CurrentDate.day = 1;
        CurrentDate.month++;
        if (CurrentDate.month > 12)
        {
            CurrentDate.month = 1;
            CurrentDate.year++;
            OnYearPassed?.Invoke(CurrentDate.year);
            YearChanged?.Invoke(CurrentDate.year);
        }
        OnMonthPassed?.Invoke(CurrentDate.year, CurrentDate.month);
        MonthChanged?.Invoke(CurrentDate.year, CurrentDate.month);
        OnDayPassed?.Invoke(CurrentDate);
        DayChanged?.Invoke(CurrentDate);
    }

    void AdvanceToNextYear()
    {
        CurrentDate.day = 1;
        CurrentDate.month = 1;
        CurrentDate.year++;
        OnYearPassed?.Invoke(CurrentDate.year);
        YearChanged?.Invoke(CurrentDate.year);
        OnMonthPassed?.Invoke(CurrentDate.year, CurrentDate.month);
        MonthChanged?.Invoke(CurrentDate.year, CurrentDate.month);
        OnDayPassed?.Invoke(CurrentDate);
        DayChanged?.Invoke(CurrentDate);
    }

    int DaysInMonth(int year, int month)
    {
        int baseDays = daysInMonths[Mathf.Clamp(month - 1, 0, 11)];
        if (useGregorianLeapYear && month == 2 && IsLeapYear(year))
            return 29;
        return baseDays;
    }

    bool IsLeapYear(int year)
    {
        // �׷�������: 4�� ������ �������� 100���δ� �ƴ�, 400���δ� �ٽ� ����
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }

    GameDate ClampDate(GameDate d)
    {
        int y = Mathf.Max(1, d.year);
        int m = Mathf.Clamp(d.month, 1, 12);
        int dim = DaysInMonth(y, m);
        int day = Mathf.Clamp(d.day, 1, dim);
        return new GameDate(y, m, day);
    }
}
