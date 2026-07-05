using UnityEngine;

public class HazardCaltrop : MonoBehaviour
{
    [SerializeField] float damagePerSecond = 5f;
    [SerializeField] float slowMultiplier = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player != null)
            player.ApplySlow(GetInstanceID(), slowMultiplier);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (HealthManager.Instance != null)
            HealthManager.Instance.TakeDamage(damagePerSecond * Time.deltaTime);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();

        if (player != null)
            player.RemoveSlow(GetInstanceID());
    }
}
