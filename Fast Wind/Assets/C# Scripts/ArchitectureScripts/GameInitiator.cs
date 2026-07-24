using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameInitiator : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameObject corePrefab;

    [Header("First Scene")]
    [SerializeField] private string firstSceneName = "MainMenuScene";

    private void Start()
    {
        StartCoroutine(BootFlow());
    }

    private IEnumerator BootFlow()
    {
        CoreBootstrapper.EnsureExists(corePrefab);

        yield return null;

        // Mala pauza u vremenu za stabilnost
        yield return new WaitForSeconds(0.5f);

        // Ucitaj Glavni Meni
        SceneManager.LoadScene(firstSceneName);


    }
}