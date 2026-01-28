using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitPoint : MonoBehaviourPunCallbacks, IInteractable
{
    public void Interact(GameObject interactor)
    {
        string key = SaveKeyProvider.GetPlayerKey();

        if (QuickSlotManager.Instance != null)
        {
            SaveData data = QuickSlotManager.Instance.ToSaveData();
            SaveManager.Save(key, data);
        }

        PhotonPlayerLocationManager.SetLocation(PlayerLocation.Room);

        GameManager.Instance?.ForceReturnToRoom();

        UIManager.Instance.SetRoomPhase();
    }
    
    public string GetPromptKey()
    {
        return "Space";
    }

    public string GetPromptHint()
    {
        return "Return";
    }
}
