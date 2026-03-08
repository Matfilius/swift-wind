using UnityEngine;

public class Enemy_Damage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int damage1;
    public HealthManager playerHealth;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            playerHealth.TakeDamage(damage1);
        }

    }
}
