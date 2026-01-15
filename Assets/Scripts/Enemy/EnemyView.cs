using UnityEngine;

public class EnemyView : MonoBehaviour
{
    [SerializeField] private Animator _anim;
    [SerializeField] private SpriteRenderer _sprite;

    private void Awake()
    {
        if (_anim == null)
        {
            _anim = GetComponent<Animator>();
        }

        if (_sprite == null)
        {
            _sprite = GetComponent<SpriteRenderer>();
        }
    }

    public void SetMove(float x)
    {
        if (x > 0)
        {
            _sprite.flipX = false;
        }
        else if (x < 0)
        {
            _sprite.flipX = true;
        }

        _anim.SetFloat("Speed", Mathf.Abs(x));
    }

    public void PlayHit()
    {
        _anim.SetTrigger("Hit");
    }

    public void PlayDead()
    {
        _anim.SetBool("IsDead", true);
    }
}
