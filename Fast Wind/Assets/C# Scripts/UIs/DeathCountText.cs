using UnityEngine;
using TMPro;

public class DeathCountText : MonoBehaviour, IDataPersistence
{
    private int deathCount = 0;


    private TextMeshProUGUI deathCountText;

    private void Awake()
    {
        deathCountText = this.GetComponent<TextMeshProUGUI>();
    }

    public void LoadData(GameData data)
    {
        this.deathCount = data.deathCount;
    }

    public void SaveData(ref GameData data)
    {
        data.deathCount = this.deathCount;
    }


    public void OnPlayerDeath()
    {
        deathCount++;
    }

    private void Update()
    {
        deathCountText.text = "Deaths: " + deathCount;
    }
}
