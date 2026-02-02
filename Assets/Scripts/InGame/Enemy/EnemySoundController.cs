using UnityEngine;

public class EnemySoundController : MonoBehaviour
{
    [SerializeField] private AudioSource _idleSource;
    [SerializeField] private AudioSource _footstepSource;
    [SerializeField] private AudioSource _effectSource;
    [SerializeField] private AudioSource _alertSource;

    [SerializeField] private AudioClip _idleLoop;
    [SerializeField] private AudioClip _footstep;
    [SerializeField] private AudioClip _hit;
    [SerializeField] private AudioClip _dead;
    [SerializeField] private AudioClip _alert;

    [SerializeField] private float _slowFootstepInterval = 0.5f;
    [SerializeField] private float _fastFootstepInterval = 0.22f;

    private float _nextFootstepTime = 0f;

    public void TryPlayFootstep(float speed01)
    {
        if (speed01 <= 0.05f)
        {
            return;
        }

        float interval = Mathf.Lerp(
            _slowFootstepInterval,
            _fastFootstepInterval,
            speed01
        );

        if (Time.time < _nextFootstepTime)
        {
            return;
        }

        _nextFootstepTime = Time.time + interval;

        if (_footstepSource != null && _footstep != null)
        {
            _footstepSource.PlayOneShot(_footstep);
        }
    }

    public void PlayIdleLoop()
    {
        if (_idleSource == null || _idleLoop == null)
        {
            return;
        }

        if (_idleSource.isPlaying)
        {
            return;
        }

        _idleSource.clip = _idleLoop;
        _idleSource.loop = true;
        _idleSource.Play();
    }

    public void StopIdleLoop()
    {
        if (_idleSource == null)
        {
            return;
        }

        _idleSource.Stop();
    }

    public void PlayHit()
    {
        if (_effectSource != null && _hit != null)
        {
            _effectSource.PlayOneShot(_hit);
        }
    }

    public void PlayDead()
    {
        if (_effectSource != null && _dead != null)
        {
            _effectSource.PlayOneShot(_dead);
        }
    }

    public void PlayAlert()
    {
        if (_alertSource != null && _alert != null)
        {
            _alertSource.PlayOneShot(_alert);
        }
    }
}
