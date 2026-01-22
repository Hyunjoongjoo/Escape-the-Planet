using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Scriptable Objects/EnemyDatabase")]
public class EnemyDatabase : ScriptableObject
{
    [SerializeField] private EnemyData[] _enemies;

    private Dictionary<EnemyId, EnemyData> _map;

    private void OnEnable()
    {
        Build();
    }

    private void Build()
    {
        _map = new Dictionary<EnemyId, EnemyData>();

        for (int i = 0; i < _enemies.Length; i++)
        {
            EnemyData data = _enemies[i];
            if (data == null)
            {
                continue;
            }

            _map[data.id] = data;
        }
    }

    public EnemyData Get(EnemyId id)
    {
        if (id == EnemyId.NONE)
        {
            return null;
        }

        if (_map == null)
        {
            Build();
        }

        return _map.TryGetValue(id, out EnemyData data) ? data : null;
    }

    public EnemyData GetRandomWeighted(EnemyId lastId = EnemyId.NONE)
    {
        if (_enemies == null || _enemies.Length == 0)
        {
            return null;
        }

        if (_enemies.Length == 1)
        {
            return _enemies[0];
        }

        int safety = 30;
        while (safety-- > 0)
        {
            EnemyData picked = RollWeighted();
            if (picked != null && picked.id != lastId)
            {
                return picked;
            }
        }

        return RollWeighted();
    }

    private EnemyData RollWeighted()
    {
        int total = 0;

        for (int i = 0; i < _enemies.Length; i++)
        {
            EnemyData data = _enemies[i];
            if (data == null || data.prefabName == null)
            {
                continue;
            }

            total += Mathf.Max(1, data.weight);
        }

        if (total <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, total);

        int acc = 0;
        for (int i = 0; i < _enemies.Length; i++)
        {
            EnemyData data = _enemies[i];
            if (data == null || data.prefabName == null)
            {
                continue;
            }

            acc += Mathf.Max(1, data.weight);

            if (roll < acc)
            {
                return data;
            }
        }

        return null;
    }
}
