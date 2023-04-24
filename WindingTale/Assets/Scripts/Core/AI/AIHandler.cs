using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Data;
using WindingTale.Core.Definitions;
using WindingTale.Core.ObjectModels;
using WindingTale.AI.Delegates;
using WindingTale.Legacy.Core.Components.Data;

namespace WindingTale.AI
{

    public class AIHandler
    {
        private IGameAction gameAction = null;

        private int lastOperatedCreatureId = 0;

        public CreatureFaction Faction
        {
            get; private set;
        }

        private List<FDCreature> creatureList = null;

        public AIHandler(IGameAction gameAction, CreatureFaction faction)
        {
            this.gameAction = gameAction;
            this.Faction = faction;

            if (this.Faction == CreatureFaction.Enemy)
            {
                creatureList = gameAction.GetAllEnemies();
            }
            else if (this.Faction == CreatureFaction.Npc)
            {
                creatureList = gameAction.GetAllNpcs();
            }
        }

        public void IsNotified()
        {
            FDCreature selectedCreature = null;

            foreach (FDCreature creature in creatureList)
            {
                if (creature.HasActioned || creature.IsFrozen())
                {
                    continue;
                }

                if (selectedCreature == null || creature.CreatureId < selectedCreature.CreatureId)
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

        private void RunAIDelegate(FDCreature creature)
        {
            AIDelegate aiDelegate = null;

            switch (creature.Data.AIType)
            {
                case CreatureData.AITypes.AIType_Aggressive:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalAggressiveDelegate(gameAction, creature);
                    }
                    else
                    {
                        aiDelegate = new AIAggressiveDelegate(gameAction, creature);
                    }
                    break;
                case CreatureData.AITypes.AIType_Defensive:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalDefensiveDelegate(gameAction, creature);
                    }
                    else
                    {
                        aiDelegate = new AIDefensiveDelegate(gameAction, creature);
                    }
                    break;
                case CreatureData.AITypes.AIType_Guard:
                    if (creature.Definition.IsMagical())
                    {
                        aiDelegate = new AIMagicalGuardDelegate(gameAction, creature);
                    }
                    else
                    {
                        aiDelegate = new AIGuardDelegate(gameAction, creature);
                    }
                    break;
                case CreatureData.AITypes.AIType_Escape:
                    aiDelegate = new AIEscapeDelegate(gameAction, creature);
                    break;
                case CreatureData.AITypes.AIType_Treasure:
                    aiDelegate = new AITreasureDelegate(gameAction, creature);
                    break;
            }

            if (aiDelegate != null)
            {
                lastOperatedCreatureId = creature.CreatureId;
                aiDelegate.TakeAction();
            }

        }

    }
}
