using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerView))]
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private PlayerStats _baseStats;
    [SerializeField] private float _hitStunTime = 0.2f;
    [SerializeField] private float _invincibleTime = 0.5f;

    [SerializeField] private WeaponHitBox _weaponHitBox;
    [SerializeField] private WeaponData _defaultWeapon;

    [SerializeField] private Transform _followTarget;
    [SerializeField] private PlayerInteraction _interaction;

    [SerializeField] private string _groundItemPrefabName = "GroundItem";
    [SerializeField] private float _dropDistance = 0.7f;
    [SerializeField] private float _dropScatterRadius = 1.0f;

    [SerializeField] private float _softBlockRadius = 0.6f;
    [SerializeField] private float _softBlockStrength = 0.5f;

    [SerializeField] private PlayerInput _playerInput;

    [SerializeField] private bool _isDead = false;

    [SerializeField] private float _hitFxCooldown = 0.08f;
    private float _nextHitFxTime = 0f;

    private PlayerModel _model;
    private PlayerView _view;
    private PlayerState _state;

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

    private Vector3 _remotePrevPos;
    private float _remoteSpeed01 = 0f;
    private bool _remoteInit = false;

    private int _netFacing = 1;

    private bool _netIsDead = false;

    public static event Action OnPlayerDead;

    public bool IsDead => _isDead;
    public Transform FollowTarget => _followTarget != null ? _followTarget : transform;

    private void Awake()
    {
        _model = new PlayerModel();
        _model.Init(_baseStats);

        _view = GetComponent<PlayerView>();
        _state = GetComponent<PlayerState>();
        _rigid = GetComponent<Rigidbody2D>();

        if (_playerInput == null)
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        if (_interaction == null)
        {
            _interaction = GetComponentInChildren<PlayerInteraction>(true);
        }

        if (_weaponHitBox == null)
        {
            _weaponHitBox = GetComponentInChildren<WeaponHitBox>(true);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PlayerRegistry.Instance?.Register(this);
    }

    public override void OnDisable()
    {
        PlayerRegistry.Instance?.Unregister(this);
        base.OnDisable();
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (_isDead) { return; }
        if (_inputEnabled == false) { return; }

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
        if (_inputEnabled == false) { return; }
        if (ctx.performed == false) { return; }

        TryAttack();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false) { return; }
        if (ctx.started == false) { return; }

        TryInteract();
    }

    public void OnSelectSlot(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false) { return; }
        if (ctx.started == false) { return; }

        int index = GetNumberKeyIndex(ctx);
        if (index < 0) { return; }

        QuickSlotManager.Instance.SelectSlot(index);
    }

    public void OnDrop(InputAction.CallbackContext ctx)
    {
        if (_inputEnabled == false) { return; }
        if (ctx.started == false) { return; }

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
        if (PlayerRegistry.Instance != null)
        {
            PlayerRegistry.Instance.Register(this);
        }

        if (photonView.IsMine && PhotonNetwork.InRoom)
        {
            PhotonPlayerLocationManager.SetLocation(PlayerLocation.InGame);
        }

        if (!photonView.IsMine)
        {
            if (_playerInput != null)
            {
                _playerInput.enabled = false;
            }

            SetInputEnabled(false);
            _weaponHitBox.SetActive(false);

            _rigid.bodyType = RigidbodyType2D.Kinematic;
            _rigid.linearVelocity = Vector2.zero;
            _rigid.angularVelocity = 0f;

            _remotePrevPos = transform.position;
            _remoteInit = true;
            _remoteSpeed01 = 0f;

            return;
        }

        if (_playerInput != null)
        {
            _playerInput.enabled = true;
        }

        EnsureWeaponApplied();

        ApplyNextDayHpRule_Local();

        string nickname = PhotonNetwork.LocalPlayer.NickName;
        UIManager.Instance.InitPlayerUI(nickname, _model.currentHP, _model.maxHP);

        StartCoroutine(BindCinemachineRoutine());
    }

    private void OnDestroy()
    {
        if (PlayerRegistry.Instance != null)
        {
            PlayerRegistry.Instance.Unregister(this);
        }
    }

    private void EnsureWeaponApplied()
    {
        if (_model.currentWeapon == null)
        {
            _model.currentWeapon = _defaultWeapon;
        }

        _weaponHitBox.ApplyWeapon(_model.currentWeapon);
        _weaponHitBox.SetActive(false);
        _weaponHitBox.SetFacing(_facing);
    }

    private void ApplyNextDayHpRule_Local()
    {
        float ratio = PhotonPlayerStateManager.GetNextDayHpRatio(PhotonNetwork.LocalPlayer);

        int hp = Mathf.RoundToInt(_model.maxHP * ratio);
        if (hp < 1)
        {
            hp = 1;
        }

        _model.currentHP = hp;
    }

    private IEnumerator BindCinemachineRoutine()
    {
        yield return null;

        var cam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include);
        if (cam == null)
        {
            yield break;
        }

        cam.Follow = FollowTarget;
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            ApplyRemoteDeadSync();
            UpdateRemoteAnimSpeed();
            _facing = _netFacing;
        }

        if (_isDead)
        {
            _view.SetDead(true);
            return;
        }

        UpdateAnimations();
    }

    private void ApplyRemoteDeadSync()
    {
        if (photonView.IsMine)
        {
            return;
        }

        if (_netIsDead && !_isDead)
        {
            _isDead = true;
            _state.ChangeState(PlayerState.State.Dead);
            _view.SetDead(true);

            if (_weaponHitBox != null)
            {
                _weaponHitBox.SetActive(false);
            }

            if (_rigid != null)
            {
                _rigid.linearVelocity = Vector2.zero;
                _rigid.bodyType = RigidbodyType2D.Kinematic;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }

    private void UpdateRemoteAnimSpeed()
    {
        if (_isDead)
        {
            _remoteSpeed01 = 0f;
            return;
        }

        if (_remoteInit == false)
        {
            _remotePrevPos = transform.position;
            _remoteInit = true;
            _remoteSpeed01 = 0f;
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - _remotePrevPos;
        _remotePrevPos = currentPos;

        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            _remoteSpeed01 = 0f;
            return;
        }

        float snapDist = 0.9f;
        if (delta.sqrMagnitude > snapDist * snapDist)
        {
            _remoteSpeed01 = 0f;
            return;
        }

        float speed = delta.magnitude / dt;

        if (_model != null && _model.moveSpeed > 0f)
        {
            _remoteSpeed01 = Mathf.Clamp01(speed / _model.moveSpeed);
        }
        else
        {
            _remoteSpeed01 = 0f;
        }
    }

    private void UpdateAnimations()
    {
        if (_isDead)
        {
            _view.SetDead(true);
            return;
        }

        if (_state.current == PlayerState.State.Attack)
        {
            _view.SetMove(_attackFacing, 0f);
            return;
        }

        float speed01 = 0f;

        if (photonView.IsMine)
        {
            speed01 = Mathf.Clamp01(_rigid.linearVelocity.magnitude / _model.moveSpeed);
        }
        else
        {
            speed01 = _remoteSpeed01;
        }

        _view.SetMove(_facing, speed01);
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) { return; }
        if (_isDead) { return; }

        Move();
        ResolveSoftPlayerBlock();
    }

    private void ResolveSoftPlayerBlock()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, _softBlockRadius);

        for (int i = 0; i < hitPlayers.Length; i++)
        {
            Collider2D otherCollider = hitPlayers[i];

            if (otherCollider.gameObject == gameObject) { continue; }
            if (!otherCollider.CompareTag("Player")) { continue; }

            PlayerController otherPlayer = otherCollider.GetComponent<PlayerController>();
            if (otherPlayer == null) { continue; }

            Vector2 myPosition = transform.position;
            Vector2 otherPosition = otherPlayer.transform.position;

            Vector2 difference = myPosition - otherPosition;
            float distance = difference.magnitude;

            if (distance <= 0.0001f) { continue; }
            if (distance < _softBlockRadius)
            {
                float pushAmount = _softBlockRadius - distance;

                Vector2 pushDirection = difference.normalized;
                Vector2 push = pushDirection * pushAmount * _softBlockStrength;

                transform.position += (Vector3)push;
            }
        }
    }

    private void Move()
    {
        if (_state.current == PlayerState.State.Attack || _state.current == PlayerState.State.Hit || _state.current == PlayerState.State.Dead)
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

    public void ApplyDamage_FromMaster(int damage)
    {
        if (!photonView.IsMine) { return; }
        if (_isDead) { return; }
        if (_isInvincible) { return; }

        photonView.RPC(nameof(RPC_PlayHit), RpcTarget.All);
        TakeDamage_OwnerOnly(damage);
    }

    public void RequestTakeDamage(int damage)
    {
        if (_isDead) { return; }
        if (_isInvincible) { return; }

        photonView.RPC(nameof(RPC_PlayHit), RpcTarget.All);
        TakeDamage_OwnerOnly(damage);
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
        if (!photonView.IsMine) { return; }

        if (_droppedAllOnDeath == false)
        {
            _droppedAllOnDeath = true;
            QuickSlotManager.Instance.DropAllOnDeath(transform.position);
        }

        _state.ChangeState(PlayerState.State.Dead);
        _isDead = true;

        _rigid.linearVelocity = Vector2.zero;
        _rigid.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        _weaponHitBox.SetActive(false);

        PhotonPlayerStateManager.SetWasDeadThisDay(true);
        PhotonPlayerStateManager.SetState(PlayerGameState.Dead);

        _view.SetDead(true);
        OnPlayerDead?.Invoke();
        SpectatorCameraManager.Instance?.StartSpectate();
    }

    private void TryAttack()
    {
        if (_isDead) { return; }
        if (!photonView.IsMine) { return; }
        if (_state.current == PlayerState.State.Dead) { return; }
        if (_state.current == PlayerState.State.Attack) { return; }
        if (Time.time < _nextAttackTime) { return; }

        if (_model.currentWeapon == null)
        {
            EnsureWeaponApplied();
        }

        _rigid.linearVelocity = Vector2.zero;

        _attackFacing = _facing;
        _nextAttackTime = Time.time + _model.currentWeapon.cooldown;

        photonView.RPC(nameof(RPC_PlayAttack), RpcTarget.All, _attackFacing);
    }

    [PunRPC]
    private void RPC_PlayAttack(int facing)
    {
        if (_isDead) { return; }

        _attackFacing = (facing >= 0) ? 1 : -1;
        _facing = _attackFacing;

        if (_weaponHitBox != null)
        {
            _weaponHitBox.SetFacing(_attackFacing);
            _weaponHitBox.SetActive(false);
        }

        _state.ChangeState(PlayerState.State.Attack);
        _view.PlayAttack();
    }

    public void OnAttackStart()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        _weaponHitBox.SetFacing(_attackFacing);
        _weaponHitBox.SetActive(true);
    }

    public void OnAttackEnd()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        _weaponHitBox.SetActive(false);
    }

    public void OnAttackAnimEnd()
    {
        if (_isDead) { return; }
        if (_state.current != PlayerState.State.Attack) { return; }

        if (!photonView.IsMine)
        {
            _state.ChangeState(PlayerState.State.Idle);
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

    public void RequestDamageFromOther(int damage, int attackerViewId)
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(
            nameof(RPC_RequestDamageToMaster),
            RpcTarget.MasterClient,
            photonView.ViewID,
            damage,
            attackerViewId
        );
    }

    [PunRPC]
    private void RPC_RequestDamageToMaster(int targetViewId, int damage, int attackerViewId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonView targetView = PhotonView.Find(targetViewId);
        if (targetView == null)
        {
            return;
        }

        PlayerController targetPlayer = targetView.GetComponent<PlayerController>();
        if (targetPlayer == null)
        {
            return;
        }

        if (targetPlayer._isDead)
        {
            return;
        }

        targetView.RPC(nameof(RPC_ApplyDamageFromMaster), targetView.Owner, damage);
    }

    [PunRPC]
    public void RPC_ApplyDamageFromMaster(int damage)
    {
        ApplyDamage_FromMaster(damage);
    }

    [PunRPC]
    private void RPC_PlayHit()
    {
        if (_isDead) { return; }
        if (_isInvincible) { return; }
        if (_state.current == PlayerState.State.Dead) { return; }
        if (Time.time < _nextHitFxTime) { return; }

        _nextHitFxTime = Time.time + _hitFxCooldown;

        _state.ChangeState(PlayerState.State.Hit);
        _view.PlayHit();
        _weaponHitBox.SetActive(false);
    }

    private void TakeDamage_OwnerOnly(int damage)
    {
        if (photonView.IsMine == false) { return; }
        if (_state.current == PlayerState.State.Dead) { return; }
        if (_isInvincible) { return; }

        _model.TakeDamage(damage);
        UIManager.Instance.UpdateHP(_model.currentHP, _model.maxHP);

        if (_model.IsDead())
        {
            Die();
            return;
        }

        if (_hitRecoverCoroutine != null)
        {
            StopCoroutine(_hitRecoverCoroutine);
            _hitRecoverCoroutine = null;
        }
        _hitRecoverCoroutine = StartCoroutine(HitRecoverRoutine());

        if (_invincibleCoroutine != null)
        {
            StopCoroutine(_invincibleCoroutine);
            _invincibleCoroutine = null;
        }
        _invincibleCoroutine = StartCoroutine(InvincibleRoutine());
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        if (_inputEnabled == false)
        {
            _moveInput = Vector2.zero;
            _rigid.linearVelocity = Vector2.zero;
        }
    }

    private void TryInteract()
    {
        _interaction.TryInteract(gameObject);
    }

    private void TryDrop()
    {
        if (!photonView.IsMine) { return; }

        QuickSlotManager.Instance.DropSelectedItem();
    }

    public void RequestDropItem(ItemId itemId)
    {
        if (!photonView.IsMine) { return; }

        Vector3 dropPos = transform.position + new Vector3(_facing * _dropDistance, 0f, 0f);

        photonView.RPC(nameof(RPC_RequestDropItem), RpcTarget.MasterClient, (int)itemId, dropPos.x, dropPos.y);
    }

    [PunRPC]
    private void RPC_RequestDropItem(int itemId, float x, float y, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) { return; }

        Vector2 pos = new Vector2(x, y);

        GameObject obj = PhotonNetwork.Instantiate(_groundItemPrefabName, pos, Quaternion.identity);

        GroundItemNetwork net = obj.GetComponent<GroundItemNetwork>();
        if (net != null)
        {
            net.SetItemId((ItemId)itemId);
        }
    }

    public void RequestDropAll(ItemId[] itemIds, Vector2 center)
    {
        if (!photonView.IsMine) { return; }

        int[] ids = new int[itemIds.Length];
        for (int i = 0; i < itemIds.Length; i++)
        {
            ids[i] = (int)itemIds[i];
        }

        photonView.RPC(nameof(RPC_RequestDropAll), RpcTarget.MasterClient, ids, center.x, center.y);
    }

    [PunRPC]
    private void RPC_RequestDropAll(int[] itemIds, float x, float y, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) { return; }

        Vector2 center = new Vector2(x, y);

        for (int i = 0; i < itemIds.Length; i++)
        {
            ItemId itemId = (ItemId)itemIds[i];

            Vector2 scatter = UnityEngine.Random.insideUnitCircle * _dropScatterRadius;
            Vector2 pos = center + scatter;

            GameObject obj = PhotonNetwork.Instantiate(_groundItemPrefabName, pos, Quaternion.identity);

            GroundItemNetwork net = obj.GetComponent<GroundItemNetwork>();
            if (net != null)
            {
                net.SetItemId(itemId);
            }
        }
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_facing);
            stream.SendNext(_isDead);
        }
        else
        {
            _netFacing = (int)stream.ReceiveNext();
            _netIsDead = (bool)stream.ReceiveNext();
        }
    }

    public void ResetForNewDay(float hpRatio)
    {
        ResetForNewDay_AllClients();

        if (photonView.IsMine)
        {
            ResetForNewDay_Local(hpRatio);
        }
    }

    public void ResetForNewDay_AllClients()
    {
        _isDead = false;
        _netIsDead = false;
        _droppedAllOnDeath = false;

        _state.ChangeState(PlayerState.State.Idle);

        _view.SetDead(false);
        _view.SetVisible(true);
        _view.ForceResetAnimator();

        if (_weaponHitBox != null)
        {
            _weaponHitBox.SetActive(false);
            _weaponHitBox.SetFacing(_facing);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            col.enabled = true;
        }

        if (_rigid != null)
        {
            _rigid.linearVelocity = Vector2.zero;
            _rigid.angularVelocity = 0f;
        }

        _isInvincible = false;

        if (_invincibleCoroutine != null)
        {
            StopCoroutine(_invincibleCoroutine);
        }
        _invincibleCoroutine = null;

        if (_hitRecoverCoroutine != null)
        {
            StopCoroutine(_hitRecoverCoroutine);
        }
        _hitRecoverCoroutine = null;
    }

    private void ResetForNewDay_Local(float hpRatio)
    {
        int hp = Mathf.RoundToInt(_model.maxHP * hpRatio);
        if (hp < 1)
        {
            hp = 1;
        }

        _model.currentHP = hp;
        UIManager.Instance.UpdateHP(_model.currentHP, _model.maxHP);

        SetInputEnabled(true);

        if (_playerInput != null)
        {
            _playerInput.enabled = true;
        }

        if (_rigid != null)
        {
            _rigid.bodyType = RigidbodyType2D.Dynamic;
            _rigid.linearVelocity = Vector2.zero;
            _rigid.angularVelocity = 0f;
        }
    }

    public void SetRoomMode()
    {
        if (photonView.IsMine)
        {
            SetInputEnabled(false);

            if (_playerInput != null)
            {
                _playerInput.enabled = true;
            }
        }
        else
        {
            SetInputEnabled(false);

            if (_playerInput != null)
            {
                _playerInput.enabled = false;
            }
        }

        _rigid.linearVelocity = Vector2.zero;

        if (!photonView.IsMine)
        {
            _rigid.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        _view.SetVisible(false);
    }

    public void SetInGameMode()
    {
        if (photonView.IsMine)
        {
            SetInputEnabled(true);

            if (_playerInput != null)
            {
                _playerInput.enabled = true;
            }

            _rigid.bodyType = RigidbodyType2D.Dynamic;
        }
        else
        {
            SetInputEnabled(false);

            if (_playerInput != null)
            {
                _playerInput.enabled = false;
            }

            _rigid.bodyType = RigidbodyType2D.Kinematic;
            _rigid.linearVelocity = Vector2.zero;
            _rigid.angularVelocity = 0f;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        _view.SetVisible(true);
    }
}