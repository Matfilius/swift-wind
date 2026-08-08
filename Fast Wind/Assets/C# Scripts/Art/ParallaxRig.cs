using UnityEngine;

public class ParallaxRig : MonoBehaviour
{
    [System.Serializable]
    public class Profile
    {
        public string name;
        public GameObject group;
        public string[] regionScenes;
    }

    [SerializeField] private Profile[] profiles;

    private void OnEnable()
    {
        RegionTracker.RegionChanged += Apply;
        Apply(RegionTracker.CurrentRegion);
    }

    private void OnDisable()
    {
        RegionTracker.RegionChanged -= Apply;
    }

    private void Apply(string regionScene)
    {
        if (profiles == null || profiles.Length < 2)
            return;

        Profile target = Resolve(regionScene);

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i].group != null)
                profiles[i].group.SetActive(profiles[i] == target);
        }
    }

    private Profile Resolve(string regionScene)
    {
        for (int i = 0; i < profiles.Length; i++)
        {
            string[] regions = profiles[i].regionScenes;
            if (regions == null)
                continue;

            for (int j = 0; j < regions.Length; j++)
            {
                if (regions[j] == regionScene)
                    return profiles[i];
            }
        }

        return profiles[0];
    }
}
