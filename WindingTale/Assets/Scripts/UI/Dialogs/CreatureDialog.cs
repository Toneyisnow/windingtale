using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Common;

namespace WindingTale.UI.CanvasControls
{
    public class CreatureDialog : MonoBehaviour
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
        private DatoControl datoControl = null;
        private Action<int> onClickCallback = null;

        private FDCreature creature = null;

        private ShowType showType = ShowType.ViewItem;

        private Action<int> OnCallback = null;


        public void Initialize(Camera camera, FDCreature creature, ShowType showType, Action<int> callback)
        {
            this.gameObject.name = "CreatureDialog";

            canvas.worldCamera = camera;

            this.creature = creature;
            this.showType = showType;
            this.onClickCallback = callback;

            Transform messageBoxBase = this.transform.Find("Canvas/ContainerBase");
            //// messageBoxBase.transform.localPosition = new Vector2(0, -150);
            Clickable clickable = messageBoxBase.gameObject.AddComponent<Clickable>();
            clickable.Initialize(() => { this.OnClicked(); });


            Transform datoBase = this.transform.Find("Canvas/DatoBase");
            datoControl = GameObjectExtension.CreateFromPrefab<DatoControl>("Prefabs/DatoControl");
            datoControl.Initialize(canvas, creature.Definition.AnimationId, new Vector2(0, 0));
            datoControl.transform.parent = datoBase;
            datoControl.transform.localPosition = new Vector3(0, 0, 0);

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
            Debug.Log("CreatureDialog clicked.");
            if (this.onClickCallback != null)
            {
                this.onClickCallback(0);
            }
        }

        private bool CanEdit
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
            RenderText(StringUtils.Digit3(hp), "Canvas/CreatureDetail/HP", FontAssets.FontSizeType.Digit);
            //HP MAX
            int hpMax = creature.Data.HpMax;
            RenderText(StringUtils.Digit3(hpMax), "Canvas/CreatureDetail/HPMAX", FontAssets.FontSizeType.Digit);
            //MP
            int mp = creature.Data.Mp;
            RenderText(StringUtils.Digit2(mp), "Canvas/CreatureDetail/MP", FontAssets.FontSizeType.Digit);
            //MP MAX
            int mpMax = creature.Data.MpMax;
            RenderText(StringUtils.Digit2(mpMax), "Canvas/CreatureDetail/MPMAX", FontAssets.FontSizeType.Digit);



        }

        private void RenderItemsContainer()
        {

        }

        private void RenderMagicsContainer()
        {

        }

        private void RenderText(string content, string anchorName, FontAssets.FontSizeType sizeType)
        {
            TextMeshPro textObj = FontAssets.ComposeTextMeshObject(content, sizeType);

            textObj.transform.parent = this.transform.Find(anchorName);
            textObj.rectTransform.pivot = new Vector2(0, 1);
            textObj.transform.localPosition = new Vector3(0, 0, 0);
            textObj.transform.localScale = new Vector3(5, 5, 1);
            textObj.gameObject.layer = 5;
        }

    }
}