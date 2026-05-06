using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameInitiator : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameObject corePrefab;

    [Header("First Scene")]
    [SerializeField] private string firstSceneName = "MainMenuScene";

    private static bool coreSpawned = false;

    private void Start()
    {
        StartCoroutine(BootFlow());
    }

    private IEnumerator BootFlow()
    {
        // Zaustavlja duplo kporianje korijena
        if (!coreSpawned)
        {
            Instantiate(corePrefab);
            coreSpawned = true;
        }

        
        yield return null;

        // Mala pauza u vremenu za stabilnost
        yield return new WaitForSeconds(0.5f);

        // Ucitaj Glavni Meni
        SceneManager.LoadScene(firstSceneName);


    }
}