using Photon.Pun;
using UnityEngine;

public class NetworkRelay : MonoBehaviourPun
{
    public static NetworkRelay Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RequestStartDay()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(nameof(RPC_RequestStartDay), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_RequestStartDay()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        GameManager.Instance.StartDay_Master();
    }

    public void RequestEndDay(DayEndReason reason)
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(nameof(RPC_RequestEndDay), RpcTarget.MasterClient, (int)reason);
    }

    [PunRPC]
    private void RPC_RequestEndDay(int reason)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        GameManager.Instance.EndDay_Master((DayEndReason)reason);
    }
}