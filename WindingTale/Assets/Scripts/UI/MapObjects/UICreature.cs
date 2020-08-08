using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;
using WindingTale.Common;
using WindingTale.UI.Common;
using WindingTale.UI.FieldMap;

namespace WindingTale.UI.MapObjects
{
    public class UICreature : UIObject
    {
        public enum AnimateStates
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

        public AnimateStates AnimateState
        {
            get; private set;
        }

        private int aniIndex = 0;

        public UICreature()
        {

        }


        public void Initialize(FDCreature creature)
        {
            Material defaultMaterial = Resources.Load<Material>(@"common-mat");

            this.Creature = creature;
            this.AnimateState = AnimateStates.Idle;

            int animationId = creature.Definition.AnimationId;
            //// icon1 = GameObjectExtension.CreateFromObj(string.Format(@"Icons/{0}/Icon_{0}_01", animationIdStr), this.transform);
            //// icon2 = GameObjectExtension.CreateFromObj(string.Format(@"Icons/{0}/Icon_{0}_02", animationIdStr), this.transform);
            //// icon3 = GameObjectExtension.CreateFromObj(string.Format(@"Icons/{0}/Icon_{0}_03", animationIdStr), this.transform);

            icon1 = GameObjectExtension.LoadCreatureIcon(animationId, 1, this.transform);
            icon2 = GameObjectExtension.LoadCreatureIcon(animationId, 2, this.transform);
            icon3 = GameObjectExtension.LoadCreatureIcon(animationId, 3, this.transform);

        }

        void Update()
        {
            if (this.AnimateState == AnimateStates.Idle)
            {
                UpdateAnimation(80);
            }
            else
            {
                UpdateAnimation(20);
            }
        }

        public void SetAnimateState(AnimateStates state)
        {
            
            switch (state)
            {
                case AnimateStates.Idle:
                    this.transform.localRotation = new Quaternion(0, 1.0f, 0, 0);
                    break;
                case AnimateStates.WalkLeft:
                    this.transform.localRotation = new Quaternion(0, -0.7f, 0, 0.7f);
                    break;
                case AnimateStates.WalkRight:
                    this.transform.localRotation = new Quaternion(0, 0.7f, 0, 0.7f);
                    break;
                case AnimateStates.WalkUp:
                    this.transform.localRotation = new Quaternion(0, 0f, 0, 0);
                    break;
                case AnimateStates.WalkDown:
                    this.transform.localRotation = new Quaternion(0, 1.0f, 0, 0);
                    break;
                default:
                    break;
            }

            this.AnimateState = state;
        }

        void UpdateAnimation(int speedFrames)
        {
            if (icon1 == null || icon2 == null || icon3 == null)
            {
                return;
            }

            int tick = Global.Instance().CurrentTick % speedFrames;
            if (tick != 0)
            {
                return;
            }

            int aniIndex = (Global.Instance().CurrentTick / speedFrames) % 4;
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

        public FDPosition GetCurrentPosition()
        {
            return FieldTransform.GetCreatureUnitPosition(this.transform.localPosition);
        }

        public Vector3 GetCurrentPixelPosition()
        {
            return this.transform.localPosition;
        }

        public void SetPixelPosition(Vector3 vector3)
        {
            this.transform.localPosition = vector3;
        }
    }
}