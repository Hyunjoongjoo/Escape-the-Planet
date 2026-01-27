using UnityEngine;
using Photon.Pun;

public class GroundItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData _data;

    private SpriteRenderer _sr;
    private GroundItemNetwork _net;

    public ItemData Data => _data;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _net = GetComponent<GroundItemNetwork>();
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
        if (_data == null)
        {
            return;
        }

        if (_net == null)
        {
            return;
        }

        if (!QuickSlotManager.Instance.HasEmptySlot())
        {
            return;
        }

        _net.RequestPickup(PhotonNetwork.LocalPlayer.ActorNumber);
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