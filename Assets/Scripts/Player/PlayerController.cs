using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerView))]
[RequireComponent(typeof(PlayerState))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStats _baseStats;
    private PlayerModel _model;
    private PlayerView _view;
    private PlayerState _state;

    private Rigidbody2D _rb;

    private Vector2 _moveInput = Vector2.zero;

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
        float y = _moveInput.y;
        float speed = _moveInput.sqrMagnitude;

        _view.SetMove(x, speed);
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
