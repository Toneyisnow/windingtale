using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Common;

namespace WindingTale.UI.CanvasControls
{
    public class CreatureDialog : CanvasControl
    {
        public enum ShowType
        {
            SelectEquipItem = 1,
            SelectUseItem = 2,
            SelectAllItem = 3,
            SelectMagic = 4,
            ViewItem = 5,
            ViewMagic = 6,
        }

        public Canvas canvas;
        private Camera camera;

        public GameObject[] anchorItems;
        public GameObject[] anchorMagics;


        private DatoControl datoControl = null;
        private Action<int> onCallback = null;

        private FDCreature creature = null;

        private ShowType showType = ShowType.ViewItem;

        private Action<int> OnCallback = null;

        private List<ItemControl> itemControlls = null;
        private List<MagicControl> magicControlls = null;

        private Transform datoBase = null;
        private Transform containerBase = null;
        private Transform creatureDetailBase = null;


        private int lastSelectedIndex = -1;

        public void Initialize(Camera camera, FDCreature creature, ShowType showType, Action<int> callback)
        {
            this.gameObject.name = "CreatureDialog";

            canvas.worldCamera = camera;
            this.camera = camera;

            this.creature = creature;
            this.showType = showType;
            this.onCallback = callback;

            containerBase = this.transform.Find("Canvas/ContainerBase");
            Clickable clickable = containerBase.GetComponent<Clickable>();
            clickable.Initialize(() => { this.OnClicked(); });

            creatureDetailBase = this.transform.Find("Canvas/CreatureDetail");
            clickable = creatureDetailBase.gameObject.GetComponent<Clickable>();
            clickable.Initialize(() => { this.OnCancelled(); });
            datoBase = this.transform.Find("Canvas/DatoBase");
            datoControl = GameObjectExtension.CreateFromPrefab<DatoControl>("Prefabs/DatoControl");
            datoControl.Initialize(datoBase, creature.Definition.AnimationId, new Vector2(0, 0));
            //// datoControl.transform.localPosition = new Vector3(0, 0, 0);

            RenderDetails();

            if (IsItemDialog)
            {
                RenderItemsContainer();
            }
            else
            {
                RenderMagicsContainer();
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        private void OnClicked()
        {
            // if allows selection, then will not allow user clicking on the dialog
            if (AllowSelection)
            {
                return;
            }

            PickAtIndex(-1);
        }

        private void OnCancelled()
        {
            Debug.Log("CreatureDialog cancelled.");
            PickAtIndex(-1);
        }

        private bool AllowSelection
        {
            get
            {
                return showType.GetHashCode() >= 1 && showType.GetHashCode() <= 4;
            }
        }

        private bool IsItemDialog
        {
            get
            {
                return !(showType == ShowType.SelectMagic || showType == ShowType.ViewMagic);
            }
        }

        private void RenderDetails()
        {
            string name = LocalizedStrings.GetCreatureName(creature.Definition.AnimationId);
            RenderText(name, "Canvas/CreatureDetail/Name", FontAssets.FontSizeType.Normal);

            string race = LocalizedStrings.GetRaceName(creature.Definition.Race);
            RenderText(race, "Canvas/CreatureDetail/Race", FontAssets.FontSizeType.Normal);

            string occupation = LocalizedStrings.GetOccupationName(creature.Definition.Occupation);
            RenderText(occupation, "Canvas/CreatureDetail/Occupation", FontAssets.FontSizeType.Normal);

            // LV
            int level = creature.Data.Level;
            RenderText(StringUtils.Digit2(level), "Canvas/CreatureDetail/LV", FontAssets.FontSizeType.Digit);
            // EX
            int ex = creature.Data.Exp;
            RenderText(StringUtils.Digit2(ex), "Canvas/CreatureDetail/EX", FontAssets.FontSizeType.Digit);
            // MV
            int mv = creature.Data.CalculatedMv;
            RenderText(StringUtils.Digit2(mv), "Canvas/CreatureDetail/MV", FontAssets.FontSizeType.Digit);
            // AP
            int ap = creature.Data.CalculatedAp;
            RenderText(StringUtils.Digit2(ap), "Canvas/CreatureDetail/AP", FontAssets.FontSizeType.Digit);
            // DP
            int dp = creature.Data.CalculatedDp;
            RenderText(StringUtils.Digit2(dp), "Canvas/CreatureDetail/DP", FontAssets.FontSizeType.Digit);

            // DX
            int dx = creature.Data.Dx;
            RenderText(StringUtils.Digit2(dx), "Canvas/CreatureDetail/DX", FontAssets.FontSizeType.Digit);
            // HIT
            int hit = creature.Data.CalculatedHit;
            RenderText(StringUtils.Digit2(hit), "Canvas/CreatureDetail/HIT", FontAssets.FontSizeType.Digit);
            // EV
            int ev = creature.Data.CalculatedEv;
            RenderText(StringUtils.Digit2(ev), "Canvas/CreatureDetail/EV", FontAssets.FontSizeType.Digit);

            //HP
            int hp = creature.Data.Hp;
            RenderText(StringUtils.Digit3(hp), "Canvas/CreatureDetail/HP", FontAssets.FontSizeType.DigitSmall);
            //HP MAX
            int hpMax = creature.Data.HpMax;
            RenderText(StringUtils.Digit3(hpMax), "Canvas/CreatureDetail/HPMAX", FontAssets.FontSizeType.DigitSmall);
            //MP
            int mp = creature.Data.Mp;
            RenderText(StringUtils.Digit3(mp), "Canvas/CreatureDetail/MP", FontAssets.FontSizeType.DigitSmall);
            //MP MAX
            int mpMax = creature.Data.MpMax;
            RenderText(StringUtils.Digit3(mpMax), "Canvas/CreatureDetail/MPMAX", FontAssets.FontSizeType.DigitSmall);



        }

        private void RenderItemsContainer()
        {
            List<int> equipedItems = new List<int>() { creature.Data.AttackItemIndex, creature.Data.DefendItemIndex };
            itemControlls = new List<ItemControl>();
            
            for(int index = 0; index < creature.Data.Items.Count; index++)
            {
                int itemId = this.creature.Data.Items[index];
                bool isEquiped = equipedItems.Contains(index);

                /*
                ItemControl itemControl = GameObjectExtension.CreateFromPrefab<ItemControl>("Prefabs/ItemControl");
                itemControl.Initialize(this.canvas, itemId, isEquiped);
                itemControl.transform.parent = anchorItems[index].transform;
                itemControl.transform.localPosition = new Vector3(0, 0, 0);
                */

                int cIndex = index;
                ItemControl itemControl = GameObjectExtension.CreateFromPrefab<ItemControl>("Prefabs/ItemControl");
                itemControl.Initialize(this.canvas, itemId, isEquiped, () => { this.OnSelectedItem(cIndex); } );
                itemControl.transform.parent = anchorItems[index].transform;
                itemControl.transform.localPosition = new Vector3(0, 0, 0);
                itemControl.transform.localScale = new Vector3(1, 1, 1);

                itemControlls.Add(itemControl);
            }

        }

        private void RenderMagicsContainer()
        {
            magicControlls = new List<MagicControl>();

            for (int index = 0; index < creature.Data.Magics.Count; index++)
            {
                int magicId = this.creature.Data.Magics[index];

                int cIndex = index;
                MagicControl magicControl = GameObjectExtension.CreateFromPrefab<MagicControl>("Prefabs/MagicControl");
                magicControl.Initialize(this.canvas, magicId, () => { this.OnSelectedMagic(cIndex); });
                magicControl.transform.parent = anchorMagics[index].transform;
                magicControl.transform.localPosition = new Vector3(0, 0, 0);
                magicControl.transform.localScale = new Vector3(1, 1, 1);

                magicControlls.Add(magicControl);
            }
        }

        private void OnSelectedItem(int itemIndex)
        {
            //// Debug.Log("OnSelectedItem: " + itemIndex);

            if (!AllowSelection)
            {
                PickAtIndex(-1);
            }

            int itemId = creature.Data.Items[itemIndex];
            ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (showType == ShowType.SelectEquipItem && !item.IsEquipment())
            {
                return;
            }
            if (showType == ShowType.SelectUseItem && !item.IsUsable())
            {
                return;
            }

            if (itemIndex == lastSelectedIndex)
            {
                // Do selected action
                PickAtIndex(itemIndex);
            }

            if (lastSelectedIndex >= 0)
            {
                itemControlls[lastSelectedIndex].SetSelected(false);
            }

            itemControlls[itemIndex].SetSelected(true);
            lastSelectedIndex = itemIndex;
        }

        private void OnSelectedMagic(int magicIndex)
        {
            Debug.Log("OnSelectedMagic: " + magicIndex);

            if (!AllowSelection)
            {
                PickAtIndex(-1);
            }

            int magicId = creature.Data.Magics[magicIndex];
            MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);
            
            if (magicIndex == lastSelectedIndex)
            {
                // Do selected action
                PickAtIndex(magicIndex);
            }

            if (lastSelectedIndex >= 0)
            {
                magicControlls[lastSelectedIndex].SetSelected(false);
            }

            magicControlls[magicIndex].SetSelected(true);
            lastSelectedIndex = magicIndex;
        }

        private void PickAtIndex(int value)
        {
            SlidingAnimation sliding = datoBase.GetComponent<SlidingAnimation>();
            sliding.Close(() =>
            {
                if (this.onCallback != null)
                {
                    this.onCallback(value);
                }
            });

            sliding = creatureDetailBase.GetComponent<SlidingAnimation>();
            sliding.Close();
            
            sliding = containerBase.GetComponent<SlidingAnimation>();
            sliding.Close();
            
        }
    }
}