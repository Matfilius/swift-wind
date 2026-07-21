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
    [SerializeField] private string regionScene = "Tutorial_Region1";

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
        Scene menuScene = SceneManager.GetActiveScene();

        AsyncOperation gameplayOp = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);

        gameplayOp.allowSceneActivation = false;

        AsyncOperation regionOp = SceneManager.LoadSceneAsync(regionScene, LoadSceneMode.Additive);


        while (gameplayOp.progress < 0.9f)
        {
            loadingBar.fillAmount = gameplayOp.progress / 0.9f;
            yield return null;
        }

        gameplayOp.allowSceneActivation = true;

        while (!gameplayOp.isDone || !regionOp.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(gameplayScene));

        yield return SceneManager.UnloadSceneAsync(menuScene);
    }
}