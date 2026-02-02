using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
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

    [SerializeField] private float _defaultDayDuration = 600f;

    [SerializeField] private float _endingDuration = 5f;

    [SerializeField] private AudioListener _roomCameraListener;
    [SerializeField] private AudioListener _playerListener;

    private float _remainTime;
    private double _dayStartNetworkTime;

    private DayState _cachedState = DayState.Idle;
    private DayEndReason _cachedEndReason = DayEndReason.TimeOver;
    private double _cachedEndingUntil = 0.0;

    private PlayerController _cachedLocalPlayer;

    public float RemainTime => _remainTime;
    public float DefaultDayDuration => _defaultDayDuration;
    public bool IsRunning { get; private set; }

    public event Action<float> OnTimeChanged;
    public event Action<GameEndType> OnGameEndTriggered;

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

        if (_cachedState == DayState.Running)
        {
            double elapsed = PhotonNetwork.Time - _dayStartNetworkTime;
            _remainTime = Mathf.Max(0f, _defaultDayDuration - (float)elapsed);

            OnTimeChanged?.Invoke(_remainTime);

            if (_remainTime <= 0f && PhotonNetwork.IsMasterClient)
            {
                EndDay_Master(DayEndReason.TimeOver);
            }

            if (PhotonNetwork.IsMasterClient && PhotonPlayerStateManager.AreAllPlayersDead())
            {
                EndDay_Master(DayEndReason.AllDead);
            }
        }

        if (_cachedState == DayState.Ending && PhotonNetwork.IsMasterClient && _cachedEndingUntil > 0.0 && PhotonNetwork.Time >= _cachedEndingUntil)
        {
            SetDayState_Master(DayState.Idle, _cachedEndReason);
        }
    }

    public void StartDay_Master()
    {
        if (!PhotonNetwork.IsMasterClient || _cachedState != DayState.Idle)
        {
            return;
        }

        Hashtable props = new Hashtable
        {
            { MatchKeys.DayStartTime, PhotonNetwork.Time },
            { MatchKeys.DayDuration, _defaultDayDuration },
            { MatchKeys.EndingUntil, 0.0 },
            { MatchKeys.DayState, (int)DayState.Running }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void EndDay_Master(DayEndReason reason)
    {
        if (!PhotonNetwork.IsMasterClient || _cachedState != DayState.Running)
        {
            return;
        }

        Hashtable props = new Hashtable
        {
            { MatchKeys.DayEndReason, (int)reason },
            { MatchKeys.EndingUntil, PhotonNetwork.Time + _endingDuration },
            { MatchKeys.DayState, (int)DayState.Ending }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void AddRepair_Master(int amount)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        int currentRepair = 0;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
            MatchKeys.Repair, out object value))
        {
            currentRepair = (int)value;
        }

        currentRepair = Mathf.Clamp(currentRepair + amount, 0, 100);

        Hashtable props = new Hashtable
    {
        { MatchKeys.Repair, currentRepair }
    };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps == null)
        {
            return;
        }

        if (changedProps.TryGetValue(MatchKeys.DayStartTime, out object start))
        {
            _dayStartNetworkTime = (double)start;
        }

        if (changedProps.TryGetValue(MatchKeys.DayDuration, out object duration))
        {
            _defaultDayDuration = Convert.ToSingle(duration);
        }

        if (changedProps.TryGetValue(MatchKeys.DayEndReason, out object reason))
        {
            _cachedEndReason = (DayEndReason)(int)reason;
        }

        if (changedProps.TryGetValue(MatchKeys.EndingUntil, out object ending))
        {
            _cachedEndingUntil = (double)ending;
        }

        if (changedProps.TryGetValue(MatchKeys.DayState, out object state))
        {
            DayState newState = (DayState)(int)state;

            if (newState != _cachedState)
            {
                _cachedState = newState;
                ApplyState_Local(newState);
            }
        }

        if (changedProps.TryGetValue(MatchKeys.Repair, out object repairValue))
        {
            int repair = (int)repairValue;

            RepairPanelUI panel = FindFirstObjectByType<RepairPanelUI>();
            if (panel != null)
            {
                panel.RefreshFromRoom(repair);
            }
        }
    }

    private void ApplyState_Local(DayState state)
    {
        if (state == DayState.Idle)
        {
            IsRunning = false;
            SetAllPlayersRoomMode();
            UIManager.Instance.SetRoomPhase();
            UIManager.Instance.ExitSpectatorMode();
            SpectatorCameraManager.Instance?.StopSpectate();

            return;
        }

        if (state == DayState.Running)
        {
            IsRunning = true;
            UIManager.Instance.ExitSpectatorMode();
            SetAllPlayersInGameMode();
            _remainTime = _defaultDayDuration;
            UIManager.Instance.SetInGamePhase();
            UIManager.Instance.ShowTimer();

            ResetAllPlayersForNewDay_AllClients();
            ActivateAndResetLocalPlayer();
            PhotonPlayerStateManager.ResetDayFlags();

            if (PhotonNetwork.IsMasterClient)
            {
                FindFirstObjectByType<ItemSpawnManager>()?.Spawn();
                FindFirstObjectByType<EnemySpawnManager>()?.StartDay();
            }

            return;
        }

        if (state == DayState.Ending)
        {
            IsRunning = false;
            OnGameEndTriggered?.Invoke(ConvertEndType(_cachedEndReason));

            SetAllPlayersRoomMode();

            UIManager.Instance.HideTimer();

            HandleInventoryByDayEndReason(_cachedEndReason);
            ApplyRepairPenalty(_cachedEndReason);

            if (PhotonNetwork.IsMasterClient)
            {
                ApplyNextDayHpRules_Master(_cachedEndReason);
                FindFirstObjectByType<ItemSpawnManager>()?.ResetForNextDay();
                FindFirstObjectByType<EnemySpawnManager>()?.ResetForNextDay();
            }
        }
    }

    private void ResetAllPlayersForNewDay_AllClients()
    {
        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            PlayerController player = players[i];
            if (player == null)
            {
                continue;
            }

            player.ResetForNewDay(1f);
        }
    }

    private void ApplyRepairPenalty(DayEndReason reason)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        int penalty = 0;

        if (reason == DayEndReason.ManualEnd)
        {
            penalty = 2;
        }
        else if (reason == DayEndReason.TimeOver)
        {
            penalty = 5;
        }
        else if (reason == DayEndReason.AllDead)
        {
            penalty = 10;
        }

        if (penalty <= 0)
        {
            return;
        }

        int current = GetRepair();
        int next = Mathf.Clamp(current - penalty, 0, 100);

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { MatchKeys.Repair, next } });

        Debug.Log($"[Repair] Penalty -{penalty} ¡æ {next}");
    }

    public int GetRepair()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
            MatchKeys.Repair, out object value))
        {
            return (int)value;
        }

        return 0;
    }

    private void HandleInventoryByDayEndReason(DayEndReason reason)
    {
        if (reason == DayEndReason.TimeOver || reason == DayEndReason.AllDead)
        {
            QuickSlotManager.Instance.ClearAllSlots();
        }

    }

    private void ActivateAndResetLocalPlayer()
    {
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
        {
            return;
        }

        PlayerController localPlayer = FindLocalPlayer();

        if (localPlayer == null)
        {
            localPlayer = spawner.SpawnLocalPlayer();
            if (localPlayer == null)
            {
                return;
            }

            _cachedLocalPlayer = localPlayer;
        }   
        ApplyPlayerStartState(localPlayer);
    }

    private void ApplyPlayerStartState(PlayerController localPlayer)
    {
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
        {
            return;
        }

        Vector3 spawnPos = spawner.GetSpawnPosition(PhotonNetwork.LocalPlayer);
        localPlayer.transform.position = spawnPos;

        float hpRatio = PhotonPlayerStateManager.GetNextDayHpRatio(PhotonNetwork.LocalPlayer);

        localPlayer.ResetForNewDay(hpRatio);
        localPlayer.SetInGameMode();

        SpectatorCameraManager.Instance?.StopSpectate();
        SpectatorCameraManager.Instance?.ForceFollow(localPlayer.FollowTarget);
    }

    private PlayerController FindLocalPlayer()
    {
        if (_cachedLocalPlayer != null)
        {
            if (_cachedLocalPlayer.gameObject != null)
            {
                return _cachedLocalPlayer;
            }

            _cachedLocalPlayer = null;
        }

        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            PlayerController player = players[i];

            if (player == null)
            {
                continue;
            }

            if (player.photonView == null)
            {
                continue;
            }

            if (player.photonView.IsMine)
            {
                _cachedLocalPlayer = player;
                return player;
            }
        }

        return null;
    }

    private void SetAllPlayersRoomMode()
    {
        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                players[i].SetRoomMode();
            }
        }
    }

    private void ApplyNextDayHpRules_Master(DayEndReason reason)
    {
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];

            float ratio = 1f;

            if (reason == DayEndReason.TimeOver)
            {
                ratio = 0.5f;
            }
            else if (reason == DayEndReason.AllDead)
            {
                ratio = 0.25f;
            }

            if (PhotonPlayerStateManager.GetWasDeadThisDay(player))
            {
                ratio *= 0.5f;
            }

            PhotonPlayerStateManager.SetNextDayHpRatio(player, ratio);
            PhotonPlayerStateManager.SetState(player, PlayerGameState.Alive);
        }
    }

    private GameEndType ConvertEndType(DayEndReason reason)
    {
        switch (reason)
        {
            case DayEndReason.TimeOver:
                return GameEndType.Fail_TimeOver;
            case DayEndReason.AllDead:
                return GameEndType.Fail_PlayerDead;
            case DayEndReason.ManualEnd:
            default:
                return GameEndType.Success;
        }
    }

    private void SetDayState_Master(DayState state, DayEndReason reason)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { MatchKeys.DayEndReason, (int)reason },
            { MatchKeys.EndingUntil, 0.0 },
            { MatchKeys.DayState, (int)state }
        });
    }

    private void SetAllPlayersInGameMode()
    {
        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                players[i].SetInGameMode();
            }
        }
    }
    public void ForceReturnToRoom()
    {
        SetAllPlayersRoomMode();
    }
    
}