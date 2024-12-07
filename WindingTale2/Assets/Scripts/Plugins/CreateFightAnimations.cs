using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using System.Linq;

namespace WindingTale.Editor
{ 
    /// <summary>
    /// Plugin for Menu in Editor - Assets -> Create Fight Animations
    /// </summary>
    public class CreateFightAnimations : EditorWindow
    {
        public enum AnimationType
        {
            Idle = 1,
            Attack = 2,
            Spell = 3,
        }

        [UnityEditor.MenuItem("Assets/Create Fight Animations")]
        static void CreateFightAnimator()
        {
            for (int aniId = 1; aniId < 999; aniId++)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(string.Format("Fights/{0}/Fight-{0}", StringUtils.Digit3(aniId)));
                if (sprites == null || sprites.Length == 0)
                {
                    continue;
                }

                CreateAnimationClips(aniId);
                CreateAnimator(aniId);
            }
        }

        private static void CreateAnimator(int animationId)
        {
            string idStr = StringUtils.Digit3(animationId);

            // Creates the controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(string.Format(@"Assets/Resources/Fights/{0}/animator_{0}.controller", idStr));

            // Add parameters
            controller.AddParameter("actionState", AnimatorControllerParameterType.Int);
            Debug.Log("Added paramter.");


            // Add StateMachines
            var rootStateMachine = controller.layers[0].stateMachine;

            // Add States
            var stateIdle = rootStateMachine.AddState("idle");
            var stateAttack = rootStateMachine.AddState("attack");

            // Add Motions
            Motion idleMotion = Resources.Load<Motion>(string.Format(@"Fights/{0}/idle_{0}", idStr));
            Motion attackMotion = Resources.Load<Motion>(string.Format(@"Fights/{0}/attack_{0}", idStr));

            if (idleMotion == null)
            {
                Debug.Log("idleMotion is null for: " + idStr);
                return;
            }
            if (attackMotion == null)
            {
                Debug.Log("attackMotion is null for: " + idStr);
                return;
            }

            stateIdle.motion = idleMotion;
            stateAttack.motion = attackMotion;
            //controller.AddMotion(idleMotion);
            //controller.AddMotion(attackMotion);

            // Not needed for this
            rootStateMachine.AddEntryTransition(stateIdle);

            // Add Transitions
            var trans1 = stateIdle.AddTransition(stateAttack);
            trans1.AddCondition(AnimatorConditionMode.Equals, 1, "actionState");
            trans1.duration = 0;

            var trans2 = stateAttack.AddTransition(stateIdle);
            trans2.AddCondition(AnimatorConditionMode.Equals, 0, "actionState");
            trans2.duration = 0;
        }


        private static void CreateAnimationClips(int animationId)
        {
            FightAnimation animationDef = DefinitionStore.Instance.GetFightAnimation(animationId);
            CreateAnimationClip(animationId, AnimationType.Idle, animationDef.IdleFrameCount);
            CreateAnimationClip(animationId, AnimationType.Attack, animationDef.AttackFrameCount);

            if (animationDef.HasSpellAnimation)
            {
                CreateAnimationClip(animationId, AnimationType.Spell, animationDef.SpellFrameCount);
            }

        }

        private static void CreateAnimationClip(int animationId, AnimationType animationType, int frameCount)
        {
            // First you need to create e Editor Curve Binding
            EditorCurveBinding curveBinding = new EditorCurveBinding();

            // I want to change the sprites of the sprite renderer, so I put the typeof(SpriteRenderer) as the binding type.
            curveBinding.type = typeof(SpriteRenderer);
            // Regular path to the gameobject that will be changed (empty string means root)
            curveBinding.path = "";
            // This is the property name to change the sprite of a sprite renderer
            curveBinding.propertyName = "m_Sprite";

            AnimationClip animClip = new AnimationClip();

            // An array to hold the object keyframes
            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frameCount];
            Sprite[] sprites = Resources.LoadAll<Sprite>(string.Format("Fights/{0}/Fight-{0}", StringUtils.Digit3(animationId)));
            if (sprites == null || sprites.Length == 0)
            {
                Debug.Log("No sprites found for: " + animationId);
                return;
            }

            for (int i = 0; i < frameCount; i++)
            {
                Sprite sprite = sprites.FirstOrDefault(sp => sp.name == string.Format("Fight-{0}-{1}-{2}.png", StringUtils.Digit3(animationId), animationType.GetHashCode(), StringUtils.Digit2(i + 1)));

                keyFrames[i] = new ObjectReferenceKeyframe();
                // set the time
                keyFrames[i].time = 0.12f * i;
                // set reference for the sprite you want
                keyFrames[i].value = sprite;
                Debug.LogWarning(sprite);
            }
            AnimationUtility.SetObjectReferenceCurve(animClip, curveBinding, keyFrames);

            if (animationType == AnimationType.Idle)
            {
                // WrapMode is not working now, using AnimationClipSettings instead
                // animClip.wrapMode = WrapMode.Loop;

                AnimationClipSettings settings = new AnimationClipSettings();
                settings.loopTime = true;
                settings.startTime = 0;
                settings.stopTime = animClip.length;
                AnimationUtility.SetAnimationClipSettings(animClip, settings);
            }
            AssetDatabase.CreateAsset(animClip, string.Format(@"Assets/Resources/Fights/{0}/{1}_{0}.anim", StringUtils.Digit3(animationId), Enum.GetName(animationType.GetType(), animationType).ToLower()));
        }
    }
}