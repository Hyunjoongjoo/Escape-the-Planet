using UnityEngine;

[System.Serializable]
public class EnemyModel
{
    public float moveSpeed;
    public int maxHP;
    public int currentHP;
    public int contactDamage;
    public bool alwaysChase;
    public float approachDistance;

    private EnemyStats _stats;

    public void Init(EnemyStats stats)
    {
        _stats = stats;

        moveSpeed = stats.baseMoveSpeed;
        maxHP = stats.baseMaxHP;
        currentHP = maxHP;
        contactDamage = stats.baseContactDamage;
    }

    public void ScaleByTime(float deltaTime)
    {
        maxHP += Mathf.RoundToInt(_stats.hpGrowthPerSecond * deltaTime);
        contactDamage += Mathf.RoundToInt(_stats.damageGrowthPerSecond * deltaTime);
        moveSpeed += _stats.speedGrowthPerSecond * deltaTime;
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;
        if (currentHP < 0)
        {
            currentHP = 0;
        }
    }

    public bool IsDead()
    {
        return currentHP <= 0;
    }
}
