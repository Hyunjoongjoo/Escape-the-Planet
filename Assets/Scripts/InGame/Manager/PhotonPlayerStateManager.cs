using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class PhotonPlayerStateManager
{
    private const string STATE_KEY = MatchKeys.PlayerState;
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
            if (value is int intValue)
            {
                return (PlayerGameState)intValue;
            }
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
            { WAS_DEAD_KEY, value }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    public static bool GetWasDeadThisDay(Player player)
    {
        if (player != null && player.CustomProperties != null && player.CustomProperties.TryGetValue(WAS_DEAD_KEY, out object value))
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is int intValue)
            {
                return intValue == 1;
            }

            if (value is byte byteValue)
            {
                return byteValue == 1;
            }
        }

        return false;
    }

    public static void ResetDayFlags()
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable table = new Hashtable
        {
            { MatchKeys.WasDeadThisDay, false },
            { MatchKeys.NextDayHpRatio, 1f }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
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
            if (value is float floatValue)
            {
                return floatValue;
            }

            if (value is double doubleValue)
            {
                return (float)doubleValue;
            }
        }

        return 1f;
    }

    public static bool AreAllPlayersDead()
    {
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            if (GetState(players[i]) == PlayerGameState.Alive)
            {
                return false;
            }
        }

        return true;
    }
}
