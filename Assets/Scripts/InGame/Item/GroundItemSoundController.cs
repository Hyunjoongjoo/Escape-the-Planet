using UnityEngine;

public class GroundItemSoundController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [SerializeField] private AudioClip _pickup;

    public void PlayPickup()
    {
        if (_audioSource == null || _pickup == null)
        {
            return;
        }

        if (!_audioSource.enabled)
        {
            return;
        }

        _audioSource.PlayOneShot(_pickup);
    }
}
