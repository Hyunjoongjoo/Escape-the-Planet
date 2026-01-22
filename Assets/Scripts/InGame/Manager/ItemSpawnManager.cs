using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemSpawnManager : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private string _groundItemPrefabName = "GroundItem";

    [SerializeField] private Transform _spawnPointsRoot;
    [SerializeField] private int _minSpawnCount = 12;
    [SerializeField] private int _maxSpawnCount = 18;

    [SerializeField] private LayerMask _blockedMask;
    [SerializeField] private float _checkRadius = 0.25f;
    [SerializeField] private int _tryCount = 12;

    private readonly List<ItemSpawnPoint> _points = new List<ItemSpawnPoint>();

    private ItemId _lastSpawnedId = ItemId.NONE;

    private void Awake()
    {
        CacheSpawnPoints();
    }

    private void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Spawn();
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
        if (_itemDatabase == null)
        {
            return;
        }

        if (_groundItemPrefabName == null)
        {
            return;
        }

        if (_points.Count == 0)
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

            int pointIdx = Random.Range(0, available.Count);
            ItemSpawnPoint point = available[pointIdx];
            available.RemoveAt(pointIdx);

            ItemData item = _itemDatabase.GetRandomWeighted(_lastSpawnedId);
            if (item == null)
            {
                continue;
            }

            _lastSpawnedId = item.id;

            float radius = item.spawnRadius;
            Vector2 spawnPos = GetRandomSpawnPosition(point.transform.position, radius);

            GameObject obj = PhotonNetwork.Instantiate(_groundItemPrefabName, spawnPos, Quaternion.identity);

            GroundItemNetwork net = obj.GetComponent<GroundItemNetwork>();
            if (net != null)
            {
                net.SetItemId(item.id);
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
}
