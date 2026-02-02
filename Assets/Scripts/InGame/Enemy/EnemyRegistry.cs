using UnityEngine;
using System.Collections.Generic;

public class EnemyRegistry : MonoBehaviour
{
    public static EnemyRegistry Instance;

    private readonly List<EnemyController> _enemies = new();

    public IReadOnlyList<EnemyController> Enemies
    {
        get
        {
            _enemies.RemoveAll(enemy => enemy == null);
            return _enemies;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Register(EnemyController enemy)
    {
        if (!_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
        }
    }

    public void Unregister(EnemyController enemy)
    {
        _enemies.Remove(enemy);
    }
}
