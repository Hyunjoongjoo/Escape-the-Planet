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

    public void SetMove(Vector2 dir, float speed01)
    {

        if (dir.x > 0f)
        {
            _sprite.flipX = false;
        }
        else if (dir.x < 0f)
        {
            _sprite.flipX = true;
        }

        _anim.SetFloat("Speed", speed01);
    }

    public void PlayHit()
    {
        _anim.SetTrigger("Hit");
    }

    public void PlayDead()
    {
        _anim.SetBool("IsDead", true);
    }

    public void SetAlpha(float alpha)
    {
        if (_sprite == null)
        {
            return;
        }

        Color c = _sprite.color;
        c.a = alpha;
        _sprite.color = c;
    }
}
