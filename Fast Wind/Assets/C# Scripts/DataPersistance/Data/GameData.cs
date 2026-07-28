using UnityEngine;

[System.Serializable]
public class GameData
{
    public int deathCount;
    public Vector3 playerPosition;
    public string currentRegionScene;
    public string lastCheckpointId;

    public GameData()
    {
        deathCount = 0;
        playerPosition = Vector3.zero;
        currentRegionScene = "";
        lastCheckpointId = "";
    }
}
