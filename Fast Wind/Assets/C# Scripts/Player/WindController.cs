using UnityEngine;
using UnityEngine.InputSystem;

public class WindController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject player;
    public ManaBar manaBar;

    [Header("Grab Settings")]
    public float cursorGrabRadius = 3f;    // Koliko blizu je kursor objektu koji se hvata
    public float followForce = 20f;
    public float maxHoldDistance = 5f;
    public float damping = 0.85f;

    [Header("Anti-Lift Settings")]
    public float belowPlayerThreshold = 0.3f;  // Koliko ispod igraca na Y osi mora biti objekat da izgubi silu dizanja
    public float liftAngleThreshold = 60f;      // Ugao konusa ispod igrača unutar kojeg se uklanja sila prema gore

    [Header("Mana Settings")]
    public float grabManaCost = 15f;    // Mana potrosena tek kad se zgrabi objekat
    public float holdManaCostPerSec = 10f;

    [Header("Wind Line Settings")]
    public int lineCount = 5;
    public float lineSpeed = 2f;
    public float spiralRadius = 0.4f;
    public float spiralFrequency = 3f;

    private PlayerInput _playerInput;
    private InputAction _windAction;
    private Rigidbody2D _heldObject;
    private LineRenderer[] _windLines;
    private float _animationTime;

    void Awake()
    {
        _playerInput = player.GetComponent<PlayerInput>();
        _windAction = _playerInput.actions["Wind"];

        _windLines = new LineRenderer[lineCount];
        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject($"WindLine_{i}");
            lineObj.transform.SetParent(transform);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            lr.startWidth = 0.05f;
            lr.endWidth = 0.01f;
            lr.positionCount = 20;
            lr.useWorldSpace = true;
            lr.enabled = false;

            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.9f, 0.9f, 0.9f, 0.6f); //Boja pri pocetku linija
            lr.endColor = new Color(0.9f, 0.9f, 0.9f, 0f);  // Boja pri kraju linija

            _windLines[i] = lr;
        }
    }

    void OnEnable()
    {
        _windAction.performed += OnWindPerformed;
        _windAction.canceled += OnWindCanceled;
    }

    void OnDisable()
    {
        _windAction.performed -= OnWindPerformed;
        _windAction.canceled -= OnWindCanceled;
    }

    private void OnWindPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        GrabbableObject[] candidates = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);

        Rigidbody2D nearest = null;
        float shortestDistance = cursorGrabRadius; // Ovo je da uhvati ako je u blizini igraca

        foreach (GrabbableObject candidate in candidates)
        {
            Rigidbody2D rb = candidate.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            // Razdaljina se mjeri od kursora
            float distance = Vector2.Distance(mouseWorld, rb.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = rb;
            }
        }

        if (nearest == null) return;

        if (!manaBar.mana.TrySpendMana(grabManaCost)) return;

        _heldObject = nearest;
        _heldObject.gravityScale = 0f;

        foreach (LineRenderer lr in _windLines)
            lr.enabled = true;
    }

    private void OnWindCanceled(InputAction.CallbackContext ctx)
    {
        if (_heldObject == null) return;

        _heldObject.gravityScale = 1f;
        _heldObject = null;

        foreach (LineRenderer lr in _windLines)
            lr.enabled = false;
    }

    private void ReleaseObject()
    {
        if (_heldObject == null) return;

        _heldObject.gravityScale = 1f;
        _heldObject = null;

        foreach (LineRenderer lr in _windLines)
            lr.enabled = false;
    }

    private bool IsObjectBelowPlayer(Vector2 objectPos)
    {
        Vector2 playerPos = player.transform.position;

        // Provjeri da li je igrac iznad objekta
        if (objectPos.y >= playerPos.y - belowPlayerThreshold) return false;

        Vector2 directionToObject = (objectPos - playerPos).normalized;
        float angle = Vector2.Angle(Vector2.down, directionToObject);

        return angle < liftAngleThreshold;
    }

    void FixedUpdate()
    {
        if (_heldObject == null) return;

        if (!manaBar.mana.TrySpendMana(holdManaCostPerSec * Time.fixedDeltaTime))
        {
            ReleaseObject();
            return;
        }

        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 playerPos = player.transform.position;

        Vector2 directionToMouse = (mouseWorld - playerPos).normalized;
        float distanceToMouse = Vector2.Distance(playerPos, mouseWorld);
        Vector2 targetPos = playerPos + directionToMouse * Mathf.Min(distanceToMouse, maxHoldDistance);

        Vector2 toTarget = targetPos - _heldObject.position;
        Vector2 force = toTarget * followForce;

        if (IsObjectBelowPlayer(_heldObject.position))
        {
            force.y = Mathf.Min(force.y, 0f);
        }

        _heldObject.AddForce(force);
        _heldObject.linearVelocity *= damping;
    }

    void Update()
    {
        if (_heldObject == null) return;

        _animationTime += Time.deltaTime * lineSpeed;

        Vector3 playerPos = player.transform.position;
        Vector3 objectPos = _heldObject.position;

        for (int i = 0; i < lineCount; i++)
        {
            float angleOffset = (Mathf.PI * 2f / lineCount) * i;
            int pointCount = _windLines[i].positionCount;

            for (int j = 0; j < pointCount; j++)
            {
                float t = j / (float)(pointCount - 1);
                Vector3 basePos = Vector3.Lerp(playerPos, objectPos, t);

                float angle = t * spiralFrequency * Mathf.PI * 2f + angleOffset - _animationTime;
                float offsetX = Mathf.Cos(angle) * spiralRadius * t;
                float offsetY = Mathf.Sin(angle) * spiralRadius * t;

                _windLines[i].SetPosition(j, basePos + new Vector3(offsetX, offsetY, 0f));
            }
        }
    }
}