using UnityEngine;

public class EnemyChaserAI : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _detectRange = 12f;
    [SerializeField] private float _lostRange = 18f;

    [SerializeField] private float _walkSpeed = 1f;
    [SerializeField] private float _runSpeed = 2.5f;

    [SerializeField] private float _idleTimeMin = 0.5f;
    [SerializeField] private float _idleTimeMax = 1.5f;
    [SerializeField] private float _walkTimeMin = 1f;
    [SerializeField] private float _walkTimeMax = 2f;

    private Rigidbody2D _rb;
    private EnemyView _view;
    private EnemyState _enemyState;

    private bool _forcedChase = false;

    private enum State 
    { 
        Idle, Patrol, Chase 
    }
    private State _state = State.Idle;

    private Vector2 _moveDir;
    private float _stateTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _view = GetComponent<EnemyView>();
        _enemyState = GetComponent<EnemyState>();
    }

    private void Start()
    {
        if (_player == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _player = player.FollowTarget;
            }
            else
            {
                Debug.LogError("[EnemyChaserAI] 플레이어컨트롤러를 찾지 못함");
            }

        }

        ChangeState(State.Idle);
    }

    private void FixedUpdate()
    {
        UpdateState();
    }

    public void ForceChase(Transform target)
    {
        _player = target;
        _forcedChase = true;
        ChangeState(State.Chase);
    }

    private void UpdateState()
    {
        if (_enemyState != null && _enemyState.current == EnemyState.State.Dead)
        {
            _rb.linearVelocity = Vector2.zero;
            _view.SetMove(Vector2.zero, 0f);
            return;
        }

        if (_player == null)
        {
            Debug.LogWarning("플레이어가 null임");
            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);

        if (!_forcedChase)
        {
            if (_state != State.Chase && dist <= _detectRange)
            {
                ChangeState(State.Chase);
            }
            else if (_state == State.Chase && dist >= _lostRange)
            {
                ChangeState(State.Idle);
            }
        }
        else
        {
            ChangeState(State.Chase);
        }

        switch (_state)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
        }
    }

    private void UpdateIdle()
    {
        _stateTimer -= Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.zero;
        _view.SetMove(Vector2.zero, 0f);

        if (_stateTimer <= 0f)
        {
            ChangeState(State.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        _stateTimer -= Time.fixedDeltaTime;

        _rb.linearVelocity = _moveDir * _walkSpeed;
        _view.SetMove(_moveDir, 0.5f);

        if (_stateTimer <= 0f)
        {
            ChangeState(State.Idle);
        }
    }

    private void UpdateChase()
    {
        Vector2 dir = ((Vector2)_player.position - _rb.position).normalized;

        _rb.linearVelocity = dir * _runSpeed;
        _view.SetMove(dir, 1f);
    }

    private void ChangeState(State next)
    {
        _state = next;

        if (next == State.Idle)
        {
            _stateTimer = Random.Range(_idleTimeMin, _idleTimeMax);
            _moveDir = Vector2.zero;
        }
        else if (next == State.Patrol)
        {
            _stateTimer = Random.Range(_walkTimeMin, _walkTimeMax);
            _moveDir = Random.value < 0.5f ? Vector2.left : Vector2.right;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _lostRange);
    }
}
