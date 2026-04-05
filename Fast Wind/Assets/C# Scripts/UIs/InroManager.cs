using UnityEngine;
using UnityEngine.InputSystem;

public class InroManager : MonoBehaviour
{
    [Header("References")]
    public InroLayer[] layers;
    [SerializeField] GameObject charmMenu;

    private int _hoveredLayerIndex = -1;

    public void OnLayerHovered(int layerIndex)
    {
        _hoveredLayerIndex = layerIndex;
        RefreshAllLayerPositions();
    }

    public void OnLayerHoverExit()
    {
        _hoveredLayerIndex = -1;
        RefreshAllLayerPositions();
    }

    public void CharmMenu(InputAction.CallbackContext context)
    {
        if (!charmMenu.activeSelf)
        {
            charmMenu.SetActive(true);
        }
        else
        {
            charmMenu.SetActive(false);
        }
    }

    public void RefreshAllLayerPositions()
    {
        float[] cumulativeOccupiedShift = new float[layers.Length];
        float runningShift = 0f;

        for (int i = 0; i < layers.Length; i++)
        {
            cumulativeOccupiedShift[i] = runningShift;
            runningShift += layers[i].GetOccupiedShift();
        }

        for (int i = 0; i < layers.Length; i++)
        {
            float extraShift = cumulativeOccupiedShift[i];

            if (_hoveredLayerIndex >= 0 && i > _hoveredLayerIndex)
                extraShift += layers[_hoveredLayerIndex].hoverShiftAmount;

            layers[i].SetTargetY(extraShift);
        }
    }

    public InroLayer GetNearestAvailableLayer(Vector3 dropPosition)
    {
        InroLayer nearest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (InroLayer layer in layers)
        {
            if (layer.layerIndex == 4) continue;
            if (layer.IsOccupied()) continue;

            Vector3? slotPos = layer.GetSlotWorldPosition();
            if (slotPos == null) continue;

            float distance = Vector3.Distance(dropPosition, slotPos.Value);
            if (distance < layer.snapDistance && distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = layer;
            }
        }

        return nearest;
    }
}