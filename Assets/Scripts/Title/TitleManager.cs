using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _startButton;
    [SerializeField] private InputField _nicknameInput;
    [SerializeField] private Text _connectionStatusText;

    private const string NICK_KEY = "NICKNAME";

    private void Start()
    {
        _connectionStatusText.text = "Ready";
        if (_nicknameInput != null)
        {
            _nicknameInput.onValueChanged.AddListener(OnNicknameChanged);

            if (PlayerPrefs.HasKey(NICK_KEY))
            {
                _nicknameInput.text = PlayerPrefs.GetString(NICK_KEY);
            }

            OnNicknameChanged(_nicknameInput.text);
        }


    }
    private void OnNicknameChanged(string value)
    {
        bool valid = !string.IsNullOrWhiteSpace(value);

        if (_startButton != null)
        {
            _startButton.interactable = valid;
        }
    }
    public void OnClickStart()
    {
        string nick = _nicknameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(nick))
        {
            Debug.LogWarning("닉네임이 비어있어서 접속 불가");
            return;
        }

        PlayerPrefs.SetString(NICK_KEY, nick);

        PhotonNetwork.NickName = nick;

        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";

        PhotonNetwork.SendRate = 40;
        PhotonNetwork.SerializationRate = 20;

        Debug.Log("연결중");
        _connectionStatusText.text = "Connecting...";
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 연결됨");
        _connectionStatusText.text = "Connected!";
        SceneManager.LoadScene("Lobby");
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Photon Disconnected : {cause}");
        _connectionStatusText.text = "Disconnected";
    }
    public void OnClickOption()
    {
        if (_panel == null)
        {
            return;
        }
    }
    public void OnClickExit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
