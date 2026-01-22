using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class InGameMatchController : MonoBehaviourPunCallbacks
{
    [SerializeField] private float _checkInterval = 0.5f;
    [SerializeField] private string _roomSceneName = "Room";

    private float _timer = 0f;
    private bool _returning = false;

    private void Update()
    {
        if (_returning == true)
        {
            return;
        }

        if (PhotonNetwork.InRoom == false)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient == false)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < _checkInterval)
        {
            return;
        }

        _timer = 0f;

        if (IsAllPlayersFinished() == true)
        {
            _returning = true;
            PhotonNetwork.LoadLevel(_roomSceneName);
        }
    }

    private bool IsAllPlayersFinished()
    {
        Player[] players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];
            if (player == null)
            {
                continue;
            }

            PlayerGameState state = PhotonPlayerStateManager.GetState(player);

            if (state == PlayerGameState.Alive)
            {
                return false;
            }
        }

        return true;
    }
}
