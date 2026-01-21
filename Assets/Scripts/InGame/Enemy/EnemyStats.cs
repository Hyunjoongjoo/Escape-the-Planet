using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    public float baseMoveSpeed = 1.5f;
    public int baseMaxHP = 30;
    public int baseContactDamage = 5;

    public float hpGrowthPerSecond = 0.1f;
    public float damageGrowthPerSecond = 0.05f;
    public float speedGrowthPerSecond = 0.01f;
}
