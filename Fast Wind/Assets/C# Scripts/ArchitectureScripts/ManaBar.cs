using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    public Mana mana;  
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
    public static int MANA_MAX = 100;
    private float manaAmount;
    private float manaRegenAmount;

    public Mana()
    {
        manaAmount = 100;
        manaRegenAmount = 30f;
    }

    public void ModifyMaxMana(float amount)
    {
        // Adjust the cap — manaAmount stays the same unless it now exceeds the new cap
        MANA_MAX += (int)amount;
        manaAmount = Mathf.Clamp(manaAmount, 0f, MANA_MAX);
    }

    public void Update()
    {
        manaAmount += manaRegenAmount * Time.deltaTime/10;
        manaAmount = Mathf.Clamp(manaAmount, 0f, MANA_MAX);
    }

    public bool TrySpendMana(float amount)
    {
        if (manaAmount >= amount)
        {
            manaAmount -= amount;
            return true;
        }
        return false;
    }

    public float GetManaNormalized()
    {
        return manaAmount / MANA_MAX;
    }
}