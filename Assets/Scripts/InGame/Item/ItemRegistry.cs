using UnityEngine;
using System.Collections.Generic;

public class ItemRegistry : MonoBehaviour
{
    public static ItemRegistry Instance;

    private List<GroundItemNetwork> _items = new List<GroundItemNetwork>();

    public IReadOnlyList<GroundItemNetwork> Items
    {
        get
        {
            _items.RemoveAll(item => item == null);
            return _items;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Register(GroundItemNetwork item)
    {
        if (!_items.Contains(item))
        {
            _items.Add(item);
        }
    }

    public void Unregister(GroundItemNetwork item)
    {
        if (_items.Contains(item))
        {
            _items.Remove(item);
        }
    }
}
