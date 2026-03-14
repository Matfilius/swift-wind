using UnityEngine;
using UnityEngine.InputSystem;

public class ForcePush : MonoBehaviour
{
    public float pushRadius = 5f;
    public float pushForce = 10f;

    public void DoPush(InputAction.CallbackContext context)
    {

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pushRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            Vector2 dirToObject = (hit.transform.position - transform.position).normalized;

            Vector2 facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

            float dot = Vector2.Dot(facingDir, dirToObject);

            if (dot < 0.707f) continue; // Outside the 90° cone, skip

            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            float falloff = 1f - (distance / pushRadius);

            rb.AddForce(dirToObject * pushForce * falloff, ForceMode2D.Impulse);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 facing = transform.right;
        float halfAngle = 45f; 

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pushRadius);

        Gizmos.color = Color.yellow;
        Vector3 leftEdge = Quaternion.Euler(0, 0, halfAngle) * facing * pushRadius;
        Vector3 rightEdge = Quaternion.Euler(0, 0, -halfAngle) * facing * pushRadius;
        Gizmos.DrawLine(transform.position, transform.position + leftEdge);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge);
    }
}
