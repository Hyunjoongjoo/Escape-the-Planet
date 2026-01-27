using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using static UnityEngine.Rendering.DebugUI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class PhotonPlayerStateManager
{
    public const string STATE_KEY = "State";

    private const string WAS_DEAD_KEY = MatchKeys.WasDeadThisDay;
    private const string NEXT_HP_KEY = MatchKeys.NextDayHpRatio;

    public static void SetState(PlayerGameState state)
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable table = new Hashtable
        {
            { STATE_KEY, (int)state }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    public static void SetState(Player target, PlayerGameState state)
    {
        if (target == null || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable table = new Hashtable
        {
            { STATE_KEY, (int)state }
        };

        target.SetCustomProperties(table);
    }

    public static PlayerGameState GetState(Player player)
    {
        if (player != null && player.CustomProperties != null && player.CustomProperties.TryGetValue(STATE_KEY, out object value))
        {
            return (PlayerGameState)(int)value;
        }

        return PlayerGameState.Alive;
    }

    public static void SetWasDeadThisDay(bool value)
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable table = new Hashtable
        {
            { WAS_DEAD_KEY, value ? 1 : 0 }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    public static bool GetWasDeadThisDay(Player player)
    {
        if (player != null && player.CustomProperties != null && player.CustomProperties.TryGetValue(WAS_DEAD_KEY, out object value))
        {
            return ((int)value) == 1;
        }

        return false;
    }

    public static void SetNextDayHpRatio(Player target, float ratio)
    {
        if (target == null || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable table = new Hashtable
        {
            { NEXT_HP_KEY, UnityEngine.Mathf.Clamp01(ratio) }
        };

        target.SetCustomProperties(table);
    }

    public static float GetNextDayHpRatio(Player player)
    {
        if (player != null && player.CustomProperties != null && player.CustomProperties.TryGetValue(NEXT_HP_KEY, out object value))
        {
            return (float)value;
        }

        return 1f;
    }

}
