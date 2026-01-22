using System;
using Photon.Pun;
using UnityEngine;
using System.Collections;


public enum GameEndType
{
    Success,
    Fail_TimeOver,
    Fail_PlayerDead
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float _timeLimit = 300f;
    [SerializeField] private float _returnDelay = 2f;
    [SerializeField] private string _roomSceneName = "Room";

    private float _remain;
    private bool _isGameEnded = false;

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
        StartTimer();
    }

    private void Update()
    {
        if (_isGameEnded == true)
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
            EndGame(GameEndType.Fail_TimeOver);
        }
    }

    public void StartTimer()
    {
        if (_isGameEnded == true)
        {
            return;
        }
        
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
        if (_isGameEnded == true)
        {
            return;
        }

        _isGameEnded = true;
        IsRunning = false;

        StopGameplaySystems();
        ShowEndUI(endType);
        HandleResult(endType);

        StartCoroutine(ReturnToRoom_Coroutine());
    }

    private void StopGameplaySystems()
    {
        EnemySpawnManager spawn = FindFirstObjectByType<EnemySpawnManager>();
        if (spawn != null)
        {
            spawn.enabled = false;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetInputEnabled(false);
        }

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].StopMove();
                enemies[i].enabled = false;
            }
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

        Debug.Log("[GameManager] Saved on success.");
    }

    private IEnumerator ReturnToRoom_Coroutine()
    {
        yield return new WaitForSeconds(_returnDelay);

        PhotonNetwork.LoadLevel(_roomSceneName);
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
