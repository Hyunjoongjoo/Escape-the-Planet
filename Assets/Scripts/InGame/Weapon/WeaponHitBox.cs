using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

[RequireComponent(typeof(BoxCollider2D))]
public class WeaponHitBox : MonoBehaviour
{
    private BoxCollider2D _col;

    private int _damage = 5;
    private bool _isActive = false;

    private Vector2 _baseOffset;
    private Vector2 _baseSize;

    private int _facing = 1;

    private PhotonView _ownerView;

    private readonly HashSet<int> _hitTargets = new HashSet<int>();

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;
        _col.enabled = false;

        _ownerView = GetComponentInParent<PhotonView>();
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

        _damage = weapon.damage;

        _baseSize = weapon.hitBoxSize;
        _baseOffset = weapon.hitBoxOffset;

        if (_col != null)
        {
            _col.size = _baseSize;
        }

        ApplyFacing();
    }

    public void SetActive(bool value)
    {
        _isActive = value;

        if (_col != null)
        {
            _col.enabled = value;
        }

        if (value)
        {
            _hitTargets.Clear();
        }
    }
    public void SetFacing(int facing)
    {
        _facing = (facing >= 0) ? 1 : -1;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
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

        if (_ownerView != null && _ownerView.IsMine == false)
        {
            return;
        }

        if (other.TryGetComponent(out EnemyController enemy))
        {
            enemy.TakeDamage(_damage);
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        PhotonView targetView = player.GetComponent<PhotonView>();
        if (targetView == null || _ownerView == null)
        {
            return;
        }

        if (targetView.ViewID == _ownerView.ViewID)
        {
            return;
        }

        if (_hitTargets.Contains(targetView.ViewID))
        {
            return;
        }

        _hitTargets.Add(targetView.ViewID);

        player.RequestDamageFromOther(_damage, _ownerView.ViewID);
    }
}
    

