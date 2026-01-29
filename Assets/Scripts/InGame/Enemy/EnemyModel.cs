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

    private EnemyData _stats;

    public void Init(EnemyData stats)
    {
        _stats = stats;

        moveSpeed = stats.baseMoveSpeed;
        maxHP = stats.baseMaxHP;
        currentHP = maxHP;
        contactDamage = stats.baseContactDamage;
    }

    public void ApplySpawnGrowth(float elapsedMinutes)
    {
        if (_stats == null)
        {
            return;
        }

        float hpMul = 1f + (_stats.hpGrowthPerMinute * elapsedMinutes);
        float dmgMul = 1f + (_stats.damageGrowthPerMinute * elapsedMinutes);
        float spdMul = 1f + (_stats.speedGrowthPerMinute * elapsedMinutes);

        maxHP = Mathf.Max(1, Mathf.RoundToInt(maxHP * hpMul));
        contactDamage = Mathf.Max(1, Mathf.RoundToInt(contactDamage * dmgMul));
        moveSpeed = Mathf.Max(0.1f, moveSpeed * spdMul);

        currentHP = maxHP;
        Debug.Log($"SPAWN FINAL DAMAGE = {contactDamage}");
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
