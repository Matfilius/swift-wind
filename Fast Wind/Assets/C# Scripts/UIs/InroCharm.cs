using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Svi tipovi charmova koji mogu biti
public enum CharmEffectType
{
    MaxMana
}

public class InroCharm : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Charm Effect")]
    public CharmEffectType effectType;
    public float baseEffectValue = 20f;

    [Header("Sprites")]
    public Texture fullTexture;
    public Texture slottedTexture;

    [Header("References")]
    public Canvas canvas;

    private RawImage _rawImage;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private InroManager _inroManager;

    private InroLayer _currentLayer;
    private Transform _inventorySlot;
    private Vector3 _inventoryPosition;

    private bool _effectActive;
    private float _appliedEffectValue;

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rectTransform = GetComponent<RectTransform>();
        _inroManager = FindFirstObjectByType<InroManager>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _inventorySlot = transform.parent;
        _inventoryPosition = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_currentLayer != null)
        {
            _currentLayer.RemoveCharm();
            _currentLayer = null;
        }

        _rawImage.texture = fullTexture;
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        InroLayer nearest = _inroManager.GetNearestAvailableLayer(transform.position);

        if (nearest != null)
        {
            bool placed = nearest.TryPlaceCharm(this);
            if (placed)
            {
                _currentLayer = nearest;
                return;
            }
        }

        ReturnToInventory();
    }

    public void SnapToLayer(InroLayer layer, float effectiveness)
    {
        transform.SetParent(layer.charmSlot);
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;
        _rawImage.texture = slottedTexture;

        _appliedEffectValue = baseEffectValue * effectiveness;
        ApplyEffect(_appliedEffectValue);
        _effectActive = true;

        Debug.Log($"{gameObject.name} placed at {effectiveness * 100}% effectiveness — applying {_appliedEffectValue} {effectType}");
    }

    public void UnapplyEffect()
    {
        if (!_effectActive) return;


        ApplyEffect(-_appliedEffectValue);
        _effectActive = false;
        _appliedEffectValue = 0f;
    }

    private void ApplyEffect(float value)
    {

        switch (effectType)
        {
            case CharmEffectType.MaxMana:
                ManaBar manaBar = FindFirstObjectByType<ManaBar>();
                if (manaBar != null)
                    manaBar.mana.ModifyMaxMana(value);
                break;
        }
    }

    public void ReturnToInventory()
    {
        transform.SetParent(_inventorySlot);
        transform.position = _inventoryPosition;
        transform.localScale = Vector3.one;
        _rawImage.texture = fullTexture;
        _currentLayer = null;
    }
}