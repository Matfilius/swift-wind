using UnityEngine.InputSystem;
using UnityEngine;


public class KunaiThrow : MonoBehaviour
{
    public GameObject kunaiPrefab;
    public Transform throwPoint;
    [SerializeField] float throwForce = 100f;
    [SerializeField] LineRenderer trajectoryLine;
    [SerializeField] int trajectoryPoints = 30;
    [SerializeField] float timeStep = 0.05f;

    Vector3 throwVector;

    void Start()
    {
        trajectoryLine.enabled = false;
    }

  public void OnKnifeThrow(InputAction.CallbackContext context)
    {
        if(context.started || context.performed)
        {
            CalculateThrowVector();
            DrawTrajectory();
        }
        
        if(context.canceled)
        {
            trajectoryLine.enabled = false;
            Throw();
        }

    }

    void CalculateThrowVector()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 distance = mousePos - throwPoint.position;
        throwVector = distance.normalized * throwForce;
    }

    void DrawTrajectory()
    {
        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.enabled = true;

        Vector2 startPos = throwPoint.position;

        Vector2 displayVelocity = throwVector / 50f;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = i * timeStep;

            Vector2 point = startPos + displayVelocity * t + 0.5f * Physics2D.gravity * t * t;

            trajectoryLine.SetPosition(i, point);
        }
    }

    void Throw()
    {
        GameObject kunai = Instantiate(kunaiPrefab, throwPoint.position, Quaternion.identity);
        kunai.transform.localScale = Vector3.one * 4f;
        Rigidbody2D rb = kunai.GetComponent<Rigidbody2D>();
        float angle = Mathf.Atan2(throwVector.y, throwVector.x) * Mathf.Rad2Deg;
        kunai.transform.rotation = Quaternion.Euler(0, 0, angle);


        rb.AddForce(throwVector);
    }
}