using UnityEngine;
using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;

public class FirebaseUserData : MonoBehaviour
{
    public static FirebaseUserData Instance { get; private set; }

    [Header("Realtime Database")]
    [SerializeField] private string _databaseUrl;

    private DatabaseReference _root;

    public bool IsReady { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await Init();
    }

    private async Task Init()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"[FirebaseUserData] Firebase dependency error: {status}");
            return;
        }

        if (string.IsNullOrEmpty(_databaseUrl))
        {
            Debug.LogError("[FirebaseUserData] Database URL is empty.");
            return;
        }

        FirebaseApp.DefaultInstance.Options.DatabaseUrl = new Uri(_databaseUrl);
        _root = FirebaseDatabase.DefaultInstance.RootReference;

        IsReady = true;
        Debug.Log("[FirebaseUserData] Firebase READY");
    }

    private string GetUserRepairPercentPath(string playerKey)
    {
        return $"users/{playerKey}/repairPercent";
    }

    public async Task<int> GetRepairPercentAsync(string playerKey)
    {
        if (IsReady == false)
        {
            Debug.LogWarning("[FirebaseUserData] Not ready.");
            return 0;
        }

        if (string.IsNullOrEmpty(playerKey))
        {
            Debug.LogWarning("[FirebaseUserData] playerKey is null.");
            return 0;
        }

        string path = GetUserRepairPercentPath(playerKey);

        DataSnapshot snap = await FirebaseDatabase.DefaultInstance.GetReference(path).GetValueAsync();
        if (snap.Exists == false || snap.Value == null)
        {
            return 0;
        }

        if (int.TryParse(snap.Value.ToString(), out int value))
        {
            return value;
        }

        return 0;
    }

    public async Task SetRepairPercentAsync(string playerKey, int value)
    {
        if (IsReady == false)
        {
            Debug.LogWarning("[FirebaseUserData] Not ready.");
            return;
        }

        if (string.IsNullOrEmpty(playerKey))
        {
            Debug.LogWarning("[FirebaseUserData] playerKey is null.");
            return;
        }

        value = Mathf.Clamp(value, 0, 100);

        string path = GetUserRepairPercentPath(playerKey);
        await FirebaseDatabase.DefaultInstance.GetReference(path).SetValueAsync(value);

        Debug.Log($"[FirebaseUserData] Saved {path} = {value}");
    }
}