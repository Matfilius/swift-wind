using System;
using System.Collections;
using System.Collections.Generic;
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
        LedgeClimbing,
        LadderClimbing
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
    [SerializeField] float groundCheckProbeLift = 0.04f;
    [SerializeField] float wallDepenetrateDistance = 0.03f;

    [Header("Ledge Climb")]
    [SerializeField] Vector2 hangFineTune;
    [SerializeField] Vector2 climbOverFineTune;
    [SerializeField] float ledgeClimbCooldown = 0.2f;
    [SerializeField] float mantleSnapPullBack = 0.12f;
    [SerializeField] float mantleStepForward = 0.1f;
    [SerializeField] float mantleHangVisualYOffset = -0.75f;
    [SerializeField] float mantleLandVisualYOffset = -1f;

    [Header("Wall Slide")]
    [SerializeField] Transform wallCheck;           
    [SerializeField] float wallCheckDistance = 0.1f;
    [SerializeField] float wallSlideSpeed = 2f;
    [SerializeField] bool requireInputForSlide = true;

    [Header("Ladder Climb")]
    [SerializeField] float climbSpeed = 6f;
    [SerializeField] float ladderSnapSpeed = 24f;
    [SerializeField] float ladderSnapXOffset = 0f;
    [SerializeField] float ladderDismountHorizontalSpeed = 10f;
    [SerializeField] float ladderDismountUpSpeed = 14f;
    [SerializeField] float ladderRegrabCooldown = 0.25f;
    [SerializeField] float ladderInputThreshold = 0.1f;
    [SerializeField] float ladderMountDismountGrace = 0.12f;

    bool _isTouchingWall;
    int _wallDirection;
    bool _isGrounded;

    public bool doubleJump { get; set; }

    public bool IsGrounded => _isGrounded;
    public bool IsLedgeClimbing => _movementState == MovementState.LedgeClimbing;
    public bool IsLadderClimbing => _movementState == MovementState.LadderClimbing;
    public bool IsInClimbMode => IsLedgeClimbing || IsLadderClimbing;
    public bool IsFacingRight => _isFacingRight;
    public int FacingSign => _isFacingRight ? 1 : -1;
    public Rigidbody2D Rigidbody => _rb;
    public Vector2 FeetPosition => groundCheck != null ? groundCheck.position : (Vector2)transform.position;

    public bool IsMovingToward(int direction)
    {
        if (direction == 0)
            return false;

        return direction > 0 ? _horizontal > 0.05f : _horizontal < -0.05f;
    }

    public bool IsTouchingWallFacing(int direction)
    {
        return _isTouchingWall && _wallDirection == direction;
    }

    public bool IsTouchingClimbable()
    {
        return _inClimbableZone;
    }

    private Rigidbody2D _rb;
    private Animator _animator;
    private BoxCollider2D _playerCollider;
    private LedgeDetect _ledgeDetect;
    private ClimbableDetector _climbableDetect;
    private Transform _ledgeCheckTransform;
    private Transform _wallCheckTransform;
    private SpriteRenderer _playerSR;

    private Vector3 _originalScale;
    private Vector2 _originalColliderSize;
    private Vector2 _originalColliderOffset;
    private float _colliderOffsetAbsX;
    private float _ledgeCheckAbsX;
    private float _wallCheckAbsX;

    private MovementState _movementState = MovementState.Normal;
    private float _horizontalInput;
    private float _horizontal;
    private float _climbInputY;
    private bool _inClimbableZone;
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
    private bool _colliderWasEnabled;
    private float _ladderSnapX;
    private float _ladderRegrabUntil;
    private float _ladderDismountAllowedAfter;
    private float _defaultGravityScale;
    private readonly Dictionary<int, float> _slowZones = new();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _defaultGravityScale = _rb.gravityScale;
        _animator = GetComponent<Animator>();
        _playerCollider = GetComponent<BoxCollider2D>();
        _playerSR = GetComponent<SpriteRenderer>();
        _ledgeDetect = GetComponentInChildren<LedgeDetect>();
        _climbableDetect = GetComponentInChildren<ClimbableDetector>();

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

        if (wallCheck != null)
        {
            _wallCheckTransform = wallCheck;
            _wallCheckAbsX = Mathf.Abs(_wallCheckTransform.localPosition.x);
        }

        _isFacingRight = true;
        ApplyFacing();
    }

    private void Start()
    {
        _movementState = MovementState.Normal;
        _rb.gravityScale = _defaultGravityScale;
        coyoteTimeCounter = coyoteTime;
        RefreshGrounded();
        RefreshClimbableZone();
    }

    private void Update()
    {
        RefreshGrounded();
        RefreshClimbableZone();

        _horizontalInput = _horizontal;
        Flip();
        jumpBufferCounter -= Time.deltaTime;
        if (_isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        TryJump();
        TryStartLadderClimb();

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
        RefreshGrounded();
        RefreshClimbableZone();

        if (_movementState == MovementState.LedgeClimbing)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _ledgeHangPosition;
            UpdateAnimatorVelocity();
            return;
        }

        if (_movementState == MovementState.LadderClimbing)
        {
            UpdateLadderClimb();
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

        CheckWalls();

        if (_ledgeDetect != null)
            _ledgeDetect.RefreshDetection();

        TryStartLedgeClimb();

        float targetX = _horizontal * speed * GetSpeedMultiplier();
        float targetY = _rb.linearVelocity.y;
        if (!IsTouchingClimbable())
        {
            int wallSide = _isTouchingWall ? _wallDirection : DetectAdjacentWallSide();
            if (wallSide != 0)
            {
                bool holdingTowardWall = (wallSide == 1 && _horizontal > 0f)
                    || (wallSide == -1 && _horizontal < 0f);

                if (holdingTowardWall)
                    targetX = 0f;

                if (!IsGrounded && _isTouchingWall)
                {
                    bool shouldSlide = requireInputForSlide ? holdingTowardWall : true;
                    if (shouldSlide && targetY < -wallSlideSpeed)
                        targetY = -wallSlideSpeed;
                }
            }
        }
        _rb.linearVelocity = new Vector2(targetX, targetY);
        UpdateAnimatorVelocity();
    }

    private void UpdateAnimatorVelocity()
    {
        _animator.SetFloat("xVelocity", Math.Abs(_rb.linearVelocity.x));
        _animator.SetFloat("yVelocity", _rb.linearVelocity.y);
    }

    #region Slow Effects

    public void ApplySlow(int sourceId, float multiplier)
    {
        _slowZones[sourceId] = multiplier;
    }

    public void RemoveSlow(int sourceId)
    {
        _slowZones.Remove(sourceId);
    }

    float GetSpeedMultiplier()
    {
        if (_slowZones.Count == 0)
            return 1f;

        float min = 1f;
        foreach (float multiplier in _slowZones.Values)
            min = Mathf.Min(min, multiplier);

        return min;
    }

    #endregion

    #region Input

    public void Move(InputAction.CallbackContext context)
    {
        Vector2 move = context.ReadValue<Vector2>();
        _horizontal = move.x;

        if (_movementState == MovementState.LadderClimbing || _inClimbableZone)
            _climbInputY = move.y;
        else
            _climbInputY = 0f;

        if (context.canceled)
        {
            _horizontal = 0f;
            _climbInputY = 0f;
        }
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
        if (_movementState == MovementState.LadderClimbing)
            return;

        if (context.started)
        {
            jumpBufferCounter = jumpBufferTime;
            TryJump();
        }

        if (context.canceled)
        {
            jumpBufferCounter = 0f;
            CutJump();
        }
    }

    void CutJump()
    {
        if (_movementState != MovementState.Normal || IsGrounded)
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

        if (_isGrounded || coyoteTimeCounter > 0f)
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

    private void CheckWalls()
    {
        _isTouchingWall = false;
        _wallDirection = 0;

        if (IsInClimbMode || _inClimbableZone)
            return;

        Bounds bounds = _playerCollider.bounds;
        float skin = 0.02f;
        float castY = wallCheck != null ? wallCheck.position.y : bounds.center.y;

        Vector2 rightOrigin = new Vector2(bounds.max.x - skin, castY);
        Vector2 leftOrigin = new Vector2(bounds.min.x + skin, castY);

        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.right, wallCheckDistance, groundLayer);
        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.left, wallCheckDistance, groundLayer);

        if (rightHit.collider != null)
        {
            _isTouchingWall = true;
            _wallDirection = 1;
        }
        else if (leftHit.collider != null)
        {
            _isTouchingWall = true;
            _wallDirection = -1;
        }
    }

    #endregion

    #region Ladder Climb

    private void TryStartLadderClimb()
    {
        if (_movementState != MovementState.Normal || _climbableDetect == null)
            return;

        if (Time.time < _ladderRegrabUntil)
            return;

        if (!_inClimbableZone)
            return;

        if (Mathf.Abs(_climbInputY) < ladderInputThreshold)
            return;

        if (_isGrounded && _climbInputY > -ladderInputThreshold)
            return;

        if (_rb.linearVelocity.y > 0.5f)
            return;

        BeginLadderClimb(_climbableDetect.LadderCenterX);
    }

    private void BeginLadderClimb(float snapX)
    {
        _movementState = MovementState.LadderClimbing;
        _ladderSnapX = snapX + ladderSnapXOffset;
        _ladderDismountAllowedAfter = Time.time + ladderMountDismountGrace;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        jumpBufferCounter = 0f;
        _isTouchingWall = false;
        _wallDirection = 0;

        if (_ledgeDetect != null)
            _ledgeDetect.ClearBufferedGrab();
    }

    private void UpdateLadderClimb()
    {
        if (_climbableDetect == null)
        {
            EndLadderClimb();
            return;
        }

        if (Mathf.Abs(_horizontal) > ladderInputThreshold
            && Time.time >= _ladderDismountAllowedAfter)
        {
            int dir = _horizontal > 0f ? 1 : -1;
            if (TryDismountLadderHorizontal(dir))
                return;
        }

        if (!_inClimbableZone)
        {
            if (_isGrounded)
                EndLadderClimb();
            else
                DismountLadder(Vector2.zero);
            return;
        }

        float snapX = Mathf.MoveTowards(
            transform.position.x,
            _ladderSnapX,
            ladderSnapSpeed * Time.fixedDeltaTime
        );

        float climbY = Mathf.Abs(_climbInputY) >= ladderInputThreshold
            ? _climbInputY * climbSpeed
            : 0f;

        float newY = transform.position.y + climbY * Time.fixedDeltaTime;
        transform.position = new Vector3(snapX, newY, transform.position.z);
        _rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();

        _animator.SetFloat("xVelocity", 0f);
        _animator.SetFloat("yVelocity", climbY);
    }

    private bool TryDismountLadderHorizontal(int dir)
    {
        int wallSide = DetectAdjacentWallSide();
        if (wallSide != 0 && dir == wallSide)
            return false;

        if (_isFacingRight != dir > 0)
        {
            _isFacingRight = dir > 0;
            ApplyFacing();
        }

        DismountLadder(new Vector2(
            dir * ladderDismountHorizontalSpeed,
            ladderDismountUpSpeed * 0.5f
        ));
        return true;
    }

    private int DetectAdjacentWallSide()
    {
        Bounds bounds = _playerCollider.bounds;
        float castY = wallCheck != null ? wallCheck.position.y : bounds.center.y;
        Vector2 center = new Vector2(bounds.center.x, castY);
        float reach = bounds.extents.x + wallCheckDistance;

        RaycastHit2D leftHit = Physics2D.Raycast(center, Vector2.left, reach, groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(center, Vector2.right, reach, groundLayer);

        float leftDist = leftHit.collider != null ? leftHit.distance : float.PositiveInfinity;
        float rightDist = rightHit.collider != null ? rightHit.distance : float.PositiveInfinity;


        const float overlapProbeRadius = 0.06f;
        float probeOffset = bounds.extents.x * 0.75f;
        if (Physics2D.OverlapCircle(center + Vector2.left * probeOffset, overlapProbeRadius, groundLayer))
            leftDist = Mathf.Min(leftDist, 0f);
        if (Physics2D.OverlapCircle(center + Vector2.right * probeOffset, overlapProbeRadius, groundLayer))
            rightDist = Mathf.Min(rightDist, 0f);

        if (float.IsPositiveInfinity(leftDist) && float.IsPositiveInfinity(rightDist))
            return 0;

        return leftDist <= rightDist ? -1 : 1;
    }

    private void DismountLadder(Vector2 velocity)
    {
        EndLadderClimb();
        _rb.linearVelocity = velocity;
        if (velocity.sqrMagnitude > 0.01f)
        {
            _animator.SetBool("isJumping", true);
            doubleJump = true;
        }
        _ladderRegrabUntil = Time.time + ladderRegrabCooldown;
    }

    private void EndLadderClimb()
    {
        if (_movementState != MovementState.LadderClimbing)
            return;

        _movementState = MovementState.Normal;
        _rb.gravityScale = _defaultGravityScale;
    }

    #endregion

    #region Ledge Climb

    private void TryStartLedgeClimb()
    {
        if (_ledgeDetect == null || !_canGrabLedge || _climbLock || _movementState != MovementState.Normal)
            return;

        if (_inClimbableZone)
            return;

        if (!_ledgeDetect.CurrentGrab.IsValid)
            return;

        BeginLedgeClimb(_ledgeDetect.CurrentGrab);
    }

    public bool TryGetMantleHangPosition(Vector2 ledgeTop, int facing, out Vector2 hangPosition)
    {
        ComputeMantlePositions(ledgeTop, facing, out hangPosition, out _);
        return true;
    }

    private void BeginLedgeClimb(LedgeGrabInfo grab)
    {
        _canGrabLedge = false;
        _movementState = MovementState.LedgeClimbing;

        int facing = FacingSign;
        ComputeMantlePositions(grab.LedgeTop, facing, out _ledgeHangPosition, out _climbOverPosition);

        _savedGravityScale = _rb.gravityScale;
        _savedBodyType = _rb.bodyType;
        _colliderWasEnabled = _playerCollider.enabled;

        _rb.gravityScale = 0f;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;

        _playerCollider.enabled = false;
        transform.position = _ledgeHangPosition;
        Physics2D.SyncTransforms();

        _animator.SetBool("canClimb", true);
    }

    private void ComputeMantlePositions(Vector2 ledgeTop, int facing, out Vector2 hang, out Vector2 climbOver)
    {
        Bounds bounds = _playerCollider.bounds;
        float halfWidth = bounds.extents.x;

        float handOffsetY = _ledgeCheckTransform != null
            ? _ledgeCheckTransform.position.y - transform.position.y
            : bounds.extents.y * 0.35f;

        float feetOffsetY = FeetPosition.y - transform.position.y;

        hang = new Vector2(
            ledgeTop.x - facing * (halfWidth + mantleSnapPullBack) + hangFineTune.x * facing,
            ledgeTop.y - handOffsetY + hangFineTune.y + mantleHangVisualYOffset
        );

        climbOver = new Vector2(
            ledgeTop.x + facing * (halfWidth * 0.55f + mantleStepForward) + climbOverFineTune.x * facing,
            ledgeTop.y - feetOffsetY + climbOverFineTune.y + mantleLandVisualYOffset
        );
    }

    public void LedgeClimbOver()
    {
        if (_movementState != MovementState.LedgeClimbing)
            return;

        _movementState = MovementState.Normal;
        _animator.SetBool("canClimb", false);
        transform.position = _climbOverPosition;
        _animator.SetBool("isJumping", false);

        _playerCollider.enabled = _colliderWasEnabled;
        Physics2D.SyncTransforms();

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
        yield return new WaitForSeconds(0.1f);
        _climbLock = false;

        yield return new WaitForSeconds(ledgeClimbCooldown);
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

    private void RefreshClimbableZone()
    {
        if (_climbableDetect == null)
        {
            _inClimbableZone = false;
            return;
        }

        if (_movementState == MovementState.LadderClimbing)
            _climbableDetect.RefreshAtColumn(_ladderSnapX);
        else
            _climbableDetect.Refresh();

        _inClimbableZone = _climbableDetect.IsInZone;
    }

    private void RefreshGrounded()
    {
        _isGrounded = CheckGrounded();
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCapsule(
            GetGroundCheckProbeCenter(),
            groundCheckSize,
            CapsuleDirection2D.Horizontal,
            0f,
            groundLayer
        );
    }

    public bool IsRigidbodyOverlappingGroundCheck(Rigidbody2D rb)
    {
        if (rb == null)
            return false;

        foreach (Collider2D col in rb.GetComponentsInChildren<Collider2D>())
        {
            if (IsColliderOverlappingGroundCheck(col))
                return true;
        }

        return false;
    }

    public bool IsColliderOverlappingGroundCheck(Collider2D collider)
    {
        if (groundCheck == null || collider == null)
            return false;

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = true;

        Collider2D[] results = new Collider2D[16];
        int count = Physics2D.OverlapCapsule(
            GetGroundCheckProbeCenter(),
            groundCheckSize,
            CapsuleDirection2D.Horizontal,
            0f,
            filter,
            results
        );

        for (int i = 0; i < count; i++)
        {
            if (results[i] == collider)
                return true;
        }

        return false;
    }

    private Vector2 GetGroundCheckProbeCenter()
    {
        return (Vector2)groundCheck.position + Vector2.up * groundCheckProbeLift;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) == 0)
            return;

        if (IsLandingCollision(collision))
        {
            _animator.SetBool("isJumping", false);
            doubleJump = false;
        }

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

    private bool IsLandingCollision(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
                return true;
        }

        return IsGrounded;
    }

    private void DepenetrateFromWalls(Collision2D collision)
    {
        if (_movementState == MovementState.LedgeClimbing || _movementState == MovementState.LadderClimbing)
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
        if (_movementState == MovementState.LedgeClimbing
            || _movementState == MovementState.LadderClimbing
            || _climbLock)
            return;

        if ((_isFacingRight && _horizontalInput < 0f) || (!_isFacingRight && _horizontalInput > 0f))
        {
            _isFacingRight = !_isFacingRight;
            ApplyFacing();
        }
    }

    private void ApplyFacing()
    {
        float prevOffsetX = _playerCollider.offset.x;

        _playerSR.flipX = !_isFacingRight;

        float newOffsetX = GetColliderOffsetX();
        _playerCollider.offset = new Vector2(newOffsetX, _playerCollider.offset.y);

        // Offset sign flip teleports the collider; nudge transform so it stays put when near a wall
        if (DetectAdjacentWallSide() != 0)
        {
            transform.position += new Vector3(
                -(newOffsetX - prevOffsetX) * Mathf.Abs(transform.localScale.x),
                0f,
                0f);
            Physics2D.SyncTransforms();
        }

        if (_ledgeCheckTransform != null)
        {
            Vector3 ledgePos = _ledgeCheckTransform.localPosition;
            ledgePos.x = _ledgeCheckAbsX * (_isFacingRight ? 1f : -1f);
            _ledgeCheckTransform.localPosition = ledgePos;
        }

        if (_wallCheckTransform != null)
        {
            Vector3 wallPos = _wallCheckTransform.localPosition;
            wallPos.x = _wallCheckAbsX * (_isFacingRight ? 1f : -1f);
            _wallCheckTransform.localPosition = wallPos;
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
