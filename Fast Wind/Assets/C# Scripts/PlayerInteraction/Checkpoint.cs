using UnityEngine;

public class Checkpoint : MonoBehaviour, IDataPersistence
{
    [SerializeField] private string id;
    [SerializeField] private SceneField regionScene;

    private bool isActivated;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            Activate();
    }

    public void Activate()
    {
        Checkpoint[] checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        foreach (Checkpoint checkpoint in checkpoints)
            checkpoint.isActivated = false;

        isActivated = true;
        RegionTracker.SetCurrentRegion(regionScene);

        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.SaveGame();
    }

    public void LoadData(GameData data)
    {
        isActivated = !string.IsNullOrEmpty(data.lastCheckpointId) && data.lastCheckpointId == id;
    }

    public void SaveData(ref GameData data)
    {
        if (!isActivated)
            return;

        data.lastCheckpointId = id;
        data.playerPosition = transform.position;
        data.currentRegionScene = regionScene;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate GUID for ID")]
    private void GenerateGuid()
    {
        id = System.Guid.NewGuid().ToString();
    }
#endif
}
