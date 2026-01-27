using System.Collections;
using System.Collections.Generic;
using System;
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

    [SerializeField] private float _dayDuration = 300f;

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
    }

    public void OnClickReady()
    {
        bool current = GetReady(PhotonNetwork.LocalPlayer);
        SetReady(!current);
        UpdateUI();
    }

    public void OnClickStartDay()
    {
        if (IsDayRunning())
        {
            return;
        }

        if (!IsAllPlayersReady())
        {
            _chatManager?.AddSystemMessage("아직 준비되지 않은 플레이어가 있습니다.");
            return;
        }

        Hashtable props = new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Running },
            { MatchKeys.DayStartTime, PhotonNetwork.Time },
            { MatchKeys.DayDuration, _dayDuration }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        GameManager.Instance.OnDayStart();

        InGameWorldController.Instance.ShowWorld();

        UIManager.Instance.SetInGamePhase();

        UpdateButtons();
    }

    public void OnClickEnterFactory()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                MatchKeys.DayState, out object stateValue))
        {
            return;
        }

        DayState state = (DayState)(int)stateValue;

        if (state != DayState.Running)
        {
            return;
        }

        InGameWorldController.Instance.ShowWorld();

        UIManager.Instance.SetInGamePhase();
    }

    public void OnClickEndDay()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        GameManager.Instance.OnDayEnd();

        Hashtable props = new Hashtable
        {
            { MatchKeys.DayState, (int)DayState.Ending }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        _chatManager?.AddSystemMessage("하루를 종료합니다.");

        UpdateButtons();
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
        bool isDayRunning = false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                MatchKeys.DayState, out object stateValue))
        {
            isDayRunning = (DayState)(int)stateValue == DayState.Running;
        }

        if (_startDayButton != null)
        {
            _startDayButton.interactable = !isDayRunning;
        }

        if (_enterFactoryButton != null)
        {
            _enterFactoryButton.interactable = isDayRunning;
        }

        if (_endDayButton != null)
        {
            _endDayButton.interactable = isDayRunning;
        }
    }

    private bool IsDayRunning()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(
                MatchKeys.DayState, out object stateValue))
        {
            return (DayState)(int)stateValue == DayState.Running;
        }

        return false;
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps == null)
        {
            return;
        }

        if (changedProps.TryGetValue(MatchKeys.DayState, out object value))
        {
            DayState state = (DayState)(int)value;

            if (state == DayState.Running)
            {
                UpdateButtons();
            }

            if (state == DayState.Ending)
            {
                UpdateButtons();
            }
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