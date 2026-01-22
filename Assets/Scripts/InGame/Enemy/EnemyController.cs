using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyView))]
[RequireComponent(typeof(EnemyState))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private float _damageInterval = 0.5f;

    private EnemyModel _model;
    private EnemyView _view;
    private EnemyState _state;
    private Rigidbody2D _rigid;

    private float _nextDamageTime = 0f;
    private bool _isInitialized;

    public float MoveSpeed => _model.moveSpeed;
    public Rigidbody2D Rigidbody => _rigid;
    public EnemyView View => _view;
    public EnemyState State => _state;

    private void Awake()
    {
        _model = new EnemyModel();
        _view = GetComponent<EnemyView>();
        _state = GetComponent<EnemyState>();
        _rigid = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {

        if (!PhotonNetwork.IsMasterClient)
        {
            enabled = false;
            return;
        }

        if (_isInitialized == true)
        {
            return;
        }

        _model.Init(_data);
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

    public void Init(EnemyData data, float elapsedMinutes)
    {
        _data = data;

        _model = new EnemyModel();
        _model.Init(_data);

        _model.ApplySpawnGrowth(elapsedMinutes);
        _isInitialized = true;
    }

    public void TakeDamage(int dmg)
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        _model.TakeDamage(dmg);
        _view.PlayHit();

        EnemyLurkerAI lurker = GetComponent<EnemyLurkerAI>();
        if (lurker != null)
        {
            lurker.OnHit();
        }

        if (_model.IsDead())
        {
            Die();
        }
    }

    public void ApplyExternalMove(Vector2 dir01, float speedMultiplier = 1f)
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        Vector2 dir = dir01.normalized;

        float speed = _model.moveSpeed * Mathf.Max(0f, speedMultiplier);
        _rigid.linearVelocity = dir * speed;

        float speed01 = 0f;
        if (_model.moveSpeed > 0f)
        {
            speed01 = _rigid.linearVelocity.magnitude / _model.moveSpeed;
        }

        _view.SetMove(dir, speed01);
        _state.ChangeState(EnemyState.State.Move);
    }

    public void StopMove()
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        _rigid.linearVelocity = Vector2.zero;
    }

    private void Die()
    {
        _state.ChangeState(EnemyState.State.Dead);

        ChaserPathChase pathAI = GetComponent<ChaserPathChase>();
        if (pathAI != null)
        {
            pathAI.enabled = false;
        }

        _view.SetMove(Vector2.zero, 0f);
        _view.PlayDead();

        _rigid.linearVelocity = Vector2.zero;
        _rigid.simulated = false;

        Destroy(gameObject, 1.5f);
    }
}
