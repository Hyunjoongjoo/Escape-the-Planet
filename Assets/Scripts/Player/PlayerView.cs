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

    public void SetMove(float x, float speed)
    {

        _anim.SetFloat("Speed", speed);

        //X축 기준으로 이미지 방향 전환
        if (x > 0)
        {
            _sprite.flipX = false;
        }
        else if (x < 0)
        {
            _sprite.flipX = true;
        }
    }

    public void PlayHit()
    {
        _anim.SetTrigger("Hit");
    }

    public void PlayAttack()
    {
        _anim.SetTrigger("Attack");
    }
}
