using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform _player;

    private readonly List<GroundItem> _nearItems = new List<GroundItem>();

    public GroundItem CurrentTarget { get; private set; }

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
        if (_nearItems.Count == 0)
        {
            CurrentTarget = null;
            return;
        }

        GroundItem nearest = null;
        float nearestSqr = float.MaxValue;

        Vector3 origin = _player != null ? _player.position : transform.position;

        for (int i = _nearItems.Count - 1; i >= 0; i--)
        {
            if (_nearItems[i] == null)
            {
                _nearItems.RemoveAt(i);
                continue;
            }

            float sqr = (_nearItems[i].transform.position - origin).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = _nearItems[i];
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
        GroundItem gi = other.GetComponent<GroundItem>();
        if (gi == null)
        {
            return;
        }

        if (_nearItems.Contains(gi) == false)
        {
            _nearItems.Add(gi);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GroundItem gi = other.GetComponent<GroundItem>();
        if (gi == null)
        {
            return;
        }

        _nearItems.Remove(gi);
    }
}
