using UnityEngine;
using System;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum GameEndType
{
    Success = 0,
    Fail_TimeOver = 1,
    Fail_PlayerDead = 2
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

    [SerializeField] private float _defaultDayDuration = 300f;

    private float _remainTime;
    private double _dayStartNetworkTime;

    public float RemainTime => _remainTime;
    public bool IsRunning { get; private set; }

    public event Action<float> OnTimeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (IsRunning == false)
        {
            return;
        }

        double elapsed = PhotonNetwork.Time - _dayStartNetworkTime;
        _remainTime = Mathf.Max(0f, _defaultDayDuration - (float)elapsed);

        OnTimeChanged?.Invoke(_remainTime);

        if (_remainTime <= 0f && PhotonNetwork.IsMasterClient)
        {
            EndDay_Master(DayEndReason.TimeOver);
        }
    }

    public void StartDay_Master()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable props = new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Running },
            { MatchKeys.DayStartTime, PhotonNetwork.Time },
            { MatchKeys.DayDuration, _defaultDayDuration }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void EndDay_Master(DayEndReason reason)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Hashtable props = new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Ending },
            { MatchKeys.DayEndReason, (int)reason }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps == null)
        {
            return;
        }

        if (changedProps.TryGetValue(MatchKeys.DayState, out object stateValue))
        {
            DayState state = (DayState)(int)stateValue;

            RefreshTimingFromRoomProps();

            if (state == DayState.Running)
            {
                HandleDayStarted();
            }
            else if (state == DayState.Ending)
            {
                HandleDayEnded();
            }
            else
            {
                // Idle µî
            }
        }

        if (changedProps.TryGetValue(MatchKeys.DayStartTime, out object startValue))
        {
            _dayStartNetworkTime = (double)startValue;
        }

        if (changedProps.TryGetValue(MatchKeys.DayDuration, out object durationValue))
        {
            _defaultDayDuration = (float)durationValue;
        }
    }

    private void RefreshTimingFromRoomProps()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayStartTime, out object startValue))
        {
            if (startValue is double doubleValue)
            {
                _dayStartNetworkTime = doubleValue;
            }
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayDuration, out object durationValue))
        {
            if (durationValue is float floatValue)
            {
                _defaultDayDuration = floatValue;
            }
        }
    }

    private void HandleDayStarted()
    {
        IsRunning = true;

        _remainTime = _defaultDayDuration;

        InGameWorldController.Instance.ShowWorld();
        UIManager.Instance.SetInGamePhase();

        if (PhotonNetwork.IsMasterClient)
        {
            FindFirstObjectByType<ItemSpawnManager>()?.Spawn();
            FindFirstObjectByType<EnemySpawnManager>()?.StartDay();
        }
    }

    private void HandleDayEnded()
    {
        IsRunning = false;

        if (PhotonNetwork.IsMasterClient)
        {
            FindFirstObjectByType<ItemSpawnManager>()?.ResetForNextDay();
            FindFirstObjectByType<EnemySpawnManager>()?.ResetForNextDay();

            Hashtable props = new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Idle }
        };

            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        InGameWorldController.Instance.HideWorld();
        UIManager.Instance.SetRoomPhase();
    }

    private void LateUpdate()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (IsRunning == false)
        {
            return;
        }

        if (PhotonPlayerStateManager.AreAllPlayersDead())
        {
            EndDay_Master(DayEndReason.AllDead);
        }
    }
}