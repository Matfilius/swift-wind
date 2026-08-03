using Unity.Cinemachine;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startLocalPosX;
    private float startLocalPosY;
    private float camStartPosX;
    private float camStartPosY;
    private float length;
    private Transform cam;
    private bool initialized;
    private bool useCinemachineCallback;

    [Header("Horizontal Parallax")]
    public float parallaxEffect = 0.8f;

    [Header("Vertical Parallax")]
    public bool followCameraY;

    [Range(0f, 1f)]
    public float verticalParallaxEffect = 1f;
    public float yOffsetFromCamera;

    [Header("Infinite Scroll")]
    public bool infiniteScroll;
    public float scrollWidth;

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

        ApplyParallax();
    }

    void EnsureInitialized()
    {
        if (initialized || Camera.main == null)
            return;

        cam = Camera.main.transform;
        camStartPosX = cam.position.x;
        camStartPosY = cam.position.y;
        startLocalPosX = transform.localPosition.x;
        startLocalPosY = transform.localPosition.y;

        if (scrollWidth > 0f)
            length = scrollWidth;
        else if (TryGetComponent(out SpriteRenderer spriteRenderer))
            length = spriteRenderer.bounds.size.x;

        initialized = true;
    }

    void ApplyParallax()
    {
        EnsureInitialized();
        if (!initialized || cam == null)
            return;

        float camDeltaX = cam.position.x - camStartPosX;
        float localX = startLocalPosX + camDeltaX * parallaxEffect;
        Vector3 localPos = transform.localPosition;
        localPos.x = localX;

        if (followCameraY)
        {
            float parentWorldY = transform.parent != null ? transform.parent.position.y : 0f;

            if (verticalParallaxEffect >= 1f)
                localPos.y = cam.position.y + yOffsetFromCamera - parentWorldY;
            else
            {
                float camDeltaY = cam.position.y - camStartPosY;
                localPos.y = startLocalPosY + camDeltaY * verticalParallaxEffect + yOffsetFromCamera;
            }
        }

        transform.localPosition = localPos;

        if (!infiniteScroll || parallaxEffect >= 1f || length <= 0f)
            return;

        float movement = camDeltaX * (1f - parallaxEffect);

        if (movement > startLocalPosX + length)
            startLocalPosX += length;
        else if (movement < startLocalPosX - length)
            startLocalPosX -= length;
    }
}
