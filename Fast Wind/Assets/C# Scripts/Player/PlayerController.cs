using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    private enum MovementState
    {
        Normal,
        Dashing,
        Rolling,
        LedgeClimbing
    }

    [Header("Player Component References")]
    [SerializeField] Transform groundCheck;

    [Header("Player Settings")]
    [SerializeField] float dashLenght = 30f;
    [SerializeField] float speed = 10f;
    [SerializeField] float jumpingPower = 20f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] GameObject afterImagePrefab;
    [SerializeField] float afterImageSpacing = 0.05f;
    [SerializeField] float rollSpeed = 20f;
    [SerializeField] float rollDuration = 0.4f;
    [SerializeField] float rollColliderHeightScale = 0.5f;
    [SerializeField] float rollShrinkDuration = 0.12f;
    [SerializeField] float rollGrowDuration = 0.12f;
    [SerializeField] float coyoteTime = 0.1f;
    float coyoteTimeCounter;
    [SerializeField] float jumpBufferTime = 0.1f;
    float jumpBufferCounter;
    [SerializeField] float jumpCutMultiplier = 0.6f;

    [Header("Collision")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.64f, 0.1f);
    [SerializeField] float wallDepenetrateDistance = 0.03f;

    [Header("Ledge Climb")]
    [SerializeField] Vector2 hangOffset = new Vector2(-0.77f, -1.17f);
    [SerializeField] Vector2 climbOverOffset = new Vector2(0.54f, 1.1f);
    [SerializeField] float ledgeClimbCooldown = 0.35f;

    public bool doubleJump { get; set; }

    public bool IsGrounded => CheckGrounded();
    public bool IsLedgeClimbing => _movementState == MovementState.LedgeClimbing;
    public bool IsFacingRight => _isFacingRight;
    public int FacingSign => _isFacingRight ? 1 : -1;
    public Rigidbody2D Rigidbody => _rb;
    public Vector2 FeetPosition => groundCheck != null ? groundCheck.position : (Vector2)transform.position;

    private Rigidbody2D _rb;
    private Animator _animator;
    private BoxCollider2D _playerCollider;
    private LedgeDetect _ledgeDetect;
    private Transform _ledgeCheckTransform;
    private SpriteRenderer _playerSR;

    private Vector3 _originalScale;
    private Vector2 _originalColliderSize;
    private Vector2 _originalColliderOffset;
    private float _colliderOffsetAbsX;
    private float _ledgeCheckAbsX;

    private MovementState _movementState = MovementState.Normal;
    private float _horizontalInput;
    private float _horizontal;
    private bool _isFacingRight = true;
    private bool _canGrabLedge = true;
    private bool _climbLock;
    private float _afterImageTimer;
    private Vector2 _climbOverPosition;
    private Vector2 _ledgeHangPosition;
    private Coroutine _ledgeCooldownRoutine;
    private float _rollVisualDrop;
    private float _savedGravityScale;
    private RigidbodyType2D _savedBodyType;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerCollider = GetComponent<BoxCollider2D>();
        _playerSR = GetComponent<SpriteRenderer>();
        _ledgeDetect = GetComponentInChildren<LedgeDetect>();

        _originalScale = transform.localScale;
        _originalColliderSize = _playerCollider.size;
        _originalColliderOffset = _playerCollider.offset;
        _colliderOffsetAbsX = Mathf.Abs(_originalColliderOffset.x);

        transform.localScale = new Vector3(
            Mathf.Abs(_originalScale.x),
            _originalScale.y,
            _originalScale.z
        );

        if (_ledgeDetect != null)
        {
            _ledgeCheckTransform = _ledgeDetect.transform;
            _ledgeCheckAbsX = Mathf.Abs(_ledgeCheckTransform.localPosition.x);
        }

        _isFacingRight = true;
        ApplyFacing();
    }

    private void Update()
    {
        _horizontalInput = _horizontal;
        Flip();
        TryStartLedgeClimb();
        jumpBufferCounter -= Time.deltaTime;
        if (IsGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        TryJump();


        if (_movementState == MovementState.LedgeClimbing)
            transform.position = _ledgeHangPosition;

        if (_movementState == MovementState.Dashing)
        {
            _afterImageTimer -= Time.deltaTime;
            if (_afterImageTimer <= 0f)
            {
                SpawnAfterImage();
                _afterImageTimer = afterImageSpacing;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_movementState == MovementState.LedgeClimbing)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _ledgeHangPosition;
            UpdateAnimatorVelocity();
            return;
        }

        if (_movementState == MovementState.Dashing)
        {
            UpdateAnimatorVelocity();
            return;
        }

        if (_movementState == MovementState.Rolling)
        {
            UpdateAnimatorVelocity();
            return;
        }

        _rb.linearVelocity = new Vector2(_horizontal * speed, _rb.linearVelocity.y);
        UpdateAnimatorVelocity();
    }

    private void UpdateAnimatorVelocity()
    {
        _animator.SetFloat("xVelocity", Math.Abs(_rb.linearVelocity.x));
        _animator.SetFloat("yVelocity", _rb.linearVelocity.y);
    }

    #region Input

    public void Move(InputAction.CallbackContext context)
    {
        _horizontal = context.ReadValue<Vector2>().x;

        if (context.canceled)
            _horizontal = 0f;
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (context.performed && _movementState == MovementState.Normal && IsGrounded)
            StartCoroutine(DashRoutine());
    }

    public void Roll(InputAction.CallbackContext context)
    {
        if (context.performed && _movementState == MovementState.Normal && IsGrounded)
            StartCoroutine(RollRoutine());
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime;  
            TryJump();                            
        }

        if (context.canceled)
            CutJump();
    }

    void CutJump()
    {
        if (_movementState != MovementState.Normal)
            return;

        if (_rb.linearVelocity.y > 0f)
        {
            _rb.linearVelocity = new Vector2(
                _rb.linearVelocity.x,
                _rb.linearVelocity.y * jumpCutMultiplier
            );
        }
    }

    void TryJump()
    {
        if (_movementState != MovementState.Normal)
            return;
        if (jumpBufferCounter <= 0f)
            return;
        if (IsGrounded || coyoteTimeCounter > 0f)
        {
            GroundJump();
            return;
        }

        if (doubleJump)
        {
            AirJump();
            return;
        }

    }

    void GroundJump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpingPower);
        _animator.SetBool("isJumping", true);
        doubleJump = true;          
        coyoteTimeCounter = 0f;      
        jumpBufferCounter = 0f;      
    }
    void AirJump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpingPower);
        _animator.SetBool("isJumping", true);
        doubleJump = false;          
        jumpBufferCounter = 0f;
    }

    #endregion

    #region Ledge Climb

    private void TryStartLedgeClimb()
    {
        if (_ledgeDetect == null || !_canGrabLedge || _climbLock || _movementState != MovementState.Normal)
            return;

        if (!_ledgeDetect.HasLedge)
            return;

        BeginLedgeClimb(_ledgeDetect.LedgePoint);
    }

    private void BeginLedgeClimb(Vector2 ledgePoint)
    {
        _canGrabLedge = false;
        _movementState = MovementState.LedgeClimbing;

        int facing = FacingSign;
        _ledgeHangPosition = ledgePoint + new Vector2(hangOffset.x * facing, hangOffset.y);
        _climbOverPosition = ledgePoint + new Vector2(climbOverOffset.x * facing, climbOverOffset.y);

        _savedGravityScale = _rb.gravityScale;
        _savedBodyType = _rb.bodyType;
        _rb.gravityScale = 0f;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        transform.position = _ledgeHangPosition;

        _animator.SetBool("canClimb", true);
    }

    public void LedgeClimbOver()
    {
        if (_movementState != MovementState.LedgeClimbing)
            return;

        _movementState = MovementState.Normal;
        _animator.SetBool("canClimb", false);
        transform.position = _climbOverPosition;
        _animator.SetBool("isJumping", false);

        _rb.bodyType = _savedBodyType;
        _rb.gravityScale = _savedGravityScale;
        _rb.linearVelocity = Vector2.zero;
        _climbLock = true;

        if (_ledgeCooldownRoutine != null)
            StopCoroutine(_ledgeCooldownRoutine);

        _ledgeCooldownRoutine = StartCoroutine(LedgeCooldownRoutine());
    }

    private IEnumerator LedgeCooldownRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        _climbLock = false;

        yield return new WaitForSeconds(ledgeClimbCooldown - 0.2f);
        _canGrabLedge = true;
        _ledgeCooldownRoutine = null;
    }

    #endregion

    #region Roll And Dash

    private IEnumerator RollRoutine()
    {
        _movementState = MovementState.Rolling;
        _animator.SetBool("roll", true);

        float targetHeight = _originalColliderSize.y * rollColliderHeightScale;
        float scaleY = Mathf.Abs(transform.localScale.y);

        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Player"),
            LayerMask.NameToLayer("Enemy"),
            true
        );

        yield return LerpRollColliderHeight(targetHeight, rollShrinkDuration, scaleY);

        float rollDirection = _isFacingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(rollDirection * rollSpeed, 0f);

        float moveDuration = Mathf.Max(0f, rollDuration - rollShrinkDuration - rollGrowDuration);
        yield return new WaitForSeconds(moveDuration);

        _rb.linearVelocity = Vector2.zero;
        yield return LerpRollColliderHeight(_originalColliderSize.y, rollGrowDuration, scaleY);

        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Player"),
            LayerMask.NameToLayer("Enemy"),
            false
        );

        _movementState = MovementState.Normal;
        _animator.SetBool("roll", false);
    }

    private IEnumerator LerpRollColliderHeight(float targetHeight, float duration, float scaleY)
    {
        float startHeight = _playerCollider.size.y;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            ApplyRollColliderHeight(targetHeight, scaleY);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float height = Mathf.Lerp(startHeight, targetHeight, t);
            ApplyRollColliderHeight(height, scaleY);
            yield return null;
        }

        ApplyRollColliderHeight(targetHeight, scaleY);
    }

    private void ApplyRollColliderHeight(float height, float scaleY)
    {
        transform.position += Vector3.up * _rollVisualDrop;

        float heightDelta = _originalColliderSize.y - height;
        float targetVisualDrop = heightDelta * 0.5f * scaleY;

        _playerCollider.size = new Vector2(_originalColliderSize.x, height);
        _playerCollider.offset = new Vector2(
            GetColliderOffsetX(),
            _originalColliderOffset.y - heightDelta * 0.5f
        );

        transform.position += Vector3.down * targetVisualDrop;
        _playerCollider.offset += Vector2.up * (targetVisualDrop / scaleY);

        _rollVisualDrop = targetVisualDrop;
        Physics2D.SyncTransforms();
    }

    private float GetColliderOffsetX()
    {
        return _colliderOffsetAbsX * FacingSign;
    }

    private IEnumerator DashRoutine()
    {
        _movementState = MovementState.Dashing;

        float dashDirection = _isFacingRight ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dashDirection * dashLenght, 0f);

        yield return new WaitForSeconds(dashDuration);

        _movementState = MovementState.Normal;
    }

    #endregion

    #region Collision And Ground

    private bool CheckGrounded()
    {
        return Physics2D.OverlapCapsule(
            groundCheck.position,
            groundCheckSize,
            CapsuleDirection2D.Horizontal,
            0f,
            groundLayer
        );
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) == 0)
            return;

        _animator.SetBool("isJumping", false);
        doubleJump = false;
        DepenetrateFromWalls(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) == 0)
            return;

        if (IsGrounded)
            _animator.SetBool("isJumping", false);

        DepenetrateFromWalls(collision);
    }

    private void DepenetrateFromWalls(Collision2D collision)
    {
        if (_movementState == MovementState.LedgeClimbing)
            return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) < 0.5f)
                continue;

            transform.position += (Vector3)(contact.normal * wallDepenetrateDistance);
        }

        Physics2D.SyncTransforms();
    }

    #endregion

    #region Facing And Effects

    private void Flip()
    {
        if (_movementState == MovementState.LedgeClimbing || _climbLock)
            return;

        if ((_isFacingRight && _horizontalInput < 0f) || (!_isFacingRight && _horizontalInput > 0f))
        {
            _isFacingRight = !_isFacingRight;
            ApplyFacing();
        }
    }

    private void ApplyFacing()
    {
        _playerSR.flipX = !_isFacingRight;

        _playerCollider.offset = new Vector2(
            GetColliderOffsetX(),
            _playerCollider.offset.y
        );

        if (_ledgeCheckTransform != null)
        {
            Vector3 ledgePos = _ledgeCheckTransform.localPosition;
            ledgePos.x = _ledgeCheckAbsX * (_isFacingRight ? 1f : -1f);
            _ledgeCheckTransform.localPosition = ledgePos;
        }
    }

    private void SpawnAfterImage()
    {
        GameObject obj = Instantiate(afterImagePrefab, transform.position, transform.rotation);
        SpriteRenderer afterImageRenderer = obj.GetComponent<SpriteRenderer>();

        afterImageRenderer.sprite = _playerSR.sprite;
        afterImageRenderer.flipX = _playerSR.flipX;
        obj.transform.localScale = transform.localScale * 1.05f;
        afterImageRenderer.color = new Color(0.3f, 0.8f, 1f, 0.4f);
    }

    #endregion
}
