using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public static class PhotonPlayerLocationManager
{
    public const string LOC_KEY = MatchKeys.Loc;

    public static void SetLocation(PlayerLocation location)
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable table = new Hashtable
        {
            { LOC_KEY, (int)location }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    public static void SetLocation(Player target, PlayerLocation location)
    {
        if (target == null || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable table = new Hashtable
        {
            { LOC_KEY, (int)location }
        };

        target.SetCustomProperties(table);
    }

    public static PlayerLocation GetLocation(Player player)
    {
        if (player != null &&
            player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(LOC_KEY, out object value))
        {
            return (PlayerLocation)(int)value;
        }

        return PlayerLocation.Room;
    }

    public static bool IsPlayerInGame(Player player)
    {
        return GetLocation(player) == PlayerLocation.InGame;
    }

    public static bool IsPlayerInRoom(Player player)
    {
        return GetLocation(player) == PlayerLocation.Room;
    }
}
