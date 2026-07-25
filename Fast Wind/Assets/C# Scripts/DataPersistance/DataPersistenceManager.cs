using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : MonoBehaviour
{

    [Header("File Storage Config")]

    [SerializeField] private string fileName;

    [SerializeField] private GameObject corePrefab;
    [SerializeField] private string gameplaySceneName = "GameplayScene";

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

    private void Start()
    {
        dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameplaySceneName)
            return;

        dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();
    }

    public void NewGame()
    {
        this.gameData = new GameData();
    }

    public void LoadGame()
    {

        this.gameData = dataHandler.Load();


        if (this.gameData == null)
        {
            Debug.Log("No game data found. Initializing default values.");
            NewGame();
        }

        if (dataPersistenceObjects == null)
            dataPersistenceObjects = FindAllDataPersistenceObjects();

        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
            dataPersistenceObj.LoadData(gameData);
    }

    public void SaveGame()
    {
        if (gameData == null)
        {
            Debug.LogWarning("No data was found. A New Game needs to be started before data can be saved.");
            return;
        }

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

    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<IDataPersistence>()
            .ToList();
    }
}
