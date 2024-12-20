using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using WindingTale.Core.Common;
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
        ViewMagic = 6,
    }

    public class CreatureInfoDialog : MonoBehaviour
    {

        public GameObject nameLabel;
        public GameObject raceLabel;
        public GameObject occupationLabel;

        public GameObject hpCurrentLabel;
        public GameObject hpMaxLabel;
        public GameObject mpCurrentLabel;
        public GameObject mpMaxLabel;


        public GameObject levelLabel;
        public GameObject apLabel;
        public GameObject dpLabel;
        public GameObject dxLabel;
        public GameObject expLabel;

        public GameObject itemsContainer;
        public GameObject magicContainer;

        public GameObject selectable_0;
        public GameObject selectable_1;
        public GameObject selectable_2;
        public GameObject selectable_3;
        public GameObject selectable_4;
        public GameObject selectable_5;
        public GameObject selectable_6;
        public GameObject selectable_7;

        public GameObject magic_0;
        public GameObject magic_1;
        public GameObject magic_2;
        public GameObject magic_3;
        public GameObject magic_4;
        public GameObject magic_5;
        public GameObject magic_6;
        public GameObject magic_7;
        public GameObject magic_8;
        public GameObject magic_9;
        public GameObject magic_10;
        public GameObject magic_11;





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

            bool isMagic = infoType == CreatureInfoType.SelectMagic || infoType == CreatureInfoType.ViewMagic;
            itemsContainer.SetActive(!isMagic);
            magicContainer.SetActive(isMagic);

            // Name
            this.nameLabel.GetComponent<TextMeshProUGUI>().text = creature.Definition.Name;

            // Race
            int raceId = creature.Definition.Race;
            this.raceLabel.GetComponent<LocalizeStringEvent>().StringReference = LocalizationManager.GetRaceString(creature.Definition.Race);

            int occupationId = creature.Definition.Occupation;
            OccupationDefinition occupation = DefinitionStore.Instance.GetOccupationDefinition(occupationId);
            this.occupationLabel.GetComponent<TextMeshProUGUI>().text = occupation.Name;

            this.hpCurrentLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.Hp);
            this.hpMaxLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.HpMax);
            this.mpCurrentLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.Mp);
            this.mpMaxLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.MpMax);

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
                for (int magicIndex = 0; magicIndex < creature.Magics.Count; magicIndex++)
                {
                    int magicId = creature.Magics[magicIndex];
                    MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);

                    GameObject magicObject = getMagicObject(magicIndex);
                    magicObject.SetActive(true);
                    var selectableText = magicObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableText.text = magic.Name;
                    var selectableButton = magicObject.GetComponent<Button>();
                    selectableButton.onClick.AddListener(delegate {
                        onSelected(magicIndex);
                        GameMain.getDefault().gameCanvas.CloseDialog();
                    });
                }
                for (int magicIndex = creature.Magics.Count; magicIndex < 12; magicIndex++)
                {
                    GameObject selectable = getMagicObject(magicIndex);
                    selectable.SetActive(false);
                }
            }





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

        private GameObject getMagicObject(int index)
        {
            switch (index)
            {
                case 0:
                    return magic_0;
                case 1:
                    return magic_1;
                case 2:
                    return magic_2;
                    case 3:
                    return magic_3;
                    case 4:
                    return magic_4;
                    case 5:
                    return magic_5;
                    case 6:
                    return magic_6;
                    case 7:
                    return magic_7;
                    case 8:
                    return magic_8;
                    case 9:
                    return magic_9;
                    case 10:
                    return magic_10;
                    case 11:
                    return magic_11;

            }
            return null;
        }

    }
}
