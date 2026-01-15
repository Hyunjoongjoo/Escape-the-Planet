using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyView))]
[RequireComponent(typeof(EnemyState))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;

    private EnemyModel _model;
    private EnemyView _view;
    private EnemyState _state;
    private Rigidbody2D _rb;

    private Transform _player;

    private void Awake()
    {
        _model = new EnemyModel();
        _view = GetComponent<EnemyView>();
        _state = GetComponent<EnemyState>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _model.Init(stats);
        _player = GameObject.FindWithTag("Player").transform;
    }

    private void FixedUpdate()
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        MoveTowardPlayer();
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
            return;
        }

        Vector2 dir = (_player.position - transform.position).normalized;
        _rb.MovePosition(_rb.position + dir * _model.moveSpeed * Time.fixedDeltaTime);

        _view.SetMove(dir.x);
        _state.ChangeState(EnemyState.State.Move);
    }


    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        if (collision.collider.CompareTag("Player"))
        {
            Debug.Log("Enemy OnCollisionStay2D with: " + collision.collider.name);
            PlayerController player = collision.collider.GetComponent<PlayerController>();
            player?.TakeDamage(_model.contactDamage);
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
        _view.PlayDead();
        Destroy(gameObject, 1.5f);
    }
}
