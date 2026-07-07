using System.Collections;
using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float damage = 15f;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] LayerMask stickLayers;
    [SerializeField] SpriteRenderer spriteRenderer;

    Rigidbody2D _rb;
    bool _stuck;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Launch(Vector2 direction)
    {
        _rb.linearVelocity = direction.normalized * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_stuck) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            HealthManager.Instance?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (((1 << collision.gameObject.layer) & stickLayers) != 0)
            StickAndFade();
    }

    void StickAndFade()
    {
        _stuck = true;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f); 

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / fadeDuration;

            spriteRenderer.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        spriteRenderer.color = endColor;
        Destroy(gameObject);
    }
}
