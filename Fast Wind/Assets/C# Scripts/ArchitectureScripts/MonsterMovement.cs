using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform[] PatrolPoints;
    public float moveSpeed;
    public int patrolDestination;
    // Update is called once per frame
    void FixedUpdate()
    {
        if(patrolDestination == 0)
        {
            transform.position = Vector2.MoveTowards(transform.position, PatrolPoints[0].position, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, PatrolPoints[0].position) < .1f)
            {
                transform.localScale = new Vector3(1, 1, 1);
                patrolDestination = 1;
            }
        }

        if (patrolDestination == 1)
        {
            transform.position = Vector2.MoveTowards(transform.position, PatrolPoints[1].position, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, PatrolPoints[1].position) < .1f)
            {
                transform.localScale = new Vector3(-1, 1, 1);
                patrolDestination = 0;
            }
        }

    }
}
