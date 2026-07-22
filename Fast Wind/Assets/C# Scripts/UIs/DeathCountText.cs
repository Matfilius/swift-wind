using UnityEngine;
using TMPro;

public class DeathCountText : MonoBehaviour
{
    private int deathCount = 0;

    private TextMeshProUGUI deathCountText;

    private void Awake()
    {
        deathCountText = this.GetComponent<TextMeshProUGUI>();
    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
