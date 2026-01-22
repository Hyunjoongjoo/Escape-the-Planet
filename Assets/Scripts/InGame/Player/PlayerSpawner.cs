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
        // 안정성: Photon / Scene 준비 한 프레임 대기
        yield return null;

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[PlayerSpawner] Not in room. Abort spawn.");
            yield break;
        }

        if (PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("[PlayerSpawner] LocalPlayer is null. Abort spawn.");
            yield break;
        }

        if (HasLocalPlayer())
        {
            Debug.Log("[PlayerSpawner] Local player already exists. Skip spawn.");
            yield break;
        }

        SpawnLocalPlayer();
    }

    private bool HasLocalPlayer()
    {
        PhotonView[] views = Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

        foreach (PhotonView v in views)
        {
            if (v.IsMine && v.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnLocalPlayer()
    {
        Vector3 spawnPos = GetSpawnPosition(PhotonNetwork.LocalPlayer);

        GameObject obj = PhotonNetwork.Instantiate(_playerPrefabName, spawnPos, Quaternion.identity);

        Debug.Log($"[PlayerSpawner] Spawned local player: {obj.name} at {spawnPos}");
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
