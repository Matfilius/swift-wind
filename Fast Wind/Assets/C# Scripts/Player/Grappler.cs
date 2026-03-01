using UnityEngine;

public class Grappler : MonoBehaviour
{
    public Camera mainCamera;
    public LineRenderer _lineRenderer;
    public DistanceJoint2D _distanceJoint;
    public GameObject player;

    void Start()
    {
        _distanceJoint.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        { 
            Vector2 mousePos = (Vector2)mainCamera.ScreenToWorldPoint(Input.mousePosition);
            player.GetComponent<PlayerController>().enabled = false;
            _lineRenderer.SetPosition(0, mousePos);
            _lineRenderer.SetPosition(1, transform.position);
            _distanceJoint.connectedAnchor = mousePos;
            _distanceJoint.enabled = true;
            _lineRenderer.enabled = true;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            _distanceJoint.enabled = false;
            _lineRenderer.enabled = false;
            player.GetComponent<PlayerController>().enabled = true;
        }
        if(_distanceJoint.enabled)
        {
            _lineRenderer.SetPosition(1, transform.position);
        }
    }
}
