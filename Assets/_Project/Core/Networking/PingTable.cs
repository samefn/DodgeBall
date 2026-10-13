using System.Collections.Generic;
using UnityEngine;

public class PingTable
{
    private const float Smoothing = 0.5f;

    private readonly Dictionary<int, float> measured = new Dictionary<int, float>();
    private readonly Dictionary<int, int> reported = new Dictionary<int, int>();

    public float LocalRoundTripMs { get; private set; }

    public void StoreRoundTrip(int playerId, float sentAt, float now)
    {
        float sample = (now - sentAt) * 1000f;
        measured[playerId] = measured.TryGetValue(playerId, out float previous)
            ? Mathf.Lerp(previous, sample, Smoothing)
            : sample;
    }

    public void StoreLocalRoundTrip(float sentAt, float now)
    {
        LocalRoundTripMs = (now - sentAt) * 1000f;
    }

    public void SetReported(int[] playerIds, int[] pings)
    {
        reported.Clear();
        if (playerIds == null || pings == null)
            return;

        for (int i = 0; i < pings.Length && i < playerIds.Length; i++)
            reported[playerIds[i]] = pings[i];
    }

    public int GetMeasured(int playerId)
    {
        return measured.TryGetValue(playerId, out float rtt) ? Mathf.RoundToInt(rtt) : -1;
    }

    public int GetReported(int playerId)
    {
        return reported.TryGetValue(playerId, out int ping) ? ping : -1;
    }

    public void Remove(int playerId)
    {
        measured.Remove(playerId);
    }

    public void Clear()
    {
        measured.Clear();
        reported.Clear();
        LocalRoundTripMs = 0f;
    }
}
