using UnityEngine;

[System.Serializable]
public class PlayerModel
{
    public float moveSpeed;
    public int maxHP;
    public int currentHP;
    public float attackPower;

    public void Init(PlayerStats model)
    {
        moveSpeed = model.baseMoveSpeed;
        maxHP = model.baseMaxHP;
        currentHP = maxHP;
        attackPower = model.baseAttackPower;
    }
    public void TakeDamage(int damage)
    {
        currentHP -= damage;

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
