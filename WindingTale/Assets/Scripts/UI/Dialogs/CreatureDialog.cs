using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

            AddSubDialog(@"Others/ConfirmButtonYes", this.transform, new Vector3(200, 102, 2), new Vector3(1, 1, 1));
            AddSubDialog(@"Others/ConfirmButtonNo", this.transform, new Vector3(200, 102, 2), new Vector3(1, 1, 1));

            // LV
            AddText("01", detail.transform, new Vector3(1.52f, 2, -1.17f), new Vector3(0.3f, 0.3f, 1));
            // EX
            AddText("99", detail.transform, new Vector3(1.52f, 2, -0.12f), new Vector3(0.3f, 0.3f, 1));
            // MV
            AddText("04", detail.transform, new Vector3(1.52f, 2, 1.02f), new Vector3(0.3f, 0.3f, 1));
            // AP
            AddText("99", detail.transform, new Vector3(1.52f, 2, 2.22f), new Vector3(0.3f, 0.3f, 1));
            // DP
            AddText("99", detail.transform, new Vector3(1.52f, 2, 3.36f), new Vector3(0.3f, 0.3f, 1));
            
            // DX
            AddText("04", detail.transform, new Vector3(5.63f, 2, 1.02f), new Vector3(0.3f, 0.3f, 1));
            // HIT
            AddText("99", detail.transform, new Vector3(5.63f, 2, 2.22f), new Vector3(0.3f, 0.3f, 1));
            // EV
            AddText("99", detail.transform, new Vector3(5.63f, 2, 3.36f), new Vector3(0.3f, 0.3f, 1));

            //HP
            AddText("099", detail.transform, new Vector3(-9, 2, -0.5f), new Vector3(0.3f, 0.3f, 1));
            //HP MAX
            AddText("999", detail.transform, new Vector3(-12f, 2, -0.5f), new Vector3(0.3f, 0.3f, 1));
            //MP
            AddText("099", detail.transform, new Vector3(-9, 2, 1.5f), new Vector3(0.3f, 0.3f, 1));
            //MP MAX
            AddText("999", detail.transform, new Vector3(-12f, 2, 1.5f), new Vector3(0.3f, 0.3f, 1));

            // LV
            // AddText("01", this.transform, new Vector3(200, 102, -5), new Vector3(8, 8, 1));

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}