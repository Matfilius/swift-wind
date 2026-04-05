using UnityEngine;
using UnityEngine.EventSystems;

public class InroLayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Layer Settings")]
    public int layerIndex;
    public float snapDistance = 80f;

    private static readonly float[] EffectivenessByLayer = { 0.25f, 0.50f, 0.75f, 1.00f };

    [Header("Jut Settings")]
    public float hoverShiftAmount = 30f;
    public float occupiedShiftAmount = 50f;
    public float animationSpeed = 10f;

    [Header("Charm Slot")]
    public RectTransform charmSlot;

    [HideInInspector] public float baseYPosition;

    private Vector2 _targetPosition;
    private RectTransform _rectTransform;
    private InroCharm _occupyingCharm;
    private InroManager _manager;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _manager = GetComponentInParent<InroManager>();
    }

    void Start()
    {
        baseYPosition = _rectTransform.anchoredPosition.y;
        _targetPosition = _rectTransform.anchoredPosition;
    }

    void Update()
    {
        _rectTransform.anchoredPosition = Vector2.Lerp(
            _rectTransform.anchoredPosition,
            _targetPosition,
            Time.deltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _manager.OnLayerHovered(layerIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _manager.OnLayerHoverExit();
    }

    public void SetTargetY(float extraShift)
    {
        _targetPosition = new Vector2(
            _rectTransform.anchoredPosition.x,
            baseYPosition + extraShift
        );
    }

    public bool TryPlaceCharm(InroCharm charm)
    {
        if (layerIndex == 4)
        {
            Debug.Log("Top layer cannot hold a charm.");
            return false;
        }

        if (_occupyingCharm != null) return false;

        _occupyingCharm = charm;

        float effectiveness = EffectivenessByLayer[layerIndex];
        charm.SnapToLayer(this, effectiveness);

        _manager.RefreshAllLayerPositions();
        return true;
    }

    public void RemoveCharm()
    {
        if (_occupyingCharm == null) return;

        _occupyingCharm.UnapplyEffect();
        _occupyingCharm = null;
        _manager.RefreshAllLayerPositions();
    }

    public bool IsOccupied() => _occupyingCharm != null;

    public Vector3? GetSlotWorldPosition()
    {
        if (charmSlot == null) return null;
        return charmSlot.position;
    }

    public float GetOccupiedShift() => _occupyingCharm != null ? occupiedShiftAmount : 0f;
}