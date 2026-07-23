using UnityEngine;
using TMPro;

public class DeathCountText : MonoBehaviour
{
    private static int deathCount = 0;


    private TextMeshProUGUI deathCountText;

    private void Awake()
    {
        deathCountText = this.GetComponent<TextMeshProUGUI>();
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
