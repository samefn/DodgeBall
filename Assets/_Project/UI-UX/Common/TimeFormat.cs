using UnityEngine;

public static class TimeFormat
{
    public static string MinutesSeconds(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}
