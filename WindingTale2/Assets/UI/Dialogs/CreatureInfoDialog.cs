using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.UI.Dialogs
{
    public enum CreatureInfoType
    {
        SelectEquipItem = 1,
        SelectUseItem = 2,
        SelectAllItem = 3,
        SelectMagic = 4,
        View = 5,
    }

    public class CreatureInfoDialog : MonoBehaviour
    {
        private Action<int> onSelected = null;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        public void Init(Action<int> onSelected)
        {
            this.onSelected = onSelected;
        }


        public void onSelected0()
        {
            this.onSelected(0);
            GameMain.getDefault().gameCanvas.CloseDialog();
        }

        public void onSelected1()
        {
            this.onSelected(1);
            GameMain.getDefault().gameCanvas.CloseDialog();
        }

        public void onSelected2()
        {
            this.onSelected(2);
            GameMain.getDefault().gameCanvas.CloseDialog();
        }

        public void onSelected3()
        {
            this.onSelected(3);
            GameMain.getDefault().gameCanvas.CloseDialog();
        }


        public void onCancel()
        {
            this.onSelected(-1);
            GameMain.getDefault().gameCanvas.CloseDialog();
        }

    }
}
