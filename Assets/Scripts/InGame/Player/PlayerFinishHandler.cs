using UnityEngine;

public class PlayerFinishHandler : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Collider2D[] _colliders;
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private SpriteRenderer[] _renderers;
    [SerializeField] private GameObject[] _objectsToDisable;

    private bool _finished = false;

    private void Awake()
    {
        if (_playerController == null)
        {
            _playerController = GetComponent<PlayerController>();
        }

        if (_rigidbody2D == null)
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (_colliders == null || _colliders.Length == 0)
        {
            _colliders = GetComponentsInChildren<Collider2D>(true);
        }

        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    public void ApplyFinishedState()
    {
        if (_finished == true)
        {
            return;
        }

        _finished = true;

        if (_playerController != null)
        {
            _playerController.SetInputEnabled(false);
        }

        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
            _rigidbody2D.simulated = false;
        }

        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                {
                    _colliders[i].enabled = false;
                }
            }
        }

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = false;
                }
            }
        }

        if (_objectsToDisable != null)
        {
            for (int i = 0; i < _objectsToDisable.Length; i++)
            {
                if (_objectsToDisable[i] != null)
                {
                    _objectsToDisable[i].SetActive(false);
                }
            }
        }
    }
}
