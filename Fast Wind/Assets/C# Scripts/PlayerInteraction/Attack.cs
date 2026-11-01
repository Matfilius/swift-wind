using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D spearHitbox;
    [SerializeField] private float damage = 30f;

    private bool hasHitThisSwing;
    private EnemyBrain _brain;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (spearHitbox != null)
            spearHitbox.enabled = false;

        _brain = GetComponentInParent<EnemyBrain>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (Brain != null)
        {
            Brain.NotifyStrikeZone(true, other.transform);
            return;
        }

        if (animator != null)
            animator.SetBool("hasTarget", true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (Brain != null)
        {
            Brain.NotifyStrikeZone(false, other.transform);
            return;
        }

        if (animator != null)
            animator.SetBool("hasTarget", false);
    }

    EnemyBrain Brain
    {
        get
        {
            if (_brain == null)
                _brain = GetComponentInParent<EnemyBrain>();
            return _brain;
        }
    }
    public void EnableHitbox()
    {
        hasHitThisSwing = false;
        if (spearHitbox != null)
            spearHitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        if (spearHitbox != null)
            spearHitbox.enabled = false;
    }

    public void TryHitPlayer(Collider2D other)
    {
        if (hasHitThisSwing || !other.CompareTag("Player"))
            return;

        hasHitThisSwing = true;
        HealthManager.Instance?.TakeDamage(damage);
    }

    public void OnEnemyDetected(Collider2D playerCollider)
    {
        if (Brain != null)
        {
            Brain.NotifyStrikeZone(true, playerCollider.transform);
            return;
        }

        if (animator != null)
            animator.SetBool("CanAttack", true);
    }

    public void OnEnemyNotDetected(Collider2D playerCollider)
    {
        if (Brain != null)
        {
            Brain.NotifyStrikeZone(false, playerCollider.transform);
            return;
        }

        if (animator != null)
            animator.SetBool("CanAttack", false);
        DisableHitbox();
    }
}
