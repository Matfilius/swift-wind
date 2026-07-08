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

<<<<<<< Updated upstream
    void Update()
    {
        //while (OnEnemyDetected())
        //{
        //    animator.SetBool("CanAttack", false);
        //}
    }

    // Update is called once per frame
=======
>>>>>>> Stashed changes

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
        //if(IsDetected(other)){

        //}
       
        if (true)
        {
            animator.SetBool("CanAttack", true);
            Debug.Log($"Player detected by child: Executing attack!");

        }
       
       

    }

<<<<<<< Updated upstream
    //bool IsDetected(Collider2D collision)
    //{
        

    //}


=======
    public void OnEnemyNotDetected(Collider2D playerCollider)
    {

        animator.SetBool("CanAttack", false);


    }

>>>>>>> Stashed changes
}
