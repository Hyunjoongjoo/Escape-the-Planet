using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyView))]
[RequireComponent(typeof(EnemyState))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyStats _stats;
    [SerializeField] private float _damageInterval = 0.5f;
    [SerializeField] private bool _useBuiltInMovement = true;

    private EnemyModel _model;
    private EnemyView _view;
    private EnemyState _state;
    private Rigidbody2D _rb;
    private Transform _player;
    private float _nextDamageTime = 0f;

    public float MoveSpeed
    {
        get { return _model.moveSpeed; }
    }

    private void Awake()
    {
        _model = new EnemyModel();
        _view = GetComponent<EnemyView>();
        _state = GetComponent<EnemyState>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _model.Init(_stats);
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        else
        {
            Debug.LogError("플레이어 태그 오브젝트를 찾지 못함");
        }
    }

    private void FixedUpdate()
    {

        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        if (_useBuiltInMovement)
        {
            UpdateMovement();
        }
    }

    private void Update()
    {
        if (_state.current != EnemyState.State.Dead)
        {
            _model.ScaleByTime(Time.deltaTime);
        }
    }

    private void MoveTowardPlayer()
    {
        if (_player == null)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 dir = (_player.position - transform.position).normalized;

        _rb.linearVelocity = dir * _model.moveSpeed;
        float speed01 = _rb.linearVelocity.magnitude / _model.moveSpeed;
        _view.SetMove(dir, speed01);
        _state.ChangeState(EnemyState.State.Move);
    }

    private void UpdateMovement()
    {
        if (_player == null)
        {
            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);


        if (_model.alwaysChase == true)
        {
            MoveTowardPlayer();
            return;
        }


        if (dist <= _model.approachDistance)
        {
            MoveTowardPlayer();
        }
        else
        {

            _view.SetMove(Vector2.zero, 0f);
            _state.ChangeState(EnemyState.State.Idle);
        }
    }


    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        if (Time.time < _nextDamageTime)
        {
            return;
        }

        if (collision.collider.CompareTag("Player"))
        {
            PlayerController player = collision.collider.GetComponent<PlayerController>();
            player?.TakeDamage(_model.contactDamage);

            _nextDamageTime = Time.time + _damageInterval;
        }
    }

    public void TakeDamage(int dmg)
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        _model.TakeDamage(dmg);
        _view.PlayHit();

        if (_model.IsDead())
        {
            Die();
        }
    }
    
    private void Die()
    {
        _state.ChangeState(EnemyState.State.Dead);

        EnemyChaserAI ai = GetComponent<EnemyChaserAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        _view.SetMove(Vector2.zero, 0f);
        _view.PlayDead();

        _rb.linearVelocity = Vector2.zero;
        _rb.simulated = false;

        Destroy(gameObject, 1.5f);
    }
}
