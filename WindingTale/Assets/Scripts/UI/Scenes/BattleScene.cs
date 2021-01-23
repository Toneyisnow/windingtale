using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Components.Algorithms;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Battle;
using WindingTale.UI.Controls;

namespace WindingTale.UI.Scenes
{
    public class BattleScene : MonoBehaviour
    {
        private enum FightPhase
        {
            Attack1,
            Attack2,
            Back1,
            Back2,
            Completed,
        }

        // Public for Unity Editor to set
        public GameObject FriendPlaceHolder;
        public GameObject EnemyPlaceHolder;

        public GameObject FriendBarPlaceHolder;
        public GameObject EnemyBarPlaceHolder;

        private FightInformation fightInformation = null;
        private FDCreature subject;
        private FDCreature target;

        private IFightAnimator subjectAnimator;
        private IFightAnimator targetAnimator;

        private FightPhase fightPhase;

        private CreatureInfoBar subjectInfoBar;
        private CreatureInfoBar targetInfoBar;

        // Start is called before the first frame update
        void Start()
        {
            DefinitionStore.Instance.LoadChapter(5);
            //DefinitionStore.Instance.LoadChapter(18);

            //CreatureDefinition def = DefinitionStore.Instance.GetCreatureDefinition(50508);

            FDCreature subject = new FDCreature(4, CreatureFaction.Friend, DefinitionStore.Instance.GetCreatureDefinition(111), FDPosition.At(0, 0));
            //// FDCreature target = new FDCreature(11, CreatureFaction.Enemy, DefinitionStore.Instance.GetCreatureDefinition(51803), FDPosition.At(0, 0));
            FDCreature target = new FDCreature(11, CreatureFaction.Enemy, DefinitionStore.Instance.GetCreatureDefinition(50505), FDPosition.At(0, 0));

            AttackInformation a1 = new AttackInformation(100, 50, false);
            AttackInformation a2 = new AttackInformation(50, 20, false);
            AttackInformation b1 = new AttackInformation(100, 50, false);
            AttackInformation b2 = new AttackInformation(50, 20, false);

            FightInformation fightInfo = new FightInformation(a1, a2, b1, b2);

            this.Initialize(subject, target, fightInfo);

            fightPhase = FightPhase.Attack1;
            Invoke("DoSubjectAttack", 1.2f);
            
        }

        public void Initialize(FDCreature subject, FDCreature target, FightInformation fightInfo)
        {
            this.fightInformation = fightInfo;
            this.subject = subject;
            this.target = target;

            subjectInfoBar = new CreatureInfoBar();
            targetInfoBar = new CreatureInfoBar();

            int subjectAniId = subject.Definition.AnimationId;
            int targetAniId = target.Definition.AnimationId;

            Transform subjectTransform;
            Transform targetTransform;
            Transform subjectBarTransform;
            Transform targetBarTransform;

            if (target.Faction == CreatureFaction.Enemy)
            {
                subjectTransform = FriendPlaceHolder.transform;
                targetTransform = EnemyPlaceHolder.transform;

                subjectBarTransform = FriendBarPlaceHolder.transform;
                targetBarTransform = EnemyBarPlaceHolder.transform;
            }
            else
            {
                subjectTransform = EnemyPlaceHolder.transform;
                targetTransform = FriendPlaceHolder.transform;

                subjectBarTransform = EnemyBarPlaceHolder.transform;
                targetBarTransform = FriendBarPlaceHolder.transform;
            }

            subjectAnimator = CreateFightObject(subjectAniId, subjectTransform);
            targetAnimator = CreateFightObject(targetAniId, targetTransform);
        }

        // Update is called once per frame
        void Update()
        {

        }

        private FightAnimator CreateFightObject(int animationId, Transform placeHolder)
        {
            GameObject obj = new GameObject();
            obj.transform.parent = placeHolder;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = new Vector3(15, 15, 15);
            obj.transform.localEulerAngles = new Vector3(0, 90, 0);

            obj.AddComponent<SpriteRenderer>();

            Animator animator = obj.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(string.Format(@"Fights/{0}/animator{0}", StringUtils.Digit3(animationId)));

            FightAnimator fightAnimator = obj.AddComponent<FightAnimator>();
            fightAnimator.Initialize(animationId);

            fightAnimator.OnAttackHittingCallback((value) =>
            {
                this.OnAttackHitting(value);
            });
            fightAnimator.OnAttackCompleteCallback(() =>
            {
                this.OnAttackComplete();
            });

            return fightAnimator;
        }


        private void OnAttackHitting(int percentage)
        {
            Debug.Log("OnAttackHitting: " + percentage);
            int value;
            switch (fightPhase)
            {
                case FightPhase.Attack1:
                    value = CalculatePercentage(fightInformation.Attack1, percentage);
                    targetInfoBar.SetHP(value);
                    targetAnimator.StartKnocked();
                    break;
                case FightPhase.Attack2:
                    value = CalculatePercentage(fightInformation.Attack2, percentage);
                    targetInfoBar.SetHP(value);
                    targetAnimator.StartKnocked();
                    break;
                case FightPhase.Back1:
                    value = CalculatePercentage(fightInformation.Back1, percentage);
                    subjectInfoBar.SetHP(value);
                    subjectAnimator.StartKnocked();
                    break;
                case FightPhase.Back2:
                    value = CalculatePercentage(fightInformation.Back2, percentage);
                    subjectInfoBar.SetHP(value);
                    subjectAnimator.StartKnocked();
                    break;
                default:
                    break;
            }

        }

        private void OnAttackComplete()
        {
            Debug.Log("OnAttackComplete.");

            FightPhase nextPhase = FightPhase.Attack1;

            switch (fightPhase)
            {
                case FightPhase.Attack1:
                    if (fightInformation.Attack2 != null)
                    {
                        nextPhase = FightPhase.Attack2;
                    }
                    else if (fightInformation.Back1 != null)
                    {
                        nextPhase = FightPhase.Back1;
                    }
                    else
                    {
                        nextPhase = FightPhase.Completed;
                    }

                    break;
                case FightPhase.Attack2:
                    if (fightInformation.Back1 != null)
                    {
                        nextPhase = FightPhase.Back1;
                    }
                    else
                    {
                        nextPhase = FightPhase.Completed;
                    }
                    break;
                case FightPhase.Back1:
                    if (fightInformation.Back2 != null)
                    {
                        nextPhase = FightPhase.Back2;
                    }
                    else
                    {
                        nextPhase = FightPhase.Completed;
                    }
                    break;
                case FightPhase.Back2:
                    nextPhase = FightPhase.Completed;
                    break;
                default:
                    nextPhase = FightPhase.Completed;
                    break;
            }

            fightPhase = nextPhase; 
            switch (nextPhase)
            {
                case FightPhase.Attack2:
                    Invoke("DoSubjectAttack", 0.2f);
                    break;
                case FightPhase.Back1:
                case FightPhase.Back2:
                    Invoke("DoTargetAttack", 0.2f);
                    break;
                case FightPhase.Completed:
                    CompletFight();
                    break;
                default:
                    break;
            }
        }

        private void DoSubjectAttack()
        {
            subjectAnimator.StartAttack();
        }

        private void DoTargetAttack()
        {
            targetAnimator.StartAttack();
        }

        private void CompletFight()
        {
            Debug.Log("Fight is completed.");
        }

        private int CalculatePercentage(AttackInformation info, int percentage)
        {
            return info.HpAfter + (info.HpBefore - info.HpAfter) * percentage / 100;
        }

    }
}