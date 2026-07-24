using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HealthManager : MonoBehaviour
{
    public static HealthManager Instance { get; private set; }
    [SerializeField] private DeathCountText _deathCount;

    [SerializeField] Image healthBar;
    [SerializeField] float healthAmount = 100f;
    [SerializeField] string gameplaySceneName = "GameplayScene";
    [SerializeField] string tutorialRegionName = "Tutorial_Region1";

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
        {
            isDead = true;
            GameEventsManager.instance.PlayerDied();              
            DataPersistenceManager.instance.SaveGame();          
            ReloadScene();                                        
        }


        if (Input.GetKeyDown(KeyCode.Return))
            TakeDamage(20);

        if (Input.GetKeyDown(KeyCode.H))
            Heal(5);
    }

    public void ReloadScene()
    {
        healthAmount = 100f;
        healthBar.fillAmount = 1f;
        SceneManager.LoadScene(gameplaySceneName);
        SceneManager.LoadSceneAsync(tutorialRegionName, LoadSceneMode.Additive);
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
