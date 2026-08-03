using Unity.Cinemachine;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startPosX;
    private float startPosY;
    private float camStartPosX;
    private float camStartPosY;
    private float length;
    private Transform cam;
    private bool useCinemachineCallback;

    [Header("Horizontal Parallax")]
    [Tooltip("1 = fixed on screen horizontally. Lower = moves faster.")]
    public float parallaxEffect = 0.8f;

    [Header("Vertical Parallax")]
    [Tooltip("Clouds/sun: on with high vertical effect. Region layers: optional, use a small value like 0.1.")]
    public bool followCameraY;

    [Tooltip("1 = fully tracks camera height. Lower values drift less vertically.")]
    [Range(0f, 1f)]
    public float verticalParallaxEffect = 1f;

    [Tooltip("Fixed height above camera center. Mainly for sun and clouds.")]
    public float yOffsetFromCamera;

    [Header("Infinite Scroll")]
    [Tooltip("Enable for global layers like clouds. Disable for region layers that use hand-placed duplicates.")]
    public bool infiniteScroll;

    [Tooltip("Distance between loop copies. Required when infinite scroll is on and this object has no SpriteRenderer.")]
    public float scrollWidth;

    void Start()
    {
        if (Camera.main == null)
            return;

        cam = Camera.main.transform;
        camStartPosX = cam.position.x;
        camStartPosY = cam.position.y;
        startPosX = transform.position.x;
        startPosY = transform.position.y;

        if (scrollWidth > 0f)
            length = scrollWidth;
        else if (TryGetComponent(out SpriteRenderer spriteRenderer))
            length = spriteRenderer.bounds.size.x;
    }

    void OnEnable()
    {
        useCinemachineCallback = FindFirstObjectByType<CinemachineBrain>() != null;
        if (useCinemachineCallback)
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    void OnDisable()
    {
        if (useCinemachineCallback)
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    void LateUpdate()
    {
        if (!useCinemachineCallback)
            ApplyParallax();
    }

    void OnCameraUpdated(CinemachineBrain brain)
    {
        if (brain == null || !brain.isActiveAndEnabled || Camera.main == null)
            return;

        cam = Camera.main.transform;
        ApplyParallax();
    }

    void ApplyParallax()
    {
        if (cam == null)
            return;

        float camDeltaX = cam.position.x - camStartPosX;
        float x = startPosX + camDeltaX * parallaxEffect;
        float y = transform.position.y;

        if (followCameraY)
        {
            if (verticalParallaxEffect >= 1f)
                y = cam.position.y + yOffsetFromCamera;
            else
            {
                float camDeltaY = cam.position.y - camStartPosY;
                y = startPosY + camDeltaY * verticalParallaxEffect + yOffsetFromCamera;
            }
        }

        transform.position = new Vector3(x, y, transform.position.z);

        if (!infiniteScroll || parallaxEffect >= 1f || length <= 0f)
            return;

        float movement = camDeltaX * (1f - parallaxEffect);

        if (movement > startPosX + length)
            startPosX += length;
        else if (movement < startPosX - length)
            startPosX -= length;
    }
}
