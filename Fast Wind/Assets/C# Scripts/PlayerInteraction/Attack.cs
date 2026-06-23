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

    // Update is called once per frame




    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            animator.SetBool("hasTarget", true);
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            animator.SetBool("hasTarget", false);
        }
        

    }

    public void OnEnemyDetected(Collider playerCollider)
    {

        animator.SetBool("CanAttack", true);
        Debug.Log($"Player detected by child: Executing attack!");

    }

}
