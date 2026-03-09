using UnityEngine;

public class KunaiThrow : MonoBehaviour
{
    public GameObject kunaiPrefab;
    public Transform throwPoint;
    public float throwForce = 100f;

    Vector3 throwVector;
    LineRenderer _lr;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            CalculateThrowVector();
            SetArrow();
        }

        if (Input.GetMouseButton(1))
        {
            CalculateThrowVector();
            SetArrow();
        }

        if (Input.GetMouseButtonUp(1))
        {
            RemoveArrow();
            Throw();
        }
    }

    void CalculateThrowVector()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 distance = mousePos - throwPoint.position;
        throwVector = distance.normalized * throwForce;
    }

    void SetArrow()
    {
        _lr.positionCount = 2;

        _lr.SetPosition(0, throwPoint.position);
        _lr.SetPosition(1, throwPoint.position + throwVector.normalized * 2);

        _lr.enabled = true;
    }

    void RemoveArrow()
    {
        _lr.enabled = false;
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