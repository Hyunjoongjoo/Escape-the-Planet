using System;
using UnityEngine;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    public event Action<int> OnSelectedChanged;
    public event Action<int, QuickSlot> OnSlotUpdated;

    [SerializeField] private QuickSlot[] _slots = new QuickSlot[5];

    public int CurrentIndex { get; private set; } = 0;
    public QuickSlot[] Slots => _slots;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = new QuickSlot();
            }
        }
    }

    private void Start()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            RaiseSlotUpdated(i);
        }
        RaiseSelectedChanged();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= _slots.Length)
        {
            return;
        }

        CurrentIndex = index;
        RaiseSelectedChanged();
    }

    public bool TryPickup(ItemData data)
    {
        if (data == null)
        {
            return false;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].IsEmpty)
            {
                _slots[i].Set(data);
                RaiseSlotUpdated(i);
                return true;
            }
        }

        return false;
    }

    public bool TryDropCurrent(out ItemData dropped)
    {
        dropped = null;

        QuickSlot slot = _slots[CurrentIndex];
        if (slot.IsEmpty)
        {
            return false;
        }

        dropped = slot.Data;
        slot.Clear();

        RaiseSlotUpdated(CurrentIndex);
        return true;
    }

    private void RaiseSelectedChanged()
    {
        OnSelectedChanged?.Invoke(CurrentIndex);
    }

    private void RaiseSlotUpdated(int index)
    {
        OnSlotUpdated?.Invoke(index, _slots[index]);
    }

    public SaveData ToSaveData()
    {
        SaveData data = new SaveData();

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].IsEmpty || _slots[i].Data == null)
            {
                data.quickSlots[i] = ItemId.NONE;
            }
            else
            {
                data.quickSlots[i] = _slots[i].Data.id;
            }
        }

        return data;
    }

    public void LoadFromSaveData(SaveData save, ItemDatabase db)
    {
        if (save == null || db == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            ItemId id = save.quickSlots[i];

            if (id == ItemId.NONE)
            {
                _slots[i].Clear();
            }
            else
            {
                ItemData item = db.Get(id);
                _slots[i].Set(item);
            }

            RaiseSlotUpdated(i);
        }

        CurrentIndex = 0;
        RaiseSelectedChanged();
    }
}
