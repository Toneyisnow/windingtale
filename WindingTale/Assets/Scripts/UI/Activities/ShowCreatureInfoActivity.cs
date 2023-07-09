using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Objects;
using WindingTale.UI.Scenes;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.UI.Activities
{
    public enum CreatureInfoType
    {
        SelectEquipItem = 1,
        SelectUseItem = 2,
        SelectAllItem = 3,
        SelectMagic = 4,
        View = 5,
    }

    public class ShowCreatureInfoActivity : ActivityBase
    {
        public FDCreature Creature { get; private set; }

        public CreatureInfoType InfoType { get; private set; }

        private Action<int> callback = null;

        public ShowCreatureInfoActivity(GameMain gameMain, FDCreature creature, CreatureInfoType infoType, Action<int> callback)
        {
            this.Creature = creature;
            this.InfoType = infoType;
            this.callback = callback;
        }

        public override void Start(GameObject gameInterface)
        {
            GameFieldScene gameFieldScene = gameInterface.GetComponent<GameFieldScene>();
            GameCanvas gameCanvas = gameFieldScene.canvas.GetComponent<GameCanvas>();
            gameCanvas.ShowCreatureDialog(this.callback);

        }

    }
}