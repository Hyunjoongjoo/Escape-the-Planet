using UnityEngine;
using System.Collections.Generic;

public class PlayerRegistry : MonoBehaviour
{
    public static PlayerRegistry Instance;

    private List<PlayerController> _players = new List<PlayerController>();
    public IReadOnlyList<PlayerController> Players
    {
        get
        {
            _players.RemoveAll(player => player == null);
            return _players;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Register(PlayerController player)
    {
        if (!_players.Contains(player))
        {
            _players.Add(player);
        }
    }

    public void Unregister(PlayerController player)
    {
        if (_players.Contains(player))
        {
            _players.Remove(player);
        }
    }
}
