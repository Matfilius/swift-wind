using UnityEngine;

public class ClimbableDetector : MonoBehaviour
{
    [SerializeField] LayerMask climbableLayer;
    [SerializeField] Vector2 probeSize = new Vector2(0.35f, 0.8f);
    [SerializeField] Vector2 probeOffset = new Vector2(0f, 0.25f);
    [SerializeField] float maxHorizontalAlignDistance = 0.55f;
    [SerializeField] float columnProbeWidth = 0.45f;
    [SerializeField] float columnProbeMinHeight = 2.5f;

    private BoxCollider2D _playerCollider;
    private Transform _playerTransform;
    private Collider2D _currentZone;
    private float _snapColumnX;
    private bool _columnLockActive;
    private float _columnLockX;
    private bool _columnInZone;

    private static readonly Collider2D[] OverlapResults = new Collider2D[8];

    public bool IsInZone => _columnLockActive ? _columnInZone : _currentZone != null;
    public float LadderCenterX => _snapColumnX;

    private void Awake()
    {
        _playerCollider = GetComponentInParent<BoxCollider2D>();
        _playerTransform = _playerCollider != null ? _playerCollider.transform : transform;

        if (climbableLayer.value == 0)
            climbableLayer = LayerMask.GetMask("Climbable");
    }

    public void Refresh()
    {
        _columnLockActive = false;
        _currentZone = FindBestClimbable();
        UpdateSnapColumn();
    }

    /// <summary>
    /// While climbing, keep checking the locked ladder column with a tall probe instead of the torso probe.
    /// </summary>
    public void RefreshAtColumn(float columnX)
    {
        _columnLockActive = true;
        _columnLockX = columnX;
        _currentZone = FindBestClimbable();
        _columnInZone = OverlapsColumn(columnX);
        _snapColumnX = columnX;
    }

    private void UpdateSnapColumn()
    {
        if (_currentZone == null)
        {
            _snapColumnX = _playerTransform != null ? _playerTransform.position.x : 0f;
            return;
        }

        _snapColumnX = _currentZone.ClosestPoint(GetProbeCenter()).x;
    }

    private Vector2 GetProbeCenter()
    {
        if (_playerTransform == null)
            return transform.position;

        // Use transform X so flip/asymmetric collider offset does not shift the probe toward a wall
        float x = _playerTransform.position.x + probeOffset.x;
        float y = _playerTransform.position.y + probeOffset.y;
        if (_playerCollider != null)
            y = _playerCollider.bounds.center.y + probeOffset.y;

        return new Vector2(x, y);
    }

    private Vector2 GetColumnProbeSize()
    {
        if (_playerCollider != null)
        {
            float height = _playerCollider.bounds.size.y + 0.25f;
            return new Vector2(columnProbeWidth, Mathf.Max(height, columnProbeMinHeight));
        }

        return new Vector2(columnProbeWidth, columnProbeMinHeight);
    }

    private bool OverlapsColumn(float columnX)
    {
        if (climbableLayer.value == 0)
            return false;

        float y = _playerCollider != null
            ? _playerCollider.bounds.center.y
            : _playerTransform.position.y;

        Vector2 center = new Vector2(columnX, y);
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(climbableLayer);
        filter.useTriggers = true;

        int count = Physics2D.OverlapBox(
            center,
            GetColumnProbeSize(),
            0f,
            filter,
            OverlapResults
        );

        if (count <= 0)
            return false;

        float maxColumnOffset = columnProbeWidth * 0.55f;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = OverlapResults[i];
            if (col == null)
                continue;

            Vector2 closest = col.ClosestPoint(center);
            if (Mathf.Abs(closest.x - columnX) <= maxColumnOffset)
                return true;
        }

        return false;
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

            // ClosestPoint works per ladder column even when several columns share one composite collider
            Vector2 closest = col.ClosestPoint(probeCenter);
            float horizontalDist = Mathf.Abs(closest.x - probeCenter.x);
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

        if (_columnLockActive)
        {
            float y = _playerCollider != null
                ? _playerCollider.bounds.center.y
                : transform.position.y;
            Vector2 columnCenter = new Vector2(_columnLockX, y);
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            Gizmos.DrawCube(columnCenter, GetColumnProbeSize());
        }
    }
#endif
}
