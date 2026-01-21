using UnityEngine;

[System.Serializable]
public class QuickSlot
{
    [SerializeField] private ItemData _data;

    public ItemData Data => _data;
    public bool IsEmpty => _data == null;

    public void Set(ItemData data)
    {
        _data = data;
    }

    public void Clear()
    {
        _data = null;
    }
}
