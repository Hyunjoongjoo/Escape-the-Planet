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

        while (FirebaseUserData.Instance != null && FirebaseUserData.Instance.IsReady == false)
        {
            await Task.Delay(100);
        }

        if (FirebaseUserData.Instance == null)
        {
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

        _repairText.text = $"¼ö¸®·ü : {_repairPercent}%";
    }

    private async void OnClickSettle()
    {
        if (_isLoading == true)
        {
            return;
        }

        if (_itemDatabase == null)
        {
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        if (SaveManager.TryLoad(key, out SaveData data) == false || data == null)
        {
            return;
        }

        int gain = CalculateRepairGainFromSaveData(data);
        if (gain <= 0)
        {
            return;
        }

        int newValue = Mathf.Clamp(_repairPercent + gain, 0, 100);
        _isLoading = true;

        await FirebaseUserData.Instance.SetRepairPercentAsync(key, newValue);

        _repairPercent = newValue;
        RefreshText();

        ClearSaveDataQuickSlots(data);
        SaveManager.Save(key, data);

        if (QuickSlotManager.Instance != null)
        {
            QuickSlotManager.Instance.ClearAllSlots(); 
        }

        _isLoading = false;
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
