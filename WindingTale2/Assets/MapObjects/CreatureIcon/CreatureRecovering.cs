using UnityEngine;

namespace WindingTale.MapObjects.CreatureIcon
{
    /// <summary>
    /// The "recovering" flash: the creature turns flat white for a short moment and
    /// then returns to its normal look. One state, no frames -- the shader does the
    /// work (see Custom/WhiteFlash).
    ///
    /// The component removes itself when the flash is over, which is what
    /// ActivityFactory.CreatureRecoverActivity polls for. Because the activity queue
    /// runs one activity at a time, several creatures recovering in the same batch
    /// (endTurnForAll) flash one after another rather than all at once.
    /// </summary>
    public class CreatureRecovering : MonoBehaviour
    {
        public const float FlashDuration = 0.1f;

        private float elapsed = 0f;
        private Creature creature = null;

        void Start()
        {
            creature = GetComponent<Creature>();
            creature?.SetWhiteFlash(true);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed < FlashDuration)
            {
                return;
            }

            creature?.SetWhiteFlash(false);
            Destroy(this);
        }

        void OnDestroy()
        {
            // Safety net: never leave the creature stuck white if the object is torn
            // down mid-flash (e.g. the creature dies while recovering).
            creature?.SetWhiteFlash(false);
        }
    }
}
