using System;
using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;


public enum GameEndType
{
    Success,
    Fail_TimeOver,
    Fail_PlayerDead
}
public enum PlayerGameState
{
    Alive = 0,
    Escaped = 1,
    Dead = 2
}

public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float _timeLimit = 300f;

    private float _remain;
    private bool _isLocalEnded = false;
    private bool _isMatchEnded = false;

    public float RemainTime => _remain;
    public float ElapsedMinutes => (_timeLimit - _remain) / 60f;
    public bool IsRunning { get; private set; }

    public event Action<float> OnTimeChanged;
    public event Action OnTimeOver;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _remain = _timeLimit;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PlayerController.OnPlayerDead += HandlePlayerDead;
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PlayerController.OnPlayerDead -= HandlePlayerDead;
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Start()
    {
        StartTimer();
    }

    private void Update()
    {
        if (_isMatchEnded == true)
        {
            return;
        }

        if (IsRunning == false)
        {
            return;
        }

        _remain -= Time.deltaTime;
        if (_remain < 0f)
        {
            _remain = 0f;
        }

        OnTimeChanged?.Invoke(_remain);

        if (_remain <= 0f)
        {
            IsRunning = false;
            OnTimeOver?.Invoke();
            if (PhotonNetwork.InRoom == true && PhotonNetwork.IsMasterClient == true)
            {
                BroadcastMatchEnd(GameEndType.Fail_TimeOver);
            }
        }
    }

    public void StartTimer()
    {       
        IsRunning = true;
        OnTimeChanged?.Invoke(_remain);
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    public void ResetTimer(float newLimit)
    {
        _timeLimit = newLimit;
        _remain = _timeLimit;
        OnTimeChanged?.Invoke(_remain);
    }

    public void EndGame(GameEndType endType)
    {
        if (_isLocalEnded == true)
        {
            return;
        }

        _isLocalEnded = true;
        IsRunning = false;

        ShowEndUI(endType);

        HandleResult(endType);

        UpdatePhotonState(endType);
        if (endType == GameEndType.Fail_TimeOver)
        {
            return;
        }
        EnterSpectatorMode();
    }

    private void EnterSpectatorMode()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.EnterSpectatorMode();
        }

        if (SpectatorCameraManager.Instance != null)
        {
            SpectatorCameraManager.Instance.StartSpectate();
        }
    }

    private void BroadcastMatchEnd(GameEndType endType)
    {
        if (_isMatchEnded == true)
        {
            return;
        }

        object[] content = new object[]
        {
            (int)endType
        };

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent(MatchEventCodes.MatchEnd, content, options, SendOptions.SendReliable);

        Debug.Log($"[GameManager] Broadcast MatchEnd: {endType}");
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != MatchEventCodes.MatchEnd)
        {
            return;
        }

        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length <= 0)
        {
            return;
        }

        GameEndType endType = (GameEndType)(int)data[0];

        ApplyMatchEnd(endType);
    }

    private void ApplyMatchEnd(GameEndType endType)
    {
        if (_isMatchEnded == true)
        {
            return;
        }

        _isMatchEnded = true;
        IsRunning = false;

        ShowEndUI(endType);

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetInputEnabled(false);
        }

        Debug.Log($"[GameManager] ApplyMatchEnd: {endType}");

        // Room 복귀는 InGameMatchController(전원 Finished) 또는 여기서 바로 처리해도 됨
        // 여기서는 "즉시 룸 복귀"가 더 자연스럽다면 마스터가 LoadLevel 하면 됨.
    }

   

    private void UpdatePhotonState(GameEndType endType)
    {
        if (PhotonNetwork.InRoom == false)
        {
            return;
        }

        if (endType == GameEndType.Success)
        {
            PhotonPlayerStateManager.SetState(PlayerGameState.Escaped);
        }
        else
        {
            PhotonPlayerStateManager.SetState(PlayerGameState.Dead);
        }
    }

    private void ShowEndUI(GameEndType endType)
    {
        UIManager ui = UIManager.Instance;
        if (ui != null)
        {
            ui.ShowGameEndPanel(endType);
        }
    }

    private void HandleResult(GameEndType endType)
    {
        if (endType != GameEndType.Success)
        {
            return;
        }

        if (QuickSlotManager.Instance == null)
        {
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        SaveData data = QuickSlotManager.Instance.ToSaveData();
        SaveManager.Save(key, data);

    }

    private void HandlePlayerDead()
    {
        EndGame(GameEndType.Fail_PlayerDead);
    }

    public EnemyModel CreateEnemyModel(EnemyData stats, EnemyModel baseModel)
    {
        float elapsedMinutes = (_timeLimit - _remain) / 60f;

        float hpMul = 1f + stats.hpGrowthPerMinute * elapsedMinutes;
        float dmgMul = 1f + stats.damageGrowthPerMinute * elapsedMinutes;
        float spdMul = 1f + stats.speedGrowthPerMinute * elapsedMinutes;

        EnemyModel m = new EnemyModel();
        m.alwaysChase = baseModel.alwaysChase;
        m.approachDistance = baseModel.approachDistance;

        m.maxHP = Mathf.Max(1, Mathf.RoundToInt(stats.baseMaxHP * hpMul));
        m.currentHP = m.maxHP;
        m.contactDamage = Mathf.Max(1, Mathf.RoundToInt(stats.baseContactDamage * dmgMul));
        m.moveSpeed = Mathf.Max(0.1f, stats.baseMoveSpeed * spdMul);

        return m;
    }
}
