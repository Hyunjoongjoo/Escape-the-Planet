using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyView))]
[RequireComponent(typeof(EnemyState))]
[RequireComponent(typeof(PhotonView))]
public class EnemyController : MonoBehaviourPun, IPunObservable
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
        if (_isInitialized)
        {
            return;
        }

        _model.Init(_data);
    }

    private void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayState, out object stateValue))
        {
            return;
        }

        if ((DayState)(int)stateValue != DayState.Running)
        {
            _rigid.linearVelocity = Vector2.zero;
            return;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

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

            if (player != null)
            {
                player.TakeDamageByEnemy(_model.contactDamage);
            }

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

    public void TakeDamage(int damage)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        _model.TakeDamage(damage);

        photonView.RPC(nameof(RPC_PlayHit), RpcTarget.All);

        EnemyLurkerAI lurker = GetComponent<EnemyLurkerAI>();
        if (lurker != null)
        {
            lurker.OnHit();
        }

        if (_model.IsDead())
        {
            Die_Master();
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

    private void Die_Master()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        _state.ChangeState(EnemyState.State.Dead);

        photonView.RPC(nameof(RPC_PlayDead), RpcTarget.All);

        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void RPC_PlayHit()
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        _view.PlayHit();
    }

    [PunRPC]
    private void RPC_PlayDead()
    {
        ChaserPathChase pathAI = GetComponent<ChaserPathChase>();
        if (pathAI != null)
        {
            pathAI.enabled = false;
        }

        _view.SetMove(Vector2.zero, 0f);
        _view.PlayDead();

        _rigid.linearVelocity = Vector2.zero;
        _rigid.simulated = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(_rigid.linearVelocity);
        }
        else
        {
            transform.position = (Vector2)stream.ReceiveNext();
            _rigid.linearVelocity = (Vector2)stream.ReceiveNext();
        }
    }
}
