using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class HealthManager : MonoBehaviour
{
    public static HealthManager Instance { get; private set; }

    [SerializeField] Image healthBar;
    [SerializeField] float healthAmount = 100f;
    [SerializeField] Transform respawnPoint;
    [SerializeField] SceneField respawnScene;
    [SerializeField] Animator _animator;
    [SerializeField] PlayerController _playerController;


    private bool isDead;

    private bool isHealing = false;

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
        Debug.Log(_playerController.IsGrounded);
        if (healthAmount <= 0 && !isDead)
            HandleDeath();

        if (Input.GetKeyDown(KeyCode.Return))
            TakeDamage(20);

        if (Input.GetKeyDown(KeyCode.H) && !isHealing && _playerController.IsGrounded)
        {
            StartCoroutine(HealRoutine());
        }
    }

    private void HandleDeath()
    {
        isDead = true;

        GameEventsManager.instance.PlayerDeath();

        healthAmount = 100f;
        if (healthBar != null)
            healthBar.fillAmount = 1f;

        if (SceneManager.GetSceneByName(respawnScene).isLoaded)
        {
            SceneManager.UnloadSceneAsync(respawnScene);
        }

        SceneManager.LoadSceneAsync(respawnScene, LoadSceneMode.Additive);
        RegionTracker.SetCurrentRegion(respawnScene);

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

    IEnumerator HealRoutine()
    {
        if (healthBar == null || healthAmount > 99)
            yield break;

        isHealing = true;

        Heal(20);

        yield return new WaitForSeconds(1.54f);

        isHealing = false;
    }

    public void Heal(float healingAmount)
    {
        _animator.SetTrigger("isHealing");
        healthAmount += healingAmount;
        healthAmount = Mathf.Clamp(healthAmount, 0, 100);
        healthBar.fillAmount = healthAmount / 100f;
    }
}
