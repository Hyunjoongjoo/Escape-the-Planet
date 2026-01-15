using UnityEngine;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;

    private string _roomName;
    private LobbyManager _lobbyManager;

    public void Setup(string roomName, int playerCount, int maxPlayers, LobbyManager lobby)
    {
        _roomName = roomName;
        _lobbyManager = lobby;

        _roomNameText.text = roomName;
        _playerCountText.text = $"{playerCount} / {maxPlayers}";
    }

    public void OnClick()
    {
        _lobbyManager.JoinRoom(_roomName);
    }
}

