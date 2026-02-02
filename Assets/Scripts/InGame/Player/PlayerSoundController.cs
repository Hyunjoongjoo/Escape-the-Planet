using UnityEngine;
using Photon.Pun;

public class PlayerSoundController : MonoBehaviourPun
{
    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private AudioClip _footstepClip;
    [SerializeField] private AudioClip _attackClip;
    [SerializeField] private AudioClip _hitClip;
    [SerializeField] private AudioClip _deadClip;
    [SerializeField] private AudioClip _dropClip;
    [SerializeField] private AudioClip _pickupClip;

    [SerializeField] private float _footstepInterval = 0.35f;

    private float _nextFootstepTime = 0f;
    private void Awake()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }
    }
    public void TryPlayFootstep(bool isMoving)
    {
        if (!isMoving)
        {
            return;
        }

        if (_footstepClip == null || _audioSource == null)
        {
            return;
        }

        if (Time.time < _nextFootstepTime)
        {
            return;
        }

        _audioSource.PlayOneShot(_footstepClip);

        _nextFootstepTime = Time.time + _footstepInterval;
    }

    public void PlayAttack()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (_audioSource == null || _attackClip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_attackClip);
    }

    public void PlayHit()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (_audioSource == null || _hitClip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_hitClip);
    }

    public void PlayDead()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (_audioSource == null || _deadClip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_deadClip);
    }

    public void PlayDrop()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (_audioSource == null || _dropClip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_dropClip);
    }

    public void PlayPickup()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (_audioSource == null || _dropClip == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_pickupClip);
    }
}