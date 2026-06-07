using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Grappler : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] LineRenderer _lineRenderer;
    [SerializeField] DistanceJoint2D _distanceJoint;

    [Header("Grapple Settings")]
    [SerializeField] TileBase grappleTile;
    [SerializeField] float maxGrappleDistance = 10f;
    [SerializeField] float ropeLength = 10f;

    [Header("Swing Settings")]
    [SerializeField] float maxSwingSpeed = 8f;

    private Rigidbody2D _rb;
    private PlayerController _playerController;
    private readonly List<Vector3> _grapplePoints = new List<Vector3>();
    private PlayerInput _playerInput;
    private InputAction _grappleAction;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _grappleAction = _playerInput.actions["Grapple"];
        _rb = GetComponent<Rigidbody2D>();
        _playerController = GetComponent<PlayerController>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        _grappleAction.performed += OnGrapple;
        _grappleAction.canceled += OnGrapple;
        ScanForGrapplePoints();
    }

    void Start()
    {
        _distanceJoint.enabled = false;
        _lineRenderer.enabled = false;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _grappleAction.performed -= OnGrapple;
        _grappleAction.canceled -= OnGrapple;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanForGrapplePoints();
    }

    private void ScanForGrapplePoints()
    {
        _grapplePoints.Clear();

        if (grappleTile == null)
        {
            Debug.LogWarning("Grappler: grappleTile is not assigned.");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                    ScanTilemap(tilemap);
            }
        }

        Debug.Log($"Grappler: found {_grapplePoints.Count} grapple points across loaded scenes.");
    }

    private void ScanTilemap(Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int cellPos in bounds.allPositionsWithin)
        {
            if (tilemap.GetTile(cellPos) == grappleTile)
                _grapplePoints.Add(tilemap.GetCellCenterWorld(cellPos));
        }
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
            if (target == null)
                return;

            float distance = Vector2.Distance(transform.position, target.Value);
            bool isAbove = target.Value.y > transform.position.y;
            bool isInRange = distance <= maxGrappleDistance;

            if (!isAbove || !isInRange)
                return;

            Vector2 targetPos = target.Value;

            _playerController.enabled = false;

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
            _playerController.enabled = true;
            _playerController.doubleJump = true;
        }
    }

    void FixedUpdate()
    {
        if (!_distanceJoint.enabled)
            return;

        _lineRenderer.SetPosition(1, transform.position);

        if (_rb.linearVelocity.magnitude > maxSwingSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSwingSpeed;
    }
}
