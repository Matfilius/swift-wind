using UnityEngine;

/// <summary>
/// Tracks overlap with climbable trigger colliders (ladders, vines tilemap).
/// Uses a tight torso-sized probe so ground tiles / wide colliders are not mistaken for ladders.
/// </summary>
public class ClimbableDetector : MonoBehaviour
{
    [SerializeField] LayerMask climbableLayer;
    [SerializeField] Vector2 probeSize = new Vector2(0.35f, 0.8f);
    [SerializeField] Vector2 probeOffset = new Vector2(0f, 0.25f);
    [SerializeField] float maxHorizontalAlignDistance = 0.55f;

    private BoxCollider2D _playerCollider;
    private Collider2D _currentZone;

    private static readonly Collider2D[] OverlapResults = new Collider2D[8];

    public bool IsInZone => _currentZone != null;
    public float LadderCenterX => _currentZone != null ? _currentZone.bounds.center.x : 0f;

    private void Awake()
    {
        _playerCollider = GetComponentInParent<BoxCollider2D>();

        if (climbableLayer.value == 0)
            climbableLayer = LayerMask.GetMask("Climbable");
    }

    public void Refresh()
    {
        _currentZone = FindBestClimbable();
    }

    private Vector2 GetProbeCenter()
    {
        if (_playerCollider == null)
            return transform.position;

        Bounds bounds = _playerCollider.bounds;
        return bounds.center + new Vector3(probeOffset.x, probeOffset.y, 0f);
    }

    private Collider2D FindBestClimbable()
    {
        if (_playerCollider == null || climbableLayer.value == 0)
            return null;

        Vector2 probeCenter = GetProbeCenter();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(climbableLayer);
        filter.useTriggers = true;

        int count = Physics2D.OverlapBox(
            probeCenter,
            probeSize,
            0f,
            filter,
            OverlapResults
        );

        if (count <= 0)
            return null;

        Collider2D best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = OverlapResults[i];
            if (col == null)
                continue;

            float horizontalDist = Mathf.Abs(col.bounds.center.x - probeCenter.x);
            if (horizontalDist > maxHorizontalAlignDistance)
                continue;

            if (horizontalDist < bestDist)
            {
                bestDist = horizontalDist;
                best = col;
            }
        }

        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 center = GetProbeCenter();
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        Gizmos.DrawCube(center, probeSize);
    }
#endif
}
