using UnityEngine;

public enum EnemyId
{
    NONE = 0,
    CHASER = 1,
    WATCHER = 2,
    LURKER = 3,
}
[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public EnemyId id = EnemyId.NONE;
    public GameObject prefab;

    public float baseMoveSpeed = 1.5f;
    public int baseMaxHP = 30;
    public int baseContactDamage = 5;

    [Range(0f, 1f)] public float hpGrowthPerMinute = 0.10f;      // 분당 +10%
    [Range(0f, 1f)] public float damageGrowthPerMinute = 0.08f;  // 분당 +8%
    [Range(0f, 1f)] public float speedGrowthPerMinute = 0.03f;   // 분당 +3%

    [Range(1, 1000)] public int weight = 100;
    public float spawnRadius = 1.2f;
}
