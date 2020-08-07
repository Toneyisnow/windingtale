using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Common;

namespace WindingTale.UI.MapObjects
{
    public class UICreature : UIObject
    {
        public enum AnimateState
        {
            Idle,
            WalkLeft,
            WalkRight,
            WalkUp,
            WalkDown,
        }

        private GameObject icon1 = null;
        private GameObject icon2 = null;
        private GameObject icon3 = null;

        public FDCreature Creature
        {
            get; private set;
        }

        private AnimateState animateState = AnimateState.Idle;

        private int aniIndex = 0;
        private int tickCount = 0;

        public UICreature()
        {

        }

        public void Initialize(FDCreature creature)
        {
            Material defaultMaterial = Resources.Load<Material>(@"common-mat");

            this.Creature = creature;
            this.animateState = AnimateState.Idle;

            string animationIdStr = StringUtils.FormatDigit3(creature.Definition.AnimationId);
            //// icon1 = GameObjectExtension.CreateFromObj(string.Format(@"Icons/{0}/Icon_{0}_01", animationIdStr), this.transform);
            //// icon2 = GameObjectExtension.CreateFromObj(string.Format(@"Icons/{0}/Icon_{0}_02", animationIdStr), this.transform);
            //// icon3 = GameObjectExtension.CreateFromObj(string.Format(@"Icons/{0}/Icon_{0}_03", animationIdStr), this.transform);

            icon1 = GameObjectExtension.LoadCreatureIcon(string.Format(@"Icon_{0}_01", animationIdStr), this.transform);
            icon2 = GameObjectExtension.LoadCreatureIcon(string.Format(@"Icon_{0}_02", animationIdStr), this.transform);
            icon3 = GameObjectExtension.LoadCreatureIcon(string.Format(@"Icon_{0}_03", animationIdStr), this.transform);

        }

        void Update()
        {
            if (tickCount++ > 80)
            {
                tickCount = 0;
                UpdateAnimation();
            }
        }

        public void SetAnimateState(AnimateState state)
        {
            switch (state)
            {
                case AnimateState.Idle:
                    this.transform.localRotation = new Quaternion(0f, 180f, 0, 0);
                    break;
                case AnimateState.WalkLeft:
                    this.transform.localRotation = new Quaternion(0f, -90f, 0, 0);
                    break;
                case AnimateState.WalkRight:
                    break;
                case AnimateState.WalkUp:
                    this.transform.localRotation = new Quaternion(0f, 0, 0, 0); 
                    break;
                case AnimateState.WalkDown:
                    this.transform.localRotation = new Quaternion(0f, 180f, 0, 0);
                    break;
                default:
                    break;
            }

            this.animateState = state;
        }

        void UpdateAnimation()
        {
            switch(this.animateState)
            {
                case AnimateState.Idle:
                    UpdateIdleAnimation();
                    break;
                case AnimateState.WalkLeft:
                    break;
                case AnimateState.WalkRight:
                    break;
                case AnimateState.WalkUp:
                    break;
                case AnimateState.WalkDown:
                    break;
                default:
                    break;
            }
        }
         
        void UpdateIdleAnimation()
        {
            if (icon1 == null || icon2 == null || icon3 == null)
            {
                return;
            }

            aniIndex = (aniIndex + 1) % 4;
            if (aniIndex == 0)
            {
                icon1.SetActive(true);
                icon2.SetActive(false);
                icon3.SetActive(false);
            }
            else if (aniIndex == 1)
            {
                icon1.SetActive(false);
                icon2.SetActive(true);
                icon3.SetActive(false);
            }
            else if (aniIndex == 2)
            {
                icon1.SetActive(false);
                icon2.SetActive(false);
                icon3.SetActive(true);
            }
            else
            {
                icon1.SetActive(false);
                icon2.SetActive(true);
                icon3.SetActive(false);
            }
        }
    }
}