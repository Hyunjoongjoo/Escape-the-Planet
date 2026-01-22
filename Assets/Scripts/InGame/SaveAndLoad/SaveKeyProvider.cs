using UnityEngine;
using Photon.Pun;

public static class SaveKeyProvider
{
    public static string GetPlayerKey()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            string userId = PhotonNetwork.LocalPlayer.UserId;
            if (string.IsNullOrEmpty(userId) == false)
            {
                return SanitizeKey(userId);
            }

            string nickname = PhotonNetwork.LocalPlayer.NickName;
            if (string.IsNullOrEmpty(nickname) == false)
            {
                return SanitizeKey(nickname);
            }
        }

        return SanitizeKey(SystemInfo.deviceUniqueIdentifier);
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
