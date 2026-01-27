using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;


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

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float _timeLimit = 300f;

    private float _remain;

    private bool _isDayEnded = false;
    private bool _dayStarted = false;

    private bool _hasStartTime = false;
    private double _startTime = 0;

    public float RemainTime => _remain;
    public bool IsRunning { get; private set; }

    public event Action<float> OnTimeChanged;
    public static event Action<GameManager> OnGameManagerReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        OnGameManagerReady?.Invoke(this);
        _remain = _timeLimit;
    }

    private void Update()
    {
        if (_isDayEnded || !IsRunning || !PhotonNetwork.InRoom)
        {
            return;
        }

        if (_hasStartTime == false)
        {
            TryInitStartTime();
            return;
        }

        double elapsed = PhotonNetwork.Time - _startTime;
        _remain = _timeLimit - (float)elapsed;

        if (_remain < 0f)
        {
            _remain = 0f;
        }

        OnTimeChanged?.Invoke(_remain);

        if (_remain <= 0f)
        {
            IsRunning = false;

            if (PhotonNetwork.IsMasterClient)
            {
                Master_EndDay(DayEndReason.TimeOver);
            }
        }
    }

    private void TryInitStartTime()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
            MatchKeys.DayStartTime, out object startValue))
        {
            return;
        }

        _startTime = (double)startValue;
        _hasStartTime = true;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
            MatchKeys.DayDuration, out object durationValue))
        {
            _timeLimit = (float)durationValue;
        }

        _remain = _timeLimit;
        IsRunning = true;
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        
        if (changedProps == null)
        {
            return;
        }

        if (changedProps.TryGetValue(MatchKeys.DayStartTime, out _))
        {
            _hasStartTime = false;
            TryInitStartTime();
        }

        if (changedProps.TryGetValue(MatchKeys.DayState, out object stateValue))
        {
            DayState state = (DayState)(int)stateValue;
            Debug.Log($"DayState Changed => {(DayState)(int)stateValue}");
            if (state == DayState.Running)
            {
                if (InGameWorldController.Instance != null)
                {
                    InGameWorldController.Instance.ShowWorld();
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetInGamePhase();
                }
            }
            else
            {
                if (InGameWorldController.Instance != null)
                {
                    InGameWorldController.Instance.HideWorld();
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetRoomPhase();
                }
            }
        }
    }



    public void OnDayStart()
    {
        if (_dayStarted)
        {
            return;
        }

        _dayStarted = true;
        _isDayEnded = false;

        IsRunning = true;
        _hasStartTime = false;

        InGameWorldController.Instance.ShowWorld();
        UIManager.Instance.SetInGamePhase();

        //플레이어 HP 복구/패널티 적용 (나중에 구현)
        //ApplyPlayerHPBuffOrPenalty();

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DelayedSpawn());
        }
    }

    public void OnDayEnd()
    {
        if (IsRunning == false)
        {
            return;
        }

        _dayStarted = false;
        _isDayEnded = true;

        IsRunning = false;
        _hasStartTime = false;

        FindFirstObjectByType<ItemSpawnManager>()?.ResetForNextDay();
        FindFirstObjectByType<EnemySpawnManager>()?.ResetForNextDay();

        InGameWorldController.Instance.HideWorld();

        UIManager.Instance.SetRoomPhase();
    }

    private IEnumerator DelayedSpawn()
    {
        yield return null;

        FindFirstObjectByType<ItemSpawnManager>()?.ResetForNextDay();
        FindFirstObjectByType<EnemySpawnManager>()?.ResetForNextDay();

        FindFirstObjectByType<ItemSpawnManager>()?.Spawn();
        FindFirstObjectByType<EnemySpawnManager>()?.StartDay();
    }

    public void Master_EndDay(DayEndReason reason)
    {
        if (!PhotonNetwork.IsMasterClient || _isDayEnded)
        {
            return;
        }

        _isDayEnded = true;
        _dayStarted = false;
        IsRunning = false;

        Hashtable props = new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Ending },
            { MatchKeys.DayEndReason, (int)reason }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        FindFirstObjectByType<ItemSpawnManager>()?.ResetForNextDay();
        FindFirstObjectByType<EnemySpawnManager>()?.ResetForNextDay();

        InGameWorldController.Instance.HideWorld();

        UIManager.Instance.SetRoomPhase();
    }
}