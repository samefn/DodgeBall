using System.Collections.Generic;

public class ConnectionTimeouts
{
    private readonly Dictionary<int, float> lastHeard = new Dictionary<int, float>();
    private readonly List<int> expired = new List<int>();
    private readonly float timeout;

    public ConnectionTimeouts(float timeoutSeconds)
    {
        timeout = timeoutSeconds;
    }

    public void MarkHeard(int clientId, float now)
    {
        lastHeard[clientId] = now;
    }

    public void Forget(int clientId)
    {
        lastHeard.Remove(clientId);
    }

    public void Clear()
    {
        lastHeard.Clear();
    }

    public List<int> CollectExpired(float now)
    {
        expired.Clear();
        foreach (KeyValuePair<int, float> entry in lastHeard)
        {
            if (now - entry.Value > timeout)
                expired.Add(entry.Key);
        }

        foreach (int clientId in expired)
            lastHeard.Remove(clientId);
        return expired;
    }
}
