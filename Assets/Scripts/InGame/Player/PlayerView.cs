using UnityEngine;

public class PlayerView : MonoBehaviour
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

    public void SetMove(int facing, float speed01)
    {
        _sprite.flipX = (facing == -1);

        _anim.SetFloat("Speed", speed01);
    }

    public void SetDead(bool value)
    {
        _anim.SetBool("IsDead", value);
    }
    public void PlayHit()
    {
        _anim.SetTrigger("Hit");
    }

    public void PlayAttack()
    {
        _anim.SetTrigger("Attack");
    }
    public void SetVisible(bool value)
    {
        if (_sprite != null)
        {
            _sprite.enabled = value;
        }
    }
    public void ForceResetAnimator()
    {
        _anim.Rebind();
        _anim.Update(0f);
        _anim.Play("IdleAndRun", 0, 0f);
        _anim.Update(0f);
    }
}
