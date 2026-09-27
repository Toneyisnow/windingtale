using UnityEngine;
using WindingTale.AI.Delegates;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;

namespace WindingTale.Scenes.GameFieldScene.Activities
{
    /// <summary>
    /// One march of the party (see GameMain.StartMarch): the point it heads for, whoever is
    /// stepping out right now, and whether the player has called it off. Every activity the
    /// march queues holds the session and checks Cancelled, so calling it off silences
    /// whatever is still waiting in the queue without having to dig it out.
    /// </summary>
    public class MarchSession
    {
        /// <summary>The tile the party marches on: the middle of the enemy, moved onto walkable ground.</summary>
        public FDPosition Target { get; private set; }

        /// <summary>The friend whose turn the march is on, or null between two of them.</summary>
        public FDCreature Current { get; set; }

        public bool Cancelled { get; set; }

        /// <summary>The frame the march began on; a cancel key from that frame is the one that started it.</summary>
        public int StartFrame { get; private set; }

        public MarchSession(FDPosition target)
        {
            this.Target = target;
            this.StartFrame = Time.frameCount;
        }
    }

    /// <summary>
    /// The cursor's slide to a tile, as SlideCursorActivity does it, but dropped the moment
    /// the march is called off.
    /// </summary>
    public class MarchSlideActivity : SlideCursorActivity
    {
        private readonly MarchSession session;

        public MarchSlideActivity(MarchSession session, FDPosition target) : base(target)
        {
            this.session = session;
        }

        public override void Start(GameMain gameMain)
        {
            if (session.Cancelled)
            {
                this.HasFinished = true;
                return;
            }

            base.Start(gameMain);
        }

        public override void Update(GameMain gameMain)
        {
            if (session.Cancelled)
            {
                this.HasFinished = true;
                return;
            }

            base.Update(gameMain);
        }
    }

    /// <summary>
    /// One friend's step of the march, in the order an enemy takes its own: with the cursor
    /// already at the creature's feet, it works out where the walk ends, the cursor slides
    /// there, and the creature walks.
    ///
    /// Calling the march off during the walk stops the creature where it is and puts it back
    /// on the tile it set out from, with its turn still unspent -- see Abort.
    /// </summary>
    public class MarchMoveActivity : ActivityBase
    {
        private enum Phase
        {
            SlidingToDestination,
            Walking,
        }

        private readonly MarchSession session;
        private readonly FDCreature creature;

        private FDMovePath path = null;
        private FDPosition origin = null;
        private Phase phase = Phase.SlidingToDestination;

        public MarchMoveActivity(MarchSession session, FDCreature creature)
        {
            this.session = session;
            this.creature = creature;
        }

        public override void Start(GameMain gameMain)
        {
            this.HasFinished = false;
            this.origin = creature.Position;

            if (session.Cancelled)
            {
                this.HasFinished = true;
                return;
            }

            // Worked out now, with everyone who marched before already where they stopped.
            path = new AIMarchDelegate(gameMain, creature).PlanMarch(session.Target);
            FDPosition destination = path != null ? path.Desitination : null;
            if (destination == null || destination.AreSame(creature.Position))
            {
                // Already as close as it can get: it stays put and ends its turn.
                this.HasFinished = true;
                return;
            }

            gameMain.gameMap.SlideCursorTo(destination, GameCanvas.DialogPosition.Bottom);
        }

        public override void Update(GameMain gameMain)
        {
            if (this.HasFinished)
            {
                return;
            }

            if (session.Cancelled)
            {
                Abort(gameMain);
                return;
            }

            if (phase == Phase.SlidingToDestination)
            {
                if (gameMain.gameMap.IsSlideBusy)
                {
                    return;
                }

                creature.PrePosition = creature.Position;
                gameMain.gameMap.MoveCreature(creature, path);
                phase = Phase.Walking;
                return;
            }

            Creature creatureObj = gameMain.gameMap.GetCreature(creature);
            if (creatureObj == null || creatureObj.GetComponent<CreatureWalk>() == null)
            {
                creature.Position = path.Desitination;
                this.HasFinished = true;
            }
        }

        /// <summary>
        /// The march was called off with this creature on its way: stop the walk, put the icon
        /// and the creature back on the tile it started from, and leave it as it was before its
        /// turn -- not moved, not acted -- so the player can take it from there.
        /// </summary>
        private void Abort(GameMain gameMain)
        {
            this.HasFinished = true;

            Creature creatureObj = gameMain.gameMap.GetCreature(creature);
            if (creatureObj != null)
            {
                CreatureWalk walk = creatureObj.GetComponent<CreatureWalk>();
                if (walk != null)
                {
                    Object.Destroy(walk);
                }

                Animator animator = creatureObj.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetInteger("state", 0);
                }
            }

            gameMain.gameMap.ResetCreaturePosition(creature, origin);
            creature.Position = origin;
            creature.PrePosition = null;
            gameMain.gameMap.SlideCursorTo(origin, GameCanvas.DialogPosition.Bottom);
        }
    }
}
