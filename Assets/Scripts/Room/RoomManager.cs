using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class PlayerSlot
    {
        public Text nickText;
        public Text readyText;
    }

    [SerializeField] private PlayerSlot[] _slots;
    [SerializeField] private ChatManager _chatManager;

    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _startDayButton;
    [SerializeField] private Button _enterFactoryButton;
    [SerializeField] private Button _endDayButton;

    private void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        PhotonPlayerLocationManager.SetLocation(PlayerLocation.Room);
        PhotonPlayerStateManager.SetState(PlayerGameState.Alive);

        SetReady(false);
        UpdateUI();
        UpdateButtons();

        ApplyCurrentDayState();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.SpawnLocalPlayer();
        }

        StartCoroutine(RefreshUIAfterJoin());
    }

    private IEnumerator RefreshUIAfterJoin()
    {
        yield return null;
        UpdateUI();
        UpdateButtons();
        ApplyCurrentDayState();
    }

    public void OnClickStartDay()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (!IsAllPlayersReady())
        {
            _chatManager?.AddSystemMessage("아직 준비되지 않은 플레이어가 있습니다.");
            return;
        }

        if (NetworkRelay.Instance == null)
        {
            return;
        }

        NetworkRelay.Instance.RequestStartDay();
    }

    public void OnClickEndDay()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (NetworkRelay.Instance == null)
        {
            return;
        }

        NetworkRelay.Instance.RequestEndDay(DayEndReason.ManualEnd);
    }

    public void OnClickEnterFactory()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayState, out object stateValue))
        {
            return;
        }

        DayState state = (DayState)(int)stateValue;

        if (state != DayState.Running)
        {
            return;
        }

        EnterFactory_Local();
    }

    private void EnterFactory_Local()
    {
        PlayerController player = FindLocalPlayer();
        if (player == null)
        {
            return;
        }

        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
        {
            return;
        }

        Vector3 spawnPos = spawner.GetSpawnPosition(PhotonNetwork.LocalPlayer);

        player.transform.position = spawnPos;
        player.SetInGameMode();

        UIManager.Instance.SetInGamePhase();
    }

    private PlayerController FindLocalPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].photonView.IsMine)
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
                    return player;
                }
            }
        }

        return null;
    }

    public void OnClickReady()
    {
        bool current = GetReady(PhotonNetwork.LocalPlayer);
        SetReady(!current);
        UpdateUI();
    }

    private void SetReady(bool value)
    {
        Hashtable table = new Hashtable
        {
            { MatchKeys.Ready, value }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    private bool GetReady(Player player)
    {
        if (player != null &&
            player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(MatchKeys.Ready, out object value))
        {
            return (bool)value;
        }

        return false;
    }

    private bool IsAllPlayersReady()
    {
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            if (!GetReady(players[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateUI()
    {
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < _slots.Length; i++)
        {
            bool active = i < players.Length;

            _slots[i].nickText.gameObject.SetActive(active);
            _slots[i].readyText.gameObject.SetActive(active);

            if (active)
            {
                Player player = players[i];
                _slots[i].nickText.text = player.NickName;
                _slots[i].readyText.text = GetReady(player) ? "준비 완료" : "준비 안됨";
            }
        }

        if (_exitButton != null)
        {
            _exitButton.interactable = !GetReady(PhotonNetwork.LocalPlayer);
        }
    }

    private void UpdateButtons()
    {
        DayState state = DayState.Idle;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayState, out object stateValue))
        {
            state = (DayState)(int)stateValue;
        }

        bool canStart = state == DayState.Idle;
        bool canEnter = state == DayState.Running;
        bool canEnd = state == DayState.Running;

        if (_startDayButton != null)
        {
            _startDayButton.interactable = canStart;
        }

        if (_enterFactoryButton != null)
        {
            _enterFactoryButton.interactable = canEnter;
        }

        if (_endDayButton != null)
        {
            _endDayButton.interactable = canEnd;
        }
    }

    private void ApplyCurrentDayState()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayState, out object stateValue))
    {
        UIManager.Instance.SetRoomPhase();
        return;
    }

    DayState state = (DayState)(int)stateValue;

    if (state == DayState.Running)
    {
        UIManager.Instance.SetInGamePhase();
    }
    else
    {
        UIManager.Instance.SetRoomPhase();
    }
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps == null)
        {
            return;
        }

        if (changedProps.ContainsKey(MatchKeys.DayState))
        {
            ApplyCurrentDayState();
            UpdateButtons();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(MatchKeys.Ready))
        {
            UpdateUI();
        }
    }

    public void OnClickLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }
}