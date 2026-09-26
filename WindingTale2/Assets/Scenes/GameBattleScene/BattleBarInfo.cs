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
    ///
    /// The bar is also the prefab Resources/Others/CreatureBar, which the field screen
    /// shows top-right while a friend's move range is up (CreatureBarPanel). There the
    /// two optional extras are wired: levelLabel ("LV-03") and the hpFill / mpFill
    /// meshes, which this component then scales itself (from the left edge) -- in the
    /// battle the runners scale those and leave them unassigned here.
    /// </summary>
    public class BattleBarInfo : MonoBehaviour
    {
        public TextMeshPro creatureNameLabel;
        public TextMeshPro hpLabel;
        public TextMeshPro mpLabel;

        public TextMeshPro levelLabel;
        public GameObject hpFill;
        public GameObject mpFill;

        private FDCreature creature;

        // Creature names / occupations are CJK and live in dedicated font atlases; the
        // default (LiberationSans) has no such glyphs and renders boxes. These are the
        // same sheets the rest of the UI uses.
        private static TMP_FontAsset creatureFont;
        private static TMP_FontAsset occupationFont;

        public void Bind(FDCreature creature)
        {
            this.creature = creature;

            EnsureLevelLabel();

            if (creatureNameLabel != null)
            {
                string occupationName = "";
                OccupationDefinition occ = DefinitionStore.Instance.GetOccupationDefinition(creature.Definition.Occupation);
                if (occ != null)
                {
                    occupationName = occ.Name;
                }

                ApplyNameFont(creatureNameLabel);
                creatureNameLabel.text = creature.Definition.Name; // + "  " + occupationName;
            }

            if (levelLabel != null)
            {
                levelLabel.text = "LV-" + StringUtils.Digit2(creature.Level);
            }

            SetHp(creature.Hp);
            SetMp(creature.Mp);
        }

        // Where the LV label sits on the bar: the LevelLabel of
        // Resources/Others/CreatureBar, right-aligned so it ends at the bar's right edge.
        private static readonly Vector2 LevelLabelPosition = new Vector2(1.5f, 4.42f);

        /// <summary>
        /// The two bars authored in the battle scene have no LV label (only the prefab the
        /// field screen shows has one), so build it beside the name the first time a bar
        /// is bound: a copy of the name label -- same size, same row -- switched to the
        /// default Latin font and right-aligned, exactly as the prefab's LevelLabel is.
        /// </summary>
        private void EnsureLevelLabel()
        {
            if (levelLabel != null || creatureNameLabel == null)
            {
                return;
            }

            GameObject copy = Instantiate(creatureNameLabel.gameObject, creatureNameLabel.transform.parent, false);
            copy.name = "LevelLabel";

            levelLabel = copy.GetComponent<TextMeshPro>();
            levelLabel.font = TMP_Settings.defaultFontAsset;
            levelLabel.horizontalAlignment = HorizontalAlignmentOptions.Right;

            levelLabel.rectTransform.anchoredPosition = LevelLabelPosition;
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
                //// hpLabel.text = StringUtils.Digit3(current) + " / " + StringUtils.Digit3(creature.HpMax);
                hpLabel.text = StringUtils.Digit3(current);
            }

            if (creature != null)
            {
                ScaleFill(hpFill, current, creature.HpMax);
            }
        }

        public void SetMp(int current)
        {
            if (mpLabel != null && creature != null)
            {
                //// mpLabel.text = StringUtils.Digit3(current) + " / " + StringUtils.Digit3(creature.MpMax);
                mpLabel.text = StringUtils.Digit3(current);
            }

            if (creature != null)
            {
                ScaleFill(mpFill, current, creature.MpMax);
            }
        }

        /// <summary>
        /// Stretches a fill mesh to current / max of its full length. Same rule as the
        /// battle runners' bar scale: a zero max is an empty bar, overshoot is a full one.
        /// </summary>
        private static void ScaleFill(GameObject fill, int current, int max)
        {
            if (fill == null)
            {
                return;
            }

            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            fill.transform.localScale = new Vector3(ratio, 1f, 1f);
        }
    }
}
