using UnityEngine;

public static class NetworkClock
{
    public const float InterpolationDelay = 0.12f;

    private const float ResyncThreshold = 1f;
    private const float Smoothing = 0.05f;

    private static float offset;
    private static bool hasOffset;

    public static float HostTime => Time.unscaledTime;

    public static float ServerNow => Time.unscaledTime + offset;
    public static float RenderTime => ServerNow - InterpolationDelay;

    public static void Reset()
    {
        hasOffset = false;
    }

    public static void OnSnapshot(float serverTime)
    {
        float sample = serverTime - Time.unscaledTime;
        if (!hasOffset || Mathf.Abs(sample - offset) > ResyncThreshold)
        {
            offset = sample;
            hasOffset = true;
            return;
        }

        offset = Mathf.Lerp(offset, sample, Smoothing);
    }
}
