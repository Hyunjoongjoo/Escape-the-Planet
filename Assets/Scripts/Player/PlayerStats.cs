using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    public float baseMoveSpeed = 3.5f;
    public int baseMaxHP = 100;
    public float baseAttackPower = 10f;
    public float baseInteractRange = 1.2f;
}
