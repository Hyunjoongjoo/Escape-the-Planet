using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;

public class SoundManager : MonoBehaviourPunCallbacks
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource _factoryAmbienceSource;

    [SerializeField] private AudioSource _uiSource;

    [SerializeField] private AudioClip _dayStartClip;
    [SerializeField] private AudioClip _dayEndClip;
    [SerializeField] private AudioClip _enterFactoryClip;
    [SerializeField] private AudioClip _repairClip;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayDayStart()
    {
        if (_uiSource == null || _dayStartClip == null)
        {
            return;
        }

        _uiSource.PlayOneShot(_dayStartClip);
    }

    public void PlayDayEnd()
    {
        if (_uiSource == null || _dayEndClip == null)
        {
            return;
        }

        _uiSource.PlayOneShot(_dayEndClip);
    }

    public void PlayEnterFactory()
    {
        if (_uiSource == null || _enterFactoryClip == null)
        {
            return;
        }

        _uiSource.PlayOneShot(_enterFactoryClip);
    }

    public void PlayRepair()
    {
        if (_uiSource == null || _dayStartClip == null)
        {
            return;
        }

        _uiSource.PlayOneShot(_repairClip);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(MatchKeys.DayState))
        {
            UpdateFactorySound();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != PhotonNetwork.LocalPlayer)
        {
            return;
        }

        if (changedProps.ContainsKey(MatchKeys.Loc))
        {
            UpdateFactorySound();
        }
    }

    private void UpdateFactorySound()
    {
        if (_factoryAmbienceSource == null)
        {
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchKeys.DayState, out object stateObj))
        {
            ExitFactory();
            return;
        }

        DayState state = (DayState)(int)stateObj;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(MatchKeys.Loc, out object locObj))
        {
            ExitFactory();
            return;
        }

        PlayerLocation location = (PlayerLocation)(int)locObj;

        if (state == DayState.Running && location == PlayerLocation.InGame)
        {
            EnterFactory();
        }
        else
        {
            ExitFactory();
        }
    }

    public void EnterFactory()
    {
        if (_factoryAmbienceSource == null)
        {
            return;
        }

        if (_factoryAmbienceSource.isPlaying)
        {
            return;
        }

        _factoryAmbienceSource.Play();
    }

    public void ExitFactory()
    {
        if (_factoryAmbienceSource == null)
        {
            return;
        }

        _factoryAmbienceSource.Stop();
    }
}
