using UnityEngine;

public class InGameSaveLoader : MonoBehaviour
{
    [SerializeField] private ItemDatabase _itemDatabase;

    private void Start()
    {
        string key = SaveKeyProvider.GetPlayerKey();

        if (SaveManager.TryLoad(key, out SaveData data))
        {
            QuickSlotManager.Instance.LoadFromSaveData(data, _itemDatabase);
            Debug.Log($"[InGameSaveLoader] Loaded for: {key}");
        }
        else
        {
            Debug.Log($"[InGameSaveLoader] No save for: {key}");
        }
    }
}
