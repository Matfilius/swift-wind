using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMotor : MonoBehaviour
{
    [Header("Facing")]
    [Tooltip("Ovo samo upaliti za enemyije koji su okrenuti desno na slici.")]
    [SerializeField] bool spriteFacesRight;

    [Header("Ground")]
    [SerializeField] LayerMask groundLayers;
    [SerializeField] float groundCheckDistance = 0.18f;
    [SerializeField] float wallCheckDistance = 0.28f;
    [SerializeField] float ledgeCheckDistance = 0.45f;

    Rigidbody2D _rb;
    Collider2D _body;
    float _desiredSpeed;
    float _lockMoveUntil;

    public int Facing { get; private set; } = -1;
    public bool IsGrounded { get; private set; }
    public bool HasWallAhead { get; private set; }
    public bool HasGroundAhead { get; private set; }
    public LayerMask GroundLayers => groundLayers;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _body = GetComponent<Collider2D>();
        _rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        if (groundLayers.value == 0)
            groundLayers = LayerMask.GetMask("Ground");

        Facing = FacingFromScale();
    }

    void Reset()
    {
        groundLayers = LayerMask.GetMask("Ground");
    }

    void FixedUpdate()
    {
        Probe();

        if (Time.time < _lockMoveUntil)
            return;

        _rb.linearVelocity = new Vector2(_desiredSpeed, _rb.linearVelocity.y);
    }

    public void Move(float speed)
    {
        _desiredSpeed = Facing * speed;
    }

    public void Stop()
    {
        _desiredSpeed = 0f;
    }

    public void Face(int worldDirection)
    {
        if (worldDirection == 0)
            return;

        Facing = worldDirection > 0 ? 1 : -1;
        Vector3 scale = transform.localScale;
        float magnitude = Mathf.Abs(scale.x);
        if (magnitude < 0.001f)
            magnitude = 1f;

        float scaleSign = spriteFacesRight ? Facing : -Facing;
        transform.localScale = new Vector3(magnitude * scaleSign, scale.y, scale.z);
    }

    public void Knockback(Vector2 velocity, float lockDuration)
    {
        _desiredSpeed = 0f;
        _lockMoveUntil = Time.time + lockDuration;
        _rb.linearVelocity = velocity;
    }

    void Probe()
    {
        if (_body == null || !_body.enabled)
        {
            IsGrounded = false;
            HasWallAhead = false;
            HasGroundAhead = false;
            return;
        }

        Bounds bounds = _body.bounds;
        Vector2 feet = new Vector2(bounds.center.x, bounds.min.y + 0.03f);
        IsGrounded = Physics2D.Raycast(feet, Vector2.down, groundCheckDistance, groundLayers);

        Vector2 forward = Vector2.right * Facing;
        float frontX = Facing > 0 ? bounds.max.x : bounds.min.x;
        Vector2 wallOrigin = new Vector2(frontX, bounds.center.y);
        HasWallAhead = Physics2D.Raycast(wallOrigin, forward, wallCheckDistance, groundLayers);

        Vector2 ledgeOrigin = new Vector2(frontX + Facing * ledgeCheckDistance, bounds.min.y + 0.05f);
        HasGroundAhead = Physics2D.Raycast(ledgeOrigin, Vector2.down, groundCheckDistance + 0.35f, groundLayers);
    }

    int FacingFromScale()
    {
        float sign = Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(sign, 0f))
            sign = 1f;
        if (!spriteFacesRight)
            sign = -sign;
        return sign >= 0f ? 1 : -1;
    }
}
