using Unity.VisualScripting;
using UnityEngine;

public class Attack : MonoBehaviour
{
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>(); 
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetBool("hasTarget", true);
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetBool("hasTarget", false);
        }
       
    }

    public void OnEnemyDetected(Collider2D playerCollider)
    {
        animator.SetBool("CanAttack", true);
         Debug.Log($"Player detected by child: Executing attack!");
    }

    public void OnEnemyNotDetected(Collider2D playerCollider)
    {

        animator.SetBool("CanAttack", false);

    }

}
