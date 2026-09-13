using System;
using System.Collections;
using System.Collections.Generic;
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
    ///
    /// The caster casts once. At its spell's hit frame the caster and the target freeze, and
    /// the magic plays on every target in turn: on targets[0] first (opening with the screen
    /// flash), then targets[0] slides off screen and targets[1] slides into its place, and so
    /// on to the last. Only then do the creatures move again and the caster finishes its pose.
    /// </summary>
    public class MagicRunner : MonoBehaviour
    {
        public static float ANIMATION_INTERVAL = 0.5f;
        public static float ANIMATION_END = 1.8f;

        /// <summary>Seconds to slide one target off screen and the next one into its place.</summary>
        public static float TARGET_SWAP_DURATION = 0.3f;

        private MagicResult magicResult;

        private GameObject subjectObject = null;
        private GameObject targetObject = null;

        private GameObject subjectHpBar;
        private GameObject subjectMpBar;
        private GameObject targetHpBar;
        private GameObject targetMpBar;


        private Animator subjectAnimator = null;
        private Animator targetAnimator = null;

        private BattleBarInfo subjectBarInfo = null;
        private BattleBarInfo targetBarInfo = null;

        /// <summary>The target standing in the target's place, as an index into magicResult.Targets.</summary>
        private int currentTargetIndex = 0;

        /// <summary>The magic's animation, or null for magics that have none yet.</summary>
        private MagicEffectDefinition effectDefinition = null;
        private bool targetIsFriend = false;
        private Vector3 targetRestPosition;

        private bool spellStarted = false;
        private EnemyHitEffect targetHitEffect = null;

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

                targetIsFriend = false;

                subjectHpBar = battleLoader.localHpBar;
                subjectMpBar = battleLoader.localMpBar;
                targetHpBar = battleLoader.foreignHpBar;
                targetMpBar = battleLoader.foreignMpBar;
            }
            else
            {
                subjectObject = battleLoader.foreignBody;
                targetObject = battleLoader.localBody;

                targetIsFriend = true;

                subjectHpBar = battleLoader.foreignHpBar;
                subjectMpBar = battleLoader.foreignMpBar;
                targetHpBar = battleLoader.localHpBar;
                targetMpBar = battleLoader.localMpBar;
            }

            // Fill in the pre-built name / occupation / HP / MP labels on each bar.
            subjectBarInfo = subjectHpBar.transform.parent.GetComponent<BattleBarInfo>();
            targetBarInfo = targetHpBar.transform.parent.GetComponent<BattleBarInfo>();
            if (subjectBarInfo != null) subjectBarInfo.Bind(magicResult.Subject);

            // Hide the MP bar entirely for creatures with no MP.
            if (subjectMpBar != null) subjectMpBar.SetActive(magicResult.Subject.MpMax > 0);

            var subjectAniId = magicResult.Subject.Definition.AnimationId;

            // Load the animation
            subjectAnimator = subjectObject.GetComponent<Animator>();
            subjectAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(subjectAniId)));
            subjectObject.GetComponent<FightBody>().Initialize(subjectAniId, onSpellingMagic, onAnimationFinish);

            // Set the HP and MP. MP comes from the result's snapshot, not the live creature:
            // the field applies the cost on its own schedule while this scene animates.
            updateSubjectMp(magicResult.MpBefore);
            updateSubjectHp(magicResult.Subject.Hp);

            targetAnimator = targetObject.GetComponent<Animator>();
            bindTarget(0);

            effectDefinition = MagicEffectDefinition.Get(magicResult.MagicId);
            targetRestPosition = targetObject.transform.localPosition;

            // The scene only configures a hit effect on one body; give the target one like it.
            targetHitEffect = targetObject.GetComponent<EnemyHitEffect>();
            if (targetHitEffect == null)
            {
                EnemyHitEffect reference = subjectObject.GetComponent<EnemyHitEffect>();
                targetHitEffect = targetObject.AddComponent<EnemyHitEffect>();
                if (reference != null)
                {
                    targetHitEffect.hitColor = reference.hitColor;
                    targetHitEffect.flashDuration = reference.flashDuration;
                    targetHitEffect.screenFlashDuration = reference.screenFlashDuration;
                    targetHitEffect.hitEffectImage = reference.hitEffectImage;
                }
            }

            onAnimationStart();
        }

        /// <summary>
        /// Puts targets[index] in the target's place: its fight animation, and its name, HP
        /// and MP on the target's bar.
        /// </summary>
        private void bindTarget(int index)
        {
            currentTargetIndex = index;
            var target = magicResult.Targets[index];

            if (targetBarInfo != null) targetBarInfo.Bind(target);
            if (targetMpBar != null) targetMpBar.SetActive(target.MpMax > 0);

            // TODO: it seems only damage magic has Battle Scene, so only take care of DamageResult here
            DamageResult damage = getDamage(index);
            updateTargetHp(damage != null ? damage.HpBefore : target.Hp);
            updateTargetMp(target.Mp);

            var targetAniId = target.Definition.AnimationId;
            targetAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(targetAniId)));
            targetAnimator.GetComponent<FightBody>().Initialize(targetAniId, (percent) => { }, () => { });

            // A frozen animator would otherwise keep showing the previous creature's sprite.
            targetAnimator.Update(0);
        }

        private DamageResult getDamage(int targetIndex)
        {
            SoloResult result;
            var target = magicResult.Targets[targetIndex];
            return magicResult.Results.TryGetValue(target.Id, out result) ? result as DamageResult : null;
        }

        private void onAnimationStart()
        {
            MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_INTERVAL, () =>
            {
                subjectAnimator.SetInteger("actionState", 1);
            });
        }

        /// <summary>
        /// When the magic is starting to run
        /// </summary>
        /// <param name="percent"></param>
        private void onSpellingMagic(int percent)
        {
            var mpCurrent = magicResult.MpBefore - magicResult.MpCost;

            updateSubjectMp(mpCurrent);

            // The magic lands the damage on every target itself, so a spell animation with
            // more than one hit point still only casts it once.
            if (spellStarted)
            {
                return;
            }
            spellStarted = true;

            // Both creatures hold their pose while the magic plays; the caster finishes its
            // animation (and so moves the battle on) only once every target has taken it.
            subjectAnimator.speed = 0;
            targetAnimator.speed = 0;
            StartCoroutine(playMagicOnTargets());
        }

        private IEnumerator playMagicOnTargets()
        {
            for (int index = 0; index < magicResult.Targets.Count; index++)
            {
                if (index > 0)
                {
                    yield return swapTarget(index);
                }

                if (effectDefinition == null)
                {
                    // No animation for this magic yet: the whole damage lands at once.
                    onAnimationHit(100);
                    continue;
                }

                bool finished = false;
                MagicEffect.Play(effectDefinition, targetObject, targetRestPosition, targetIsFriend, index == 0,
                    onAnimationHit, () => finished = true);
                while (!finished)
                {
                    yield return null;
                }
            }

            subjectAnimator.speed = 1;
            targetAnimator.speed = 1;
        }

        /// <summary>
        /// Slides the current target off screen, puts targets[index] in its place and slides
        /// that one back in to where the target stands, TARGET_SWAP_DURATION in all. Each side
        /// leaves by its own screen edge: an enemy (standing on the left) to the left, a friend
        /// to the right, so neither crosses the caster.
        /// </summary>
        private IEnumerator swapTarget(int index)
        {
            Transform body = targetObject.transform;
            Vector3 offScreen = targetRestPosition + body.parent.InverseTransformVector(offScreenOffset());
            float half = TARGET_SWAP_DURATION / 2;

            for (float elapsed = 0; elapsed < half; elapsed += Time.deltaTime)
            {
                float t = elapsed / half;
                body.localPosition = Vector3.LerpUnclamped(targetRestPosition, offScreen, t * t);
                yield return null;
            }

            body.localPosition = offScreen;
            bindTarget(index);

            for (float elapsed = 0; elapsed < half; elapsed += Time.deltaTime)
            {
                float t = elapsed / half;
                body.localPosition = Vector3.LerpUnclamped(offScreen, targetRestPosition, 1 - (1 - t) * (1 - t));
                yield return null;
            }
            body.localPosition = targetRestPosition;
        }

        /// <summary>
        /// World offset that carries the target body a whole screen width towards its own
        /// edge -- enough to clear the view wherever in the frame the creature is drawn.
        /// </summary>
        private Vector3 offScreenOffset()
        {
            Camera camera = MagicEffect.FindSceneCamera(gameObject.scene);
            if (camera == null)
            {
                return Vector3.zero;
            }

            Transform body = targetObject.transform;
            float distance = Vector3.Dot(body.position - camera.transform.position, camera.transform.forward);
            float viewWidth = 2 * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * camera.aspect;
            float towardEdge = targetIsFriend ? 1 : -1;
            return camera.transform.right * towardEdge * viewWidth;
        }

        private void onAnimationHit(int percent)
        {
            DamageResult damage = getDamage(currentTargetIndex);

            // Magic never knocks the target back. A hit flashes it red; a miss leaves it be.
            bool landed = !(damage != null && damage.HasMissed);
            if (landed && targetHitEffect != null)
            {
                targetHitEffect.OnMagicHit();
            }

            if (damage != null)
            {
                var currentHp = damage.HpBefore + (damage.HpAfter - damage.HpBefore) * percent / 100;
                updateTargetHp(currentHp);
            }
        }

        private void onAnimationFinish()
        {
            MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_END, () =>
            {
                SceneManager.UnloadSceneAsync("GameBattleScene");
            });
        }

        private void updateSubjectHp(int current)
        {
            var subjectHpScale = getBarScale(current, magicResult.Subject.HpMax);
            subjectHpBar.transform.localScale = new Vector3(subjectHpScale, 1, 1);

            if (subjectBarInfo != null) subjectBarInfo.SetHp(current);
        }

        private void updateSubjectMp(int current)
        {
            var subjectMpScale = getBarScale(current, magicResult.Subject.MpMax);
            subjectMpBar.transform.localScale = new Vector3(subjectMpScale, 1, 1);

            if (subjectBarInfo != null) subjectBarInfo.SetMp(current);
        }

        private void updateTargetHp(int current)
        {
            var target = magicResult.Targets[currentTargetIndex];
            var targetHpScale = getBarScale(current, target.HpMax);
            Debug.Log("updateTargetHp: " + targetHpScale + ", " + current + ", " + target.HpMax);
            targetHpBar.transform.localScale = new Vector3(targetHpScale, 1, 1);

            if (targetBarInfo != null) targetBarInfo.SetHp(current);
        }

        private void updateTargetMp(int current)
        {
            var target = magicResult.Targets[currentTargetIndex];
            var targetMpScale = getBarScale(current, target.MpMax);
            targetMpBar.transform.localScale = new Vector3(targetMpScale, 1, 1);

            if (targetBarInfo != null) targetBarInfo.SetMp(current);
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
