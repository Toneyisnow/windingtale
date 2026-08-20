using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene;
using WindingTale.Scenes.GameFieldScene.Activities;

namespace WindingTale.AI.Delegates
{
    /// <summary>
    /// Shared behaviour of every spell caster: magic first, from where it stands.
    ///
    /// A caster spends its turn looking for a spell worth casting -- an attack on the weakest
    /// opponent in reach, or help for the worst hurt of its own side -- and casts it without
    /// moving, since walking first would cost it the spell. Only when it finds nothing worth
    /// casting does it defer the turn (PendAction) and come back once its team mates have
    /// acted, to do whatever its subclass does without magic: charge, or hold position.
    /// </summary>
    public abstract class AIMagicalDelegate : AIDelegate
    {
        public AIMagicalDelegate(GameMain gameMain, FDAICreature c) : base(gameMain, c)
        {

        }

        public override void TakeAction()
        {
            bool pending = (this.creature as FDAICreature)?.PendingAction ?? false;

            if (pending)
            {
                // Second time round: no magic was worth casting, so fall back on fighting.
                this.TakePendAction();
                return;
            }

            if (this.NeedAndCanRecover())
            {
                this.SelfRecover();
                return;
            }

            this.TakeMagicAction();
        }

        /// <summary>
        /// What the caster does with the turn it deferred, once every team mate has acted.
        /// </summary>
        protected abstract void TakePendAction();

        /// <summary>
        /// Casts the best spell available, or defers the turn when there is none.
        /// </summary>
        private void TakeMagicAction()
        {
            List<MagicDefinition> magics = this.GetAvailableMagics();

            // For each spell it could cast, who it would cast it on. A spell with nobody
            // worth casting it on is dropped.
            List<MagicDefinition> castableMagics = new List<MagicDefinition>();
            List<FDCreature> candidates = new List<FDCreature>();

            foreach (MagicDefinition magic in magics)
            {
                FDCreature candidate = null;
                if (magic.Type == MagicType.Attack || magic.Type == MagicType.Offensive)
                {
                    candidate = this.FindOffensiveCandidate(magic);
                }
                else if (magic.Type == MagicType.Recover || magic.Type == MagicType.Defensive)
                {
                    candidate = this.FindDefensiveCandidate(magic);
                }

                if (candidate != null)
                {
                    castableMagics.Add(magic);
                    candidates.Add(candidate);
                }
            }

            if (castableMagics.Count == 0)
            {
                Debug.Log("AI Magical: creature=" + creature.Id + " found no magic to spell, pending action.");
                this.PendAction();
                return;
            }

            int magicIndex = ChooseMagicIndex(castableMagics);
            MagicDefinition selectedMagic = castableMagics[magicIndex];
            FDCreature selectedCandidate = candidates[magicIndex];

            FDPosition magicPosition = this.LocateMagicPosition(selectedMagic, selectedCandidate);

            Debug.Log(string.Format("AI Magical: creature={0} spells magic={1} at {2} targeting creature={3}",
                creature.Id, selectedMagic.MagicId, magicPosition, selectedCandidate.Id));

            gameMain.PushActivity(new SlideCursorActivity(magicPosition));
            gameMain.PushActivity((game) => game.creatureMagic(this.creature, magicPosition, selectedMagic.MagicId));
        }

        /// <summary>
        /// The spells the creature could cast right now: known, defined, and paid for out of
        /// the MP it has left. Walked from the last learned to the first, so the strongest
        /// spell is the one considered first.
        /// </summary>
        protected List<MagicDefinition> GetAvailableMagics()
        {
            List<MagicDefinition> magics = new List<MagicDefinition>();

            if (!this.creature.CanSpellMagic())
            {
                return magics;
            }

            for (int index = this.creature.Magics.Count - 1; index >= 0; index--)
            {
                MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(this.creature.Magics[index]);
                if (magic == null)
                {
                    continue;
                }

                if (this.creature.Mp >= magic.MpCost)
                {
                    magics.Add(magic);
                }
            }

            return magics;
        }

