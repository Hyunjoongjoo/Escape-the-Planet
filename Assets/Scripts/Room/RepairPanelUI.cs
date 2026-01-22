using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class RepairPanelUI : MonoBehaviour
{
    [SerializeField] private Text _repairText;
    [SerializeField] private Button _settleButton;

    [SerializeField] private ItemDatabase _itemDatabase;

    private int _repairPercent = 0;
    private bool _isLoading = false;

    private void Awake()
    {
        if (_settleButton != null)
        {
            _settleButton.onClick.AddListener(OnClickSettle);
        }
    }

    private async void Start()
    {
        await LoadRepairPercent();
    }

    private async Task LoadRepairPercent()
    {
        if (_isLoading == true)
        {
            return;
        }

        _isLoading = true;

        //Firebase 준비될 때까지 대기
        while (FirebaseUserData.Instance != null && FirebaseUserData.Instance.IsReady == false)
        {
            await Task.Delay(100);
        }

        if (FirebaseUserData.Instance == null)
        {
            Debug.LogError("[RepairPanelUI] FirebaseUserData.Instance is null.");
            _isLoading = false;
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        _repairPercent = await FirebaseUserData.Instance.GetRepairPercentAsync(key);
        RefreshText();

        _isLoading = false;
    }

    private void RefreshText()
    {
        if (_repairText == null)
        {
            return;
        }

        _repairText.text = $"수리률 : {_repairPercent}%";
    }

    private async void OnClickSettle()
    {
        if (_isLoading == true)
        {
            return;
        }

        if (_itemDatabase == null)
        {
            Debug.LogWarning("[RepairPanelUI] ItemDatabase is null.");
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        if (SaveManager.TryLoad(key, out SaveData data) == false || data == null)
        {
            Debug.Log($"[RepairPanelUI] No save for: {key}");
            return;
        }

        int gain = CalculateRepairGainFromSaveData(data);

        if (gain <= 0)
        {
            Debug.Log("[RepairPanelUI] No items to settle.");
            return;
        }

        int newValue = Mathf.Clamp(_repairPercent + gain, 0, 100);

        _isLoading = true;

        await FirebaseUserData.Instance.SetRepairPercentAsync(key, newValue);

        _repairPercent = newValue;
        RefreshText();

        ClearSaveDataQuickSlots(data);
        SaveManager.Save(key, data);

        _isLoading = false;

        Debug.Log($"[RepairPanelUI] Settled repair: +{gain} -> {_repairPercent}% (SaveData cleared)");
    }

    private int CalculateRepairGainFromSaveData(SaveData data)
    {
        if (data == null || data.quickSlots == null)
        {
            return 0;
        }

        int total = 0;

        for (int i = 0; i < data.quickSlots.Length; i++)
        {
            ItemId itemId = data.quickSlots[i];

            if (itemId == ItemId.NONE)
            {
                continue;
            }

            ItemData itemData = _itemDatabase.GetItem(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[RepairPanelUI] ItemData not found for: {itemId}");
                continue;
            }

            total += Mathf.Max(0, Mathf.RoundToInt(itemData.repairPoint));
        }

        return total;
    }

    private void ClearSaveDataQuickSlots(SaveData data)
    {
        if (data == null || data.quickSlots == null)
        {
            return;
        }

        for (int i = 0; i < data.quickSlots.Length; i++)
        {
            data.quickSlots[i] = ItemId.NONE;
        }
    }
}
