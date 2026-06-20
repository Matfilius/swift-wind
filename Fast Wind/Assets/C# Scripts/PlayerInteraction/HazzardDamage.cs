using UnityEngine;

public class HazardDamage : MonoBehaviour
{
    [SerializeField] float damage = 20f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        if (HealthManager.Instance != null)
            HealthManager.Instance.TakeDamage(damage);
    }
}