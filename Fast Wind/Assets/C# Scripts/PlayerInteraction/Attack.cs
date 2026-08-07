using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D spearHitbox;
    [SerializeField] private float damage = 30f;

    private bool hasHitThisSwing;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (spearHitbox != null)
            spearHitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            animator.SetBool("hasTarget", true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            animator.SetBool("hasTarget", false);
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
        animator.SetBool("CanAttack", true);
    }

    public void OnEnemyNotDetected(Collider2D playerCollider)
    {
        animator.SetBool("CanAttack", false);
        DisableHitbox();
    }
}
