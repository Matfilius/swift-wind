using UnityEngine;
using TMPro;

public class DeathCountText : MonoBehaviour, IDataPersistence
{
    private int deathCount = 0;

    private TextMeshProUGUI deathCountText;

    private void Awake()
    {
        deathCountText = GetComponent<TextMeshProUGUI>();
    }

    public void LoadData(GameData data)
    {
        deathCount = data.deathCount;
    }

    public void SaveData(ref GameData data)
    {
        data.deathCount = deathCount;
    }

    private void Start()
    {
        GameEventsManager.instance.onPlayerDeath += OnPlayerDeath;
    }

    private void OnDestroy()
    {
        if (GameEventsManager.instance != null)
            GameEventsManager.instance.onPlayerDeath -= OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        deathCount++;
    }

    private void Update()
    {
        deathCountText.text = "Deaths: " + deathCount;
    }
}
