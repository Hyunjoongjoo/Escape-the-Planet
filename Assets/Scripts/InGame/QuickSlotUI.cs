using UnityEngine;
using UnityEngine.UI;

public class QuickSlotUI : MonoBehaviour
{
    [SerializeField] private RectTransform _highlight;  
    [SerializeField] private RectTransform[] _slotPoints;    

    [SerializeField] private Transform _iconRoot;
    [SerializeField] private Vector2 _iconSize = new Vector2(60f, 60f);

    [SerializeField] private float _unselectedAlpha = 0.125f;
    [SerializeField] private float _selectedAlpha = 1f;

    private Image[] _icons;
    private QuickSlotManager _mgr;

    private void Awake()
    {
        EnsureIconsCreated();
    }

    private void Start()
    {
        _mgr = QuickSlotManager.Instance;

        _mgr.OnSelectedChanged += HandleSelectedChanged;
        _mgr.OnSlotUpdated += HandleSlotUpdated;

        HandleSelectedChanged(_mgr.CurrentIndex);

        for (int i = 0; i < _mgr.Slots.Length; i++)
        {
            HandleSlotUpdated(i, _mgr.Slots[i]);
        }
    }

    private void OnDestroy()
    {
        if (_mgr != null)
        {
            _mgr.OnSelectedChanged -= HandleSelectedChanged;
            _mgr.OnSlotUpdated -= HandleSlotUpdated;
        }
    }

    private void EnsureIconsCreated()
    {
        if (_slotPoints == null || _slotPoints.Length == 0)
        {
            Debug.LogWarning("[QuickSlotUI] SlotPoints not set.");
            return;
        }

        if (_iconRoot == null)
        {
            _iconRoot = transform;
        }

        if (_icons != null && _icons.Length == _slotPoints.Length)
        {
            return;
        }

        _icons = new Image[_slotPoints.Length];

        for (int i = 0; i < _slotPoints.Length; i++)
        {
            RectTransform point = _slotPoints[i];
            if (point == null)
            {
                continue;
            }

            GameObject go = new GameObject($"Icon_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_iconRoot, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = point.anchorMin;
            rt.anchorMax = point.anchorMax;
            rt.pivot = point.pivot;
            rt.anchoredPosition = point.anchoredPosition;
            rt.sizeDelta = _iconSize;

            Image img = go.GetComponent<Image>();
            img.enabled = false;              // 처음엔 비어있으니 안 보이게
            img.preserveAspect = true;
            img.raycastTarget = false;        // 클릭 안 받을거면 끄는게 좋음

            _icons[i] = img;
        }
    }

    private void HandleSelectedChanged(int index)
    {
        if (_icons == null)
        {
            return;
        }

        for (int i = 0; i < _icons.Length; i++)
        {
            if (_icons[i] == null)
            {
                continue;
            }

            Color c = _icons[i].color;
            c.a = (i == index) ? _selectedAlpha : _unselectedAlpha;
            _icons[i].color = c;
        }
    }

    private void HandleSlotUpdated(int index, QuickSlot slot)
    {
        if (_icons == null || index < 0 || index >= _icons.Length)
        {
            return;
        }

        if (slot == null || slot.IsEmpty || slot.Data == null)
        {
            _icons[index].enabled = false;
            _icons[index].sprite = null;
            return;
        }

        _icons[index].enabled = true;
        _icons[index].sprite = slot.Data.sprite;
    }
}
