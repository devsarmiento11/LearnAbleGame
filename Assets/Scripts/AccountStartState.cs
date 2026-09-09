using System;
using System.Text;
using UnityEngine;

public static class AccountStartState
{
    private static string Key(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return null;
        return "AccountStarted.v1." + Convert.ToBase64String(Encoding.UTF8.GetBytes(accountId.Trim()));
    }

    public static bool HasStarted(string accountId)
    {
        string key = Key(accountId);
        return key != null && PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void MarkStarted(string accountId)
    {
        string key = Key(accountId);
        if (key == null) return;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }
}
