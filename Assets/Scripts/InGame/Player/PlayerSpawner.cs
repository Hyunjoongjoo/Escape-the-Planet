using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private string _playerPrefabName = "Player";

    [SerializeField] private Transform[] _spawnPoints;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        yield return null;

        if (!PhotonNetwork.InRoom)
        {
            yield break;
        }

        if (PhotonNetwork.LocalPlayer == null)
        {
            yield break;
        }

        if (HasLocalPlayer())
        {
            yield break;
        }

        SpawnLocalPlayer();
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

    private void SpawnLocalPlayer()
    {
        Vector3 spawnPos = GetSpawnPosition(PhotonNetwork.LocalPlayer);

        GameObject player = PhotonNetwork.Instantiate(_playerPrefabName, spawnPos, Quaternion.identity);

        player.transform.SetParent(InGameWorldController.Instance.WorldRoot.transform, true);
        
    }

    private Vector3 GetSpawnPosition(Player player)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            return Vector3.zero;
        }

        int index = Mathf.Clamp(player.ActorNumber - 1, 0, _spawnPoints.Length - 1);
        return _spawnPoints[index].position;
    }
}
