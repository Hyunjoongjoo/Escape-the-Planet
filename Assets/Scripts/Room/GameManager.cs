using UnityEngine;
using System;
using System.Collections;
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

    private void Start()
    {
        StartCoroutine(HidePlayersAfterSpawn());
    }

    private IEnumerator HidePlayersAfterSpawn()
    {
        yield return null;

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                players[i].gameObject.SetActive(false);
            }
        }
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

    private void EnterInGame_Local()
    {
        PlayerController[] players =
            FindObjectsByType<PlayerController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        PlayerController localPlayer = null;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null &&
                players[i].photonView != null &&
                players[i].photonView.IsMine)
            {
                localPlayer = players[i];
                break;
            }
        }

        if (localPlayer != null)
        {
            localPlayer.gameObject.SetActive(true);
            localPlayer.SetInputEnabled(true);
        }

        UIManager.Instance.SetInGamePhase();
    }

    public void EnterFactory_Local()
    {
        EnterInGame_Local();
    }

    public void StartDay_Master()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Running },
            { MatchKeys.DayStartTime, PhotonNetwork.Time },
            { MatchKeys.DayDuration, _defaultDayDuration }
        });
    }

    public void EndDay_Master(DayEndReason reason)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Ending },
            { MatchKeys.DayEndReason, (int)reason }
        });
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

            RefreshTiming();

            if (state == DayState.Running)
            {
                HandleDayStarted();
            }
            else if (state == DayState.Ending)
            {
                HandleDayEnded();
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

    private void RefreshTiming()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayStartTime, out object startValue))
        {
            _dayStartNetworkTime = (double)startValue;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayDuration, out object durationValue))
        {
            _defaultDayDuration = (float)durationValue;
        }
    }

    private void HandleDayStarted()
    {
        IsRunning = true;
        _remainTime = _defaultDayDuration;

        EnterInGame_Local();

        PhotonPlayerStateManager.ResetDayFlags();

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

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { MatchKeys.DayState, (int)DayState.Idle }
            });
        }

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