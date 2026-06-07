using UnityEngine;

public class Enemy_Damage : MonoBehaviour
{
    [SerializeField] int damage1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        HealthManager playerHealth = HealthManager.Instance;
        if (playerHealth != null)
            playerHealth.TakeDamage(damage1);
    }
}
