using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private string _playerPrefabName = "Player";

    [SerializeField] private Transform[] _spawnPoints;

    public PlayerController SpawnLocalPlayer()
    {
        if (!PhotonNetwork.InRoom)
        {
            return null;
        }

        if (PhotonNetwork.LocalPlayer == null)
        {
            return null; ;
        }

        if (HasLocalPlayer())
        {
            return null;
        }

        Vector3 spawnPos = GetSpawnPosition(PhotonNetwork.LocalPlayer);

        GameObject player = PhotonNetwork.Instantiate(_playerPrefabName, spawnPos, Quaternion.identity);
        Transform world = GameManager.Instance.InGameWorldTransform;
        player.transform.SetParent(world, true);
        player.GetComponent<PhotonView>().RPC("RPC_SetParentToWorld", RpcTarget.AllBuffered);
        PlayerController controller = player.GetComponent<PlayerController>();

        if (controller != null)
        {
            controller.SetRoomMode();
        }

        return controller;
    }

    private bool HasLocalPlayer()
    {
        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            PlayerController player = players[i];

            if (player == null)
            {
                continue;
            }

            if (player.photonView != null && player.photonView.IsMine)
            {
                return true;
            }
        }

        return false;
    }
    public Vector3 GetSpawnPosition(Player player)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            return Vector3.zero;
        }

        int index = Mathf.Clamp(player.ActorNumber - 1, 0, _spawnPoints.Length - 1);
        return _spawnPoints[index].position;
    }
}
