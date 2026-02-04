using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkRelay : MonoBehaviourPunCallbacks
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

    public void RequestRepair(int amount)
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(nameof(RPC_RequestRepair), RpcTarget.MasterClient, amount);
    }

    [PunRPC]
    private void RPC_RequestRepair(int amount)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        GameManager.Instance.AddRepair_Master(amount);
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

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }


        RebindMasterAuthority();
    }

    private void RebindMasterAuthority()
    {
        IReadOnlyList<EnemyController> Enemies = EnemyRegistry.Instance.Enemies;

        foreach (EnemyController enemy in Enemies)
        {
            enemy.OnMasterChanged();
        }

        EnemySpawnManager enemySpawner = FindFirstObjectByType<EnemySpawnManager>();
        if (enemySpawner != null)
        {
            enemySpawner.OnMasterChanged();
        }

        ItemSpawnManager itemSpawner = FindFirstObjectByType<ItemSpawnManager>();
        if (itemSpawner != null)
        {
            itemSpawner.OnMasterChanged();
        }
    }

    public void BroadcastEnding()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(nameof(RPC_BroadcastEnding), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_BroadcastEnding()
    {
        PhotonPlayerLocationManager.SetLocation(PlayerLocation.Room);
    }  
}