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

        private GameObject localBody = null;
        private GameObject foreignBody = null;

        private GameObject subjectHpBar;
        private GameObject subjectMpBar;
        private GameObject targetHpBar;
        private GameObject targetMpBar;


        private Animator subjectAnimator = null;
        private Animator targetAnimator = null;

        private BattleBarInfo subjectBarInfo = null;
        private BattleBarInfo targetBarInfo = null;

        // Remote (ranged) attack: the Target node is hidden while the subject's attack
        // animation is before its RemoteAttackFrame, then revealed; after all attack
        // rounds the Subject node is hidden. (Reverses naturally for enemy attacks.)
        private GameObject targetNode = null;
        private GameObject subjectNode = null;
        private GameObject subjectBar = null;   // subject's HP/MP bar panel
        private GameObject subjectTai = null;   // subject's Tai (local only)
        private bool subjectIsLocal = false;
        private FightAnimation subjectFightAnimation = null;
        private bool remoteAttackEnabled = false;
        private bool targetHiddenForRemote = false;
        private bool subjectExtrasHidden = false;

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

            localBody = battleLoader.localBody;
            foreignBody = battleLoader.foreignBody;

            // Ensure both bodies can receive hit effects.
            // Copy settings from whichever body already has the component configured in the editor.
            EnemyHitEffect foreignEffect = foreignBody.GetComponent<EnemyHitEffect>();
            EnemyHitEffect localEffect   = localBody.GetComponent<EnemyHitEffect>();
            EnemyHitEffect referenceEffect = foreignEffect != null ? foreignEffect : localEffect;

            if (localEffect == null)
                CopyHitEffect(localBody.AddComponent<EnemyHitEffect>(), referenceEffect);
            if (foreignEffect == null)
                CopyHitEffect(foreignBody.AddComponent<EnemyHitEffect>(), referenceEffect);

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

            // Fill in the pre-built name / occupation / HP / MP labels on each bar.
            subjectBarInfo = subjectHpBar.transform.parent.GetComponent<BattleBarInfo>();
            targetBarInfo = targetHpBar.transform.parent.GetComponent<BattleBarInfo>();
            if (subjectBarInfo != null) subjectBarInfo.Bind(attackResult.Subject);
            if (targetBarInfo != null) targetBarInfo.Bind(attackResult.Target);

            var subjectAniId = attackResult.Subject.Definition.AnimationId;

            // Load the animation
            subjectAnimator = subjectObject.GetComponent<Animator>();
            subjectAnimator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(subjectAniId)));
            subjectObject.GetComponent<FightBody>().Initialize(subjectAniId, onAnimationHit, onAnimationFinish);

            // Remote (ranged) attack setup. The Target is hidden during the windup, the
            // Subject is hidden after the attack — works in both directions.
            subjectFightAnimation = DefinitionStore.Instance.GetFightAnimation(subjectAniId);
            targetNode = targetObject.transform.parent != null
                ? targetObject.transform.parent.gameObject
                : targetObject;
            subjectNode = subjectObject.transform.parent != null
                ? subjectObject.transform.parent.gameObject
                : subjectObject;
            remoteAttackEnabled = subjectFightAnimation != null
                && subjectFightAnimation.RemoteAttackFrame > 0;

            // The subject's bar/tai are hidden once the attack reaches RemoteAttackFrame
            // (Tai is a local-only object), and re-shown before the next attack round.
            subjectIsLocal = (subjectObject == localBody);
            subjectBar = (subjectHpBar != null && subjectHpBar.transform.parent != null)
                ? subjectHpBar.transform.parent.gameObject
                : null;
            subjectTai = subjectIsLocal ? battleLoader.localTai : null;

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

                // Enter remote state for this attack round: hide the Target node now;
                // Update() reveals it once the animation reaches RemoteAttackFrame. This
                // re-runs for every attack, so a second attack hides it again.
                if (remoteAttackEnabled)
                {
                    HideTargetForRemote();
                }

                MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_INTERVAL, () =>
                {
                    subjectAnimator.SetInteger("actionState", 1);
                });
            }
            else
            {
                // The ranged attack rounds are all done now (one or two): hide the Subject
                // (attacker) node immediately. SetActive(false) is idempotent across the
                // back-damage / end branches below.
                if (remoteAttackEnabled)
                {
                    HideSubject();
                }

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
                        //// SceneManager.UnloadSceneAsync("GameBattleScene");
                    });
                }
            }

        }

        private void onAnimationHit(int percent)
        {
            // Make sure a remote-hidden Target is visible before it takes the hit.
            if (targetHiddenForRemote)
            {
                ShowTarget();
            }

            // Determine which body is being hit in this animation round
            GameObject hitObject;
            if (currentAnimationIndex < attackResult.Damages.Count)
                hitObject = targetObject;   // subject is attacking, target takes the hit
            else
                hitObject = subjectObject;  // counter-attack, subject takes the hit

            // Camera orientation maps world -X to screen-right.
            // localBody steps back to screen-right (-X world), foreignBody steps back to screen-left (+X world).
            Vector3 knockbackDir = (hitObject == localBody) ? new Vector3(-1, 0, 0) : new Vector3(1, 0, 0);

            EnemyHitEffect hitEffect = hitObject.GetComponent<EnemyHitEffect>();
            if (hitEffect != null)
                hitEffect.OnHit(knockbackDir);

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
            // Backstop: never leave the Target node hidden after a round ends.
            if (targetHiddenForRemote)
            {
                ShowTarget();
            }

            currentAnimationIndex++;
            onAnimationStart();
        }

        private void HideTargetForRemote()
        {
            if (targetNode != null)
            {
                targetNode.SetActive(false);
                targetHiddenForRemote = true;
            }

            // Re-show the subject bar/tai before this attack round (they were hidden a
            // frame before the previous round's RemoteAttackFrame).
            SetSubjectExtrasActive(true);
            subjectExtrasHidden = false;
        }

        private void ShowTarget()
        {
            targetHiddenForRemote = false;
            if (targetNode != null)
            {
                targetNode.SetActive(true);
            }
        }

        private void SetSubjectExtrasActive(bool active)
        {
            if (!subjectIsLocal)
            {
                return;
            }
            if (subjectBar != null)
            {
                subjectBar.SetActive(active);
            }
            if (subjectTai != null)
            {
                subjectTai.SetActive(active);
            }
        }

        private void HideSubject()
        {
            if (subjectNode != null)
            {
                subjectNode.SetActive(false);
            }
        }

        private void updateSubjectHp(int current)
        {
            var subjectHpScale = getBarScale(current, attackResult.Subject.HpMax);
            subjectHpBar.transform.localScale = new Vector3(subjectHpScale, 1, 1);

            if (subjectBarInfo != null) subjectBarInfo.SetHp(current);
        }

        private void updateTargetHp(int current)
        {
            var targetHpScale = getBarScale(current, attackResult.Target.HpMax);
            Debug.Log("updateTargetHp: " + targetHpScale + ", " + current + ", " + attackResult.Target.HpMax);
            targetHpBar.transform.localScale = new Vector3(targetHpScale, 1, 1);

            if (targetBarInfo != null) targetBarInfo.SetHp(current);
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

        private void CopyHitEffect(EnemyHitEffect dst, EnemyHitEffect src)
        {
            if (src == null) return;
            dst.knockbackForce        = src.knockbackForce;
            dst.knockbackRecoveryTime = src.knockbackRecoveryTime;
            dst.hitColor              = src.hitColor;
            dst.flashDuration         = src.flashDuration;
            dst.screenFlashDuration   = src.screenFlashDuration;
            dst.hitEffectImage        = src.hitEffectImage;
        }

        // Update is called once per frame
        void Update()
        {
            // Track the subject's attack frame for the remote-attack transitions.
            if (remoteAttackEnabled && subjectAnimator != null && subjectFightAnimation != null
                && (targetHiddenForRemote || !subjectExtrasHidden))
            {
                AnimatorStateInfo state = subjectAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("attack"))
                {
                    float frame = state.normalizedTime * subjectFightAnimation.AttackFrameCount;

                    // Hide the subject's bar/tai one frame before the target appears.
                    if (!subjectExtrasHidden && frame >= subjectFightAnimation.RemoteAttackFrame - 1)
                    {
                        SetSubjectExtrasActive(false);
                        subjectExtrasHidden = true;
                    }

                    // Reveal the Target node once the attack reaches RemoteAttackFrame.
                    if (targetHiddenForRemote && frame >= subjectFightAnimation.RemoteAttackFrame)
                    {
                        ShowTarget();
                    }
                }
            }

            var now = DateTime.Now;
            if (animationFinished && (now - animationFinishTime).TotalMilliseconds > 1800)
            {

            }
        }
    }

}