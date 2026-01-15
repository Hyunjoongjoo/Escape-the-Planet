using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class PlayerSlot
    {
        public GameObject root;
        public Text nickText;
        public Text readyText;
    }

    [SerializeField] private PlayerSlot[] _slots;
    [SerializeField] private ChatManager _chatManager;
    [SerializeField] private Button _exitButton;

    private const string READY_KEY = "Ready";

    private bool _gameStarted = false;


    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            UpdateUI();
        }
    }
    public override void OnJoinedRoom()
    {
        SetReady(false);
        UpdateUI();
    }
    //방장이 바뀔때
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdateUI();
        CheckAutoStart();
    }
    public void OnClickReady()
    {
        bool current = GetReady(PhotonNetwork.LocalPlayer);
        bool next = !current;

        SetReady(next);
        UpdateExitButton();

        if (next)
        {
            _chatManager?.AddSystemMessage("준비 중에는 나가기 버튼이 비활성화됩니다.\n나가려면 준비를 해제하세요.");
        }
    }

    private void SetReady(bool value)
    {
        Hashtable table = new Hashtable();
        table[READY_KEY] = value;
        PhotonNetwork.LocalPlayer.SetCustomProperties(table);
    }

    private bool GetReady(Player player)
    {
        if (player.CustomProperties.TryGetValue(READY_KEY, out object value))
        {
            return (bool)value;
        }

        return false;
    }
    private void CheckAutoStart()
    {
        if (_gameStarted)
        {
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!GetReady(player))
            {
                return;
            }
        }
        _gameStarted = true;
        PhotonNetwork.LoadLevel("InGame");
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
        UpdateExitButton();

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
        if (changedProps.ContainsKey(READY_KEY))
        {
            UpdateUI();
            CheckAutoStart();
        }
    }
    private void UpdateExitButton()
    {
        if (_exitButton == null)
        {
            return;
        }

        bool isReady = GetReady(PhotonNetwork.LocalPlayer);

        //Ready면 나가기 불가
        _exitButton.interactable = !isReady;
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

