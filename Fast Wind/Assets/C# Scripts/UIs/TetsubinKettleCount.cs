using UnityEngine;
using UnityEngine.UI;

public class TetsubinKettleCount : MonoBehaviour
{
    public int tetsubinKettleNumber;
    public int numTetsubinKettle;

    public Image[] kettles;
    public Sprite tetsubinKettle;
    public Sprite darkTetsubinKettle;

    public Color fullColor = Color.white;
    public Color usedColor = new Color(1f, 1f, 1f, 0.55f);

    public void Update()
    {
        if(tetsubinKettleNumber > numTetsubinKettle)
        {
            tetsubinKettleNumber = numTetsubinKettle;
        }

        for(int i = 0; i < kettles.Length; i++)
        {
            if(i < tetsubinKettleNumber)
            {
                kettles[i].sprite = tetsubinKettle;
                kettles[i].color = fullColor;
            }
            else
            {
                kettles[i].sprite = darkTetsubinKettle;
                kettles[i].color = usedColor;
            }

            if (i < numTetsubinKettle)
            {
                kettles[i].enabled = true;
            }
            else
            {
                kettles[i].enabled = false;
            }
        }
    }
}
