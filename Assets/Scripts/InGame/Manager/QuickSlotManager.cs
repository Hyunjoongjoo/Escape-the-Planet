using UnityEngine;
using System;
using Photon.Pun;

public class QuickSlotManager : MonoBehaviourPun
{
    public static QuickSlotManager Instance { get; private set; }

    public event Action<int> OnSelectedChanged;
    public event Action<int, QuickSlot> OnSlotUpdated;

    [SerializeField] private QuickSlot[] _slots = new QuickSlot[5];

    [SerializeField] private string _groundItemPrefabName = "GroundItem";
    [SerializeField] private float _dropScatterRadius = 1.0f;


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

    public void DropSelectedItem()
    {
        ItemData data = GetSelectedItemData();
        if (data == null)
        {
            return;
        }

        RemoveSelectedItem();

        Vector2 dropPos = GetDropPosition();
        photonView.RPC(nameof(RPC_RequestDrop), RpcTarget.MasterClient, (int)data.id, dropPos.x, dropPos.y);
    }

    [PunRPC]
    private void RPC_RequestDrop(int itemId, float x, float y, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Vector2 pos = new Vector2(x, y);

        GameObject obj = PhotonNetwork.Instantiate(_groundItemPrefabName, pos, Quaternion.identity);

        GroundItemNetwork net = obj.GetComponent<GroundItemNetwork>();
        if (net != null)
        {
            net.SetItemId((ItemId)itemId);
        }
    }

    private ItemData GetSelectedItemData()
    {
        if (CurrentIndex < 0 || CurrentIndex >= _slots.Length)
        {
            return null;
        }

        QuickSlot slot = _slots[CurrentIndex];
        if (slot == null || slot.IsEmpty)
        {
            return null;
        }

        return slot.Data;
    }

    private void RemoveSelectedItem()
    {
        if (CurrentIndex < 0 || CurrentIndex >= _slots.Length)
        {
            return;
        }

        _slots[CurrentIndex].Clear();
        RaiseSlotUpdated(CurrentIndex);
    }

    public void DropAllOnDeath(Vector2 deathPos)
    {
        ItemId[] items = GetAllItemIds();
        if (items == null || items.Length == 0)
        {
            return;
        }

        ClearAllSlots();

        int[] ids = new int[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            ids[i] = (int)items[i];
        }

        photonView.RPC(nameof(RPC_RequestDropAll), RpcTarget.MasterClient, ids, deathPos.x, deathPos.y);
    }

    [PunRPC]
    private void RPC_RequestDropAll(int[] itemIds, float x, float y, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Vector2 center = new Vector2(x, y);

        for (int i = 0; i < itemIds.Length; i++)
        {
            ItemId itemId = (ItemId)itemIds[i];

            Vector2 scatter = UnityEngine.Random.insideUnitCircle * _dropScatterRadius;
            Vector2 pos = center + scatter;

            GameObject obj = PhotonNetwork.Instantiate(_groundItemPrefabName, pos, Quaternion.identity);

            GroundItemNetwork net = obj.GetComponent<GroundItemNetwork>();
            if (net != null)
            {
                net.SetItemId(itemId);
            }
        }
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

        SaveManager.Save(
            SaveKeyProvider.GetPlayerKey(),
            ToSaveData()
        );
    }

    private Vector2 GetDropPosition()
    {
        PlayerController player = FindLocalPlayer();
        if (player == null)
        {
            return Vector2.zero;
        }

        return player.transform.position;
    }

    private PlayerController FindLocalPlayer()
    {
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController p in players)
        {
            PhotonView v = p.GetComponent<PhotonView>();

            if (v != null && v.IsMine)
            {
                return p;
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
