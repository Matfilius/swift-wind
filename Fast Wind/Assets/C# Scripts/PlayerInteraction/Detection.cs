using UnityEngine;

public class Detection : MonoBehaviour
{

    [SerializeField] private Attack attackScript;

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player"))
        {
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
            attackScript.OnEnemyNotDetected(other);
        }

    }

}
