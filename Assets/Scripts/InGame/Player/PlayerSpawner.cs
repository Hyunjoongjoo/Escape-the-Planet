using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

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

        PlayerController controller = player.GetComponent<PlayerController>();

        if (controller != null)
        {
            controller.SetRoomMode();
        }

        return controller;
    }

    private bool HasLocalPlayer()
    {
        PhotonView[] views = Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

        foreach (PhotonView view in views)
        {
            if (view == null)
            {
                continue;
            }

            if (view.IsMine && view.GetComponent<PlayerController>() != null)
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
