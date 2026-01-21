using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform _player;

    private readonly List<IInteractable> _nearTargets = new List<IInteractable>();

    public IInteractable CurrentTarget { get; private set; }

    private void Awake()
    {
        if (_player == null)
        {
            _player = transform.root;
        }
    }

    private void Update()
    {
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        if (_nearTargets.Count == 0)
        {
            CurrentTarget = null;
            return;
        }

        IInteractable nearest = null;
        float nearestSqr = float.MaxValue;

        Vector3 origin = _player != null ? _player.position : transform.position;

        for (int i = _nearTargets.Count - 1; i >= 0; i--)
        {
            if (_nearTargets[i] == null)
            {
                _nearTargets.RemoveAt(i);
                continue;
            }

            MonoBehaviour mb = _nearTargets[i] as MonoBehaviour;
            if (mb == null)
            {
                continue;
            }

            float sqr = (mb.transform.position - origin).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = _nearTargets[i];
            }
        }

        CurrentTarget = nearest;
    }

    public bool TryInteract(GameObject interactor)
    {
        if (CurrentTarget == null)
        {
            return false;
        }

        CurrentTarget.Interact(interactor);
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable it = other.GetComponent<IInteractable>();
        if (it == null)
        {
            return;
        }

        if (_nearTargets.Contains(it) == false)
        {
            _nearTargets.Add(it);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable it = other.GetComponent<IInteractable>();
        if (it == null)
        {
            return;
        }

        _nearTargets.Remove(it);
    }
}
