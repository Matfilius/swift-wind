using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : MonoBehaviour
{
    [Header("File Storage Config")]
    [SerializeField] private string fileName;

    [Header("Scenes")]
    [SerializeField] private GameObject corePrefab;
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    [SerializeField] private string defaultRegionScene = "Tutorial_Region1";

    private GameData gameData;
    private List<IDataPersistence> dataPersistenceObjects;

    private FileDataHandler dataHandler;

    public static DataPersistenceManager instance { get; private set; }

    private void Awake()
    {
        CoreBootstrapper.EnsureExists(corePrefab);

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        Debug.Log("Save file path: " + Path.Combine(Application.persistentDataPath, fileName));
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameplaySceneName)
            return;

        StartCoroutine(InitializeGame());
    }

    public void NewGame()
    {
        gameData = new GameData();
        gameData.currentRegionScene = defaultRegionScene;
    }

    public void LoadGame()
    {
        StartCoroutine(InitializeGame());
    }

    public void SaveGame()
    {
        if (gameData == null)
        {
            Debug.LogWarning("No data was found. A New Game needs to be started before data can be saved.");
            return;
        }

        if (!string.IsNullOrEmpty(RegionTracker.CurrentRegion))
            gameData.currentRegionScene = RegionTracker.CurrentRegion;

        if (dataPersistenceObjects == null)
            dataPersistenceObjects = FindAllDataPersistenceObjects();

        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
            dataPersistenceObj.SaveData(ref gameData);

        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private IEnumerator InitializeGame()
    {
        gameData = dataHandler.Load();

        if (gameData == null)
        {
            Debug.Log("No game data found. Initializing default values.");
            NewGame();
        }

        string regionToLoad = string.IsNullOrEmpty(gameData.currentRegionScene)
            ? defaultRegionScene
            : gameData.currentRegionScene;

        yield return EnsureRegionLoaded(regionToLoad);

        RegionTracker.SetCurrentRegion(regionToLoad);

        dataPersistenceObjects = FindAllDataPersistenceObjects();

        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
            dataPersistenceObj.LoadData(gameData);
    }

    private IEnumerator EnsureRegionLoaded(string regionSceneName)
    {
        if (IsSceneLoaded(regionSceneName))
            yield break;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(regionSceneName, LoadSceneMode.Additive);

        while (loadOperation != null && !loadOperation.isDone)
            yield return null;
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        }

        return false;
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<IDataPersistence>()
            .ToList();
    }
}
