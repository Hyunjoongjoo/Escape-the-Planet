using UnityEngine;

public class EnemyWatcherAI : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _detectRange = 3f;     // 근접해야 감시 시작
    [SerializeField] private float _keepDistance = 5f;    // 감시 상태에서 유지할 거리
    [SerializeField] private float _keepTolerance = 0.5f; // 거리 오차 허용
    [SerializeField] private float _cancelRange = 10f;    // 이 이상 멀어지면 감시 취소

    [SerializeField] private float _alarmTime = 3f;        // 감시 유지 시 호출까지 시간
    [SerializeField] private float _callRange = 30f;       // 추적자 호출 범위
    [SerializeField] private bool _resetTimerOnCancel = true;

    private Rigidbody2D _rb;
    private EnemyView _view;
    private EnemyState _enemyState;
    private EnemyController _controller;

    private bool _watching = false;
    private bool _called = false;
    private float _watchTimer = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _view = GetComponent<EnemyView>();
        _enemyState = GetComponent<EnemyState>();
        _controller = GetComponent<EnemyController>();
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
                Debug.LogError("[EnemyWatcherAI] 플레이어컨트롤러를 찾지 못함");
            }
        }
    }

    private void FixedUpdate()
    {
        if (_player == null)
        {
            return;
        }

        if (_enemyState.current == EnemyState.State.Dead)
        {
            _rb.linearVelocity = Vector2.zero;
            _view.SetMove(Vector2.zero, 0f);
            return;
        }

        float dist = Vector2.Distance(_rb.position, _player.position);

        if (!_watching && dist <= _detectRange)
        {
            _watching = true;
            _watchTimer = 0f;
        }

        if (_watching && dist >= _cancelRange)
        {
            _watching = false;
            _rb.linearVelocity = Vector2.zero;
            _view.SetMove(Vector2.zero, 0f);

            if (_resetTimerOnCancel)
            {
                _watchTimer = 0f;
                _called = false;
            }

            return;
        }

        if (!_watching)
        {
            _rb.linearVelocity = Vector2.zero;
            _view.SetMove(Vector2.zero, 0f);
            return;
        }

        Vector2 dirToPlayer = ((Vector2)_player.position - _rb.position).normalized;

        _view.SetMove(dirToPlayer, 0f);

        float speed = _controller.MoveSpeed;

        float minDist = _keepDistance - _keepTolerance;
        float maxDist = _keepDistance + _keepTolerance;

        if (dist > maxDist)
        {
            _rb.linearVelocity = dirToPlayer * speed;
            _view.SetMove(dirToPlayer, 0.5f);
        }
        else if (dist < minDist)
        {
            _rb.linearVelocity = -dirToPlayer * speed;
            _view.SetMove(-dirToPlayer, 0.5f);
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
            _view.SetMove(dirToPlayer, 0f);
        }

        if (!_called)
        {
            _watchTimer += Time.fixedDeltaTime;

            if (_watchTimer >= _alarmTime)
            {
                CallNearbyChasers();
                _called = true;
            }
        }
    }

    private void CallNearbyChasers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _callRange);

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyChaserAI chaser = hits[i].GetComponent<EnemyChaserAI>();
            if (chaser != null)
            {
                chaser.ForceChase(_player);
            }
        }

        Debug.Log("[EnemyWatcherAI] 추적자 호출");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _cancelRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _callRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _keepDistance);
    }
}

