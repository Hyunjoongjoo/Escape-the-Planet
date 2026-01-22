using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PhotonPlayerStateManager
{
    public const string STATE_KEY = "State";

    public static void SetState(PlayerGameState state)
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable table = new Hashtable();
        table[STATE_KEY] = (int)state;
        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    public static PlayerGameState GetState(Player player)
    {
        if (player == null)
        {
            return PlayerGameState.Alive;
        }

        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(STATE_KEY, out object value))
        {
            return (PlayerGameState)(int)value;
        }

        return PlayerGameState.Alive;
    }
}
