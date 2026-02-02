using System;
using System.Collections.Generic;
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
        if (Instance != null && Instance != this)
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

    public bool HasEmptySlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && _slots[i].IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    public void DropSelectedItem()
    {
        if (TryDropCurrent(out ItemData dropped) == false)
        {
            return;
        }

        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer == null)
        {
            TryPickup(dropped);
            return;
        }

        localPlayer.RequestDropItem(dropped.id);
    }

    public void DropAllOnDeath(Vector2 deathPos)
    {
        ItemId[] items = GetAllItemIds();
        if (items == null || items.Length == 0)
        {
            return;
        }

        ClearAllSlots();

        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer == null)
        {
            return;
        }

        localPlayer.RequestDropAll(items, deathPos);
    }

    private ItemId[] GetAllItemIds()
    {
        ItemId[] temp = new ItemId[_slots.Length];
        int count = 0;

        for (int i = 0; i < _slots.Length; i++)
        {
            QuickSlot slot = _slots[i];

            if (slot == null || slot.IsEmpty || slot.Data == null)
            {
                continue;
            }

            temp[count] = slot.Data.id;
            count++;
        }

        if (count == 0)
        {
            return Array.Empty<ItemId>();
        }

        ItemId[] result = new ItemId[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = temp[i];
        }

        return result;
    }

    public void ClearAllSlots()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Clear();
            RaiseSlotUpdated(i);
        }

        CurrentIndex = 0;
        RaiseSelectedChanged();

        SaveManager.Save(SaveKeyProvider.GetPlayerKey(), ToSaveData());
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

    private PlayerController FindLocalPlayer()
    {
        IReadOnlyList <PlayerController> players = PlayerRegistry.Instance.Players;

        foreach (PlayerController player in players)
        {
            Photon.Pun.PhotonView view = player.GetComponent<Photon.Pun.PhotonView>();
            if (view != null && view.IsMine)
            {
                return player;
            }
        }

        return null;
    }

    private void RaiseSelectedChanged()
    {
        OnSelectedChanged?.Invoke(CurrentIndex);
    }

    private void RaiseSlotUpdated(int index)
    {
        OnSlotUpdated?.Invoke(index, _slots[index]);
    }
}