using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour, IDataPersistence
{
    private enum MovementState
    {
        Normal,
        Dashing,
        Rolling,
        LedgeClimbing,
        LadderClimbing,
        WallSliding,
        WallSlideLanding
    }

    private enum WallSlidePhase
    {
        Intro,
        Loop
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

    [Header("Attacking")]
    [SerializeField] Transform attackCheck;
    [SerializeField] LayerMask attackHitLayers;
    [SerializeField] float attackDamage = 20f;
    [SerializeField] float comboBufferTime = 0.25f;

    [Header("Ledge Climb")]
    [SerializeField] Vector2 hangFineTune;
    [SerializeField] Vector2 climbOverFineTune;
    [SerializeField] float ledgeClimbCooldown = 0.2f;
    [SerializeField] float ledgeMinJumpAge = 0.5f;
    [SerializeField] float mantleSnapPullBack = 0.12f;
    [SerializeField] float mantleStepForward = 0.1f;
    [SerializeField] float mantleHangVisualYOffset = -0.75f;
    [SerializeField] float mantleLandVisualYOffset = -1f;

    [Header("Wall Slide")]
    [SerializeField] Transform wallCheck;           
    [SerializeField] float wallCheckDistance = 0.1f;
    [SerializeField] float wallSlideSpeed = 2f;
    [SerializeField] bool requireInputForSlide = true;
    [SerializeField] float wallSlideSnapOffset = 0.06f;
    [SerializeField] float wallSlideLandingGroundDistance = 0.4f;
    [SerializeField] float wallSlideGroundProbeWallOffset = 0.12f;
    [SerializeField] float wallSlideFloorNormalMin = 0.5f;
    [Header("Wall Slide Animation")]
    [SerializeField] float wallSlideIntroClipLength = 0.45f;
    [SerializeField] float wallSlideIntroAnimSpeed = 0.7f;
    [SerializeField] float wallSlideLoopAnimSpeed = 0.8f;
    [SerializeField] float wallSlideLandingClipLength = 0.267f;
    [SerializeField] float wallSlideLandingAnimSpeed = 0.5f;
    [SerializeField] int wallSlideWallMissLimit = 6;
    [SerializeField] float wallSlideReentryCooldown = 0.3f;
    [SerializeField] float wallSlideJumpReentryCooldown = 0.8f;
    [SerializeField] float wallSlideDoubleJumpReentryCooldown = 1.4f;
    [SerializeField] float wallSlideDismountHoldTime = 0.2f;
    [SerializeField] float wallSlideDismountInputThreshold = 0.1f;
    [SerializeField] float wallSlideJumpAwayMultiplier = 0.6f;
    [SerializeField] float wallSlideMinJumpAge = 0.2f;
    [SerializeField] float wallSlideMinStartGroundClearance = 1.5f;
    [SerializeField] float wallSlideMinDurationForLanding = 0.35f;

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
    public bool IsWallSliding => _movementState == MovementState.WallSliding
        || _movementState == MovementState.WallSlideLanding;
    public bool IsInClimbMode => IsLedgeClimbing || IsLadderClimbing;
    public bool IsFacingRight => _isFacingRight;
    public int FacingSign => _isFacingRight ? 1 : -1;
    public Rigidbody2D Rigidbody => _rb;
    public Vector2 FeetPosition => groundCheck != null ? groundCheck.position : (Vector2)transform.position;

    public bool IsGrabbableWindLiftBlocked(Rigidbody2D rb)
    {
        return rb != null && _launchedFromGrabbable != null && _launchedFromGrabbable == rb;
    }

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
    private Transform _attackCheckTransform;
    private Collider2D _attackCheckCollider;
    private SpriteRenderer _playerSR;

    private Vector3 _originalScale;
    private Vector2 _originalColliderSize;
    private Vector2 _originalColliderOffset;
    private float _colliderOffsetAbsX;
    private float _ledgeCheckAbsX;
    private float _wallCheckAbsX;
    private float _attackCheckAbsX;
    private float _attackCheckColliderOffsetAbsX;

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
    private WallSlidePhase _wallSlidePhase;
    private int _wallSlideWallSide;
    private int _wallSlideWallMissCount;
    private float _flipBlockedUntil;
    private float _wallSlideReentryBlockedUntil;
    private float _wallSlideDismountTimer;
    private bool _postWallSlideJumpDoubleJumpPending;
    private float _lastJumpTime = -999f;
    private float _wallSlideStartTime;
    private Coroutine _wallSlideLandingRoutine;
    private Rigidbody2D _lastGrabbableStoodOn;
    private Rigidbody2D _launchedFromGrabbable;

    private static readonly int WallSlideIntroHash = Animator.StringToHash("WallSlideIntro");
    private static readonly int WallSlideLoopHash = Animator.StringToHash("WallSlideLoop");
    private static readonly int SuddenAttack1Hash = Animator.StringToHash("Sudden Attack 1");
    private static readonly int SuddenAttack2Hash = Animator.StringToHash("Sudden Attack 2");
    private static readonly int SuddenAttack3Hash = Animator.StringToHash("Sudden Attack 3");
    private static readonly int SuddenAttack4Hash = Animator.StringToHash("Sudden Attack 4");
    private static readonly int AttackButtonHash = Animator.StringToHash("Attack_button");
    private static readonly int ComboQueuedHash = Animator.StringToHash("comboQueued");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int MovementHash = Animator.StringToHash("Movement");
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");

    private enum AttackStep
    {
        None,
        Unsheathe,
        SwingRight,
        SwingLeft,
        Sheathe
    }

    private AttackStep _attackStep;
    private AttackStep _lastSwingStep;
    private bool _comboQueued;
    private bool _restartQueued;
    private bool _attackStepAdvanced;
    private bool _attackActive;
    private bool _comboBuffering;
    private float _comboBufferUntil;
    private float _sheatheStartedTime;
    private int _hazardsLayer;
    private bool _ignoredHazardCollision;
    private readonly Dictionary<int, float> _slowZones = new();
    private readonly HashSet<Enemy_Damage> _hitEnemiesThisStrike = new();
    private readonly Collider2D[] _attackOverlapResults = new Collider2D[16];
    private int _trackedAttackStateHash;

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

        if (attackCheck != null)
        {
            _attackCheckTransform = attackCheck;
            _attackCheckAbsX = Mathf.Abs(_attackCheckTransform.localPosition.x);
            _attackCheckCollider = attackCheck.GetComponent<Collider2D>();
            if (_attackCheckCollider != null)
            {
                _attackCheckColliderOffsetAbsX = Mathf.Abs(_attackCheckCollider.offset.x);
                _attackCheckCollider.isTrigger = true;
                _attackCheckCollider.enabled = false;
            }
        }

        if (attackHitLayers.value == 0)
            attackHitLayers = 1 << LayerMask.NameToLayer("Enemy");

        _isFacingRight = true;
        ApplyFacing();

        _hazardsLayer = LayerMask.NameToLayer("Hazzards");
    }

    private void Start()
    {
        _movementState = MovementState.Normal;
        _rb.gravityScale = _defaultGravityScale;
        coyoteTimeCounter = coyoteTime;
        RefreshGrounded();
        RefreshGrabbableWindBlock();
        RefreshClimbableZone();
    }

    private void Update()
    {
        RefreshGrounded();
        RefreshGrabbableWindBlock();
        RefreshClimbableZone();
        RefreshDoubleJump();

        _horizontalInput = _horizontal;
        TryJump();
        Flip();
        jumpBufferCounter -= Time.deltaTime;
        if (_isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            _postWallSlideJumpDoubleJumpPending = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        TryStartLadderClimb();
        UpdateAttackState();

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
        RefreshGrabbableWindBlock();
        RefreshClimbableZone();
        RefreshDoubleJump();

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

        if (_movementState == MovementState.WallSlideLanding)
        {
            _rb.linearVelocity = Vector2.zero;
            UpdateAnimatorVelocity();
            return;
        }

        if (_movementState == MovementState.WallSliding)
        {
            UpdateWallSlide();
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

        if (_movementState == MovementState.Normal && ShouldStartWallSlide())
            BeginWallSlide();

        float targetX = IsGroundAttackLocked() ? 0f : _horizontal * speed * GetSpeedMultiplier();
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
            }
        }
        _rb.linearVelocity = new Vector2(targetX, targetY);
        UpdateAnimatorVelocity();
    }

    public void LoadData(GameData data)
    {
        if (string.IsNullOrEmpty(data.lastCheckpointId))
            return;

        transform.position = data.playerPosition;
    }

    public void SaveData(ref GameData data)
    {
    }

    private void UpdateAnimatorVelocity()
    {
        _animator.SetFloat("xVelocity", Math.Abs(_rb.linearVelocity.x));
        _animator.SetFloat("yVelocity", _rb.linearVelocity.y);
    }

    private bool IsAttacking()
    {
        return _attackActive || IsInAnyAttackState();
    }

    private bool IsInAnyAttackState()
    {
        return IsInAttackState(SuddenAttack1Hash)
            || IsInAttackState(SuddenAttack2Hash)
            || IsInAttackState(SuddenAttack3Hash)
            || IsInAttackState(SuddenAttack4Hash);
    }

    private int GetCurrentAttackStateHash()
    {
        if (IsInAttackState(SuddenAttack1Hash))
            return SuddenAttack1Hash;
        if (IsInAttackState(SuddenAttack2Hash))
            return SuddenAttack2Hash;
        if (IsInAttackState(SuddenAttack3Hash))
            return SuddenAttack3Hash;
        if (IsInAttackState(SuddenAttack4Hash))
            return SuddenAttack4Hash;
        return 0;
    }

    private static int GetAttackStateHash(AttackStep step)
    {
        return step switch
        {
            AttackStep.Unsheathe => SuddenAttack1Hash,
            AttackStep.SwingRight => SuddenAttack2Hash,
            AttackStep.SwingLeft => SuddenAttack3Hash,
            AttackStep.Sheathe => SuddenAttack4Hash,
            _ => 0
        };
    }

    private bool IsGroundAttackLocked()
    {
        return IsAttacking() && _isGrounded;
    }

    private bool IsInAttackState(int stateHash)
    {
        if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash)
            return true;

        return _animator.IsInTransition(0)
            && _animator.GetNextAnimatorStateInfo(0).shortNameHash == stateHash;
    }

    private void UpdateAttackState()
    {
        int currentAttackHash = GetCurrentAttackStateHash();
        if (currentAttackHash != 0 && currentAttackHash != _trackedAttackStateHash)
            _hitEnemiesThisStrike.Clear();
        _trackedAttackStateHash = currentAttackHash;

        _animator.SetBool(IsAttackingHash, _attackActive);

        if (!_attackActive || _attackStep == AttackStep.None)
            return;

        if (_comboBuffering)
        {
            UpdateComboBuffer();
            return;
        }

        if (_animator.IsInTransition(0))
            return;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != GetAttackStateHash(_attackStep))
            return;

        if (_attackStepAdvanced || state.normalizedTime < 0.95f)
            return;

        _attackStepAdvanced = true;
        AdvanceAttackCombo();
    }

    private void BeginAttack()
    {
        _attackActive = true;
        _comboQueued = false;
        _restartQueued = false;
        _comboBuffering = false;
        _hitEnemiesThisStrike.Clear();
        PlayAttackStep(AttackStep.Unsheathe);

        if (_isGrounded)
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    private void PlayAttackStep(AttackStep step)
    {
        _attackStep = step;
        _attackStepAdvanced = false;
        _comboBuffering = false;
        _attackActive = true;
        if (step == AttackStep.SwingRight || step == AttackStep.SwingLeft)
            _lastSwingStep = step;
        if (step == AttackStep.Sheathe)
            _sheatheStartedTime = Time.time;

        _animator.SetBool(IsAttackingHash, true);
        _animator.SetBool(ComboQueuedHash, false);
        _animator.ResetTrigger(AttackButtonHash);
        _animator.Play(GetAttackStateHash(step), 0, 0f);
    }

    private static AttackStep GetNextSwing(AttackStep currentSwing)
    {
        return currentSwing == AttackStep.SwingRight
            ? AttackStep.SwingLeft
            : AttackStep.SwingRight;
    }

    private void StartComboBuffer()
    {
        _comboBuffering = true;
        _comboBufferUntil = Time.time + comboBufferTime;
    }

    private void UpdateComboBuffer()
    {
        if (_comboQueued)
        {
            _comboQueued = false;
            _comboBuffering = false;
            PlayAttackStep(GetNextSwing(_attackStep));
            return;
        }

        if (Time.time >= _comboBufferUntil)
        {
            _comboBuffering = false;
            PlayAttackStep(AttackStep.Sheathe);
        }
    }

    private void AdvanceAttackCombo()
    {
        switch (_attackStep)
        {
            case AttackStep.Unsheathe:
                PlayAttackStep(AttackStep.SwingRight);
                break;
            case AttackStep.SwingRight:
            case AttackStep.SwingLeft:
                if (_comboQueued)
                {
                    _comboQueued = false;
                    PlayAttackStep(GetNextSwing(_attackStep));
                }
                else
                {
                    StartComboBuffer();
                }
                break;
            case AttackStep.Sheathe:
                if (_restartQueued)
                {
                    _restartQueued = false;
                    _comboQueued = false;
                    PlayAttackStep(AttackStep.Unsheathe);
                }
                else
                {
                    FinishAttack();
                }
                break;
        }
    }

    private void FinishAttack()
    {
        EndAttackTracking();
        if (_animator.GetBool("isJumping"))
            _animator.Play(JumpingHash, 0, 0f);
        else
            _animator.Play(MovementHash, 0, 0f);
    }

    private void EndAttackTracking()
    {
        _attackActive = false;
        _attackStep = AttackStep.None;
        _attackStepAdvanced = false;
        _comboQueued = false;
        _restartQueued = false;
        _comboBuffering = false;
        _hitEnemiesThisStrike.Clear();
        DisableAttackHitbox();
        _animator.SetBool(IsAttackingHash, false);
        _animator.SetBool(ComboQueuedHash, false);
        _animator.ResetTrigger(AttackButtonHash);
    }

    public void EnableAttackHitbox()
    {
        _hitEnemiesThisStrike.Clear();
        if (_attackCheckCollider != null)
            _attackCheckCollider.enabled = true;
    }

    public void DisableAttackHitbox()
    {
        if (_attackCheckCollider != null)
            _attackCheckCollider.enabled = false;
    }

    public void DealAttackDamage()
    {
        if (_attackCheckCollider == null)
            return;

        bool wasEnabled = _attackCheckCollider.enabled;
        _attackCheckCollider.enabled = true;
        Physics2D.SyncTransforms();

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(attackHitLayers);
        filter.useTriggers = true;

        int count = _attackCheckCollider.Overlap(filter, _attackOverlapResults);

        if (!wasEnabled)
            _attackCheckCollider.enabled = false;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _attackOverlapResults[i];
            if (hit == null)
                continue;

            Enemy_Damage enemy = hit.GetComponentInParent<Enemy_Damage>();
            if (enemy == null || !_hitEnemiesThisStrike.Add(enemy))
                continue;

            enemy.TakeDamage(attackDamage);
        }
    }

    private void CancelAttack()
    {
        bool wasInAttack = IsInAnyAttackState();
        EndAttackTracking();

        if (wasInAttack)
            _animator.Play(MovementHash, 0, 0f);
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

    public void Attack(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        if (_movementState != MovementState.Normal)
            return;

        if (_attackStep == AttackStep.Unsheathe
            || _attackStep == AttackStep.SwingRight
            || _attackStep == AttackStep.SwingLeft
            || _comboBuffering)
        {
            _comboQueued = true;
            return;
        }

        if (_attackStep == AttackStep.Sheathe)
        {
            if (Time.time < _sheatheStartedTime + comboBufferTime)
            {
                _restartQueued = false;
                PlayAttackStep(GetNextSwing(_lastSwingStep));
                return;
            }

            _restartQueued = true;
            return;
        }

        if (IsAttacking())
            return;

        BeginAttack();
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
        if (_movementState == MovementState.WallSlideLanding)
            return;

        if (_movementState == MovementState.WallSliding)
        {
            if (jumpBufferCounter <= 0f)
                return;

            JumpOffWallSlide();
            return;
        }

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
        CancelAttack();
        _postWallSlideJumpDoubleJumpPending = false;
        _launchedFromGrabbable = GetGrabbableUnderFeet() ?? _lastGrabbableStoodOn;
        _lastJumpTime = Time.time;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpingPower);
        _animator.SetBool("isJumping", true);
        doubleJump = true;          
        coyoteTimeCounter = 0f;      
        jumpBufferCounter = 0f;      
    }

    void AirJump()
    {
        if (_postWallSlideJumpDoubleJumpPending)
        {
            BlockWallSlideReentry(wallSlideDoubleJumpReentryCooldown);
            _postWallSlideJumpDoubleJumpPending = false;
        }

        CancelAttack();
        _lastJumpTime = Time.time;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpingPower);
        _animator.SetBool("isJumping", true);
        doubleJump = false;
        jumpBufferCounter = 0f;
    }

    private void CheckWalls()
    {
        _isTouchingWall = false;
        _wallDirection = 0;

        if (IsInClimbMode || IsWallSliding || _inClimbableZone)
            return;

        if (!IsGrounded)
        {
            int side = DetectAdjacentWallSide();
            if (side != 0)
            {
                _isTouchingWall = true;
                _wallDirection = side;
            }

            return;
        }

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

    #region Wall Slide

    private bool ShouldStartWallSlide()
    {
        if (_inClimbableZone || IsGrounded)
            return false;

        if (Time.time < _wallSlideReentryBlockedUntil)
            return false;

        if (_ledgeDetect != null && _ledgeDetect.CurrentGrab.IsValid)
            return false;

        int wallSide = DetectAdjacentWallSide();
        if (wallSide == 0)
            return false;

        if (requireInputForSlide)
        {
            bool holdingTowardWall = (wallSide == 1 && _horizontal > 0f)
                || (wallSide == -1 && _horizontal < 0f);
            if (!holdingTowardWall)
                return false;
        }

        if (Time.time - _lastJumpTime < wallSlideMinJumpAge)
            return false;

        if (GetGroundDistanceBelowFeet(wallSide) < wallSlideMinStartGroundClearance)
            return false;

        return true;
    }

    private void BeginWallSlide()
    {
        _movementState = MovementState.WallSliding;
        _wallSlideWallSide = DetectAdjacentWallSide();
        _wallSlidePhase = WallSlidePhase.Intro;
        _wallSlideWallMissCount = 0;
        _wallSlideDismountTimer = 0f;
        _wallSlideStartTime = Time.time;
        jumpBufferCounter = 0f;

        CancelAttack();
        _animator.SetBool("isJumping", false);
        _animator.SetBool("isWallSliding", true);
        _animator.SetInteger("wallSlidePhase", 0);
        _animator.SetTrigger("wallSlideStart");

        _isTouchingWall = true;
        _wallDirection = _wallSlideWallSide;

        SetWallSlideHazardCollisionIgnored(true);

        bool faceTowardWall = _wallSlideWallSide == 1;
        if (_isFacingRight != faceTowardWall)
        {
            _isFacingRight = faceTowardWall;
            ApplyFacing();
        }
    }

    private void UpdateWallSlide()
    {
        if (_ledgeDetect != null)
            _ledgeDetect.RefreshDetection();

        TryStartLedgeClimb();
        if (_movementState != MovementState.WallSliding)
            return;

        SyncWallSlideAnimatorPhase();

        if (!IsStillOnWallSlideWall())
        {
            _wallSlideWallMissCount++;
            if (_wallSlideWallMissCount >= wallSlideWallMissLimit)
            {
                TryFinishWallSlideAtGround();
                return;
            }
        }
        else
        {
            _wallSlideWallMissCount = 0;
        }

        if (IsWallSlideAtGround())
        {
            TryFinishWallSlideAtGround();
            return;
        }

        if (TryDismountWallSlide())
            return;

        SnapToWallSlide();

        float slideY = -wallSlideSpeed;
        float newY = transform.position.y + slideY * Time.fixedDeltaTime;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        _rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        UpdateAnimatorVelocity();
    }

    private void SyncWallSlideAnimatorPhase()
    {
        if (_wallSlidePhase == WallSlidePhase.Loop)
            return;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash == WallSlideLoopHash
            || (state.shortNameHash == WallSlideIntroHash && state.normalizedTime >= 0.95f))
        {
            AdvanceWallSlideToLoop();
        }
    }

    private void AdvanceWallSlideToLoop()
    {
        if (_wallSlidePhase == WallSlidePhase.Loop)
            return;

        _wallSlidePhase = WallSlidePhase.Loop;
        _animator.SetInteger("wallSlidePhase", 1);
    }

    private bool TryDismountWallSlide()
    {
        bool holdingAwayFromWall = (_wallSlideWallSide == 1 && _horizontal < -wallSlideDismountInputThreshold)
            || (_wallSlideWallSide == -1 && _horizontal > wallSlideDismountInputThreshold);

        if (!holdingAwayFromWall)
        {
            _wallSlideDismountTimer = 0f;
            return false;
        }

        _wallSlideDismountTimer += Time.fixedDeltaTime;
        if (_wallSlideDismountTimer < wallSlideDismountHoldTime)
            return false;

        DismountWallSlide();
        return true;
    }

    private void DismountWallSlide()
    {
        int wallSide = _wallSlideWallSide;
        int awayDir = -wallSide;
        bool shouldFaceRight = awayDir > 0;

        EndWallSlide();

        if (_isFacingRight != shouldFaceRight)
        {
            _isFacingRight = shouldFaceRight;
            ApplyFacing();
        }

        _flipBlockedUntil = Time.time + 0.15f;

        float awaySpeed = awayDir * speed * GetSpeedMultiplier() * 0.35f;
        _rb.linearVelocity = new Vector2(awaySpeed, Mathf.Min(_rb.linearVelocity.y, 0f));
        _animator.SetBool("isJumping", false);
    }

    private void JumpOffWallSlide()
    {
        int awayDir = -_wallSlideWallSide;
        float jumpX = awayDir * speed * GetSpeedMultiplier() * wallSlideJumpAwayMultiplier;

        _animator.SetBool("isJumping", true);
        _flipBlockedUntil = Time.time + 0.15f;
        EndWallSlide(skipReentryCooldown: true);
        BlockWallSlideReentry(wallSlideJumpReentryCooldown);
        _postWallSlideJumpDoubleJumpPending = true;
        _rb.linearVelocity = new Vector2(jumpX, jumpingPower);
        doubleJump = true;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
    }

    private void BlockWallSlideReentry(float duration)
    {
        _wallSlideReentryBlockedUntil = Mathf.Max(
            _wallSlideReentryBlockedUntil,
            Time.time + duration
        );
    }

    private static float GetAnimPlaybackDuration(float clipLength, float animSpeed)
    {
        return clipLength / Mathf.Max(animSpeed, 0.01f);
    }

    private float GetWallSlideLandingDuration()
    {
        return GetAnimPlaybackDuration(wallSlideLandingClipLength, wallSlideLandingAnimSpeed);
    }

    private bool IsStillOnWallSlideWall()
    {
        Bounds bounds = _playerCollider.bounds;
        float castY = wallCheck != null ? wallCheck.position.y : bounds.center.y;
        Vector2 origin = new Vector2(bounds.center.x, castY);
        Vector2 dir = _wallSlideWallSide > 0 ? Vector2.right : Vector2.left;

        return Physics2D.Raycast(
            origin,
            dir,
            bounds.extents.x + wallCheckDistance + 0.5f,
            groundLayer
        ).collider != null;
    }

    private void SetWallSlideHazardCollisionIgnored(bool ignore)
    {
        if (_hazardsLayer < 0 || _ignoredHazardCollision == ignore)
            return;

        Physics2D.IgnoreLayerCollision(gameObject.layer, _hazardsLayer, ignore);
        _ignoredHazardCollision = ignore;
    }

    private float GetGroundDistanceBelowFeet(int wallSide = 0)
    {
        if (groundCheck == null)
            return float.MaxValue;

        Vector2 origin = groundCheck.position;
        if (wallSide != 0)
            origin.x -= wallSide * wallSlideGroundProbeWallOffset;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            50f,
            groundLayer
        );

        if (hit.collider == null || hit.normal.y < wallSlideFloorNormalMin)
            return float.MaxValue;

        return hit.distance;
    }

    private bool IsNearWallSlideGround()
    {
        return GetGroundDistanceBelowFeet(_wallSlideWallSide) <= wallSlideLandingGroundDistance;
    }

    private bool IsWallSlideAtGround()
    {
        return IsNearWallSlideGround();
    }

    private bool ShouldPlayWallSlideLanding()
    {
        return Time.time - _wallSlideStartTime >= wallSlideMinDurationForLanding;
    }

    private void TryFinishWallSlideAtGround()
    {
        if (!IsWallSlideAtGround())
        {
            EndWallSlide();
            return;
        }

        if (ShouldPlayWallSlideLanding())
            BeginWallSlideLanding();
        else
            EndWallSlide();
    }

    private static bool HasFloorContact(Collision2D collision, float floorNormalMin)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y >= floorNormalMin)
                return true;
        }

        return false;
    }

    private void TryBeginWallSlideLandingFromGroundContact(Collision2D collision)
    {
        if (_movementState != MovementState.WallSliding)
            return;

        if (!HasFloorContact(collision, wallSlideFloorNormalMin))
            return;

        TryFinishWallSlideAtGround();
    }

    private void SnapToWallSlide()
    {
        Bounds bounds = _playerCollider.bounds;
        float castY = wallCheck != null ? wallCheck.position.y : bounds.center.y;
        Vector2 origin = new Vector2(bounds.center.x, castY);
        Vector2 dir = _wallSlideWallSide > 0 ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            dir,
            bounds.extents.x + wallCheckDistance + 0.5f,
            groundLayer
        );

        if (hit.collider == null)
            return;

        float targetEdgeX = hit.point.x - _wallSlideWallSide * wallSlideSnapOffset;
        float currentEdgeX = _wallSlideWallSide > 0 ? bounds.max.x : bounds.min.x;
        float deltaX = targetEdgeX - currentEdgeX;

        if (Mathf.Abs(deltaX) > 0.0001f)
        {
            transform.position += new Vector3(deltaX, 0f, 0f);
            Physics2D.SyncTransforms();
        }
    }

    private void BeginWallSlideLanding()
    {
        if (_movementState != MovementState.WallSliding)
            return;

        _movementState = MovementState.WallSlideLanding;
        _climbLock = true;
        _rb.linearVelocity = Vector2.zero;
        _animator.SetInteger("wallSlidePhase", 2);

        if (_wallSlideLandingRoutine != null)
            StopCoroutine(_wallSlideLandingRoutine);

        _wallSlideLandingRoutine = StartCoroutine(WallSlideLandingRoutine());
    }

    private IEnumerator WallSlideLandingRoutine()
    {
        yield return new WaitForSeconds(GetWallSlideLandingDuration());
        FinishWallSlideLanding();
    }

    public void FinishWallSlideLanding()
    {
        if (_movementState != MovementState.WallSlideLanding)
            return;

        if (_wallSlideLandingRoutine != null)
        {
            StopCoroutine(_wallSlideLandingRoutine);
            _wallSlideLandingRoutine = null;
        }

        FaceAwayFromWall();
        _animator.SetBool("isJumping", false);
        doubleJump = false;
        _climbLock = false;
        _movementState = MovementState.Normal;
        _animator.SetInteger("wallSlidePhase", 0);
        _animator.SetBool("isWallSliding", false);
        SetWallSlideHazardCollisionIgnored(false);
    }

    private void FaceAwayFromWall()
    {
        bool shouldFaceRight = _wallSlideWallSide == -1;
        if (_isFacingRight == shouldFaceRight)
            return;

        _isFacingRight = shouldFaceRight;
        ApplyFacing();
    }

    private void EndWallSlide(bool skipReentryCooldown = false)
    {
        if (_movementState == MovementState.WallSlideLanding)
        {
            if (_wallSlideLandingRoutine != null)
                StopCoroutine(_wallSlideLandingRoutine);

            _wallSlideLandingRoutine = null;
            _climbLock = false;
        }

        if (!IsWallSliding)
            return;

        _movementState = MovementState.Normal;
        _wallSlidePhase = WallSlidePhase.Intro;
        _wallSlideWallSide = 0;
        _wallSlideWallMissCount = 0;
        _wallSlideDismountTimer = 0f;
        if (!skipReentryCooldown)
            BlockWallSlideReentry(wallSlideReentryCooldown);
        _isTouchingWall = false;
        _wallDirection = 0;
        _animator.ResetTrigger("wallSlideStart");
        _animator.SetInteger("wallSlidePhase", 0);
        _animator.SetBool("isWallSliding", false);
        SetWallSlideHazardCollisionIgnored(false);
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

        CancelAttack();

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
        if (_ledgeDetect == null || !_canGrabLedge || _climbLock)
            return;

        if (_movementState != MovementState.Normal && _movementState != MovementState.WallSliding)
            return;

        if (_inClimbableZone)
            return;

        if (!_ledgeDetect.CurrentGrab.IsValid)
            return;

        if (Time.time - _lastJumpTime < ledgeMinJumpAge)
            return;

        if (_movementState == MovementState.WallSliding)
            EndWallSlide();

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

        int facing = FacingSign;
        ComputeMantlePositions(grab.LedgeTop, facing, out _ledgeHangPosition, out _climbOverPosition);

        if (IsOverlappingSpikesAt(_ledgeHangPosition) || IsOverlappingSpikesAt(_climbOverPosition))
        {
            HealthManager.Instance?.TakeDamage(200f);
            _canGrabLedge = true;
            return;
        }

        _movementState = MovementState.LedgeClimbing;

        CancelAttack();

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

    private bool IsOverlappingSpikesAt(Vector2 transformPosition)
    {
        Vector2 center = transformPosition + _playerCollider.offset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, _playerCollider.size, 0f);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponentInParent<HazardDamage>() != null)
                return true;
        }

        return false;
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
        CancelAttack();
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
        CancelAttack();

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

    private void RefreshDoubleJump()
    {
        if( _isGrounded)
        {
            doubleJump = true;
        }
    }

    private void RefreshGrabbableWindBlock()
    {
        Rigidbody2D grabbableUnderFeet = GetGrabbableUnderFeet();
        if (grabbableUnderFeet != null)
            _lastGrabbableStoodOn = grabbableUnderFeet;

        if (_isGrounded && grabbableUnderFeet == null)
        {
            _launchedFromGrabbable = null;
            _lastGrabbableStoodOn = null;
        }
    }

    private Rigidbody2D GetGrabbableUnderFeet()
    {
        if (groundCheck == null)
            return null;

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
            GrabbableObject grabbable = results[i].GetComponentInParent<GrabbableObject>();
            if (grabbable == null)
                continue;

            Rigidbody2D rb = grabbable.GetComponent<Rigidbody2D>();
            if (rb != null)
                return rb;
        }

        return null;
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
            TryBeginWallSlideLandingFromGroundContact(collision);

            if (_movementState != MovementState.WallSliding)
                return;

            _animator.SetBool("isJumping", false);
            doubleJump = false;
            _postWallSlideJumpDoubleJumpPending = false;
        }

        DepenetrateFromWalls(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) == 0)
            return;

        if (_movementState == MovementState.WallSliding)
            TryBeginWallSlideLandingFromGroundContact(collision);
        else if (IsGrounded)
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
        if (_movementState == MovementState.LedgeClimbing
            || _movementState == MovementState.LadderClimbing
            || IsWallSliding)
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
            || _movementState == MovementState.WallSliding
            || _movementState == MovementState.WallSlideLanding
            || _climbLock
            || Time.time < _flipBlockedUntil)
            return;

        if ((_isFacingRight && _horizontalInput < 0f) || (!_isFacingRight && _horizontalInput > 0f))
        {
            if (IsAttacking())
                CancelAttack();

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

        if (_attackCheckTransform != null)
        {
            Vector3 attackPos = _attackCheckTransform.localPosition;
            attackPos.x = _attackCheckAbsX * (_isFacingRight ? 1f : -1f);
            _attackCheckTransform.localPosition = attackPos;

            if (_attackCheckCollider != null)
            {
                Vector2 attackOffset = _attackCheckCollider.offset;
                attackOffset.x = _attackCheckColliderOffsetAbsX * (_isFacingRight ? 1f : -1f);
                _attackCheckCollider.offset = attackOffset;
            }
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
