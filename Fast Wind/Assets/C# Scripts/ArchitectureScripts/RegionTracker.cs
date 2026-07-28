public static class RegionTracker
{
    public static string CurrentRegion { get; private set; }

    public static void SetCurrentRegion(string regionSceneName)
    {
        if (string.IsNullOrEmpty(regionSceneName))
            return;

        CurrentRegion = regionSceneName;
    }
}
