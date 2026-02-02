using Photon.Pun;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyView))]
[RequireComponent(typeof(EnemyState))]
[RequireComponent(typeof(PhotonView))]
public class EnemyController : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private float _damageInterval = 0.5f;
    [SerializeField] private int _dataId = -1;
    [SerializeField] private EnemyDatabase _enemyDatabase;

    [SerializeField] private float _baseHitRange = 0.45f;
    [SerializeField] private float _netTolerance = 0.15f;
    [SerializeField] private float _speedCompensation = 0.15f;

    private EnemyModel _model;
    private EnemyView _view;
    private EnemyState _state;
    private Rigidbody2D _rigid;

    private Vector2 _netTargetPos;
    [SerializeField] private float _netPosLerpSpeed = 30f;

    [SerializeField] private float _deadDestroyDelay = 1.2f;
    private WaitForSeconds _deadWait;

    private float _nextDamageTime = 0f;
    private bool _isInitialized;

    private Vector2 _netMoveDir;
    private float _netAnimSpeed01;
    private float _netAlpha = 1f;

    public float MoveSpeed => _model.moveSpeed;
    public Rigidbody2D Rigidbody => _rigid;
    public EnemyView View => _view;
    public EnemyState State => _state;
    public bool IsDead => _state.current == EnemyState.State.Dead;
    public float CurrentAnimSpeed01 { get; set; }
    public EnemyId DataId => (EnemyId)_dataId;

    private void Awake()
    {
        _model = new EnemyModel();
        _view = GetComponent<EnemyView>();
        _state = GetComponent<EnemyState>();
        _rigid = GetComponent<Rigidbody2D>();
        _deadWait = new WaitForSeconds(_deadDestroyDelay);
    }

    private void Start()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_data == null)
        {
            return;
        }

        _model.Init(_data);
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                    MatchKeys.DayState, out object stateValue))
            {
                _rigid.linearVelocity = Vector2.zero;
            }
            else if ((DayState)(int)stateValue != DayState.Running)
            {
                _rigid.linearVelocity = Vector2.zero;
            }

            return;
        }

        Vector2 smooth = Vector2.Lerp(
            _rigid.position,
            _netTargetPos,
            1f - Mathf.Exp(-_netPosLerpSpeed * Time.fixedDeltaTime)
        );

        _rigid.MovePosition(smooth);

        UpdateRemoteAnimation();
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

        if (!collision.collider.CompareTag("Player"))
        {
            return;
        }

        PlayerController player = collision.collider.GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (player.IsDead || player.NetIsInRoom)
        {
            return;
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);

        float dynamicRange = _baseHitRange + _netTolerance + (_model.moveSpeed * _speedCompensation);

        if (dist > dynamicRange)
        {
            return;
        }

        player.RequestDamageFromOther(_model.contactDamage, photonView.ViewID);

        EnemyLurkerAI lurker = GetComponent<EnemyLurkerAI>();
        if (lurker != null)
        {
            lurker.OnHit();
        }
        _nextDamageTime = Time.time + _damageInterval;
    }

    public void Init(EnemyData data, float elapsedMinutes)
    {
        _data = data;

        _dataId = (int)data.id;

        _model = new EnemyModel();
        _model.Init(_data);
        _model.ApplySpawnGrowth(elapsedMinutes);

        _isInitialized = true;

        photonView.RPC(nameof(RPC_InitData), RpcTarget.OthersBuffered, _dataId, elapsedMinutes);
    }

    [PunRPC]
    private void RPC_InitData(int dataId, float elapsedMinutes)
    {
        if (_enemyDatabase == null)
        {
            return;
        }

        EnemyData data = _enemyDatabase.Get((EnemyId)dataId);
        if (data == null)
        {
            return;
        }

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

        if (_model.IsDead())
        {
            Die_Master(); 
            return;   
        }

        photonView.RPC(nameof(RPC_PlayHit), RpcTarget.All);

        EnemyLurkerAI lurker = GetComponent<EnemyLurkerAI>();
        if (lurker != null)
        {
            lurker.OnHit();
        }
    }

    public void RequestTakeDamage(int damage, int attackerViewId)
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        photonView.RPC(
            nameof(RPC_RequestDamageToMaster),
            RpcTarget.MasterClient,
            photonView.ViewID,
            damage,
            attackerViewId
        );
    }

    [PunRPC]
    private void RPC_RequestDamageToMaster(int enemyViewId, int damage, int attackerViewId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonView enemyView = PhotonView.Find(enemyViewId);
        if (enemyView == null)
        {
            return;
        }

        EnemyController enemy = enemyView.GetComponent<EnemyController>();
        if (enemy == null)
        {
            return;
        }

        enemy.TakeDamage(damage);
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

        float speed01 = Mathf.Clamp01(_rigid.linearVelocity.magnitude / _model.moveSpeed);

        CurrentAnimSpeed01 = speed01;

        _view.SetMove(dir, speed01);
        _state.ChangeState(EnemyState.State.Move);
    }

    private void UpdateRemoteAnimation()
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            return;
        }

        _view.SetMove(_netMoveDir, _netAnimSpeed01);
    }

    public void StopMove()
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        _rigid.linearVelocity = Vector2.zero;

        CurrentAnimSpeed01 = 0f;
    }

    private void Die_Master()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        _state.ChangeState(EnemyState.State.Dead);

        _rigid.linearVelocity = Vector2.zero;

        _rigid.simulated = false;

        photonView.RPC(nameof(RPC_PlayDead), RpcTarget.All);

        StartCoroutine(DestroyAfterDeadAnim());
    }

    private IEnumerator DestroyAfterDeadAnim()
    {
        yield return _deadWait;

        PoolManager.Instance.ReturnEnemy(this);
    }

    [PunRPC]
    private void RPC_PlayHit()
    {
        if (_state.current == EnemyState.State.Dead)
        {
            return;
        }

        if (_model.IsDead())
        {
            Die_Master();
            return;
        }

        _view.PlayHit();
    }

    [PunRPC]
    private void RPC_PlayDead()
    {
        _state.ChangeState(EnemyState.State.Dead);

        if (_rigid != null)
        {
            _rigid.linearVelocity = Vector2.zero;
            _rigid.simulated = false;
        }

        ChaserPathChase pathAI = GetComponent<ChaserPathChase>();
        if (pathAI != null)
        {
            pathAI.enabled = false;
        }

        _view.PlayDead();
    }

    public void RequestHitFromPlayer(int damage, Vector2 attackerPos)
    {
        photonView.RPC(
            nameof(RPC_RequestHitFromPlayer),
            RpcTarget.MasterClient,
            photonView.ViewID,
            damage,
            attackerPos
        );
    }

    [PunRPC]
    private void RPC_RequestHitFromPlayer(int enemyViewId, int damage, Vector2 attackerPos)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonView enemyView = PhotonView.Find(enemyViewId);
        if (enemyView == null)
        {
            return;
        }

        EnemyController enemy = enemyView.GetComponent<EnemyController>();
        if (enemy == null || enemy.IsDead)
        {
            return;
        }

        float dist = Vector2.Distance(attackerPos, enemy.transform.position);

        float hitRange = 0.45f + 0.15f + (_model.moveSpeed * 0.15f);

        if (dist <= hitRange)
        {
            enemy.TakeDamage(damage);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_rigid.position);

            Vector2 dir = _rigid.linearVelocity.sqrMagnitude > 0.0001f
                ? _rigid.linearVelocity.normalized
                : Vector2.zero;

            stream.SendNext(dir);
            stream.SendNext(CurrentAnimSpeed01);
            stream.SendNext(_view.CurrentAlpha);
        }
        else
        {
            _netTargetPos = (Vector2)stream.ReceiveNext();
            _netMoveDir = (Vector2)stream.ReceiveNext();
            _netAnimSpeed01 = (float)stream.ReceiveNext();
            _netAlpha = (float)stream.ReceiveNext();

            _view.SetAlpha(_netAlpha);
        }
    }
    public void OnMasterChanged()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            EnableMasterAI();
        }
        else
        {
            DisableMasterAI();
        }
    }

    private void EnableMasterAI()
    {
        EnemyChaserAI chaser = GetComponent<EnemyChaserAI>();
        if (chaser != null)
        {
            chaser.enabled = true;
        }

        EnemyWatcherAI watcher = GetComponent<EnemyWatcherAI>();
        if (watcher != null)
        {
            watcher.enabled = true;
        }

        EnemyLurkerAI lurker = GetComponent<EnemyLurkerAI>();
        if (lurker != null)
        {
            lurker.enabled = true;
        }
    }

    private void DisableMasterAI()
    {
        EnemyChaserAI chaser = GetComponent<EnemyChaserAI>();
        if (chaser != null)
        {
            chaser.enabled = false;
        }

        EnemyWatcherAI watcher = GetComponent<EnemyWatcherAI>();
        if (watcher != null)
        {
            watcher.enabled = false;
        }

        EnemyLurkerAI lurker = GetComponent<EnemyLurkerAI>();
        if (lurker != null)
        {
            lurker.enabled = false;
        }
    }
    public void ResetForPool()
    {
        _rigid.simulated = true;
        _rigid.linearVelocity = Vector2.zero;

        _state.ChangeState(EnemyState.State.Idle);

        CurrentAnimSpeed01 = 0f;
        _view.SetAlpha(1f);

        ChaserPathChase pathAI = GetComponent<ChaserPathChase>();
        if (pathAI != null)
        {
            pathAI.enabled = true;
        }
    }
}
