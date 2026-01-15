using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;


public class ChatManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private InputField _inputField;
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _messagePrefab;
    [SerializeField] private ScrollRect _scrollRect;

    private InputAction _enterAction;
    private Coroutine _sendRoutine;

    private void Awake()
    {
        if (_inputField == null || _content == null || _messagePrefab == null || _scrollRect == null)
        {
            return;
        }
        _inputField.onEndEdit.AddListener(OnEndEdit);
        _enterAction = new InputAction(name: "ChatEnter", type: InputActionType.Button);
        _enterAction.AddBinding("<Keyboard>/enter");
        _enterAction.AddBinding("<Keyboard>/numpadEnter");
        _enterAction.performed += OnEnterPerformed;
    }
    private void OnDestroy()
    {
        if (_inputField != null)
        {
            _inputField.onEndEdit.RemoveListener(OnEndEdit);
        }

        if (_enterAction != null)
        {
            _enterAction.performed -= OnEnterPerformed;
            _enterAction.Disable();
            _enterAction.Dispose();
        }
    }
    public override void OnEnable()
    {
        base.OnEnable();
        _enterAction?.Enable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        _enterAction?.Disable();
    }
    private void OnEnterPerformed(InputAction.CallbackContext ctx)
    {
        if (_inputField == null)
        {
            return;
        }

        if (_inputField.isFocused)
        {
            return;
        }

        StartSendRoutine();
    }
    private void StartSendRoutine()
    {
        if (_sendRoutine != null)
        {
            return;
        }
        _sendRoutine = StartCoroutine(WaitSend());
    }
    private IEnumerator WaitSend()
    {
        yield return null;
        TrySend();
        _sendRoutine = null;
    }
    private void OnEndEdit(string value)
    {
        if (_inputField == null)
        {
            return;
        }
        StartSendRoutine();
    }
    private void TrySend()
    {
        //널
        if (_inputField == null || _content == null || _messagePrefab == null || _scrollRect == null)
        {
            return;
        }

        string message = _inputField.text;

        message = message.Trim();

        //빈 문자열
        if (string.IsNullOrEmpty(message))
        {
            _inputField.ActivateInputField();
            return;
        }
        //메세지 길이 제한
        if (message.Length > 80)
        {
            message = message.Substring(0, 80);
        }

        photonView.RPC(nameof(ReceiveMessage), RpcTarget.All, PhotonNetwork.NickName, message);

        _inputField.text = "";
        _inputField.ActivateInputField();
    }

    public void AddSystemMessage(string message)
    {
        if (_content == null || _messagePrefab == null || _scrollRect == null)
        {
            return;
        }

        GameObject obj = Instantiate(_messagePrefab, _content);

        Text text = obj.GetComponent<Text>();
        if (text != null)
        {
            text.text = $"[SYSTEM] : {message}";
        }

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    [PunRPC]
    private void ReceiveMessage(string sender, string message)
    {
        GameObject obj = Instantiate(_messagePrefab, _content);

        Text text = obj.GetComponent<Text>();
        if (text != null)
        {
            text.text = $"{sender} : {message}";
        }

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }
}
