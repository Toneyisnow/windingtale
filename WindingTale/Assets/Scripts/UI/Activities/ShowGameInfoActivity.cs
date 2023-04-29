using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public class ShowGameInfoActivity : ActivityBase
    {
        public FDCreature Creature { get; private set; }

        public CreatureInfoType InfoType { get; private set; }


        public ShowGameInfoActivity(GameMain gameMain)
        {

        }

        public override void Start(IGameInterface gameInterface)
        {
            throw new System.NotImplementedException();
        }


        public override void Update(IGameInterface gameInterface)
        {
            throw new System.NotImplementedException();
        }
    }
}