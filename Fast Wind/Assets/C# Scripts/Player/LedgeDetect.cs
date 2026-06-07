using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LedgeDetect : MonoBehaviour
{
    [SerializeField] float radius = 0.18f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float wallCheckDistance = 0.15f;
    [SerializeField] float clearAboveDistance = 0.2f;

    public bool HasLedge { get; private set; }
    public Vector2 LedgePoint { get; private set; }

    private PlayerController _player;
    private Collider2D _collider;

    private void Awake()
    {
        _player = GetComponentInParent<PlayerController>();
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    private void FixedUpdate()
    {
        HasLedge = false;

        if (_player == null || _player.IsLedgeClimbing)
            return;

        if (_player.IsGrounded)
            return;

        int facing = _player.FacingSign;
        Vector2 probePosition = transform.position;

        if (!Physics2D.OverlapCircle(probePosition, radius, groundLayer))
            return;

        Vector2 wallOrigin = probePosition + Vector2.up * 0.05f;
        if (!Physics2D.Raycast(wallOrigin, Vector2.right * facing, wallCheckDistance, groundLayer))
            return;

        Vector2 aboveProbe = probePosition + Vector2.up * clearAboveDistance;
        if (Physics2D.OverlapCircle(aboveProbe, radius * 0.45f, groundLayer))
            return;

        if (probePosition.y < _player.FeetPosition.y + 0.05f)
            return;

        HasLedge = true;
        LedgePoint = probePosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (_player == null)
            _player = GetComponentInParent<PlayerController>();

        int facing = _player != null ? _player.FacingSign : 1;
        Gizmos.color = HasLedge ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        Vector2 wallOrigin = (Vector2)transform.position + Vector2.up * 0.05f;
        Gizmos.DrawLine(wallOrigin, wallOrigin + Vector2.right * facing * wallCheckDistance);

        Vector2 aboveProbe = (Vector2)transform.position + Vector2.up * clearAboveDistance;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(aboveProbe, radius * 0.45f);
    }
}
