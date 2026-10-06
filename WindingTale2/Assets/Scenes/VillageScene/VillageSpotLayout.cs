using UnityEngine;

/// <summary>
/// Where the cursor may stand in each village picture, in world coordinates on the cursor
/// plane: six spots per village (1-3), by position index. Index 0 is the way on to the
/// next chapter (see VillageScene.OnProceed), indices 1-5 are the shops, each matched to
/// the shop of the same index. Index 5 is only reachable by the secret route, not the
/// left/right walk.
///
/// Kept as an asset (Resources/Village/VillageSpotLayout) rather than in code so the spots
/// can be tuned against the picture in the editor: turn on VillageScene.spotTuningMode
/// and play the village scene. A ScriptableObject edited in play mode keeps its changes.
/// </summary>
[CreateAssetMenu(fileName = "VillageSpotLayout", menuName = "WindingTale/Village Spot Layout")]
public class VillageSpotLayout : ScriptableObject
{
    public const string ResourcePath = "Village/VillageSpotLayout";

    /// <summary>How many spots each village has: pos 0 through pos 5.</summary>
    public const int SpotCount = 6;

    // The defaults are village 1's tuned layout. Villages 2 and 3 start from it as a
    // placeholder until they are tuned against their own pictures.
    public Vector2[] village1 = DefaultSpots();
    public Vector2[] village2 = DefaultSpots();
    public Vector2[] village3 = DefaultSpots();

    private static Vector2[] DefaultSpots()
    {
        return new[]
        {
            new Vector2(-6.4f, -5.2f), // pos 0 -- proceed to next chapter
            new Vector2(-8.2f, -1.8f), // pos 1 -- shop 1
            new Vector2(-8.9f, 3.3f),  // pos 2 -- shop 2
            new Vector2(3.2f, 1.8f),   // pos 3 -- shop 3
            new Vector2(1.2f, -3.6f),  // pos 4 -- shop 4
            new Vector2(-1.8f, 5.7f),  // pos 5 -- shop 5, special route
        };
    }

    /// <summary>
    /// The live spot array of a village -- the asset's own, so writing into it edits the
    /// layout. An unknown village id gets village 1's.
    /// </summary>
    public Vector2[] GetSpots(int villageId)
    {
        switch (villageId)
        {
            case 2: return village2;
            case 3: return village3;
            default: return village1;
        }
    }

    /// <summary>The layout asset, or the built-in defaults if the asset is missing.</summary>
    public static VillageSpotLayout Load()
    {
        VillageSpotLayout layout = Resources.Load<VillageSpotLayout>(ResourcePath);
        if (layout == null)
        {
            Debug.LogWarning("Cannot load village spot layout: Resources/" + ResourcePath + "; using the defaults.");
            layout = CreateInstance<VillageSpotLayout>();
        }

        return layout;
    }

    private void OnValidate()
    {
        // Keep every village at exactly six spots, whatever the inspector did to the arrays.
        village1 = Resize(village1);
        village2 = Resize(village2);
        village3 = Resize(village3);
    }

    private static Vector2[] Resize(Vector2[] spots)
    {
        if (spots != null && spots.Length == SpotCount)
        {
            return spots;
        }

        Vector2[] resized = DefaultSpots();
        for (int i = 0; spots != null && i < spots.Length && i < SpotCount; i++)
        {
            resized[i] = spots[i];
        }

        return resized;
    }
}
