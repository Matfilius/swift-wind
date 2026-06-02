using UnityEngine;

public class LedgeDetect : MonoBehaviour
{
    [SerializeField] private float radius;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private PlayerController player;

    public bool canDetect;

    private void Update()
    {
        if (canDetect)
            player.ledgeDetected = Physics2D.OverlapCircle(transform.position, radius, whatIsGround);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            canDetect = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            canDetect = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
