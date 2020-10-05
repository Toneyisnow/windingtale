using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;

namespace WindingTale.UI.Dialogs
{

    public class CreatureDialog : CanvasDialog
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

        private FDCreature creature = null;

        private ShowType showType = ShowType.ViewItem;

        private Action<int> OnCallback = null;

        public void Initialize(Canvas canvas, FDCreature creature, ShowType showType, Action<int> callback = null)
        {
            if (creature == null)
            {
                throw new ArgumentNullException("creature");
            }

            base.Initialize(canvas);

            this.OnCallback = callback;
            this.gameObject.name = "CreatureDialog";

            this.transform.localPosition = new Vector3(0, 0, 0);
            this.transform.localScale = new Vector3(1f, 1f, 1f);

            this.creature = creature;
            this.showType = showType;
        }

        // Start is called before the first frame update
        void Start()
        {
            GameObject dato = AddSubDialog(@"Others/CreatureDato", this.transform, new Vector3(-352, 202, 0), new Vector3(30, 1, 30));
            GameObject detail = AddSubDialog(@"Others/CreatureDetail", this.transform, new Vector3(144, 202, 0), new Vector3(30, 1, 30));
            GameObject container = AddSubDialog(@"Others/ContainerBase", this.transform, new Vector3(-5, -126, 0), new Vector3(37, 1, 37),
                () => { Debug.Log("Clicked Container."); OnCallback(1); });

            // AddSubDialog(@"Others/ConfirmButtonYes", this.transform, new Vector3(200, 102, 2), new Vector3(1, 1, 1));
            // AddSubDialog(@"Others/ConfirmButtonNo", this.transform, new Vector3(200, 102, 2), new Vector3(1, 1, 1));

            AddToDetails(detail);

            if (showType == ShowType.SelectMagic || showType == ShowType.ViewMagic)
            {
                AddMagicsToContainer(container);
            }
            else
            {
                AddItemsToContainer(container);
            }
        }

        void AddToDetails(GameObject detail)
        {
            // LV
            int level = creature.Data.Level;
            AddText(StringUtils.Digit2(level), detail.transform, new Vector3(1.52f, 2, -1.17f), new Vector3(0.3f, 0.3f, 1));
            // EX
            int ex = creature.Data.Ex;
            AddText(StringUtils.Digit2(ex), detail.transform, new Vector3(1.52f, 2, -0.12f), new Vector3(0.3f, 0.3f, 1));
            // MV
            int mv = creature.Data.CalculatedMv;
            AddText(StringUtils.Digit2(mv), detail.transform, new Vector3(1.52f, 2, 1.02f), new Vector3(0.3f, 0.3f, 1));
            // AP
            int ap = creature.Data.CalculatedAp;
            AddText(StringUtils.Digit2(ap), detail.transform, new Vector3(1.52f, 2, 2.22f), new Vector3(0.3f, 0.3f, 1));
            // DP
            int dp = creature.Data.CalculatedDp;
            AddText(StringUtils.Digit2(dp), detail.transform, new Vector3(1.52f, 2, 3.36f), new Vector3(0.3f, 0.3f, 1));

            // DX
            int dx = creature.Data.CalculatedDx;
            AddText(StringUtils.Digit2(dx), detail.transform, new Vector3(5.63f, 2, 1.02f), new Vector3(0.3f, 0.3f, 1));
            // HIT
            int hit = creature.Data.CalculatedHit;
            AddText(StringUtils.Digit2(hit), detail.transform, new Vector3(5.63f, 2, 2.22f), new Vector3(0.3f, 0.3f, 1));
            // EV
            int ev = creature.Data.CalculatedEv;
            AddText(StringUtils.Digit2(ev), detail.transform, new Vector3(5.63f, 2, 3.36f), new Vector3(0.3f, 0.3f, 1));

            //HP
            int hp = creature.Data.Hp;
            AddText(StringUtils.Digit3(hp), detail.transform, new Vector3(-9, 2, -0.5f), new Vector3(0.3f, 0.3f, 1));
            //HP MAX
            int hpMax = creature.Data.HpMax;
            AddText(StringUtils.Digit3(hpMax), detail.transform, new Vector3(-12f, 2, -0.5f), new Vector3(0.3f, 0.3f, 1));
            //MP
            int mp = creature.Data.Mp;
            AddText(StringUtils.Digit3(mp), detail.transform, new Vector3(-9, 2, 1.5f), new Vector3(0.3f, 0.3f, 1));
            //MP MAX
            int mpMax = creature.Data.MpMax;
            AddText(StringUtils.Digit3(mpMax), detail.transform, new Vector3(-12f, 2, 1.5f), new Vector3(0.3f, 0.3f, 1));

            // LV
            // AddText("01", this.transform, new Vector3(200, 102, -5), new Vector3(8, 8, 1));

        }

        void AddItemsToContainer(GameObject container)
        {
            float zOrder = 2f;

            float intervalX = -12f;
            float intervalY = 1.7f;

            float startX = 14.0f;
            float startY = -2.55f;

            float xOffsetIcon = -1.0f;
            float xOffsetName = -3.0f;
            float xOffsetAttr = -7.2f;
            float xOffsetValue = -10f;

            Vector3 scale = new Vector3(0.35f, 0.35f, 1);
            for(int i =  0; i < creature.Data.Items.Count; i++)
            {
                int x = i / 4;
                int y = i % 4;

                float baseX = startX + intervalX * x;
                float baseY = startY + intervalY * y;

                // Icon
                AddControl("Others/IconAttack_1", container.transform, new Vector3(baseX + xOffsetIcon, zOrder, baseY), new Vector3(1f, 1f, 1f));

                // Name
                AddText("炎龙剑", container.transform, new Vector3(baseX + xOffsetName, zOrder, baseY), scale);

                // Attr
                AddText("+AP", container.transform, new Vector3(baseX + xOffsetAttr, zOrder, baseY), scale);

                // Value
                int value = 15;
                AddText(StringUtils.Digit3(value), container.transform, new Vector3(baseX + xOffsetValue, zOrder, baseY), scale);

            }
        }

        void AddMagicsToContainer(GameObject container)
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}