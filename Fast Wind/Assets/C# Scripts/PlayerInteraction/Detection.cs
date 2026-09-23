using UnityEngine;

public class Detection : MonoBehaviour
{

    [SerializeField] private Attack attackScript;

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player"))
        {
            EnemyBrain brain = GetComponentInParent<EnemyBrain>();
            if (brain != null)
                brain.NotifyStrikeZone(true, other.transform);
            else if (attackScript != null)
                attackScript.OnEnemyDetected(other);
        }
    }

    private void Awake()
    {
        attackScript = GetComponentInParent<Attack>();
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EnemyBrain brain = GetComponentInParent<EnemyBrain>();
            if (brain != null)
                brain.NotifyStrikeZone(false, other.transform);
            else if (attackScript != null)
                attackScript.OnEnemyNotDetected(other);
        }

    }

}
