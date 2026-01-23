using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SortY : MonoBehaviour
{
    [SerializeField] private Transform _pivot;
    [SerializeField] private float _scale = 100f;
    [SerializeField] private int _baseOrder = 0;

    private SpriteRenderer _sprite;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();

        if (_pivot == null)
        {
            _pivot = transform;
        }
    }

    private void LateUpdate()
    {
        float y = _pivot.position.y;
        _sprite.sortingOrder = _baseOrder - Mathf.RoundToInt(y * _scale);
    }
}
