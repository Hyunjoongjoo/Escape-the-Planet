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
    [SerializeField] private Text _timeText;

    [SerializeField] private GameObject _gameEndPanel;
    [SerializeField] private Text _gameEndText;

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
            HandleTimeChanged(GameManager.Instance.RemainTime);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged -= HandleTimeChanged;
        }
    }

    public void SetRoomPhase()
    {
        if (_roomUIRoot != null)
        {
            _roomUIRoot.SetActive(true);
        }

        if (_inGameUIRoot != null)
        {
            _inGameUIRoot.SetActive(false);
        }

        if (_gameEndPanel != null)
        {
            _gameEndPanel.SetActive(false);
        }
    }

    public void SetInGamePhase()
    {

        if (_roomUIRoot == null || _inGameUIRoot == null)
        {
            return;
        }

        _roomUIRoot.SetActive(false);
        _inGameUIRoot.SetActive(true);

        if (_gameEndPanel != null)
        {
            _gameEndPanel.SetActive(false);
        }
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
    public void InitPlayerUI(string playerName, int currentHP, int maxHP)
    {
        _playerNameText.text = playerName;
        _hpSlider.maxValue = maxHP;
        _hpSlider.value = currentHP;
        _hpText.text = $"{currentHP} / {maxHP}";
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        _hpSlider.value = currentHP;
        _hpText.text = $"{currentHP} / {maxHP}";
    }

    public void ShowGameEndPanel(GameEndType endType)
    {
        if (_gameEndPanel == null || _gameEndText == null)
        {
            return;
        }

        _gameEndPanel.SetActive(true);

        switch (endType)
        {
            case GameEndType.Fail_TimeOver:
                _gameEndText.text = "TIME OVER";
                break;

            case GameEndType.Fail_PlayerDead:
                _gameEndText.text = "YOU DIED";
                break;
        }
    }
}
