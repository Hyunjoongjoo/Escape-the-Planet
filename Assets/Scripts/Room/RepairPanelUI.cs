using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class RepairPanelUI : MonoBehaviour
{
    [SerializeField] private Text _repairText;
    [SerializeField] private Button _settleButton;

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
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        string key = SaveKeyProvider.GetPlayerKey(); // 이미 너 프로젝트에 존재

        // Firebase 준비될때까지 잠깐 대기
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
        if (_isLoading)
        {
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        int gain = CalculateRepairGainFromQuickSlot();

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

        // 정산 후 퀵슬롯 비우기(선택)
        ClearQuickSlot();

        _isLoading = false;

        Debug.Log($"[RepairPanelUI] RepairPercent +{gain} -> {_repairPercent}%");
    }

    private int CalculateRepairGainFromQuickSlot()
    {
        if (QuickSlotManager.Instance == null)
        {
            return 0;
        }

        int count = 0;

        QuickSlot[] slots = QuickSlotManager.Instance.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].IsEmpty == false && slots[i].Data != null)
            {
                count++;
            }
        }

        // 규칙:
        // 아이템 1개 = 수리 2%
        int gain = count * 2;

        return gain;
    }

    private void ClearQuickSlot()
    {
        if (QuickSlotManager.Instance == null)
        {
            return;
        }

        QuickSlot[] slots = QuickSlotManager.Instance.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Clear();
            }
        }

        // UI 업데이트 강제
        for (int i = 0; i < slots.Length; i++)
        {
            QuickSlotManager.Instance.SelectSlot(QuickSlotManager.Instance.CurrentIndex);
        }
    }
}