        /// <summary>
        /// Who to aim an attack spell at: of the opponents the spell can reach, the one with
        /// the least HP left. Null when it can reach nobody.
        /// </summary>
        private FDCreature FindOffensiveCandidate(MagicDefinition magic)
        {
            FDCreature candidate = null;

            foreach (FDCreature c in this.GetOppositeCreatures())
            {
                if (c is FDAICreature ai && !ai.IsNoticable())
                {
                    continue;
                }

                if (!this.IsWithinMagicReach(magic, c))
                {
                    continue;
                }

                if (candidate == null || c.Hp < candidate.Hp)
                {
                    candidate = c;
                }
            }

            return candidate;
        }

        /// <summary>
        /// Who to aim a healing or buffing spell at: of its own side within reach, the one the
        /// spell would actually do something for, worst hurt first. Null when there is nobody.
        /// </summary>
        private FDCreature FindDefensiveCandidate(MagicDefinition magic)
        {
            FDCreature candidate = null;

            foreach (FDCreature c in this.GetSameSideCreatures())
            {
                if (!this.IsWithinMagicReach(magic, c))
                {
                    continue;
                }

                if (!HasDefensiveEffectOn(magic, c))
                {
                    continue;
                }

                if (candidate == null || c.Hp < candidate.Hp)
                {
                    candidate = c;
                }
            }

            return candidate;
        }

        /// <summary>
        /// Whether the spell can be made to cover the target: cast as far out as its range
        /// allows, the blast still has to reach the target. Cross magic (光子炮) only fires
        /// along a row or a column, so the target has to be lined up with the caster.
        /// </summary>
        private bool IsWithinMagicReach(MagicDefinition magic, FDCreature target)
        {
            if (GetDirectDistance(this.creature.Position, target.Position) > magic.EffectRange + magic.EffectScope)
            {
                return false;
            }

            if (magic.IsCross
                && target.Position.X != this.creature.Position.X
                && target.Position.Y != this.creature.Position.Y)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Where to centre the spell so that it covers the candidate: on the candidate itself
        /// when that is within casting range, otherwise pulled back along the line towards the
        /// caster until it is -- the candidate is then at the edge of the blast, which
        /// IsWithinMagicReach has already guaranteed is close enough.
        /// </summary>
        private FDPosition LocateMagicPosition(MagicDefinition magic, FDCreature candidate)
        {
            FDPosition from = this.creature.Position;
            FDPosition to = candidate.Position;

            int overflow = GetDirectDistance(from, to) - magic.EffectRange;
            if (overflow <= 0)
            {
                return to;
            }

            int deltaX = to.X - from.X;
            int deltaY = to.Y - from.Y;

            int pullY = Math.Min(overflow, Math.Abs(deltaY));
            int pullX = Math.Min(overflow - pullY, Math.Abs(deltaX));

            return FDPosition.At(to.X - Math.Sign(deltaX) * pullX, to.Y - Math.Sign(deltaY) * pullY);
        }

        /// <summary>
        /// Whether the spell would do the target any good: healing for the wounded, an
        /// antidote for the poisoned, a thaw for the frozen, a buff for anyone.
        /// </summary>
        private static bool HasDefensiveEffectOn(MagicDefinition magic, FDCreature target)
        {
            if (magic.Type == MagicType.Recover)
            {
                return target.Hp < target.HpMax;
            }

            switch (magic.MagicId)
            {
                case 404:
                    return target.HasEffect(CreatureEffects.Poisoned);
                case 405:
                    return target.HasEffect(CreatureEffects.Frozen);
                case 401:
                case 402:
                case 403:
                case 407:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Picks one of the castable spells at random, weighted by how keen the AI is on each
        /// (MagicDefinition.AiConsiderRate) -- so a caster does not open with its strongest
        /// spell every single battle.
        /// </summary>
        private static int ChooseMagicIndex(List<MagicDefinition> magics)
        {
            int totalConsideration = 0;
            foreach (MagicDefinition magic in magics)
            {
                totalConsideration += magic.AiConsiderRate;
            }

            if (totalConsideration <= 0)
            {
                return UnityEngine.Random.Range(0, magics.Count);
            }

            int ranValue = UnityEngine.Random.Range(0, totalConsideration);
            for (int index = 0; index < magics.Count; index++)
            {
                ranValue -= magics[index].AiConsiderRate;
                if (ranValue < 0)
                {
                    return index;
                }
            }

            return 0;
        }

    }
}
