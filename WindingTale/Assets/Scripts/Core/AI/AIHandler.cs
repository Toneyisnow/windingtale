using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Core.Definitions;
using WindingTale.AI.Delegates;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.AI
{

    public class AIHandler
    {

        private int lastOperatedCreatureId = 0;

        public CreatureFaction Faction
        {
            get; private set;
        }

        private GameMain gameMain = null;

        private List<FDAICreature> creatureList = null;

        public AIHandler(GameMain gameMain, CreatureFaction faction)
        {
            this.gameMain = gameMain;
            this.Faction = faction;
        }

        public void IsNotified()
        {
            FDAICreature selectedCreature = null;

            foreach (FDAICreature creature in creatureList)
            {
                if (creature.HasActioned || creature.HasEffect(CreatureEffects.Frozen))
                {
                    continue;
                }

                if (selectedCreature == null || creature.Id < selectedCreature.Id)
                {
                    selectedCreature = creature;
                }
            }

            if (selectedCreature == null)
            {
                return;
            }

            RunAIDelegate(selectedCreature);
        }

        private void RunAIDelegate(FDAICreature creature)
        {
            AIDelegate aiDelegate = null;

            switch (creature.AIType)
            {
                case AITypes.AIType_Aggressive:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalAggressiveDelegate(gameMain, creature);
                    }
                    else
                    {
                        aiDelegate = new AIAggressiveDelegate(gameMain, creature);
                    }
                    break;
                case AITypes.AIType_Defensive:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalDefensiveDelegate(gameMain, creature);
                    }
                    else
                    {
                        aiDelegate = new AIDefensiveDelegate(gameMain, creature);
                    }
                    break;
                case AITypes.AIType_Guard:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalGuardDelegate(gameMain, creature);
                    }
                    else
                    {
                        aiDelegate = new AIGuardDelegate(gameMain, creature);
                    }
                    break;
                case AITypes.AIType_Escape:
                    aiDelegate = new AIEscapeDelegate(gameMain, creature);
                    break;
                case AITypes.AIType_Treasure:
                    aiDelegate = new AITreasureDelegate(gameMain, creature);
                    break;
            }

            if (aiDelegate != null)
            {
                lastOperatedCreatureId = creature.Id;
                aiDelegate.TakeAction();
            }

        }

    }
}
