using System;
using System.IO;
using UnityEngine;

public class SaveManager
{
    private const string DefaultPlayerKey = "local";

    private static string NormalizeKey(string playerKey)
    {
        if (string.IsNullOrWhiteSpace(playerKey) == true)
        {
            return DefaultPlayerKey;
        }

        return playerKey.Trim();
    }

    private static string GetSavePath(string playerKey)
    {
        string key = NormalizeKey(playerKey);
        return Path.Combine(Application.persistentDataPath, $"save_{key}.json");
    }

    public static bool Save(string playerKey, SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveManager] Save failed: data is null");
            return false;
        }

        string path = GetSavePath(playerKey);

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);

            Debug.Log($"[SaveManager] Saved: {path}\n{json}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save failed: {path}\n{e}");
            return false;
        }
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

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);

            Debug.Log($"[SaveManager] Loaded: {path}\n{json}");
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load failed: {path}\n{e}");
            data = null;
            return false;
        }
    }

    public static bool Delete(string playerKey)
    {
        string path = GetSavePath(playerKey);

        try
        {
            if (File.Exists(path) == true)
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] Deleted: {path}");
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Delete failed: {path}\n{e}");
            return false;
        }
    }
}
