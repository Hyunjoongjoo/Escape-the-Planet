using UnityEngine;

public interface IInteractable
{
    void Interact (GameObject interactor);
    string GetPromptKey();
    string GetPromptHint();
}
