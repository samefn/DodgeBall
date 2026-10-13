using System.Collections.Generic;
using UnityEngine;

public static class BanList
{
    private const string PrefsKey = "Dodgeball.BannedIPs";
    private static readonly object Sync = new object();
    private static HashSet<string> addresses;

    public static IReadOnlyCollection<string> Addresses
    {
        get
        {
            lock (Sync)
            {
                Load();
                return new List<string>(addresses);
            }
        }
    }

    public static bool IsBanned(string address)
    {
        lock (Sync)
        {
            Load();
            return addresses.Contains(address);
        }
    }

    public static void Ban(string address)
    {
        if (string.IsNullOrEmpty(address))
            return;

        lock (Sync)
        {
            Load();
            if (addresses.Add(address))
                Save();
        }
    }

    public static void Unban(string address)
    {
        lock (Sync)
        {
            Load();
            if (addresses.Remove(address))
                Save();
        }
    }

    private static void Load()
    {
        if (addresses != null)
            return;

        addresses = new HashSet<string>();
        string saved = PlayerPrefs.GetString(PrefsKey, string.Empty);
        foreach (string address in saved.Split(','))
        {
            if (!string.IsNullOrWhiteSpace(address))
                addresses.Add(address.Trim());
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetString(PrefsKey, string.Join(",", addresses));
        PlayerPrefs.Save();
    }
}
