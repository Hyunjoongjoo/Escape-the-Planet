using UnityEngine;

public class RoomSaveLoader : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;

    private void Start()
    {
        if (_itemDatabase == null)
        {
            Debug.LogWarning("[RoomSaveLoader] ItemDatabase is null");
            return;
        }

        if (QuickSlotManager.Instance == null)
        {
            Debug.LogWarning("[RoomSaveLoader] QuickSlotManager.Instance is null");
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        if (SaveManager.TryLoad(key, out SaveData data))
        {
            QuickSlotManager.Instance.LoadFromSaveData(data, _itemDatabase);
            Debug.Log("[RoomSaveLoader] QuickSlot loaded from SaveData");
        }
        else
        {
            Debug.Log($"[RoomSaveLoader] No save for: {key}");
        }
    }
}
