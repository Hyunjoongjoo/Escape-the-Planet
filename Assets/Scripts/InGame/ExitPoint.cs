using UnityEngine;
using Photon.Pun;

public class ExitPoint : MonoBehaviourPunCallbacks, IInteractable
{
    public void Interact(GameObject interactor)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.EndGame(GameEndType.Success);
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
