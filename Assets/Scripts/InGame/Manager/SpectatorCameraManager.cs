using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorCameraManager : MonoBehaviour
{
    public static SpectatorCameraManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera _cam;

    [SerializeField] private InputActionReference _nextAction;
    [SerializeField] private InputActionReference _prevAction;

    private readonly List<PlayerController> _targets = new List<PlayerController>();
    private int _currentIndex = 0;
    private bool _isSpectating = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (_nextAction != null && _nextAction.action != null)
        {
            _nextAction.action.performed += OnNext;
        }

        if (_prevAction != null && _prevAction.action != null)
        {
            _prevAction.action.performed += OnPrevious;
        }
    }

    private void OnDisable()
    {
        if (_nextAction != null && _nextAction.action != null)
        {
            _nextAction.action.performed -= OnNext;
        }

        if (_prevAction != null && _prevAction.action != null)
        {
            _prevAction.action.performed -= OnPrevious;
        }
    }

    public void StartSpectate()
    {
        _isSpectating = true;

        RefreshTargets();
        FocusToFirstAlive();

        EnableSpectatorInput(true);
    }

    public void StopSpectate()
    {
        _isSpectating = false;
        _targets.Clear();
        _currentIndex = 0;

        EnableSpectatorInput(false);
    }

    private void EnableSpectatorInput(bool enable)
    {
        if (_nextAction != null && _nextAction.action != null)
        {
            if (enable) 
            { 
                _nextAction.action.Enable(); 
            }
            else 
            { 
                _nextAction.action.Disable(); 
            }
        }

        if (_prevAction != null && _prevAction.action != null)
        {
            if (enable) 
            { 
                _prevAction.action.Enable(); 
            }
            else 
            { 
                _prevAction.action.Disable(); 
            }
        }
    }

    private void OnNext(InputAction.CallbackContext ctx)
    {
        if (_isSpectating == false)
        {
            return;
        }

        NextTarget();
    }

    private void OnPrevious(InputAction.CallbackContext ctx)
    {
        if (_isSpectating == false)
        {
            return;
        }

        PrevTarget();
    }


    private void RefreshTargets()
    {
        _targets.Clear();

        IReadOnlyList<PlayerController> players = PlayerRegistry.Instance.Players;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null)
            {
                continue;
            }

            if (players[i].IsDead)
            {
                continue;
            }

            if (players[i].gameObject.activeInHierarchy == false)
            {
                continue;
            }

            _targets.Add(players[i]);
        }
    }

    private void FocusToFirstAlive()
    {
        if (_targets.Count == 0)
        {
            return;
        }

        _currentIndex = 0;
        ApplyCameraToCurrent();
    }

    public void NextTarget()
    {
        if (_targets.Count == 0)
        {
            return;
        }

        _currentIndex = (_currentIndex + 1) % _targets.Count;
        ApplyCameraToCurrent();
    }

    public void PrevTarget()
    {
        if (_targets.Count == 0)
        {
            return;
        }

        _currentIndex--;
        if (_currentIndex < 0)
        {
            _currentIndex = _targets.Count - 1;
        }

        ApplyCameraToCurrent();
    }

    private void ApplyCameraToCurrent()
    {
        if (_cam == null || _targets.Count == 0)
        {
            return;
        }

        PlayerController target = _targets[_currentIndex];
        if (target == null)
        {
            return;
        }

        Transform follow = target.FollowTarget != null
            ? target.FollowTarget
            : target.transform;

        _cam.Follow = follow;
        _cam.LookAt = follow;

        UIManager.Instance.UpdateSpectatorTarget(target);
    }
    public void ForceFollow(Transform follow)
    {
        if (_cam == null || follow == null)
        {
            return;
        }

        _cam.Follow = follow;
        _cam.LookAt = follow;
    }
}
