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

    /// <summary>
    /// Runner function for Magic animations in the Battle Scene.
    /// </summary>
    public class MagicRunner : MonoBehaviour
    {
        public static float ANIMATION_INTERVAL = 0.5f;
        public static float ANIMATION_END = 1.8f;

        private MagicResult magicResult;

        private GameObject subjectObject = null;
        private GameObject targetObject = null;

        private GameObject subjectHpBar;
        private GameObject subjectMpBar;
        private GameObject targetHpBar;
        private GameObject targetMpBar;


        private Animator subjectAnimator = null;
        private Animator targetAnimator = null;

        private int currentAnimationIndex = 0;

        private bool animationFinished = false;
        private DateTime animationFinishTime;


        // Start is called before the first frame update
        void Start()
        {
        }

        public void Initialize(BattleLoader battleLoader, MagicResult magicResult)
        {
            this.magicResult = magicResult;
            CreatureFaction faction = magicResult.Subject.Faction;
            if (faction == CreatureFaction.Friend || faction == CreatureFaction.Npc)
            {
                subjectObject = battleLoader.localBody;
                targetObject = battleLoader.foreignBody;

                subjectHpBar = battleLoader.localHpBar;
                subjectMpBar = battleLoader.localMpBar;
                targetHpBar = battleLoader.foreignHpBar;
                targetMpBar = battleLoader.foreignMpBar;
            }
            else
            {
                subjectObject = battleLoader.foreignBody;
                targetObject = battleLoader.localBody;

                subjectHpBar = battleLoader.foreignHpBar;
                subjectMpBar = battleLoader.foreignMpBar;
                targetHpBar = battleLoader.localHpBar;
                targetMpBar = battleLoader.localMpBar;
            }

            var subjectAniId = magicResult.Subject.Definition.AnimationId;

            // Load the animation
            subjectAnimator = subjectObject.GetComponent<Animator>();
            subjectAnimator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(subjectAniId)));
            subjectObject.GetComponent<FightBody>().Initialize(subjectAniId, onSpellingMagic, onAnimationFinish);

            // Set the HP and MP
            var subjectInitialMp = magicResult.Subject.Mp;
            updateSubjectMp(subjectInitialMp);
            updateSubjectHp(magicResult.Subject.Hp);

            // TODO: it seems only damage magic has Battle Scene, so only take care of DamageResult here
            var target = magicResult.Targets[0];
            var targetAniId = target.Definition.AnimationId;
            var damageResult = magicResult.Results[target.Id] as DamageResult;

            var targetInitialHp = damageResult.HpBefore;
            updateTargetHp(0, targetInitialHp);
            updateTargetMp(0, target.Mp);

            // Load the animation
            targetAnimator = targetObject.GetComponent<Animator>();
            targetAnimator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(targetAniId)));
            targetAnimator.GetComponent<FightBody>().Initialize(targetAniId, (percent) => { }, onAnimationFinish);

            

            currentAnimationIndex = 0;
            onAnimationStart();
        }

        private void onAnimationStart()
        {
            DamageResult damage = null;
            if (currentAnimationIndex < magicResult.Targets.Count)
            {
                var target = magicResult.Targets[currentAnimationIndex];
                damage = magicResult.Results[target.Id] as DamageResult;
                MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_INTERVAL, () =>
                {
                    subjectAnimator.SetInteger("actionState", 1);
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

        /// <summary>
        /// When the magic is starting to run
        /// </summary>
        /// <param name="percent"></param>
        private void onSpellingMagic(int percent)
        {
            var mpCurrent = magicResult.Subject.Mp - magicResult.MpCost;

            updateSubjectMp(mpCurrent);

            // TODO: Should add magic animation later to call onAnimationHit
            onAnimationHit(percent);
        }

        private void onAnimationHit(int percent)
        {
            EnemyHitEffect hitEffect = GameObject.FindFirstObjectByType<EnemyHitEffect>();
            hitEffect.OnHit(new Vector3(1, 1, 1));

            DamageResult damage;
            if (currentAnimationIndex < magicResult.Results.Count)
            {
                var target = magicResult.Targets[currentAnimationIndex];
                damage = magicResult.Results[target.Id] as DamageResult;
                var currentHp = damage.HpBefore + (damage.HpAfter - damage.HpBefore) * percent / 100;
                updateTargetHp(0, currentHp);
            }
        }

        private void onAnimationFinish()
        {
            currentAnimationIndex++;
            onAnimationStart();
        }

        private void updateSubjectHp(int current)
        {
            var subjectHpScale = getBarScale(current, magicResult.Subject.HpMax);
            subjectHpBar.transform.localScale = new Vector3(subjectHpScale, 1, 1);
        }
        
        private void updateSubjectMp(int current)
        {
            var subjectMpScale = getBarScale(current, magicResult.Subject.MpMax);
            subjectMpBar.transform.localScale = new Vector3(subjectMpScale, 1, 1);
        }

        private void updateTargetHp(int resultIndex, int current)
        {
            var target = magicResult.Targets[resultIndex];
            DamageResult damageResult = magicResult.Results[target.Id] as DamageResult;
            var targetHpScale = getBarScale(current, target.HpMax);
            Debug.Log("updateTargetHp: " + targetHpScale + ", " + current + ", " + target.HpMax);
            targetHpBar.transform.localScale = new Vector3(targetHpScale, 1, 1);
        }

        private void updateTargetMp(int resultIndex, int current)
        {
            var target = magicResult.Targets[resultIndex];
            var targetMpScale = getBarScale(current, target.MpMax);
            subjectMpBar.transform.localScale = new Vector3(targetMpScale, 1, 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        private float getBarScale(int value, int maxValue)
        {
            if (maxValue == 0)
            {
                return 0;
            }

            if (value > maxValue)
            {
                return 1;
            }

            if (value < 0)
            {
                return 0;
            }
            return (float)value / maxValue;
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