using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Photon.Pun;
using System;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerView))]
[RequireComponent(typeof(PlayerState))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStats _baseStats;
    [SerializeField] private float _hitStunTime = 0.2f;
    [SerializeField] private float _invincibleTime = 0.5f;

    [SerializeField] private WeaponHitBox _weaponHitBox;
    [SerializeField] private WeaponData _defaultWeapon;

    [SerializeField] private Transform _followTarget;
    [SerializeField] private PlayerInteraction _interaction;

    [SerializeField] private GroundItem _groundItemPrefab;
    [SerializeField] private float _dropDistance = 0.7f;

    [SerializeField] private bool _isDead = false;

    private PlayerModel _model;
    private PlayerView _view;
    private PlayerState _state;
    private PhotonView _photonView;

    private Rigidbody2D _rigid;
    private bool _isInvincible = false;
    private float _nextAttackTime = 0f;

    private int _attackFacing = 1;
    private int _facing = 1;

    private Vector2 _moveInput = Vector2.zero;

    private Coroutine _invincibleCoroutine;
    private Coroutine _hitRecoverCoroutine;

    private bool _inputEnabled = true;
    private bool _droppedAllOnDeath = false;

    public static event Action OnPlayerDead;

    public bool IsDead => _isDead;

    public Transform FollowTarget
    {
        get
        {
            return _followTarget != null ? _followTarget : transform;
        }
    }

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _model = new PlayerModel();
        _model.Init(_baseStats);
        _model.currentWeapon = _defaultWeapon;

        _weaponHitBox.ApplyWeapon(_model.currentWeapon);

        _view = GetComponent<PlayerView>();
        _state = GetComponent<PlayerState>();
        _rigid = GetComponent<Rigidbody2D>();
        if (_interaction == null)
        {
            _interaction = GetComponentInChildren<PlayerInteraction>(true);
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false)
        {
            return;
        }
        Vector2 input = ctx.ReadValue<Vector2>();
        _moveInput = input.normalized;

        if (_moveInput.x > 0.01f)
        {
            _facing = 1;
        }
        else if (_moveInput.x < -0.01f)
        {
            _facing = -1;
        }

            _weaponHitBox.SetFacing(_facing);
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false)
        {
            return;
        }

        if (ctx.performed == false)
        {
            return;
        }

        TryAttack();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false)
        {
            return;
        }

        if (ctx.started == false)
        {
            return;
        }

        TryInteract();
    }

    public void OnSelectSlot(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false)
        {
            return;
        }

        if (ctx.started == false)
        {
            return;
        }

        int index = GetNumberKeyIndex(ctx);
        if (index < 0)
        {
            return;
        }

        QuickSlotManager.Instance.SelectSlot(index);
    }

    public void OnDrop(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false)
        {
            return;
        }

        if (ctx.started == false)
        {
            return;
        }

        TryDrop();
    }

    private int GetNumberKeyIndex(InputAction.CallbackContext ctx)
    {
        string controlName = ctx.control.name;

        if (controlName == "1") { return 0; }
        if (controlName == "2") { return 1; }
        if (controlName == "3") { return 2; }
        if (controlName == "4") { return 3; }
        if (controlName == "5") { return 4; }

        return -1;
    }
    private void Start()
    {
        string nickname = "Player";
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
        {
            nickname = PhotonNetwork.LocalPlayer.NickName;
        }
        UIManager.Instance.InitPlayerUI(nickname, _model.currentHP, _model.maxHP);
    }

    private void Update()
    {
        if (_photonView != null && !_photonView.IsMine)
        {
            return;
        }

        if (_isDead)
        {
            return;
        }
        UpdateAnimations();
    }

    private void UpdateAnimations()
    {

        if (_state.current == PlayerState.State.Attack)
        {
            _view.SetMove(_attackFacing, 0f);
            return;
        }

        if (_moveInput.x > 0.01f)
        {
            _facing = 1;
        }
        else if (_moveInput.x < -0.01f)
        {
            _facing = -1;
        }

        float speed01 = 0f;

        if (_model.moveSpeed > 0f)
        {
            speed01 = Mathf.Clamp01(_rigid.linearVelocity.magnitude / _model.moveSpeed);
        }

        _view.SetMove(_facing, speed01);
    }

    private void FixedUpdate()
    {
        if (_photonView != null && !_photonView.IsMine)
        {
            return;
        }

        if (_isDead)
        {
            return;
        }
        Move();
    }    

    private void Move()
    {
        if (_state.current == PlayerState.State.Attack ||
            _state.current == PlayerState.State.Hit ||
            _state.current == PlayerState.State.Dead)
        {
            _rigid.linearVelocity = Vector2.zero;
            return;
        }

        _rigid.linearVelocity = _moveInput * _model.moveSpeed;

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            _state.ChangeState(PlayerState.State.Move);
        }
        else
        {
            _state.ChangeState(PlayerState.State.Idle);
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (_state.current == PlayerState.State.Dead)
        {
            return;
        }

        if (_isInvincible == true)
        {
            return;
        }

        _model.TakeDamage(damage);
        UIManager.Instance.UpdateHP(_model.currentHP, _model.maxHP);

        if (_model.IsDead())
        {
            Die();
            return;
        }

        _state.ChangeState(PlayerState.State.Hit);
        _view.PlayHit();

        _weaponHitBox.SetActive(false);

        if (_hitRecoverCoroutine != null)
        {
            StopCoroutine(_hitRecoverCoroutine);
        }
        _hitRecoverCoroutine = StartCoroutine(HitRecoverRoutine());

        if (_invincibleCoroutine != null)
        {
            StopCoroutine(_invincibleCoroutine);
        }
        _invincibleCoroutine = StartCoroutine(InvincibleRoutine());
    }
    private IEnumerator HitRecoverRoutine()
    {
        yield return new WaitForSeconds(_hitStunTime);

        if (_state.current == PlayerState.State.Dead)
        {
            yield break;
        }

        if (_state.current == PlayerState.State.Hit)
        {
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _state.ChangeState(PlayerState.State.Move);
            }
            else
            {
                _state.ChangeState(PlayerState.State.Idle);
            }
        }

        _hitRecoverCoroutine = null;
    }
    private IEnumerator InvincibleRoutine()
    {
        _isInvincible = true;

        for (int i = 0; i < 5; i++)
        {
            _view.SetVisible(false);
            yield return new WaitForSeconds(0.1f);

            _view.SetVisible(true);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(_invincibleTime);

        _isInvincible = false;
        _invincibleCoroutine = null;
    }
    private void Die()
    {
        if (_photonView != null && !_photonView.IsMine)
        {
            return;
        }

        if (_droppedAllOnDeath == false)
        {
            _droppedAllOnDeath = true;

            if (QuickSlotManager.Instance != null)
            {
                QuickSlotManager.Instance.DropAllOnDeath(transform.position);
            }
        }

        _state.ChangeState(PlayerState.State.Dead);

        _isDead = true;

        _rigid.linearVelocity = Vector2.zero;
        _rigid.angularVelocity = 0f;
        _rigid.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) 
        { 
            col.isTrigger = true;
        }
        _moveInput = Vector2.zero;

        if (_hitRecoverCoroutine != null)
        {
            StopCoroutine(_hitRecoverCoroutine);
            _hitRecoverCoroutine = null;
        }

        if (_invincibleCoroutine != null)
        {
            StopCoroutine(_invincibleCoroutine);
            _invincibleCoroutine = null;
        }

        _weaponHitBox.SetActive(false);

        _view.SetDead(true);
        OnPlayerDead?.Invoke();
    }
    private void TryAttack()
    {
        if (_state.current == PlayerState.State.Dead)
        {
            return;
        }

        if (_state.current == PlayerState.State.Attack)
        {
            return;
        }

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        if (_model.currentWeapon == null)
        {
            return;
        }

        _rigid.linearVelocity = Vector2.zero;

        _attackFacing = _facing;

        _nextAttackTime = Time.time + _model.currentWeapon.cooldown;

        _weaponHitBox.SetFacing(_facing);

        _state.ChangeState(PlayerState.State.Attack);
        _view.PlayAttack();

    }
    
    public void OnAttackStart()
    {
        _weaponHitBox.SetFacing(_attackFacing);
        _weaponHitBox.SetActive(true);
    }

    public void OnAttackEnd()
    {      
        _weaponHitBox.SetActive(false);   
    }
    public void OnAttackAnimEnd()
    {
        if (_state.current != PlayerState.State.Attack)
        {
            return;
        }

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            _state.ChangeState(PlayerState.State.Move);
        }
        else
        {
            _state.ChangeState(PlayerState.State.Idle);

        }
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        if (_inputEnabled == false)
        {
            _moveInput = Vector2.zero;

            if (_rigid != null)
            {
                _rigid.linearVelocity = Vector2.zero;
            }
        }
    }

    public void EnterSpectatorMode()
    {
        SetInputEnabled(false);

        _isDead = true;

        _rigid.linearVelocity = Vector2.zero;
        _rigid.angularVelocity = 0f;

        _rigid.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (_weaponHitBox != null)
        {
            _weaponHitBox.SetActive(false);
        }

        // 관전 표시용
        _view.SetVisible(true);
    }

    private void TryInteract()
    {
        if (_interaction == null)
        {
            return;
        }

        _interaction.TryInteract(gameObject);
    }

    private void TryDrop()
    {
        if (QuickSlotManager.Instance == null)
        {
            return;
        }

        if (_groundItemPrefab == null)
        {
            return;
        }

        if (QuickSlotManager.Instance.TryDropCurrent(out ItemData dropped) == false)
        {
            return;
        }

        Vector3 dropPos = transform.position + new Vector3(_facing * _dropDistance, 0f, 0f);

        GroundItem gi = Instantiate(_groundItemPrefab, dropPos, Quaternion.identity);
        gi.Setup(dropped);
    }
}
