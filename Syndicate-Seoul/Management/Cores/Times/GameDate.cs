using System;
using UnityEngine;

[Serializable]
public class GameDate
{
    public int year;
    public int month;
    public int day;

    public GameDate(int year, int month, int day)
    {
        this.year = Mathf.Max(1, year);
        this.month = Mathf.Clamp(month, 1, 12);
        this.day = Mathf.Max(1, day);
    }

    public override string ToString()
    {
        return $"{year:D4}-{month:D2}-{day:D2}";
    }
}
