﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;
using WindingTale.Common;
using WindingTale.UI.Common;
using WindingTale.UI.FieldMap;
using WindingTale.UI.Components;
using System;

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
            Dying,
            // Disposed,
        }

        private GameObject icon1 = null;
        private GameObject icon2 = null;
        private GameObject icon3 = null;

        private float dyingStartTimestamp = 0;
        private float dyingDuration = 1.8f;

        public int CreatureId
        {
            get; private set;
        }

        public int AnimationId
        {
            get; private set;
        }

        private IGameInterface gameInterface = null;

        public AnimateStates AnimateState
        {
            get; private set;
        }

        public UICreature()
        {
            
        }


        public void Initialize(IGameInterface gameInterface, int creatureId, int animationId)
        {
            this.gameInterface = gameInterface;
            this.CreatureId = creatureId;

            this.AnimateState = AnimateStates.Idle;

            icon1 = AssetManager.Instance().InstantiateIconGO(this.transform, animationId, 1);
            icon2 = AssetManager.Instance().InstantiateIconGO(this.transform, animationId, 2);
            icon3 = AssetManager.Instance().InstantiateIconGO(this.transform, animationId, 3);

            var box = this.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.0f, 2.0f, 2.0f);
            box.center = new Vector3(0f, 1f, 0f);
        }

        void Update()
        {
            if (this.AnimateState == AnimateStates.Dying)
            {
                UpdateDyingAnimation(Constants.TICK_PER_FRAME);
            }
            //else if (this.AnimateState == AnimateStates.Disposed)
            //{
                // Do nothing, waiting for GameInterface to remove it
            //}
            else if (this.AnimateState == AnimateStates.Idle)
            {
                UpdateAnimation(Constants.TICK_PER_FRAME * 4);
            }
            else
            {
                UpdateAnimation(Constants.TICK_PER_FRAME);
            }
        }
        protected override void OnTouched()
        {
            Debug.Log("Creature Touched.");
            gameInterface.TouchCreature(this.CreatureId);

        }

        public void SetAnimateState(AnimateStates state)
        {
            
            switch (state)
            {
                case AnimateStates.Idle:
                    this.transform.localRotation = new Quaternion(0, 1.0f, 0, 0);
                    break;
                case AnimateStates.Dying:
                    this.transform.localRotation = new Quaternion(0, 1.0f, 0, 0);
                    dyingStartTimestamp = Time.time;
                    icon1.SetActive(false);
                    icon2.SetActive(true);
                    icon3.SetActive(false);
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

        private void UpdateAnimation(int speedFrames)
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

        private void UpdateDyingAnimation(int speedFrames)
        {
            float nowTime = Time.time;
            if (nowTime > dyingStartTimestamp + dyingDuration)
            {
                // this.AnimateState = AnimateStates.Disposed;
                Destroy(this.gameObject);
                return;
            }

            float rotateY = 90 + (nowTime - dyingStartTimestamp) * speedFrames * 80;
            icon2.transform.localRotation = Quaternion.Euler(0, rotateY, 0);
        }

        public FDPosition GetCurrentPosition()
        {
            return FieldTransform.GetObjectUnitPosition(this.transform.localPosition);
        }

        public Vector3 GetCurrentPixelPosition()
        {
            return this.transform.localPosition;
        }

        public void SetPixelPosition(Vector3 vector3)
        {
            this.transform.localPosition = vector3;
        }

        public void SetHasActioned(bool hasActioned)
        {
            if (hasActioned)
            {
                icon1.GetComponentInChildren<MeshRenderer>().material = AssetManager.MaterialGrey;
                icon2.GetComponentInChildren<MeshRenderer>().material = AssetManager.MaterialGrey;
                icon3.GetComponentInChildren<MeshRenderer>().material = AssetManager.MaterialGrey;
            }
            else
            {
                icon1.GetComponentInChildren<MeshRenderer>().material = AssetManager.MaterialDefault;
                icon2.GetComponentInChildren<MeshRenderer>().material = AssetManager.MaterialDefault;
                icon3.GetComponentInChildren<MeshRenderer>().material = AssetManager.MaterialDefault;
            }
        }
    }
}