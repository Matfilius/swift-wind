using System.Collections;
using UnityEngine;

public class Enemy_Damage : MonoBehaviour
{
    [SerializeField] int damage1;
    [SerializeField] float enemyHealth = 100f;
    [SerializeField] Color hitFlashColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] float hitFlashDuration = 0.15f;

    private bool _isDead;
    private SpriteRenderer[] _spriteRenderers;
    private Color[] _originalColors;
    private Coroutine _hitFlashRoutine;

    private void Awake()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        _originalColors = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _originalColors[i] = _spriteRenderers[i].color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isDead || !collision.gameObject.CompareTag("Player"))
            return;

        HealthManager playerHealth = HealthManager.Instance;
        if (playerHealth != null)
            playerHealth.TakeDamage(damage1);
    }

    public void TakeDamage(float damage)
    {
        if (_isDead || damage <= 0f)
            return;

        enemyHealth -= damage;
        if (enemyHealth <= 0f)
        {
            _isDead = true;
            foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
                col.enabled = false;
        }

        if (_hitFlashRoutine != null)
            StopCoroutine(_hitFlashRoutine);
        _hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        float duration = Mathf.Max(0.01f, hitFlashDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            ApplyFlash(pulse);
            yield return null;
        }

        ApplyFlash(0f);
        _hitFlashRoutine = null;

        if (_isDead)
            Destroy(gameObject);
    }

    private void ApplyFlash(float amount)
    {
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] != null)
                _spriteRenderers[i].color = Color.Lerp(_originalColors[i], hitFlashColor, amount);
        }
    }
}
