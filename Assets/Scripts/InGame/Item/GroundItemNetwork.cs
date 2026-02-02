using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GroundItemNetwork : MonoBehaviourPun
{
    [SerializeField] private GroundItem _groundItem;
    [SerializeField] private ItemDatabase _itemDatabase;

    private bool _pickedUp = false;

    public ItemId ItemId { get; private set; }

    private void Awake()
    {
        if (_groundItem == null)
        {
            _groundItem = GetComponent<GroundItem>();
        }

        _pickedUp = false;
    }

    public void SetItemId(ItemId itemId)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        ItemId = itemId;

        photonView.RPC(nameof(RPC_SetItemId), RpcTarget.AllBuffered, (int)itemId);
    }

    [PunRPC]
    private void RPC_SetItemId(int itemId)
    {
        if (_groundItem == null)
        {
            return;
        }

        if (_itemDatabase == null)
        {
            return;
        }

        ItemData data = _itemDatabase.GetItem((ItemId)itemId);
        if (data != null)
        {
            _groundItem.Setup(data);
        }
    }

    public void RequestPickup(int actorNumber)
    {
        photonView.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, actorNumber);
    }

    [PunRPC]
    private void RPC_RequestPickup(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        if (_pickedUp)
        {
            return;
        }

        _pickedUp = true;

        photonView.RPC(nameof(RPC_PickupApproved), RpcTarget.All, actorNumber);

        PoolManager.Instance.ReturnItem(gameObject);
    }

    public void ResetForPool()
    {
        _pickedUp = false;
    }

    [PunRPC]
    private void RPC_PickupApproved(int actorNumber)
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
        {
            return;
        }

        if (_groundItem == null || _groundItem.Data == null)
        {
            return;
        }

        QuickSlotManager.Instance.TryPickup(_groundItem.Data);
    }
}