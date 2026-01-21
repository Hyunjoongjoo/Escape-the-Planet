using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public int damage = 5;

    public Vector2 hitBoxSize = new Vector2(1.2f, 0.8f);
    public Vector2 hitBoxOffset = new Vector2(0.6f, 0f);

    public float attackDuration = 0.25f;
    public float cooldown = 0.3f;

}
