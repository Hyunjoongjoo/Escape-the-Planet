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
        }
        else
        {
            Debug.Log($"No save for: {key}");
        }
    }
}
