using UnityEngine;

public class RoomSaveLoader : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;

    private void Start()
    {
        if (_itemDatabase == null)
        {
            return;
        }

        if (QuickSlotManager.Instance == null)
        {
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        if (SaveManager.TryLoad(key, out SaveData data))
        {
            QuickSlotManager.Instance.LoadFromSaveData(data, _itemDatabase);
        }
        else
        {
        }
    }
}
