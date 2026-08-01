using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startPos;
    private float length;
    private Transform cam;
    public float parallaxEffect;

    void Start()
    {
        cam = Camera.main.transform;
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }
              
    void FixedUpdate()
    {
        if (cam == null) return;
        float distance = cam.position.x * parallaxEffect;
        float movement = cam.position.x * (1f - parallaxEffect);
        transform.position = new Vector3(startPos + distance, transform.position.y, transform.position.z);

        if (movement > startPos + length)
        {
            startPos += length;
        }          
        else if (movement < startPos - length)
        {
            startPos -= length;
        }
            
    }
}
