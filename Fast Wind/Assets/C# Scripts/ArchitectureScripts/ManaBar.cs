using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    private Mana mana;

    private Image barImage;
    private void Awake()
    {
        barImage = transform.Find("Bar").GetComponent<Image>();

        mana = new Mana();

    }

    private void Update()
    {
        mana.Update();

        barImage.fillAmount = mana.GetManaNormalized();
    }
}
public class Mana
{
    public const int MANA_MAX = 100;

    private Image barImage;
    private float manaAmount;
    private float manaRegenAmount;
    public Mana()
    {
        manaAmount = 0;
        manaRegenAmount = 30f;
    }

    public void Update()
    {
        manaAmount += manaRegenAmount * Time.deltaTime;
        manaAmount = Mathf.Clamp(manaAmount, 0f, MANA_MAX);


        if (Input.GetKeyDown(KeyCode.Q))
        {
            TakeDamage(20);
        }
    }

    public void TakeDamage(float damage)
    {
        manaAmount -= damage;
        barImage.fillAmount = manaAmount / 100f;
    }

    public void TrySpendMana(int amount)
    {
        if(manaAmount >= amount)
        {
            manaAmount -= amount;
        }
    }

    public float GetManaNormalized()
    {
        return manaAmount / MANA_MAX;
    }

}

