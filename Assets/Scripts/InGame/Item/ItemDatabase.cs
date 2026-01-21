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
}
