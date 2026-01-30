using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RepairPanelUI : MonoBehaviour
{
    [SerializeField] private Text _repairText;
    [SerializeField] private Button _settleButton;

    [SerializeField] private ItemDatabase _itemDatabase;

    private int _currentRepair = 0;

    private void Awake()
    {
        if (_settleButton != null)
        {
            _settleButton.onClick.AddListener(OnClickSettle);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            RefreshFromRoom(GameManager.Instance.GetRepair());
        }
    }

    public void RefreshFromRoom(int repair)
    {
        _currentRepair = repair;
        RefreshText();
    }

    private void RefreshText()
    {
        if (_repairText == null)
        {
            return;
        }

        _repairText.text = $"¼ö¸®·ü : {_currentRepair}%";
    }

    private void OnClickSettle()
    {
        if (GameManager.Instance == null)
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

        NetworkRelay.Instance.RequestRepair(gain);

        ClearSaveDataQuickSlots(data);
        SaveManager.Save(key, data);

        QuickSlotManager.Instance?.ClearAllSlots();
    }

    private int CalculateRepairGainFromSaveData(SaveData data)
    {
        if (data.quickSlots == null)
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

            ItemData item = _itemDatabase.GetItem(itemId);
            if (item == null)
            {
                continue;
            }

            total += Mathf.Max(0, Mathf.RoundToInt(item.repairPoint));
        }

        return total;
    }

    private void ClearSaveDataQuickSlots(SaveData data)
    {
        for (int i = 0; i < data.quickSlots.Length; i++)
        {
            data.quickSlots[i] = ItemId.NONE;
        }
    }
}
