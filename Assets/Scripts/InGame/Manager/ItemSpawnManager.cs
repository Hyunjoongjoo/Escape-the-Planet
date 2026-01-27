using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemSpawnManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private string _groundItemPrefabName = "GroundItem";

    [SerializeField] private Transform _spawnPointsRoot;
    [SerializeField] private int _minSpawnCount = 12;
    [SerializeField] private int _maxSpawnCount = 18;

    [SerializeField] private LayerMask _blockedMask;
    [SerializeField] private float _checkRadius = 0.25f;
    [SerializeField] private int _tryCount = 12;

    private bool _spawnedThisDay = false;

    private readonly List<ItemSpawnPoint> _points = new List<ItemSpawnPoint>();

    private ItemId _lastSpawnedId = ItemId.NONE;

    private void Awake()
    {
        CacheSpawnPoints();
    }

    private void CacheSpawnPoints()
    {
        _points.Clear();

        if (_spawnPointsRoot == null)
        {
            return;
        }

        _spawnPointsRoot.GetComponentsInChildren(true, _points);
    }

    public void Spawn()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (_spawnedThisDay)
        {
            return;
        }

        _spawnedThisDay = true;

        if (_itemDatabase == null || string.IsNullOrEmpty(_groundItemPrefabName) || _points.Count == 0)
        {
            return;
        }

        int randomCount = Random.Range(_minSpawnCount, _maxSpawnCount + 1);
        int spawnCount = Mathf.Clamp(randomCount, 0, _points.Count);

        List<ItemSpawnPoint> available = new List<ItemSpawnPoint>(_points);

        for (int i = 0; i < spawnCount; i++)
        {
            if (available.Count == 0)
            {
                break;
            }

            int pointIndex = Random.Range(0, available.Count);
            ItemSpawnPoint point = available[pointIndex];
            available.RemoveAt(pointIndex);

            ItemData item = _itemDatabase.GetRandomWeighted(_lastSpawnedId);
            if (item == null)
            {
                continue;
            }

            _lastSpawnedId = item.id;

            Vector2 spawnPosition = GetRandomSpawnPosition(
                point.transform.position,
                item.spawnRadius
            );

            GameObject spawnedObject = PhotonNetwork.Instantiate(
                _groundItemPrefabName,
                spawnPosition,
                Quaternion.identity
            );

            spawnedObject.transform.SetParent(InGameWorldController.Instance.WorldRoot.transform, true);

            GroundItemNetwork network = spawnedObject.GetComponent<GroundItemNetwork>();
            if (network != null)
            {
                network.SetItemId(item.id);
            }
        }
    }
    private Vector2 GetRandomSpawnPosition(Vector2 center, float radius)
    {
        if (radius <= 0f)
        {
            return center;
        }

        for (int i = 0; i < _tryCount; i++)
        {
            Vector2 pos = center + Random.insideUnitCircle * radius;

            if (_blockedMask.value != 0)
            {
                Collider2D hit = Physics2D.OverlapCircle(pos, _checkRadius, _blockedMask);
                if (hit != null)
                {
                    continue;
                }
            }

            return pos;
        }

        return center;
    }
    public void ResetForNextDay()
    {
        _spawnedThisDay = false;
        _lastSpawnedId = ItemId.NONE;

        foreach (Transform child in transform)
        {
            PhotonNetwork.Destroy(child.gameObject);
        }
    }
}
