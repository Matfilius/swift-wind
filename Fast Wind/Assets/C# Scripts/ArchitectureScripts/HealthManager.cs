using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    public static HealthManager Instance { get; private set; }

    [SerializeField] Image healthBar;
    [SerializeField] float healthAmount = 100f;
    [SerializeField] Transform respawnPoint;

    private bool isDead;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (healthAmount <= 0 && !isDead)
            HandleDeath();

        if (Input.GetKeyDown(KeyCode.Return))
            TakeDamage(20);

        if (Input.GetKeyDown(KeyCode.H))
            Heal(5);
    }

    private void HandleDeath()
    {
        isDead = true;

        GameEventsManager.instance.PlayerDeath();

        healthAmount = 100f;
        if (healthBar != null)
            healthBar.fillAmount = 1f;

        if (respawnPoint != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.transform.position = respawnPoint.position;
        }

        isDead = false;
    }

    public void TakeDamage(float damage)
    {
        if (healthBar == null)
            return;

        healthAmount -= damage;
        healthBar.fillAmount = healthAmount / 100f;
    }

    public void Heal(float healingAmount)
    {
        if (healthBar == null)
            return;

        healthAmount += healingAmount;
        healthAmount = Mathf.Clamp(healthAmount, 0, 100);
        healthBar.fillAmount = healthAmount / 100f;
    }
}
