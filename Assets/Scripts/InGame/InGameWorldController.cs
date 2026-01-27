using UnityEngine;

public class InGameWorldController : MonoBehaviour
{
    public static InGameWorldController Instance { get; private set; }

    [SerializeField] private GameObject _worldRoot;

    public GameObject WorldRoot => _worldRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowWorld()
    {
        _worldRoot.SetActive(true);
    }

    public void HideWorld()
    {
        _worldRoot.SetActive(false);
    }
}
