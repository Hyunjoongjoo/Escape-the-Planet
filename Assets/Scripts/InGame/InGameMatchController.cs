using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class InGameMatchController : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent == null)
        {
            return;
        }

        if (photonEvent.Code == MatchEventCodes.DayStartBroadcast)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            FindFirstObjectByType<ItemSpawnManager>()?.Spawn();
            FindFirstObjectByType<EnemySpawnManager>()?.StartDay();
        }
    }
}
