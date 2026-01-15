using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerView))]
[RequireComponent(typeof(PlayerState))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStats _baseStats;
    [SerializeField] private float _hitStunTime = 0.2f;
    [SerializeField] private float _invincibleTime = 0.5f;

    private PlayerModel _model;
    private PlayerView _view;
    private PlayerState _state;

    private Rigidbody2D _rb;
    private bool _isInvincible = false;

    private Vector2 _moveInput = Vector2.zero;

    private Coroutine _invincibleCoroutine;
    private Coroutine _hitRecoverCoroutine;

    private void Awake()
    {
        _model = new PlayerModel();
        _model.Init(_baseStats);
        _view = GetComponent<PlayerView>();
        _state = GetComponent<PlayerState>();
        _rb = GetComponent<Rigidbody2D>();
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        _moveInput = input.normalized;
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }

        TryAttack();
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == false)
        {
            return;
        }

        TryInteract();
    }
    private void Start()
    {
        UIManager.Instance.InitPlayerUI("PlayerName", _model.currentHP, _model.maxHP);
    }
    private void FixedUpdate()
    {
        Move();
    }

    private void Update()
    {
        UpdateAnimations();
    }

    private void Move()
    {
        if (_state.current == PlayerState.State.Attack ||
            _state.current == PlayerState.State.Hit ||
            _state.current == PlayerState.State.Dead)
        {
            return;
        }

        _rb.MovePosition(_rb.position + _moveInput * _model.moveSpeed * Time.fixedDeltaTime);

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            _state.ChangeState(PlayerState.State.Move);
        }
        else
        {
            _state.ChangeState(PlayerState.State.Idle);
        }
    }

    private void UpdateAnimations()
    {
        float x = _moveInput.x;
        float speed = _moveInput.sqrMagnitude;

        _view.SetMove(x, speed);
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
        _state.ChangeState(PlayerState.State.Dead);

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

        _view.SetDead(true);
    }
    private void TryAttack()
    {
        //_state.ChangeState(PlayerState.State.Attack);
        //_view.PlayAttack();
    }

    private void TryInteract()
    {
        // 나중에 구현
    }
}
