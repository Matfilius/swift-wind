using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitiator : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string coreSceneName = "Core";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private async void Start()
    {
        await StartGameFlow();
    }

    private async Task StartGameFlow()
    {
        // 1. Ucita korijene sisteme prvo
        await LoadSceneAsync(coreSceneName, LoadSceneMode.Additive);

        // Mala pauza u vremenu za stabilnost
        await Task.Delay(200);

        // 2. Ucita se glavni meni
        await LoadMainMenu();

        // 3. Skloni se boot scena
        await UnloadSceneAsync(SceneManager.GetActiveScene().name);
    }

    private async Task LoadMainMenu()
    {
        await LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
    }

    private async Task LoadGameplayFlow()
    {
        await LoadSceneAsync(gameplaySceneName, LoadSceneMode.Additive);

    }


    private async Task LoadSceneAsync(string sceneName, LoadSceneMode mode)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
            return;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);

        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private async Task UnloadSceneAsync(string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            return;

        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }
}

