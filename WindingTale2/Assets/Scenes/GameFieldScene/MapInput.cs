using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WindingTale.Scenes.GameFieldScene
{
    /// <summary>
    /// The one switch for the player's control of the field map: keyboard, mouse clicks
    /// on tiles, creatures and menu items, and the camera controls all ask
    /// <see cref="IsBlocked"/> before they act on input, so shutting the map off is a
    /// single place.
    ///
    /// The map is blocked automatically while the battle animation scene is loaded on
    /// top of it. Anything else that should freeze the map (a cutscene, a full-screen
    /// overlay) calls <see cref="SetBlocked"/> with a reason of its own and clears it
    /// when done; the map stays blocked while any reason is standing.
    /// </summary>
    public static class MapInput
    {
        private const string BattleSceneName = "GameBattleScene";

        private static readonly HashSet<string> reasons = new HashSet<string>();

        /// <summary>Whether the map should ignore the player's input right now.</summary>
        public static bool IsBlocked
        {
            get { return reasons.Count > 0 || IsBattleSceneLoaded(); }
        }

        /// <summary>
        /// Adds or removes a named reason for blocking the map. Blocking twice for the
        /// same reason is one block, and any one <c>SetBlocked(reason, false)</c> lifts it.
        /// </summary>
        public static void SetBlocked(string reason, bool blocked)
        {
            if (blocked)
            {
                reasons.Add(reason);
            }
            else
            {
                reasons.Remove(reason);
            }
        }

        private static bool IsBattleSceneLoaded()
        {
            // The scene counts from the moment it starts loading (it is not valid, let
            // alone loaded, for the first frames) until it is gone.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == BattleSceneName)
                {
                    return true;
                }
            }

            return false;
        }

        // Static state survives a play-mode restart when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            reasons.Clear();
        }
    }
}
