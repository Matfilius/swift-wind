using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Grappler : MonoBehaviour
{
    public Camera mainCamera;
    public LineRenderer _lineRenderer;
    public DistanceJoint2D _distanceJoint;
    public GameObject player;
    public PlayerController playerController;

    [Header("Grapple Settings")]
    public Tilemap tilemap;
    public TileBase grappleTile;
    public float maxGrappleDistance = 10f;
    public float ropeLength = 10f;

    [Header("Swing Settings")]
    public float maxSwingSpeed = 8f;

    private Rigidbody2D _rb;
    private List<Vector3> _grapplePoints = new List<Vector3>();
    private PlayerInput _playerInput;
    private InputAction _grappleAction;

    void Awake()
    {
        _playerInput = player.GetComponent<PlayerInput>();
        _grappleAction = _playerInput.actions["Grapple"];
        _rb = player.GetComponent<Rigidbody2D>();
        ScanForGrapplePoints();
    }

    void Start()
    {
        _distanceJoint.enabled = false;
        _lineRenderer.enabled = false;
    }

    void OnEnable()
    {
        _grappleAction.performed += OnGrapple;
        _grappleAction.canceled += OnGrapple;
    }

    void OnDisable()
    {
        _grappleAction.performed -= OnGrapple;
        _grappleAction.canceled -= OnGrapple;
    }

    private void ScanForGrapplePoints()
    {
        _grapplePoints.Clear();
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int cellPos in bounds.allPositionsWithin)
        {
            if (tilemap.GetTile(cellPos) == grappleTile)
            {
                _grapplePoints.Add(tilemap.GetCellCenterWorld(cellPos));
            }
        }

        Debug.Log($"Found {_grapplePoints.Count} grapple points.");
    }

    private Vector3? GetNearestPoint()
    {
        Vector3? nearest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (Vector3 point in _grapplePoints)
        {
            float distance = Vector2.Distance(transform.position, point);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = point;
            }
        }

        return nearest;
    }

    private void OnGrapple(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Vector3? target = GetNearestPoint();
            if (target == null) return;

            float distance = Vector2.Distance(transform.position, target.Value);
            bool isAbove = target.Value.y > transform.position.y;
            bool isInRange = distance <= maxGrappleDistance;

            if (!isAbove || !isInRange) return;

            Vector2 targetPos = target.Value;

            player.GetComponent<PlayerController>().enabled = false;

            _lineRenderer.SetPosition(0, targetPos);
            _lineRenderer.SetPosition(1, transform.position);

            _distanceJoint.connectedAnchor = targetPos;
            _distanceJoint.distance = ropeLength;
            _distanceJoint.enabled = true;
            _lineRenderer.enabled = true;
        }
        else if (ctx.canceled)
        {
            _distanceJoint.enabled = false;
            _lineRenderer.enabled = false;
            player.GetComponent<PlayerController>().enabled = true;
            playerController.doubleJump = true;
        }
    }

    void FixedUpdate()
    {
        if (_distanceJoint.enabled)
        {
            _lineRenderer.SetPosition(1, transform.position);

            if (_rb.linearVelocity.magnitude > maxSwingSpeed)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxSwingSpeed;
            }
        }
    }
}