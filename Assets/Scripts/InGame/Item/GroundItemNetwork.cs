using UnityEngine;
using Photon.Pun;

public class GroundItemNetwork : MonoBehaviourPun
{
    [SerializeField] private GroundItem _groundItem;
    [SerializeField] private ItemDatabase _itemDatabase;

    private void Awake()
    {
        if (_groundItem == null)
        {
            _groundItem = GetComponent<GroundItem>();
        }
    }

    public void SetItemId(ItemId itemId)
    {
        if (!photonView.IsMine)
        {
            return;
        }

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
        if (info.Sender == null || info.Sender.ActorNumber != actorNumber)
        {
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonNetwork.Destroy(gameObject);
    }
}
