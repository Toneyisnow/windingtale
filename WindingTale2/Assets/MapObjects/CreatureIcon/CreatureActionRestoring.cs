using UnityEngine;

namespace WindingTale.MapObjects.CreatureIcon
{
    /// <summary>
    /// The 行动术 (406) flash: a creature given a fresh action flickers between the grey
    /// "already acted" look and its normal one FlashCount times, FlashPeriod a flicker
    /// (grey for the first half, normal for the second), and ends on its normal look.
    ///
    /// The turn state itself is reset before this starts (Creature.ResetTurnState); this
    /// only swaps the materials. The component removes itself when the flicker is over,
    /// which is what ActivityFactory.CreatureActionRestoredActivity polls for.
    /// </summary>
    public class CreatureActionRestoring : MonoBehaviour
    {
        public const int FlashCount = 8;
        public const float FlashPeriod = 0.25f;

        private float elapsed = 0f;
        private bool isGrey = false;
        private Creature creature = null;

        void Start()
        {
            creature = GetComponent<Creature>();
            SetGrey(true);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= FlashCount * FlashPeriod)
            {
                SetGrey(false);
                Destroy(this);
                return;
            }

            SetGrey((elapsed % FlashPeriod) < FlashPeriod / 2f);
        }

        void OnDestroy()
        {
            // Never leave the creature looking as if it had already acted.
            SetGrey(false);
        }

        private void SetGrey(bool grey)
        {
            if (grey == isGrey || creature == null)
            {
                return;
            }

            creature.SetGreyout(grey);
            isGrey = grey;
        }
    }
}
