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

}
