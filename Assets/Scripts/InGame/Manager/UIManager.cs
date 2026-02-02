using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject _roomUIRoot;
    [SerializeField] private GameObject _inGameUIRoot;

    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Text _hpText;
    [SerializeField] private Text _playerNameText;

    [SerializeField] private GameObject _gameTimePanel;
    [SerializeField] private Text _timeText;

    [SerializeField] private GameObject _gameEndPanel;
    [SerializeField] private Text _gameEndText;

    [SerializeField] private float _gameEndShowTime = 2.5f;

    [SerializeField] private GameObject _spectatorPanel;
    [SerializeField] private Text _spectatorNameText;
    [SerializeField] private Slider _spectatorHpSlider;
    [SerializeField] private Text _spectatorHpText;

    private Coroutine _gameEndRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged += HandleTimeChanged;
            GameManager.Instance.OnGameEndTriggered += HandleGameEnd;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged -= HandleTimeChanged;
            GameManager.Instance.OnGameEndTriggered -= HandleGameEnd;
        }
    }

    public void SetRoomPhase()
    {
        _roomUIRoot.SetActive(true);
        _inGameUIRoot.SetActive(false);
        FindFirstObjectByType<RepairPanelUI>()?.RefreshFromRoom(GameManager.Instance.GetRepair());
    }

    public void SetInGamePhase()
    {

        _roomUIRoot.SetActive(false);
        _inGameUIRoot.SetActive(true);
    }

    private void HandleTimeChanged(float remain)
    {
        if (_timeText == null)
        {
            return;
        }

        int min = Mathf.FloorToInt(remain / 60f);
        int sec = Mathf.FloorToInt(remain % 60f);

        _timeText.text = $"{min:00}:{sec:00}";
    }
    public void InitPlayerUI(string playerName, float currentHP, float maxHP)
    {
        _playerNameText.text = playerName;
        _hpSlider.maxValue = maxHP;
        _hpSlider.value = currentHP;
        _hpText.text = $"{currentHP} / {maxHP}";
    }

    public void UpdateHP(float currentHP, float maxHP)
    {
        _hpSlider.value = currentHP;
        _hpText.text = $"{currentHP} / {maxHP}";
    }

    private void HandleGameEnd(GameEndType type)
    {
        if (_gameEndRoutine != null)
        {
            StopCoroutine(_gameEndRoutine);
        }

        _gameEndRoutine = StartCoroutine(GameEndRoutine(type));
    }

    private IEnumerator GameEndRoutine(GameEndType type)
    {
        _gameEndPanel.SetActive(true);

        switch (type)
        {
            case GameEndType.Success:
                _gameEndText.text = "Success";
                break;
            case GameEndType.Fail_TimeOver:
                _gameEndText.text = "TIME OVER";
                break;
            case GameEndType.Fail_PlayerDead:
                _gameEndText.text = "YOU DIED";
                break;
        }

        PlayerController local = FindLocalPlayer();
        if (local != null)
        {
            local.SetInputEnabled(false);
        }

        yield return new WaitForSeconds(_gameEndShowTime);

        _gameEndPanel.SetActive(false);

        if (local != null && GameManager.Instance != null)
        {
            if (GameManager.Instance.RemainTime > 0f)
            {
                local.SetInputEnabled(true);
            }
        }
    }

    private PlayerController FindLocalPlayer()
    {
        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].photonView.IsMine)
            {
                return players[i];
            }
        }

        return null;
    }

    public void ShowTimer()
    {
        if (_gameTimePanel != null)
        {
            _gameTimePanel.SetActive(true);
        }
    }

    public void HideTimer()
    {
        if (_gameTimePanel != null)
        {
            _gameTimePanel.SetActive(false);
        }
    }

    public void UpdateSpectatorTarget(PlayerController target)
    {
        if (target == null)
        {
            _spectatorNameText.text = "";
            _spectatorHpSlider.value = 0f;
            _spectatorHpText.text = "";
            return;
        }

        string name = target.photonView.Owner.NickName;
        float hp = target.CurrentHP;
        float maxHp = target.MaxHP;

        _spectatorNameText.text = name;
        _spectatorHpSlider.maxValue = maxHp;
        _spectatorHpSlider.value = hp;
        _spectatorHpText.text = $"{hp} / {maxHp}";
    }

    public void EnterSpectatorMode()
    {
        _spectatorPanel.SetActive(true);
    }

    public void ExitSpectatorMode()
    {
        _spectatorPanel.SetActive(false);
    }
}
