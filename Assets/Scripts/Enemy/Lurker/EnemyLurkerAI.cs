using UnityEngine;

public class EnemyLurkerAI : MonoBehaviour
{
    [SerializeField] private Transform _player;

    [SerializeField] private float _detectRange = 6f;     // 플레이어 인지 거리
    [SerializeField] private float _hideRange = 10f;      // 이 이상 멀면 다시 배회로

    [SerializeField] private float _patrolTimeMin = 1f;
    [SerializeField] private float _patrolTimeMax = 2f;

    [SerializeField] private float _hideTimeMin = 0.4f;
    [SerializeField] private float _hideTimeMax = 1.2f;

    [SerializeField] private float _fleeSpeedMultiplier = 1.4f;
    [SerializeField] private float _minFleeTime = 1.0f;

    [SerializeField] private float _revealRange = 12f;    //이 거리에서 반투명
    [SerializeField] private float _fullRevealRange = 7f; //이 거리에서 보이게
    [SerializeField, Range(0f, 1f)] private float _revealAlpha = 0.2f;

    [SerializeField, Range(0f, 1f)] private float _fleeStartAlpha = 0.6f;
    [SerializeField] private float _fleeFadeDuration = 0.05f;

    [SerializeField] private SpriteRenderer _sprite;

    private Rigidbody2D _rigid;
    private EnemyView _view;
    private EnemyState _enemyState;
    private EnemyController _controller;

    private enum State 
    { 
        Patrol, Hidden, Chase, Flee 
    }
    private State _state = State.Patrol;

    private Vector2 _moveDir;
    private float _stateTimer;
    private float _fleeTimer;

    private float _fleeFadeTimer = 0f;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _view = GetComponent<EnemyView>();
        _enemyState = GetComponent<EnemyState>();
        _controller = GetComponent<EnemyController>();

        if (_sprite == null)
        {
            _sprite = GetComponent<SpriteRenderer>();
        }
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
                Debug.LogError("[EnemyLurkerAI] 플레이어컨트롤러를 찾지 못함");
            }
        }

        ChangeState(State.Patrol);
    }

    private void FixedUpdate()
    {
        if (_player == null)
        {
            return;
        }

        if (_enemyState.current == EnemyState.State.Dead)
        {
            _rigid.linearVelocity = Vector2.zero;
            _view.SetMove(Vector2.zero, 0f);
            SetAlpha(1f);
            return;
        }

        float dist = Vector2.Distance(_rigid.position, _player.position);

        UpdateVisibility(dist);

        switch (_state)
        {
            case State.Patrol:
                UpdatePatrol(dist);
                break;
            case State.Hidden:
                UpdateHidden(dist);
                break;
            case State.Chase:
                UpdateChase(dist);
                break;
            case State.Flee:
                UpdateFlee(dist);
                break;
        }
    }

    private void UpdatePatrol(float dist)
    {
        if (dist <= _detectRange)
        {
            ChangeState(State.Hidden);
            return;
        }

        _stateTimer -= Time.fixedDeltaTime;

        float speed = _controller.MoveSpeed * 0.5f;
        _rigid.linearVelocity = _moveDir * speed;

        _view.SetMove(_moveDir, 0.5f);

        if (_stateTimer <= 0f)
        {
            ChangeState(State.Patrol);
        }
    }

    private void UpdateHidden(float dist)
    {
        _rigid.linearVelocity = Vector2.zero;
        _view.SetMove(Vector2.zero, 0f);

        _stateTimer -= Time.fixedDeltaTime;

        if (dist >= _hideRange)
        {
            ChangeState(State.Patrol);
            return;
        }

        if (_stateTimer <= 0f)
        {
            ChangeState(State.Chase);
        }
    }

    private void UpdateChase(float dist)
    {
        Vector2 dir = ((Vector2)_player.position - _rigid.position).normalized;

        float speed = _controller.MoveSpeed;
        _rigid.linearVelocity = dir * speed;

        _view.SetMove(dir, 1f);

        if (dist >= _hideRange)
        {
            ChangeState(State.Patrol);
        }
    }

    private void UpdateFlee(float dist)
    {
        _fleeTimer += Time.fixedDeltaTime;

        Vector2 awayDir = (_rigid.position - (Vector2)_player.position).normalized;

        float speed = _controller.MoveSpeed * _fleeSpeedMultiplier;
        _rigid.linearVelocity = awayDir * speed;

        _view.SetMove(awayDir, 1f);

        if (_fleeTimer >= _minFleeTime && dist >= _hideRange)
        {
            ChangeState(State.Patrol);
        }
    }

    private void ChangeState(State next)
    {
        _state = next;

        if (next == State.Hidden)
        {
            _stateTimer = Random.Range(_hideTimeMin, _hideTimeMax);
            _rigid.linearVelocity = Vector2.zero;
        }
        else if (next == State.Patrol)
        {
            _stateTimer = Random.Range(_patrolTimeMin, _patrolTimeMax);
            _moveDir = Random.value < 0.5f ? Vector2.left : Vector2.right;
        }
        else if (next == State.Flee)
        {
            _fleeTimer = 0f;
            _fleeFadeTimer = 0f;
        }
    }
    private void UpdateVisibility(float dist)
    {
        if (_sprite == null)
        {
            return;
        }

        if (_state == State.Hidden)
        {
            SetAlpha(0f);
            return;
        }

        if (_state == State.Flee)
        {
            _fleeFadeTimer += Time.fixedDeltaTime;

            float t = 1f;
            if (_fleeFadeDuration > 0f)
            {
                t = Mathf.Clamp01(_fleeFadeTimer / _fleeFadeDuration);
            }

            float a = Mathf.Lerp(_fleeStartAlpha, 0f, t);
            SetAlpha(a);
            return;
        }

        if (dist > _revealRange)
        {
            SetAlpha(0f);
            return;
        }

        if (dist > _fullRevealRange)
        {
            SetAlpha(_revealAlpha);
            return;
        }

        SetAlpha(1f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_enemyState.current == EnemyState.State.Dead)
        {
            return;
        }

        if (_state == State.Chase && collision.collider.CompareTag("Player"))
        {
            ChangeState(State.Flee);
        }
    }
    private void SetAlpha(float alpha)
    {
        Color color = _sprite.color;
        color.a = alpha;
        _sprite.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _hideRange);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _revealRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, _fullRevealRange);
    }
}
