using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyMotor))]
public class EnemyBrain : MonoBehaviour
{
    enum State
    {
        Patrol,
        Chase,
        Attack,
        Hurt,
        Dead
    }

    [Header("Ranges")]
    [SerializeField] float aggroRadius = 8f;
    [SerializeField] float deaggroRadius = 12f;
    [SerializeField] float attackRange = 2.4f;
    [SerializeField] float attackVerticalRange = 2.2f;
    [SerializeField] float patrolDistance = 4f;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 2.4f;
    [SerializeField] float chaseSpeed = 4.2f;

    [Header("Combat")]
    [SerializeField] float attackCooldown = 0.85f;
    [SerializeField] float recoverDuration = 0.35f;
    [SerializeField] float hurtDuration = 0.28f;
    [SerializeField] float hurtKnockback = 7f;
    [SerializeField] float hurtHop = 3f;

    static readonly int HasTargetHash = Animator.StringToHash("hasTarget");
    static readonly int CanAttackHash = Animator.StringToHash("CanAttack");

    EnemyMotor _motor;
    Animator _animator;
    Attack _attack;
    Transform _player;
    State _state;
    float _homeX;
    float _nextFlipTime;
    float _nextAttackTime;
    float _hurtUntil;
    float _attackTimeout;
    float _recoverUntil;
    bool _strikeStarted;
    bool _hasCombatParams;
    int _strikeZoneCount;

    bool PlayerInStrikeZone => _strikeZoneCount > 0;

    void Awake()
    {
        _motor = GetComponent<EnemyMotor>();
        _animator = GetComponent<Animator>();
        _attack = GetComponent<Attack>();
        _hasCombatParams = HasParameter(HasTargetHash) && HasParameter(CanAttackHash);

        MonsterMovement legacyPatrol = GetComponent<MonsterMovement>();
        if (legacyPatrol != null)
            legacyPatrol.enabled = false;
    }

    void Start()
    {
        _homeX = transform.position.x;
        CachePlayer();
        EnterPatrol();
    }

    void Update()
    {
        if (_state == State.Dead)
            return;

        CachePlayer();

        switch (_state)
        {
            case State.Patrol:
                TickPatrol();
                break;
            case State.Chase:
                TickChase();
                break;
            case State.Attack:
                TickAttack();
                break;
            case State.Hurt:
                TickHurt();
                break;
        }
    }

    public void NotifyStrikeZone(bool inside, Transform player)
    {
        if (inside)
        {
            _strikeZoneCount++;
            if (player != null)
                _player = player;
        }
        else
        {
            _strikeZoneCount = Mathf.Max(0, _strikeZoneCount - 1);
        }
    }

    public void NotifyHurt()
    {
        if (_state == State.Dead)
            return;

        EnterHurt();
    }

    public void NotifyDied()
    {
        _state = State.Dead;
        SetCombat(false, false);
        if (_attack != null)
            _attack.DisableHitbox();
        _motor.Stop();
        _motor.enabled = false;
        enabled = false;
    }

    void TickPatrol()
    {
        if (CanSeePlayer())
        {
            EnterChase();
            return;
        }

        if (!_motor.IsGrounded)
        {
            _motor.Stop();
            return;
        }

        float fromHome = transform.position.x - _homeX;
        bool pastLeash = Mathf.Abs(fromHome) > patrolDistance && Mathf.Sign(fromHome) == _motor.Facing;
        bool blocked = _motor.HasWallAhead || !_motor.HasGroundAhead;

        if (pastLeash || blocked)
        {
            if (Time.time < _nextFlipTime)
            {
                _motor.Stop();
                return;
            }

            _motor.Face(-_motor.Facing);
            _nextFlipTime = Time.time + 0.35f;
        }

        _motor.Move(walkSpeed);
    }

    void TickChase()
    {
        if (!CanSeePlayer())
        {
            EnterPatrol();
            return;
        }

        int direction = DirectionToPlayer();
        _motor.Face(direction);

        if (_motor.IsGrounded && Time.time >= _nextAttackTime && PlayerInAttackRange())
        {
            EnterAttack();
            return;
        }

        if (!_motor.IsGrounded)
            return;

        if (_motor.HasWallAhead || !_motor.HasGroundAhead)
            _motor.Stop();
        else
            _motor.Move(chaseSpeed);
    }

