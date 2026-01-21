using UnityEngine;
using UnityEngine.UI;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Text _hpText;
    [SerializeField] private Text _playerNameText;
    [SerializeField] private Text _timeText;

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
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged -= HandleTimeChanged;
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
}
