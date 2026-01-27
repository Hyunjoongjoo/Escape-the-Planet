using Photon.Pun;
using UnityEngine;

public class EnemyLurkerAI : MonoBehaviour
{
    [SerializeField] private Transform _player;

    [SerializeField] private float _detectRange = 6f;
    [SerializeField] private float _hideRange = 10f;

    [SerializeField] private float _wanderMoveTimeMin = 0.6f;
    [SerializeField] private float _wanderMoveTimeMax = 1.5f;
    [SerializeField] private float _wanderIdleTimeMin = 0.25f;
    [SerializeField] private float _wanderIdleTimeMax = 0.9f;
    [SerializeField] private float _wanderSpeedMultiplier = 0.5f;

    [SerializeField] private float _wanderObstacleRayDist = 0.6f;
    [SerializeField] private LayerMask _blockMask;

    [SerializeField] private float _hideTimeMin = 0.4f;
    [SerializeField] private float _hideTimeMax = 1.2f;

    [SerializeField] private float _fleeSpeedMultiplier = 1.4f;
    [SerializeField] private float _minFleeTime = 1.0f;

    [SerializeField] private float _revealRange = 12f;
    [SerializeField] private float _fullRevealRange = 7f;
    [SerializeField, Range(0f, 1f)] private float _revealAlpha = 0.2f;

    [SerializeField, Range(0f, 1f)] private float _fleeStartAlpha = 0.6f;
    [SerializeField] private float _fleeFadeDuration = 0.05f;

    [SerializeField] private SpriteRenderer _sprite;

    private EnemyView _view;
    private EnemyState _enemyState;
    private EnemyController _controller;

    private enum State
    {
        Patrol,
        Hidden,
        Chase,
        Flee
    }

    private enum PatrolMode
    {
        Move,
        Idle
    }

    private State _state = State.Patrol;
    private PatrolMode _patrolMode = PatrolMode.Move;

    private Vector2 _wanderDir = Vector2.zero;
    private float _wanderTimer = 0f;

    private float _stateTimer;
    private float _fleeTimer;
    private float _fleeFadeTimer = 0f;

    private void Awake()
    {
        _view = GetComponent<EnemyView>();
        _enemyState = GetComponent<EnemyState>();
        _controller = GetComponent<EnemyController>();

        if (_sprite == null)
        {
            _sprite = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        PlayerController.OnPlayerDead += HandlePlayerDead;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDead -= HandlePlayerDead;
    }

    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            enabled = false;
            return;
        }

        ChangeState(State.Patrol);
    }

    private void FixedUpdate()
    {

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                MatchKeys.DayState, out object stateValue))
        {
            return;
        }

        if ((DayState)(int)stateValue != DayState.Running)
        {
            _controller.StopMove();
            SetAlpha(0f);
            return;
        }

        _player = FindClosestAlivePlayer();

        if (_enemyState.current == EnemyState.State.Dead)
        {
            _controller.StopMove();
            SetAlpha(1f);
            return;
        }

        if (_player == null)
        {
            SetAlpha(0f);

            if (_state != State.Patrol)
            {
                ChangeState(State.Patrol);
            }

            UpdatePatrol(float.MaxValue);
            return;
        }

        PlayerController player = _player.GetComponentInParent<PlayerController>();
        if (player != null && player.IsDead)
        {
            HandlePlayerDead();
            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);

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

    private Transform FindClosestAlivePlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        float bestDist = float.MaxValue;
        Transform best = null;

        for (int i = 0; i < players.Length; i++)
        {
            PlayerController player = players[i];
            if (player == null)
            {
                continue;
            }

            if (player.IsDead)
            {
                continue;
            }

            Transform transform = player.FollowTarget;

            float dist = Vector2.Distance(base.transform.position, transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = transform;
            }
        }

        return best;
    }

    private void UpdatePatrol(float dist)
    {
        if (dist <= _detectRange)
        {
            ChangeState(State.Hidden);
            return;
        }

        _wanderTimer -= Time.fixedDeltaTime;

        if (_patrolMode == PatrolMode.Idle)
        {
            _controller.StopMove();

            if (_wanderTimer <= 0f)
            {
                StartPatrolMove();
            }

            return;
        }

        if (_wanderDir == Vector2.zero)
        {
            PickNewWanderDir();
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, _wanderDir, _wanderObstacleRayDist, _blockMask);
        if (hit.collider != null)
        {
            StartPatrolIdle();
            return;
        }

        _controller.ApplyExternalMove(_wanderDir, _wanderSpeedMultiplier);

        if (_wanderTimer <= 0f)
        {
            StartPatrolIdle();
        }
    }

    private void PickNewWanderDir()
    {
        _wanderDir = Random.insideUnitCircle.normalized;
        if (_wanderDir == Vector2.zero)
        {
            _wanderDir = Vector2.right;
        }
    }

    private void StartPatrolMove()
    {
        _patrolMode = PatrolMode.Move;
        _wanderTimer = Random.Range(_wanderMoveTimeMin, _wanderMoveTimeMax);
        PickNewWanderDir();
    }

    private void StartPatrolIdle()
    {
        _patrolMode = PatrolMode.Idle;
        _wanderTimer = Random.Range(_wanderIdleTimeMin, _wanderIdleTimeMax);
        _wanderDir = Vector2.zero;
    }

    private void UpdateHidden(float dist)
    {
        _controller.StopMove();

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
        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        _controller.ApplyExternalMove(dir, 1f);

        if (dist >= _hideRange)
        {
            ChangeState(State.Patrol);
        }
    }

    private void UpdateFlee(float dist)
    {
        _fleeTimer += Time.fixedDeltaTime;

        Vector2 awayDir = ((Vector2)transform.position - (Vector2)_player.position).normalized;

        float speedMul = _fleeSpeedMultiplier;
        _controller.ApplyExternalMove(awayDir, speedMul);

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
        }
        else if (next == State.Patrol)
        {
            StartPatrolMove();
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

    public void OnHit()
    {
        if (_enemyState.current == EnemyState.State.Dead)
        {
            return;
        }

        ChangeState(State.Flee);
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
        if (_sprite == null)
        {
            return;
        }

        Color c = _sprite.color;
        c.a = alpha;
        _sprite.color = c;
    }

    private void HandlePlayerDead()
    {
        _player = null;

        _controller.StopMove();

        ChangeState(State.Patrol);
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
