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
        EnsureCollider();

        if (_col != null)
        {
            _col.isTrigger = true;
            _col.enabled = false;
        }
    }
    private void EnsureCollider()
    {
        if (_col == null)
        {
            _col = GetComponent<BoxCollider2D>();
        }
    }
    public void ApplyWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            return;
        }

        EnsureCollider();
        if (_col == null)
        {
            return;
        }

        _damage = weapon.damage;

        _baseSize = weapon.hitBoxSize;
        _baseOffset = weapon.hitBoxOffset;

        _col.size = _baseSize;

        ApplyFacing();
    }

    public void SetActive(bool value)
    {
        EnsureCollider();
        if (_col == null)
        {
            return;
        }

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
        EnsureCollider();
        if (_col == null)
        {
            return;
        }

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
