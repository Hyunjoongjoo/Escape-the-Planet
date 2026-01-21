using UnityEngine;

public class GroundItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _data;

    private SpriteRenderer _sr;

    public ItemData Data => _data;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        Apply();
    }

    private void OnValidate()
    {
        _sr = GetComponent<SpriteRenderer>();
        Apply();
    }

    public void Setup(ItemData data)
    {
        _data = data;
        Apply();
    }

    private void Apply()
    {
        if (_sr == null)
        {
            return;
        }

        _sr.sprite = (_data != null) ? _data.sprite : null;
    }

    public void Interact(GameObject interactor)
    {
        bool ok = QuickSlotManager.Instance.TryPickup(_data);

        if (ok)
        {
            Destroy(gameObject);
        }
    }

    public string GetPromptKey()
    {
        return "Space";
    }

    public string GetPromptHint()
    {
        return "";
    }
}
