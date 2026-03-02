using UnityEngine;

public class AfterImage : MonoBehaviour
{
    public float fadeTime = 0.3f;
    private float timer;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        timer = fadeTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        float alpha = timer / fadeTime;

        Color c = sr.color;
        c.a = alpha;
        sr.color = c;

        if (timer <= 0)
            Destroy(gameObject);
    }
}