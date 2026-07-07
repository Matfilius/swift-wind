using UnityEngine;

public class Detection : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Attack attackScript;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only trigger if the target has the "Enemy" tag
        if (other.CompareTag("Player"))
        {
            attackScript.OnEnemyDetected(other);
        }
    }

    private void Awake()
    {
        attackScript = GetComponentInParent<Attack>();
    }
}
