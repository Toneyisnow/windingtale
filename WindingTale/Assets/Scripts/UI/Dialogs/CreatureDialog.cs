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
            GameObject dato = AddControl(@"Others/CreatureDato", this.transform, new Vector3(-352, 202, 0), new Vector3(30, 1, 30));
            GameObject detail = AddControl(@"Others/CreatureDetail", this.transform, new Vector3(144, 202, 0), new Vector3(30, 1, 30));
            GameObject container = AddControl(@"Others/ContainerBase", this.transform, new Vector3(-5, -126, 0), new Vector3(37, 1, 37),
                () => { Debug.Log("Clicked Container."); OnCallback(1); });

            AddControl(@"Others/ConfirmButtonYes", this.transform, new Vector3(200, 102, 2), new Vector3(1, 1, 1));
            AddControl(@"Others/ConfirmButtonNo", this.transform, new Vector3(200, 102, 2), new Vector3(1, 1, 1));


        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}