using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
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
        public GameObject selectable_0;
        public GameObject selectable_1;
        public GameObject selectable_2;
        public GameObject selectable_3;
        public GameObject selectable_4;
        public GameObject selectable_5;
        public GameObject selectable_6;
        public GameObject selectable_7;


        private FDCreature creature = null;
        private CreatureInfoType infoType = CreatureInfoType.View;
        private Action<int> onSelected = null;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        public void Init(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected)
        {
            this.creature = creature;
            this.infoType = infoType;
            this.onSelected = onSelected;

            if (infoType != CreatureInfoType.SelectMagic)
            {
                for(int itemIndex = 0; itemIndex < creature.Items.Count; itemIndex ++)
                {
                    int itemId = creature.Items[itemIndex];
                    ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
                    
                    GameObject selectable = getSelectableObject(itemIndex);
                    selectable.SetActive(true);
                    var selectableText = selectable.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableText.text = item.Name;
                    var selectableButton = selectable.GetComponent<Button>();
                    selectableButton.onClick.AddListener(delegate { 
                        onSelected(itemIndex);
                        GameMain.getDefault().gameCanvas.CloseDialog();
                    });
                }
                for (int itemIndex = creature.Items.Count; itemIndex < 8; itemIndex++)
                {
                    GameObject selectable = getSelectableObject(itemIndex);
                    selectable.SetActive(false);
                }

            } else
            {
                /// TODO: Show magic list
            }





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


        private GameObject getSelectableObject(int index)
        {
            switch (index)
            {
                case 0:
                    return selectable_0;
                case 1:
                    return selectable_1;
                case 2:
                    return selectable_2;
                case 3:
                    return selectable_3;
                case 4:
                    return selectable_4;
                case 5:
                    return selectable_5;
                case 6:
                    return selectable_6;
                case 7:
                    return selectable_7;
            }
            return null;
        }   



    }
}
