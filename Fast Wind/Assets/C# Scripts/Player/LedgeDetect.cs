using UnityEngine;

public struct LedgeGrabInfo
{
    public bool IsValid;
    public Vector2 LedgeTop;
    public Vector2 WallNormal;
}

[RequireComponent(typeof(Collider2D))]
[DefaultExecutionOrder(-50)]
public class LedgeDetect : MonoBehaviour
{
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float wallCheckDistance = 0.35f;
    [SerializeField] float lipSearchInset = 0.08f;
    [SerializeField] float minLipAboveFeet = 0.12f;
    [SerializeField] float maxLipAboveFeet = 2.4f;
    [SerializeField] float headClearance = 0.35f;
    [SerializeField] float grabMemoryDuration = 0.15f;
    [SerializeField] bool requireInputTowardWall = true;

    public LedgeGrabInfo CurrentGrab { get; private set; }

    private PlayerController _player;
    private BoxCollider2D _playerCollider;
    private LedgeGrabInfo _bufferedGrab;
    private float _grabValidUntil;

    private static readonly RaycastHit2D[] LipHits = new RaycastHit2D[8];

    private void Awake()
    {
        _player = GetComponentInParent<PlayerController>();
        _playerCollider = _player != null ? _player.GetComponent<BoxCollider2D>() : null;

        Collider2D probe = GetComponent<Collider2D>();
        if (probe != null)
            probe.isTrigger = true;
    }

    public void RefreshDetection()
    {
        if (_player != null && _player.IsInClimbMode)
        {
            ClearBufferedGrab();
            return;
        }

        if (TryDetectLedge(out LedgeGrabInfo grab))
        {
            CurrentGrab = grab;
            return;
        }

        if (Time.time <= _grabValidUntil && _bufferedGrab.IsValid)
        {
            // Buffer must respect the same blocks as live detection (added for ladder work — was missing)
            if (_player.IsInClimbMode || _player.IsTouchingClimbable())
            {
                ClearBufferedGrab();
                return;
            }

            CurrentGrab = _bufferedGrab;
            return;
        }

        CurrentGrab = default;
    }

    public void ClearBufferedGrab()
    {
        CurrentGrab = default;
        _bufferedGrab = default;
        _grabValidUntil = 0f;
    }

    private bool TryDetectLedge(out LedgeGrabInfo grab)
    {
        grab = default;

        if (_player == null || _playerCollider == null)
            return false;

        if (_player.IsInClimbMode || _player.IsGrounded)
            return false;

        if (_player.IsTouchingClimbable())
            return false;

        int facing = _player.FacingSign;

        if (requireInputTowardWall
            && !_player.IsMovingToward(facing)
            && !_player.IsTouchingWallFacing(facing))
            return false;

        Bounds bounds = _playerCollider.bounds;
        float skin = 0.03f;
        Vector2 castDir = Vector2.right * facing;

        float lowY = bounds.min.y + bounds.size.y * 0.22f;
        Vector2 lowOrigin = GetForwardOrigin(bounds, facing, skin, lowY);

        RaycastHit2D lowWallHit = Physics2D.Raycast(lowOrigin, castDir, wallCheckDistance, groundLayer);
        if (!IsValidSurfaceHit(lowWallHit))
            return false;

        if (!TryFindLedgeTop(lowWallHit, facing, bounds, out Vector2 ledgeTop, out Vector2 wallNormal))
            return false;

        float feetY = _player.FeetPosition.y;
        if (ledgeTop.y < feetY + minLipAboveFeet)
            return false;

        if (ledgeTop.y > feetY + maxLipAboveFeet)
            return false;

        Vector2 headCheckOrigin = new Vector2(
            GetForwardOrigin(bounds, facing, skin, ledgeTop.y + headClearance).x,
            ledgeTop.y + headClearance
        );

        RaycastHit2D headBlock = Physics2D.Raycast(headCheckOrigin, castDir, wallCheckDistance, groundLayer);
        if (IsValidSurfaceHit(headBlock))
            return false;

        grab = new LedgeGrabInfo
        {
            IsValid = true,
            LedgeTop = ledgeTop,
            WallNormal = wallNormal
        };

        _bufferedGrab = grab;
        _grabValidUntil = Time.time + grabMemoryDuration;
        return true;
    }

    private bool TryFindLedgeTop(
        RaycastHit2D lowWallHit,
        int facing,
        Bounds bounds,
        out Vector2 ledgeTop,
        out Vector2 wallNormal)
    {
        ledgeTop = default;
        wallNormal = lowWallHit.normal;

        Vector2 castDir = Vector2.right * facing;
        float scanDepth = bounds.size.y + 1.2f;
        float bestY = float.MinValue;
        bool found = false;

        for (int i = 0; i < 5; i++)
        {
            float inset = lipSearchInset + i * 0.05f;
            Vector2 downStart = new Vector2(
                lowWallHit.point.x + castDir.x * inset,
                bounds.max.y + 0.25f
            );

            int hitCount = Physics2D.RaycastNonAlloc(
                downStart,
                Vector2.down,
                LipHits,
                scanDepth,
                groundLayer
            );

            for (int h = 0; h < hitCount; h++)
            {
                RaycastHit2D lipHit = LipHits[h];
                if (!IsValidSurfaceHit(lipHit))
                    continue;

                if (lipHit.normal.y < 0.65f)
                    continue;

                if (lipHit.point.y <= bestY)
                    continue;

                bestY = lipHit.point.y;
                ledgeTop = lipHit.point;
                wallNormal = lipHit.normal;
                found = true;
            }
        }

        return found;
    }

    private static Vector2 GetForwardOrigin(Bounds bounds, int facing, float skin, float y)
    {
        float x = facing == 1 ? bounds.max.x - skin : bounds.min.x + skin;
        return new Vector2(x, y);
    }

    private static bool IsValidSurfaceHit(RaycastHit2D hit)
    {
        return hit.collider != null && IsValidCollider(hit.collider);
    }

    private static bool IsValidCollider(Collider2D col)
    {
        if (col == null)
            return false;

        if (col.GetComponent<GrabbableObject>() != null)
            return false;

        if (col.GetComponentInParent<GrabbableObject>() != null)
            return false;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (_player == null)
            _player = GetComponentInParent<PlayerController>();

        if (_player == null)
            return;

        BoxCollider2D col = _player.GetComponent<BoxCollider2D>();
        if (col == null)
            return;

        int facing = _player.FacingSign;
        Bounds bounds = col.bounds;
        float skin = 0.03f;

        float lowY = bounds.min.y + bounds.size.y * 0.22f;
        Vector2 lowOrigin = GetForwardOrigin(bounds, facing, skin, lowY);
        Vector2 castDir = Vector2.right * facing;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(lowOrigin, lowOrigin + castDir * wallCheckDistance);

        Vector2 lipScanStart = new Vector2(
            lowOrigin.x + castDir.x * lipSearchInset,
            bounds.max.y + 0.25f
        );
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(lipScanStart, lipScanStart + Vector2.down * (bounds.size.y + 1.2f));

        if (CurrentGrab.IsValid)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(CurrentGrab.LedgeTop, 0.08f);
        }
    }
}
