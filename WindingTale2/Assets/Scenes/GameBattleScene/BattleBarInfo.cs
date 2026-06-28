using TMPro;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;

namespace WindingTale.Scenes.GameBattleScene
{
    /// <summary>
    /// Drives the pre-built text labels around a battle HP/MP bar. The label objects
    /// (CreatureName, HpLabel, MpLabel) are authored in the scene as children of the bar
    /// and wired into these references in the Inspector (same pattern as
    /// CreatureInfoDialog). This component only fills in their text at load:
    ///  - CreatureName: "&lt;name&gt;  &lt;occupation&gt;"
    ///  - HpLabel:      "&lt;currentHP&gt; / &lt;maxHP&gt;"
    ///  - MpLabel:      "&lt;currentMP&gt; / &lt;maxMP&gt;"
    /// </summary>
    public class BattleBarInfo : MonoBehaviour
    {
        public TextMeshPro creatureNameLabel;
        public TextMeshPro hpLabel;
        public TextMeshPro mpLabel;

        private FDCreature creature;

        // Creature names / occupations are CJK and live in dedicated font atlases; the
        // default (LiberationSans) has no such glyphs and renders boxes. These are the
        // same sheets the rest of the UI uses.
        private static TMP_FontAsset creatureFont;
        private static TMP_FontAsset occupationFont;

        public void Bind(FDCreature creature)
        {
            this.creature = creature;

            if (creatureNameLabel != null)
            {
                string occupationName = "";
                OccupationDefinition occ = DefinitionStore.Instance.GetOccupationDefinition(creature.Definition.Occupation);
                if (occ != null)
                {
                    occupationName = occ.Name;
                }

                ApplyNameFont(creatureNameLabel);
                creatureNameLabel.text = creature.Definition.Name + "  " + occupationName;
            }

            SetHp(creature.Hp);
            SetMp(creature.Mp);
        }

        /// <summary>
        /// Points the name label at the creature-name font atlas (FZB_Creature) so the
        /// name renders, with the occupation atlas (FZB_Occupation) as a fallback so the
        /// occupation text in the same label resolves too.
        /// </summary>
        private static void ApplyNameFont(TextMeshPro label)
        {
            if (creatureFont == null)
            {
                creatureFont = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/zh/FZB_Creature");
            }
            if (occupationFont == null)
            {
                occupationFont = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/zh/FZB_Occupation");
            }

            if (creatureFont == null)
            {
                Debug.LogWarning("[BattleBarInfo] Font 'Fonts/FontAssets/zh/FZB_Creature' not found.");
                return;
            }

            if (occupationFont != null
                && creatureFont.fallbackFontAssetTable != null
                && !creatureFont.fallbackFontAssetTable.Contains(occupationFont))
            {
                creatureFont.fallbackFontAssetTable.Add(occupationFont);
            }

            label.font = creatureFont;
        }

        public void SetHp(int current)
        {
            if (hpLabel != null && creature != null)
            {
                hpLabel.text = StringUtils.Digit3(current) + " / " + StringUtils.Digit3(creature.HpMax);
            }
        }

        public void SetMp(int current)
        {
            if (mpLabel != null && creature != null)
            {
                mpLabel.text = StringUtils.Digit3(current) + " / " + StringUtils.Digit3(creature.MpMax);
            }
        }
    }
}
