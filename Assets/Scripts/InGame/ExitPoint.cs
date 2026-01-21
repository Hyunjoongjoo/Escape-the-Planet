using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class ExitPoint : MonoBehaviourPunCallbacks, IInteractable
{
    //[SerializeField] private bool _saveOnExit = true;
    //[SerializeField] private string _roomSceneName = "Room";

    //public void Interact(GameObject interactor)
    //{
    //    if (_saveOnExit == true)
    //    {
    //        SaveGame();
    //    }

    //    ExitGame();
    //}

    //private void SaveGame()
    //{
    //    SaveData data = QuickSlotManager.Instance.ToSaveData();
    //    SaveManager.Save(data);

    //    Debug.Log("[ExitPoint] Saved game data.");
    //}

    //private void ExitGame()
    //{
    //    Debug.Log("[ExitPoint] Leaving room...");
    //    PhotonNetwork.LeaveRoom();
    //}

    //public override void OnLeftRoom()
    //{
    //    Debug.Log("[ExitPoint] Left room. Loading Room scene...");
    //    SceneManager.LoadScene(_roomSceneName);
    //}

    //public string GetPromptKey()
    //{
    //    return "Space";
    //}

    //public string GetPromptHint()
    //{
    //    return "Exit";
    //}


    [SerializeField] private string _roomSceneName = "Room";

    public void Interact(GameObject interactor)
    {
        string key = SaveKeyProvider.GetPlayerKey();

        SaveData data = QuickSlotManager.Instance.ToSaveData();
        SaveManager.Save(key, data);

        SceneManager.LoadScene(_roomSceneName);
    }

    public string GetPromptKey()
    {
        return "Space";
    }

    public string GetPromptHint()
    {
        return "Exit";
    }
}
