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
using UnityEngine.UI;
using WindingTale.UI.Audio;

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

        // True once this round's attack animation has actually been started. The previous
        // round's attack state lingers for a moment after its finish event (FightBody only
        // returns the animator to idle 0.1s later), and Update must not mistake that stale
        // state for the new round's swing and reveal the hidden Target at once.
        private bool attackRoundStarted = false;

        private int currentAnimationIndex = 0;

        // The strike (animation index) that last flashed for a critical hit: an attack clip
        // can fire several hit events per strike, and the flash is once per strike.
        private int criticalFlashedIndex = -1;

        // The white screen-edge flash of a critical hit: up at once, then fading out.
        // Halved from 0.3 s (user: end it twice as fast); the magic flash is separate.
        private const float CriticalFlashSeconds = 0.15f;

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

            // Hide the MP bar entirely for creatures with no MP.
            if (subjectMpBar != null) subjectMpBar.SetActive(attackResult.Subject.MpMax > 0);
            if (targetMpBar != null) targetMpBar.SetActive(attackResult.Target.MpMax > 0);

            var subjectAniId = attackResult.Subject.Definition.AnimationId;

            // Load the animation
            subjectAnimator = subjectObject.GetComponent<Animator>();
            subjectAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(subjectAniId)));
            subjectObject.GetComponent<FightBody>().Initialize(subjectAniId, onAnimationHit, onAnimationFinish, onAnimationHitCue);

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
            // (the bar on either side; Tai is a local-only object), and re-shown before
            // the next attack round.
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
            targetAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(
                string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(targetAniId)));
            targetAnimator.GetComponent<FightBody>().Initialize(targetAniId, onAnimationHit, onAnimationFinish, onAnimationHitCue);

            var targetInitialHp = attackResult.Damages.Count > 0
                ? attackResult.Damages[0].HpBefore : attackResult.Target.Hp;
            updateTargetHp(targetInitialHp);

            // A physical attack never spends MP, so each MP bar just shows current / max.
            updateMpBar(subjectMpBar, attackResult.Subject);
            updateMpBar(targetMpBar, attackResult.Target);


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
                attackRoundStarted = false;
                if (remoteAttackEnabled)
                {
                    HideTargetForRemote();
                }

                MonoBehaviourUtils.ExecuteWithDelay(this, ANIMATION_INTERVAL, () =>
                {
                    subjectAnimator.SetInteger("actionState", 1);
                    attackRoundStarted = true;
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
                        SceneManager.UnloadSceneAsync("GameBattleScene");
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

            // Determine which body is being hit in this animation round, and the damage
            // being applied to it, so a miss (hit value 0) can skip the knockback.
            GameObject hitObject = getRoundHitObject(out DamageResult hitDamage);

            // Camera orientation maps world -X to screen-right.
            // localBody steps back to screen-right (-X world), foreignBody steps back to screen-left (+X world).
            Vector3 knockbackDir = (hitObject == localBody) ? new Vector3(-1, 0, 0) : new Vector3(1, 0, 0);

            // A miss (no HP change) plays no knockback animation.
            bool applyKnockback = hitDamage != null && !hitDamage.HasMissed;

            // The strike sound was already started, a moment ahead: see onAnimationHitCue.

            // A critical hit flashes the screen edge white, once, on its first hit frame.
            if (applyKnockback && hitDamage.IsCritical && criticalFlashedIndex != currentAnimationIndex)
            {
                criticalFlashedIndex = currentAnimationIndex;
                StartCoroutine(FlashCriticalEdge());
            }

            EnemyHitEffect hitEffect = hitObject.GetComponent<EnemyHitEffect>();
            if (hitEffect != null)
                hitEffect.OnHit(knockbackDir, applyKnockback);

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

        /// <summary>
        /// A white ring round the screen edge (the magic screen flash's shape), on an overlay
        /// canvas above both cameras, fading out over CriticalFlashSeconds.
        /// </summary>
        private IEnumerator FlashCriticalEdge()
        {
            GameObject canvasObject = new GameObject("CriticalFlash");
            SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            GameObject imageObject = new GameObject("EdgeRing");
            imageObject.transform.SetParent(canvasObject.transform, false);
            RawImage image = imageObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Texture2D ring = MagicEffect.CreateEdgeRingTexture(Color.white);
            image.texture = ring;

            float elapsed = 0f;
            while (elapsed < CriticalFlashSeconds)
            {
                float t = elapsed / CriticalFlashSeconds;
                image.color = new Color(1, 1, 1, 1f - t * t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(canvasObject);
            Destroy(ring);
        }

        /// <summary>
        /// The body taking this animation round's hits, and the damage it takes: the target
        /// while the subject attacks, the subject while the target counters.
        /// </summary>
        private GameObject getRoundHitObject(out DamageResult hitDamage)
        {
            if (currentAnimationIndex < attackResult.Damages.Count)
            {
                hitDamage = attackResult.Damages[currentAnimationIndex];
                return targetObject;
            }

            int backHitIndex = currentAnimationIndex - attackResult.Damages.Count;
            hitDamage = backHitIndex < attackResult.BackDamages.Count
                ? attackResult.BackDamages[backHitIndex] : null;
            return subjectObject;
        }

        /// <summary>
        /// Every strike is heard: a thwack when it lands, a swish through the air when it
        /// misses. FightBody calls this SoundEffectTable.BattleHitLeadSeconds ahead of each
        /// hit (onAnimationHit), so the sound is heard on the hit frame and not after it.
        /// </summary>
        private void onAnimationHitCue(int hitIndex)
        {
            GameObject hitObject = getRoundHitObject(out DamageResult hitDamage);
            bool landed = hitDamage != null && !hitDamage.HasMissed;

            // The striker is whoever is not taking this hit: the subject, or the target countering.
            FDCreature striker = (hitObject == targetObject) ? attackResult.Subject : attackResult.Target;
            playStrikeSound(landed, striker);
        }

        private void playStrikeSound(bool landed, FDCreature striker)
        {
            if (landed)
            {
                int strikerAnimationId = striker?.Definition != null ? striker.Definition.AnimationId : 0;
                SoundEffects.PlayBattleHit(strikerAnimationId, striker?.GetAttackItem());
            }
            else
            {
                SoundEffects.Play(SoundEffect.BattleMiss);
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
            // previous round's RemoteAttackFrame).
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

        // The bar goes for either side's ranged attacker, on the same frame; subjectTai is
        // only ever set for our side (the Tai is a local-only object).
        private void SetSubjectExtrasActive(bool active)
        {
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
            targetHpBar.transform.localScale = new Vector3(targetHpScale, 1, 1);

            if (targetBarInfo != null) targetBarInfo.SetHp(current);
        }

        private void updateMpBar(GameObject mpBar, FDCreature creature)
        {
            if (mpBar != null)
            {
                mpBar.transform.localScale = new Vector3(getBarScale(creature.Mp, creature.MpMax), 1, 1);
            }
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
            if (remoteAttackEnabled && attackRoundStarted && subjectAnimator != null && subjectFightAnimation != null
                && (targetHiddenForRemote || !subjectExtrasHidden))
            {
                AnimatorStateInfo state = subjectAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("attack"))
                {
                    float frame = state.normalizedTime * subjectFightAnimation.AttackFrameCount;

                    // Hide the subject's bar/tai on the same frame the creature leaves the
                    // animation, i.e. when the attack reaches RemoteAttackFrame.
                    if (!subjectExtrasHidden && frame >= subjectFightAnimation.RemoteAttackFrame)
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