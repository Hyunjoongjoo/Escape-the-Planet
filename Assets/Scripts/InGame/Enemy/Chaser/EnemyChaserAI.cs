using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class EnemyChaserAI : MonoBehaviour
{
    [SerializeField] private float _detectRange = 12f;
    [SerializeField] private float _lostRange = 18f;

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
        if (_enemy.IsDead)
        {
            return;
        }

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
            _enemy.StopMove();
            _enemy.CurrentAnimSpeed01 = 0f;
            return;
        }

        if (_forcedChase == false)
        {
            _player = FindClosestAlivePlayer();

            if (_state == State.Chase && _pathChase != null)
            {
                _pathChase.SetTarget(_player);
            }
        }

        if (_player == null)
        {
            if (_pathChase != null)
            {
                _pathChase.ClearTarget();
            }

            _enemy.StopMove();
            _enemy.CurrentAnimSpeed01 = 0f;
            return;
        }

        PlayerController player = _player.GetComponent<PlayerController>();
        if (player != null && player.IsDead)
        {
            if (_forcedChase)
            {
                CancelForceChase();
            }

            if (_state != State.Patrol)
            {
                ChangeState(State.Patrol);
            }

            UpdateWander();
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

    private Transform FindClosestAlivePlayer()
    {
        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        float bestDist = float.MaxValue;
        Transform best = null;

        for (int i = 0; i < players.Count; i++)
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

    private void UpdateWander()
    {
        if (_enemy.IsDead)
        {
            return;
        }

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
            _enemy.CurrentAnimSpeed01 = 0.5f;
            _enemy.ApplyExternalMove(_wanderDir, _wanderSpeedMultiplier);
        }
        else
        {
            _enemy.CurrentAnimSpeed01 = 0f;
            _enemy.StopMove();
        }
    }

    private void ChangeState(State next)
    {
        if (_enemy.IsDead)
        {
            return;
        }

        if (_state == State.Patrol && next == State.Chase)
        {
            if (_enemy != null)
            {
                EnemySoundController sound = _enemy.GetComponent<EnemySoundController>();
                if (sound != null)
                {
                    sound.PlayAlert();
                }
            }
        }

        _state = next;

        if (next == State.Patrol)
        {
            if (_pathChase != null)
            {
                _pathChase.ClearTarget();
            }

            _enemy.CurrentAnimSpeed01 = 0f;
            _enemy.StopMove();

            _stateTimer = 0f;
            _isWanderMoving = false;
        }
        else if (next == State.Chase)
        {
            _enemy.CurrentAnimSpeed01 = 1f;

            if (_pathChase != null)
            {
                _pathChase.SetTarget(_player);
            }
        }
    }

    public void ForceChase(Transform target)
    {
        if (_enemy.IsDead)
        {
            return;
        }

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

    private void CancelForceChase()
    {
        _forcedChase = false;

        if (_pathChase != null)
        {
            _pathChase.ClearTarget();
        }
    }
    private void HandlePlayerDead()
    {
        CancelForceChase();

        if (_state != State.Patrol)
        {
            ChangeState(State.Patrol);
        }

        _enemy.CurrentAnimSpeed01 = 0f;
        _enemy.StopMove();

        _stateTimer = 0f;
        _isWanderMoving = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, _detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, _lostRange);
    }
}
