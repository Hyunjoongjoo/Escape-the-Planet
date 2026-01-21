using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Scriptable Objects/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemData[] _items;

    private Dictionary<ItemId, ItemData> _map;

    private void OnEnable()
    {
        Build();
    }

    private void Build()
    {
        _map = new Dictionary<ItemId, ItemData>();

        for (int i = 0; i < _items.Length; i++)
        {
            ItemData data = _items[i];
            if (data == null)
            {
                continue;
            }

            _map[data.id] = data;
        }
    }

    public ItemData Get(ItemId id)
    {
        if (id == ItemId.NONE)
        {
            return null;
        }

        if (_map == null)
        {
            Build();
        }

        return _map.TryGetValue(id, out ItemData data) ? data : null;
    }
    //public ItemData GetRandom()
    //{
    //    if (_items == null || _items.Length == 0)
    //    {
    //        return null;
    //    }

    //    int safety = 200;
    //    while (safety-- > 0)
    //    {
    //        int idx = Random.Range(0, _items.Length);
    //        ItemData data = _items[idx];
    //        if (data != null && data.id != ItemId.NONE)
    //        {
    //            return data;
    //        }
    //    }

    //    return null;
    //}
    public ItemData GetRandomWeighted(ItemId excludeId = ItemId.NONE)
    {
        if (_items == null || _items.Length == 0)
        {
            return null;
        }

        int total = 0;

        for (int i = 0; i < _items.Length; i++)
        {
            ItemData it = _items[i];
            if (it == null)
            {
                continue;
            }

            if (it.id == ItemId.NONE)
            {
                continue;
            }

            if (excludeId != ItemId.NONE && it.id == excludeId)
            {
                continue;
            }

            total += Mathf.Max(0, it.weight);
        }

        if (total <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, total);
        int acc = 0;

        for (int i = 0; i < _items.Length; i++)
        {
            ItemData it = _items[i];
            if (it == null)
            {
                continue;
            }

            if (it.id == ItemId.NONE)
            {
                continue;
            }

            if (excludeId != ItemId.NONE && it.id == excludeId)
            {
                continue;
            }

            acc += Mathf.Max(0, it.weight);

            if (roll < acc)
            {
                return it;
            }
        }

        return null;
    }
}