    void TickAttack()
    {
        _motor.Stop();
        SetCombat(false, false);

        if (_animator == null)
        {
            if (Time.time >= _attackTimeout)
                FinishAttack();
            return;
        }

        AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(0);
        bool inSwing = current.IsName("Enemy attack");
        if (inSwing)
            _strikeStarted = true;

        if (_strikeStarted && !inSwing && !_animator.IsInTransition(0))
        {
            if (_recoverUntil <= 0f)
                _recoverUntil = Time.time + recoverDuration;
            else if (Time.time >= _recoverUntil)
                FinishAttack();
        }

        if (Time.time >= _attackTimeout)
            FinishAttack();
    }

    void TickHurt()
    {
        if (Time.time < _hurtUntil)
            return;

        if (CanSeePlayer())
            EnterChase();
        else
            EnterPatrol();
    }

    void EnterPatrol()
    {
        _state = State.Patrol;
        SetCombat(false, false);
    }

    void EnterChase()
    {
        _state = State.Chase;
        SetCombat(false, false);
    }

    void EnterAttack()
    {
        _state = State.Attack;
        _strikeStarted = false;
        _recoverUntil = 0f;
        _attackTimeout = Time.time + (_animator != null ? 3.5f : 0.45f);
        _motor.Stop();
        _motor.Face(DirectionToPlayer());
        if (_attack != null)
            _attack.DisableHitbox();

        // One swing only. Leaving both animator bools off stops the prepare clip
        // from chaining into a second swing after the player walks away.
        SetCombat(false, false);
        if (_animator != null)
            _animator.Play("Enemy attack", 0, 0f);
    }

    void FinishAttack()
    {
        if (_state != State.Attack)
            return;

        SetCombat(false, false);
        if (_attack != null)
            _attack.DisableHitbox();
        _nextAttackTime = Time.time + attackCooldown;

        if (CanSeePlayer())
            EnterChase();
        else
            EnterPatrol();
    }

    void EnterHurt()
    {
        _state = State.Hurt;
        _hurtUntil = Time.time + hurtDuration;
        _strikeStarted = false;
        SetCombat(false, false);
        if (_attack != null)
            _attack.DisableHitbox();

        int away = -DirectionToPlayer();
        _motor.Knockback(new Vector2(away * hurtKnockback, hurtHop), hurtDuration);
        _nextAttackTime = Time.time + 0.25f;
    }

    bool CanSeePlayer()
    {
        if (_player == null)
            return false;

        if (PlayerInStrikeZone)
            return true;

        float range = _state == State.Patrol ? aggroRadius : deaggroRadius;
        Vector2 origin = SightOrigin();
        Vector2 delta = (Vector2)_player.position - origin;
        if (delta.sqrMagnitude > range * range)
            return false;

        if (delta.sqrMagnitude < 0.0001f)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(origin, delta.normalized, delta.magnitude, _motor.GroundLayers);
        return hit.collider == null;
    }

    bool PlayerInAttackRange()
    {
        if (PlayerInStrikeZone)
            return true;

        if (_player == null)
            return false;

        Vector2 delta = _player.position - transform.position;
        if (Mathf.Abs(delta.y) > attackVerticalRange || Mathf.Abs(delta.x) > attackRange)
            return false;

        if (Mathf.Abs(delta.x) > 0.2f && Mathf.Sign(delta.x) != _motor.Facing)
            return false;

        return true;
    }

    int DirectionToPlayer()
    {
        if (_player == null)
            return _motor.Facing;

        float dx = _player.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.05f)
            return _motor.Facing;

        return dx > 0f ? 1 : -1;
    }

    void SetCombat(bool hasTarget, bool canAttack)
    {
        if (_animator == null || !_hasCombatParams)
            return;

        _animator.SetBool(HasTargetHash, hasTarget);
        _animator.SetBool(CanAttackHash, canAttack);
    }

    bool HasParameter(int hash)
    {
        if (_animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.nameHash == hash)
                return true;
        }

        return false;
    }

    Vector2 SightOrigin()
    {
        Collider2D body = GetComponent<Collider2D>();
        if (body == null)
            return transform.position;

        return body.bounds.center;
    }

    void CachePlayer()
    {
        if (_player != null)
            return;

        _strikeZoneCount = 0;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
            _player = found.transform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = new Color(1f, 0.35f, 0.25f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
