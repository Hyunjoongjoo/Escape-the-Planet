using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System;

public static class SaveKeyProvider
{
    private const string LOCAL_KEY = "PERSISTENT_PLAYER_KEY";

    public static string GetPlayerKey()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            if (string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.UserId) == false)
            {
                return SanitizeKey(PhotonNetwork.LocalPlayer.UserId);
            }
        }

        if (PlayerPrefs.HasKey(LOCAL_KEY) == false)
        {
            string newKey = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(LOCAL_KEY, newKey);
            PlayerPrefs.Save();
        }

        return SanitizeKey(PlayerPrefs.GetString(LOCAL_KEY));
    }

    public static string GetPlayerKey(Player player)
    {
        if (player != null)
        {
            string userId = player.UserId;
            if (string.IsNullOrEmpty(userId) == false)
            {
                return SanitizeKey(userId);
            }

            string nickname = player.NickName;
            if (string.IsNullOrEmpty(nickname) == false)
            {
                return SanitizeKey(nickname);
            }
        }

        return "Unknown";
    }

    private static string SanitizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return "Unknown";
        }

        key = key.Replace(".", "_");
        key = key.Replace("#", "_");
        key = key.Replace("$", "_");
        key = key.Replace("[", "_");
        key = key.Replace("]", "_");
        key = key.Replace("/", "_");

        return key;
    }
}
