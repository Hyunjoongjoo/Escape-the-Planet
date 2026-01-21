using UnityEngine;
using System.IO;

public class SaveManager
{
    private static string GetSavePath(string playerKey)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{playerKey}.json");
    }

    public static void Save(string playerKey, SaveData data)
    {
        if (data == null)
        {
            return;
        }

        string path = GetSavePath(playerKey);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log($"[SaveManager] Saved: {path}\n{json}");
    }

    public static bool TryLoad(string playerKey, out SaveData data)
    {
        data = null;

        string path = GetSavePath(playerKey);

        if (File.Exists(path) == false)
        {
            Debug.Log($"[SaveManager] No save file: {path}");
            return false;
        }

        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<SaveData>(json);

        Debug.Log($"[SaveManager] Loaded: {path}\n{json}");
        return data != null;
    }

    public static void Delete(string playerKey)
    {
        string path = GetSavePath(playerKey);

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveManager] Deleted: {path}");
        }
    }
}
