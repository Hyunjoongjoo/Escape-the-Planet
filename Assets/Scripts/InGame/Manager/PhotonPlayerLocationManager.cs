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

        Hashtable table = new Hashtable();
        table[LOC_KEY] = (int)location;
        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    public static PlayerLocation GetLocation(Player player)
    {
        if (player == null)
        {
            return PlayerLocation.Room;
        }

        if (player.CustomProperties != null && player.CustomProperties.TryGetValue(LOC_KEY, out object value))
        {
            return (PlayerLocation)(int)value;
        }

        return PlayerLocation.Room;
    }
}
