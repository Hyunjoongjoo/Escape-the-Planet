using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float _timeLimit = 300f; // 5Ка
    private float _remain;

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

    private void Start()
    {
        StartTimer();
    }

    private void Update()
    {
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
