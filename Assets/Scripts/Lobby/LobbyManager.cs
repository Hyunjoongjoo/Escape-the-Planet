using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Text _nickText;
    [SerializeField] private Button _quickJoinButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _backToTitleButton;
    [SerializeField] private InputField _roomNameInputField;
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _roomButtonPrefab;


    private Dictionary<string, GameObject> _roomButtons = new Dictionary<string, GameObject>();
    private Dictionary<string, RoomInfo> _cachedRoomList = new Dictionary<string, RoomInfo>();

    private void Start()
    {
        if (!PhotonNetwork.IsConnectedAndReady || string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            SceneManager.LoadScene("Title");
            return;
        }
        if (_nickText != null)
        {
            _nickText.text = $"닉네임 : {PhotonNetwork.NickName}";
        }
        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"방 생성 성공 : {PhotonNetwork.CurrentRoom.Name}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패 : {message} ({returnCode})");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 입장 실패 : {message} ({returnCode})");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                if (_cachedRoomList.ContainsKey(info.Name))
                {
                    _cachedRoomList.Remove(info.Name);
                }
            }
            else
            {
                _cachedRoomList[info.Name] = info;
            }
        }

        UpdateRoomList();
    }
    private void UpdateRoomList()
    {
        // 기존 버튼 제거
        foreach (var btn in _roomButtons.Values)
        {
            Destroy(btn);
        }
        _roomButtons.Clear();

        // 새로운 목록 생성
        foreach (RoomInfo info in _cachedRoomList.Values)
        {
            GameObject newButton = Instantiate(_roomButtonPrefab, _content);
            newButton.GetComponent<RoomButton>().Setup(info.Name, info.PlayerCount, info.MaxPlayers, this);

            _roomButtons.Add(info.Name, newButton);
        }
    }
    //빠른 참여
    public void OnClick_QuickJoin()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError($"랜덤 입장 실패 : {message} ({returnCode})");
        CreateRoom("Room_" + Random.Range(0, 9999));
    }

    //방 참여
    public void OnClick_JoinRoom()
    {
        string roomName = _roomNameInputField.text;
        if (string.IsNullOrEmpty(roomName))
        {
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
    }

    //방 생성
    public void OnClick_CreateRoom()
    {
        string roomName = _roomNameInputField.text;
        if (string.IsNullOrEmpty(roomName))
        {
            return;
        }
        CreateRoom(roomName);
    }

    //타이틀로 돌아가기
    public void OnClick_BackToTitle()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Title");
    }

    private void CreateRoom(string roomName)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    //방 클릭 , 참가
    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Room");
    }

}

