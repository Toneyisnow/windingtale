using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindingTale.Core.Definitions;
using WindingTale.AI.Delegates;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;
using System.Diagnostics;

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

        private List<FDCreature> creatures = null;

        public AIHandler(GameMain gameMain, CreatureFaction faction)
        {
            this.gameMain = gameMain;
            this.Faction = faction;

            
        }

        public void Notified()
        {
            UnityEngine.Debug.Log("AIHandler Notified");

            List<FDCreature> creatures = null;
            if (this.Faction == CreatureFaction.Enemy)
            {
                creatures = gameMain.GameMap.Enemies;
            }
            else if (this.Faction == CreatureFaction.Npc)
            {
                creatures = gameMain.GameMap.Npcs;
            }

            FDCreature selectedCreature = null;
            foreach (FDCreature creature in creatures)
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

            UnityEngine.Debug.Log("AIHandler Found creature");
            RunAIDelegate(selectedCreature as FDAICreature);

            return;
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
