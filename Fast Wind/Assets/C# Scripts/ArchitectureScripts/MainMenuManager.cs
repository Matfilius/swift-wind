using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject loadingBarObject;
    [SerializeField] private Image loadingBar;
    [SerializeField] private GameObject[] objectsToHide;

    [Header("Scenes")]
    [SerializeField] private string gameplayScene = "GameplayScene";

    [Header("Core")]
    [SerializeField] private GameObject corePrefab;

    private void Awake()
    {
        loadingBarObject.SetActive(false);
    }

    private void Start()
    {
        EnsureCoreExists();
    }

    // Provjerava da korjien postoji
    private void EnsureCoreExists()
    {
        if (FindFirstObjectByType<GameManager>() == null)
        {
            Instantiate(corePrefab);
        }
    }

    public void StartGame()
    {
        HideMenu();
        loadingBarObject.SetActive(true);

        StartCoroutine(LoadGameplay());
    }

    private void HideMenu()
    {
        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] != null)
                objectsToHide[i].SetActive(false);
        }
    }

    private IEnumerator LoadGameplay()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameplayScene);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBar.fillAmount = progress;

            // Aktiviraj scenu kad je skroz ucitano
            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.2f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}