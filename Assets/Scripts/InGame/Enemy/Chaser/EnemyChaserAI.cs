using UnityEngine;

public class EnemyChaserAI : MonoBehaviour
{
    [Header("Detect")]
    [SerializeField] private float _detectRange = 12f;
    [SerializeField] private float _lostRange = 18f;

    [Header("Patrol (Wander)")]
    [SerializeField] private float _wanderMoveTimeMin = 0.5f;
    [SerializeField] private float _wanderMoveTimeMax = 1.2f;
    [SerializeField] private float _wanderIdleTimeMin = 0.6f;
    [SerializeField] private float _wanderIdleTimeMax = 1.4f;
    [SerializeField, Range(0.05f, 1f)] private float _wanderSpeedMultiplier = 0.25f;


    private EnemyController _enemy;
    private ChaserPathChase _pathChase;
    private Transform _player;

    private enum State
    {
        Patrol,
        Chase
    }

    private State _state;

    private bool _forcedChase = false;

    private float _stateTimer = 0f;
    private bool _isWanderMoving = false;
    private Vector2 _wanderDir = Vector2.right;

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
        _pathChase = GetComponent<ChaserPathChase>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }

        ChangeState(State.Patrol);
    }

    private void FixedUpdate()
    {
        if (_player == null)
        {
            if (_pathChase != null)
            {
                _pathChase.ClearTarget();
            }

            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);

        if (_forcedChase)
        {
            if (_state != State.Chase)
            {
                ChangeState(State.Chase);
            }

            return;
        }

        if (_state == State.Patrol && dist <= _detectRange)
        {
            ChangeState(State.Chase);
        }
        else if (_state == State.Chase && dist >= _lostRange)
        {
            ChangeState(State.Patrol);
        }

        if (_state == State.Patrol)
        {
            UpdateWander();
        }
    }

    private void UpdateWander()
    {
        _stateTimer -= Time.fixedDeltaTime;

        if (_stateTimer <= 0f)
        {
            _isWanderMoving = !_isWanderMoving;

            if (_isWanderMoving)
            {
                _stateTimer = Random.Range(_wanderMoveTimeMin, _wanderMoveTimeMax);
                _wanderDir = Random.insideUnitCircle.normalized;
                if (_wanderDir.sqrMagnitude < 0.001f)
                {
                    _wanderDir = Vector2.right;
                }
            }
            else
            {
                _stateTimer = Random.Range(_wanderIdleTimeMin, _wanderIdleTimeMax);
            }
        }

        if (_isWanderMoving)
        {
            _enemy.ApplyExternalMove(_wanderDir, _wanderSpeedMultiplier);
        }
        else
        {
            _enemy.View.SetMove(_wanderDir, 0f);
            _enemy.State.ChangeState(EnemyState.State.Idle);
        }
    }

    private void ChangeState(State next)
    {
        _state = next;

        if (next == State.Patrol)
        {
            if (_pathChase != null)
            {
                _pathChase.ClearTarget();
            }

            _stateTimer = 0f;
            _isWanderMoving = false;
        }
        else if (next == State.Chase)
        {
            if (_pathChase != null)
            {
                _pathChase.SetTarget(_player);
            }
        }
    }

    public void ForceChase(Transform target)
    {
        if (target == null)
        {
            return;
        }

        _forcedChase = true;

        if (_pathChase != null)
        {
            _pathChase.ForceChase(target);
        }

        ChangeState(State.Chase);
    }

    //public void CancelForceChase()
    //{
    //    _forcedChase = false;
    //    _forcedTarget = null;
    //}

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // 감지 범위 (추적 시작)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, _detectRange);

        // 추적 해제 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, _lostRange);
    }
}
