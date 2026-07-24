using UnityEngine;

public static class CoreBootstrapper
{
    public static void EnsureExists(GameObject corePrefab)
    {
        if (corePrefab == null)
            return;

        if (Object.FindFirstObjectByType<GameEventsManager>() != null)
            return;

        Object.Instantiate(corePrefab);
    }
}
