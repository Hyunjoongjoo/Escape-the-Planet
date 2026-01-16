using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WeaponHitBox : MonoBehaviour
{
    private BoxCollider2D _col;

    private int _damage = 5;
    private bool _isActive = false;

    private Vector2 _baseOffset;
    private Vector2 _baseSize;

    private int _facing = 1;

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
        _col.enabled = false;
    }

    public void ApplyWeapon(WeaponData weapon)
    {
        _damage = weapon.damage;

        _baseSize = weapon.hitBoxSize;
        _baseOffset = weapon.hitBoxOffset;

        _col.size = _baseSize;

        ApplyFacing();
    }

    public void SetActive(bool value)
    {
        _isActive = value;
        _col.enabled = value;
    }
    public void SetFacing(int facing)
    {
        _facing = (facing >= 0) ? 1 : -1;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        Vector2 off = _baseOffset;
        off.x = Mathf.Abs(off.x) * _facing;
        _col.offset = off;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isActive == false)
        {
            return;
        }

        if (other.TryGetComponent(out EnemyController enemy) == false)
        {
            return;
        }

        enemy.TakeDamage(_damage);
    }
    
}
