using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.FightObjects;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.GameBattleScene
{

    public class BattleLoader : MonoBehaviour
    {
        public static float ANIMATION_INTERVAL = 0.5f;
        public static float ANIMATION_END = 1.8f;

        public GameObject localBody;
        public GameObject foreignBody;

        private AttackResult attackResult;

        private GameObject subjectObject = null;
        private GameObject targetObject = null;

        private Animator subjectAnimator = null;
        private Animator targetAnimator = null;

        private int currentAnimationIndex = 0;

        private bool animationFinished = false;
        private DateTime animationFinishTime;

        
        // Start is called before the first frame update
        void Start()
        {

            // Load the Battle Subject and Target

            attackResult = GlobalVariables.Get<AttackResult>("AttackResult");

            if (attackResult == null)
            {
                Debug.LogWarning("GameBattleScene: Loading attackResult failed, ignore the animations.");
                return;
            }

            CreatureFaction faction = attackResult.Subject.Faction;
            if (faction == CreatureFaction.Friend || faction == CreatureFaction.Friend)
            {
                subjectObject = localBody;
                targetObject = foreignBody;
            }
            else
            {   
                subjectObject = foreignBody;
                targetObject = localBody;
            }

            var subjectAniId = attackResult.Subject.Definition.AnimationId;

            // Load the animation
            subjectAnimator = subjectObject.GetComponent<Animator>();
            subjectAnimator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(subjectAniId)));
            subjectObject.GetComponent<FightBody>().Initialize(subjectAniId, onAnimationFinish);




            var targetAniId = attackResult.Target.Definition.AnimationId;

            // Load the animation
            targetAnimator = targetObject.GetComponent<Animator>();
            targetAnimator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(targetAniId)));
            targetAnimator.GetComponent<FightBody>().Initialize(targetAniId, onAnimationFinish);


            currentAnimationIndex = 0;
            onAnimationStart();
        }

        private void onAnimationStart()
        {
            DamageResult damage = null;
            if (currentAnimationIndex < attackResult.Damages.Count)
            {
                damage = attackResult.Damages[currentAnimationIndex];

                MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_INTERVAL, () =>
                {
                    subjectAnimator.SetInteger("actionState", 1);
                });
            }
            else
            {
                int backIndex = currentAnimationIndex - attackResult.Damages.Count;
                if (backIndex < attackResult.BackDamages.Count)
                {
                    damage = attackResult.BackDamages[backIndex];
                    MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_INTERVAL, () =>
                    {
                        targetAnimator.SetInteger("actionState", 1);
                    });
                }
                else
                {
                    MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_END, () =>
                    {
                        SceneManager.UnloadSceneAsync("GameBattleScene");
                    });
                }
            }
            
        }

        private void onAnimationFinish()
        {
            currentAnimationIndex++;
            onAnimationStart();
        }

        // Update is called once per frame
        void Update()
        {
            var now = DateTime.Now;
            if (animationFinished && (now - animationFinishTime).TotalMilliseconds > 1800)
            {
                
            }


        }
    }

}