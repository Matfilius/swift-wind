using System;

public static class RegionTracker
{
    public static event Action<string> RegionChanged;

    public static string CurrentRegion { get; private set; }

    public static void SetCurrentRegion(string regionSceneName)
    {
        if (string.IsNullOrEmpty(regionSceneName) || CurrentRegion == regionSceneName)
            return;

        CurrentRegion = regionSceneName;
        RegionChanged?.Invoke(regionSceneName);
    }
}
