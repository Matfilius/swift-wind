using UnityEngine;
using System.Linq;
using NUnit.Framework;

public class DataPersistenceManager : MonoBehaviour
{

    private GameData gameData;

    public static DataPersistenceManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Multiple Data Persistance Managers in scene.");
        }
        instance = this;
    }

    private void Start()
    {
        this.dataPersistenceObjects = FindAllDataPersistanceObjects();
        LoadGame();
    }

    public void NewGame()
    {
        this.gameData = new GameData();
    }

    public void SaveGame()
    {

    }

    public void LoadGame()
    {
        if (this.gameData == null)
        {
            Debug.Log("No game data found. Default loaded");
            NewGame();
        }

    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {

    }
}
