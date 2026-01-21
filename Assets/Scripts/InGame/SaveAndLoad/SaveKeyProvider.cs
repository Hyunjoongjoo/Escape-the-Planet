using UnityEngine;
using Photon.Pun;

public static class SaveKeyProvider
{
    public static string GetPlayerKey()
    {
        string nick = PhotonNetwork.NickName;

        if (string.IsNullOrWhiteSpace(nick))
        {
            nick = "Unknown";
        }

        return nick.Trim();
    }
}
