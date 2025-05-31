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

    public class AttackRunner : MonoBehaviour
    {
        public static float ANIMATION_INTERVAL = 0.5f;
        public static float ANIMATION_END = 1.8f;

        private AttackResult attackResult;

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

        public void Initialize(BattleLoader battleLoader, AttackResult attackResult)
        {
            this.attackResult = attackResult;

            CreatureFaction faction = attackResult.Subject.Faction;
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

            var subjectAniId = attackResult.Subject.Definition.AnimationId;

            // Load the animation
            subjectAnimator = subjectObject.GetComponent<Animator>();
            subjectAnimator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(subjectAniId)));
            subjectObject.GetComponent<FightBody>().Initialize(subjectAniId, onAnimationHit, onAnimationFinish);

            // Set the HP and MP
            var subjectInitialHp = attackResult.BackDamages.Count > 0
                ? attackResult.BackDamages[0].HpBefore : attackResult.Subject.Hp;
            updateSubjectHp(subjectInitialHp);

            var targetAniId = attackResult.Target.Definition.AnimationId;

            // Load the animation
            targetAnimator = targetObject.GetComponent<Animator>();
            targetAnimator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(targetAniId)));
            targetAnimator.GetComponent<FightBody>().Initialize(targetAniId, onAnimationHit, onAnimationFinish);

            var targetInitialHp = attackResult.Damages.Count > 0
                ? attackResult.Damages[0].HpBefore : attackResult.Target.Hp;
            updateTargetHp(targetInitialHp);


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

        private void onAnimationHit(int percent)
        {
            EnemyHitEffect hitEffect = GameObject.FindFirstObjectByType<EnemyHitEffect>();
            hitEffect.OnHit(new Vector3(1, 1, 1));


            DamageResult damage;
            if (currentAnimationIndex < attackResult.Damages.Count)
            {
                damage = attackResult.Damages[currentAnimationIndex];
                var currentHp = damage.HpBefore + (damage.HpAfter - damage.HpBefore) * percent / 100;
                updateTargetHp(currentHp);
            }
            else
            {
                int backIndex = currentAnimationIndex - attackResult.Damages.Count;
                if (backIndex < attackResult.BackDamages.Count)
                {
                    damage = attackResult.BackDamages[backIndex];
                    var currentHp = damage.HpBefore + (damage.HpAfter - damage.HpBefore) * percent / 100;
                    updateSubjectHp(currentHp);
                }
            }
        }

        private void onAnimationFinish()
        {
            currentAnimationIndex++;
            onAnimationStart();
        }

        private void updateSubjectHp(int current)
        {
            var subjectHpScale = getBarScale(current, attackResult.Subject.HpMax);
            subjectHpBar.transform.localScale = new Vector3(subjectHpScale, 1, 1);

        }

        private void updateTargetHp(int current)
        {
            var targetHpScale = getBarScale(current, attackResult.Target.HpMax);
            Debug.Log("updateTargetHp: " + targetHpScale + ", " + current + ", " + attackResult.Target.HpMax);
            targetHpBar.transform.localScale = new Vector3(targetHpScale, 1, 1);
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