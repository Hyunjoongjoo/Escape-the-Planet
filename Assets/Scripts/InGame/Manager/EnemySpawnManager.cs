using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Photon.Pun;

public class EnemySpawnManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private EnemyDatabase _enemyDatabase;

    [SerializeField] private Transform _spawnPointsRoot;

    [SerializeField] private int _maxAlive = 8;

    [SerializeField] private float _startInterval = 12f;

    [SerializeField] private float _endInterval = 5f;

    [SerializeField] private LayerMask _blockedMask;
    [SerializeField] private float _checkRadius = 0.3f;
    [SerializeField] private int _tryCount = 12;

    private readonly List<EnemySpawnPoint> _points = new List<EnemySpawnPoint>();
    private readonly List<EnemyController> _alive = new List<EnemyController>();

    private float _time;
    private EnemyId _lastSpawnedId = EnemyId.NONE;

    private bool _spawnStartedThisDay = false;

    private void Awake()
    {
        CachePoints();
    }

    private void CachePoints()
    {
        _points.Clear();

        if (_spawnPointsRoot == null)
        {
            return;
        }

        _spawnPointsRoot.GetComponentsInChildren(true, _points);

    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.IsRunning == false)
        {
            return;
        }

        if (_spawnStartedThisDay == false)
        {
            return;
        }

        CleanupAlive();

        if (_alive.Count >= _maxAlive)
        {
            return;
        }

        float interval = GetSpawnInterval();
        _time += Time.deltaTime;

        if (_time >= interval)
        {
            _time = 0f;
            SpawnOne();
        }
    }

    public void StartDay()
    {
        if (_spawnStartedThisDay)
        {
            return;
        }

        _spawnStartedThisDay = true;
        _time = 0f;
        _lastSpawnedId = EnemyId.NONE;
    }

    public void ResetForNextDay()
    {
        _spawnStartedThisDay = false;

        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] != null)
            {
                PhotonNetwork.Destroy(_alive[i].gameObject);
            }
        }

        _alive.Clear();
    }

    private void CleanupAlive()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null)
            {
                _alive.RemoveAt(i);
            }
        }
    }

    private float GetSpawnInterval()
    {
        float timeLimit = 300f;
        float remain = GameManager.Instance.RemainTime;

        float progress = Mathf.Clamp01(1f - (remain / timeLimit));

        return Mathf.Lerp(_startInterval, _endInterval, progress);
    }

    private void SpawnOne()
    {
        if (_enemyDatabase == null || _points.Count == 0)
        {
            return;
        }

        EnemyData enemyData = _enemyDatabase.GetRandomWeighted(_lastSpawnedId);
        if (enemyData == null || string.IsNullOrEmpty(enemyData.prefabName))
        {
            return;
        }

        EnemySpawnPoint point = _points[Random.Range(0, _points.Count)];
        Vector2 spawnPosition = GetRandomSpawnPosition(
            point.transform.position,
            enemyData.spawnRadius
        );

        GameObject spawnedObject = PhotonNetwork.Instantiate(
            enemyData.prefabName,
            spawnPosition,
            Quaternion.identity
        );

        spawnedObject.transform.SetParent(InGameWorldController.Instance.WorldRoot.transform, true);

        EnemyController enemy = spawnedObject.GetComponent<EnemyController>();
        if (enemy == null)
        {
            return;
        }

        enemy.Init(enemyData, GameManager.Instance.RemainTime / 60f);

        _alive.Add(enemy);
        _lastSpawnedId = enemyData.id;
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
