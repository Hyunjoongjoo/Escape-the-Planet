using UnityEngine;

public class InGameSaveLoader : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;

    private void Start()
    {
        if (_itemDatabase == null)
        {
            Debug.LogWarning("[InGameSaveLoader] ItemDatabase is null");
            return;
        }

        if (QuickSlotManager.Instance == null)
        {
            Debug.LogWarning("[InGameSaveLoader] QuickSlotManager.Instance is null");
            return;
        }

        string key = SaveKeyProvider.GetPlayerKey();

        if (SaveManager.TryLoad(key, out SaveData data))
        {
            QuickSlotManager.Instance.LoadFromSaveData(data, _itemDatabase);
        }
        else
        {
            Debug.Log($"No save for: {key}");
        }
    }
}
