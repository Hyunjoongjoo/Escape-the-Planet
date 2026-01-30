using Photon.Pun;
using UnityEngine;

public class EnemyWatcherAI : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _detectRange = 4f;     // 근접해야 감시 시작
    [SerializeField] private float _keepDistance = 6f;    // 감시 상태에서 유지할 거리
    [SerializeField] private float _keepTolerance = 1f; // 거리 오차 허용
    [SerializeField] private float _cancelRange = 10f;    // 이 이상 멀어지면 감시 취소

    [SerializeField] private float _alarmTime = 3f;        // 감시 유지 시 호출까지 시간
    [SerializeField] private float _callRange = 30f;       // 추적자 호출 범위
    [SerializeField] private bool _resetTimerOnCancel = true;

    [SerializeField] private float _moveSpeedMultiplier = 0.8f;

    private Rigidbody2D _rigid;
    private EnemyView _view;
    private EnemyState _enemyState;
    private EnemyController _controller;

    private bool _watching = false;
    private bool _called = false;
    private float _watchTimer = 0f;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _view = GetComponent<EnemyView>();
        _enemyState = GetComponent<EnemyState>();
        _controller = GetComponent<EnemyController>();
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
            StopWatchMove();
            return;
        }

        if (_enemyState.current == EnemyState.State.Dead)
        {
            StopWatchMove();
            return;
        }

        _player = FindClosestAlivePlayer();

        if (_player == null)
        {
            ResetWatchState();
            return;
        }

        PlayerController player = _player.GetComponentInParent<PlayerController>();
        if (player != null && player.IsDead)
        {
            ResetWatchState();
            _player = null;
            return;
        }

        float dist = Vector2.Distance(_rigid.position, _player.position);

        if (!_watching && dist <= _detectRange)
        {
            _watching = true;
            _watchTimer = 0f;
            _called = false;
        }

        if (_watching && dist >= _cancelRange)
        {
            CancelWatching();
            return;
        }

        if (!_watching)
        {
            StopWatchMove();
            return;
        }

        Vector2 dirToPlayer = ((Vector2)_player.position - _rigid.position).normalized;
        _view.SetMove(dirToPlayer, 0f);

        float speed = _controller.MoveSpeed * _moveSpeedMultiplier;

        float minDist = _keepDistance - _keepTolerance;
        float maxDist = _keepDistance + _keepTolerance;

        if (dist > maxDist)
        {
            _controller.CurrentAnimSpeed01 = 1f;
            _rigid.linearVelocity = dirToPlayer * speed;
        }
        else if (dist < minDist)
        {
            _controller.CurrentAnimSpeed01 = 1f;
            _rigid.linearVelocity = -dirToPlayer * speed;
        }
        else
        {
            _controller.CurrentAnimSpeed01 = 0f;
            _rigid.linearVelocity = Vector2.zero;
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

            if (player.IsDead || player.NetIsInRoom)
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

    private void StopWatchMove()
    {
        _rigid.linearVelocity = Vector2.zero;
        _controller.CurrentAnimSpeed01 = 0f;
    }

    private void CancelWatching()
    {
        _watching = false;

        if (_resetTimerOnCancel)
        {
            _watchTimer = 0f;
            _called = false;
        }

        StopWatchMove();
    }

    private void ResetWatchState()
    {
        _watching = false;
        _called = false;
        _watchTimer = 0f;

        StopWatchMove();
    }

    private void HandlePlayerDead()
    {
        _player = null;
        ResetWatchState();
    }

    private void CallNearbyChasers()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _callRange);

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyChaserAI chaser = hits[i].GetComponent<EnemyChaserAI>();
            if (chaser != null)
            {
                if (_player != null)
                {
                    chaser.ForceChase(_player);
                }
            }
        }

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

