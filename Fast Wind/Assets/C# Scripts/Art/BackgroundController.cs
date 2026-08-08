using Unity.Cinemachine;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
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

    private Vector2 designPos;
    private float camStartY;
    private bool camStartYSet;
    private bool cinemachineDriven;

    private void Awake()
    {
        designPos = transform.position;
    }

    private void OnEnable()
    {
        cinemachineDriven = FindFirstObjectByType<CinemachineBrain>() != null;

        if (cinemachineDriven)
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        if (cinemachineDriven)
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    private void LateUpdate()
    {
        if (!cinemachineDriven)
            ApplyParallax();
    }

    private void OnCameraUpdated(CinemachineBrain brain)
    {
        if (brain != null && brain.isActiveAndEnabled)
            ApplyParallax();
    }

    private void ApplyParallax()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector3 camPos = mainCamera.transform.position;

        if (!camStartYSet)
        {
            camStartY = camPos.y;
            camStartYSet = true;
        }

        Vector3 pos = transform.position;

        if (infiniteScroll && scrollWidth > 0f)
        {
            float scrollOffset = scrollWidth * Mathf.Floor(camPos.x * (1f - parallaxEffect) / scrollWidth);
            pos.x = designPos.x + camPos.x * parallaxEffect + scrollOffset;
        }
        else
        {
            pos.x = designPos.x + camPos.x * parallaxEffect;
        }

        if (followCameraY)
        {
            if (verticalParallaxEffect >= 1f)
                pos.y = camPos.y + yOffsetFromCamera;
            else
                pos.y = designPos.y + (camPos.y - camStartY) * verticalParallaxEffect + yOffsetFromCamera;
        }

        transform.position = pos;
    }
}
