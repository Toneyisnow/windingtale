using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Localization;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;

namespace WindingTale.Scenes.GameFieldScene.Activities
{
    public class TalkActivity : ActivityBase
    {

        /// <summary>
        /// The creature on the map that is talking. If null, need to fetch again during Start.
        /// </summary>        
        private FDCreature creature = null;

        private int creatureId = 0;

        /// <summary>
        /// The raw text to be displayed.
        /// </summary>
        private LocalizedString rawText = null;

        /// <summary>
        /// Pre-assembled text, used instead of rawText when set (multi-message lines).
        /// </summary>
        private string resolvedText = null;

        /// <summary>
        /// Need to confirm Yes or No
        /// </summary>
        private bool needConfirm = false;

        private Action<int> onSelected = null;

        private Nullable<int> selectedResult = null;

        private bool isMessage = false;

        /// <summary>
        /// Show the TalkDialog and wait for the user to click close or confirm.
        /// </summary>
        /// <param name="gameMain"></param>
        /// <param name="creature"></param>
        /// <param name="message"></param>
        public TalkActivity(FDMessage message, FDCreature creature = null, Action<int> onSelected = null)
        {
            this.creature = creature;
            this.onSelected = onSelected;

            this.rawText = LocalizationManager.GetFDMessageString(message);
            this.needConfirm = message.MessageType == FDMessage.MessageTypes.Confirm;

            this.isMessage = true;
        }

        /// <summary>
        /// Show several message lines as ONE dialog. The lines are resolved and joined
        /// here because no single LocalizedString can span table entries; each entry
        /// keeps its own '#' line-break markers, which TalkDialog turns into newlines.
        /// Used for the level-up summary (header + one line per improved stat).
        /// </summary>
        public TalkActivity(List<FDMessage> messages, FDCreature creature = null, Action<int> onSelected = null)
        {
            this.creature = creature;
            this.onSelected = onSelected;

            StringBuilder builder = new StringBuilder();
            foreach (FDMessage message in messages)
            {
                builder.Append(LocalizationManager.GetFDMessageString(message).GetLocalizedString());
            }
            this.resolvedText = builder.ToString();

            // Still needed as the fallback text source; the resolved string wins.
            this.rawText = LocalizationManager.GetFDMessageString(messages[0]);
            this.needConfirm = false;

            this.isMessage = true;
        }

        public TalkActivity(Conversation conversation, int creatureId = 0, Action<int> onSelected = null)
        {
            this.creatureId = creatureId;
            
            this.onSelected = onSelected;

            this.rawText = LocalizationManager.GetConversationString(conversation);
            this.needConfirm = false;
        }

        public override void Start(GameMain gameMain)
        {
            // A creature can be scripted to talk after it has died (a dying line, or a
            // conversation that plays out over the death). It is gone from the live list
            // by then, so fall back to DeadCreatures for its portrait and faction.
            bool isDead = false;
            if (this.creature == null && this.creatureId > 0)
            {
                this.creature = gameMain.gameMap.Map.GetCreatureById(this.creatureId);
                if (this.creature == null)
                {
                    this.creature = gameMain.gameMap.Map.DeadCreatures.Find(c => c.Id == this.creatureId);
                    isDead = this.creature != null;
                }
            }

            int creatureAnimationId = this.creature?.Definition?.AnimationId ?? 0;

            // Friend talks from the Bottom; Enemy / Npc (and the narrator) from the Top.
            GameCanvas.DialogPosition dialogPosition = this.creature?.Faction == CreatureFaction.Friend
                ? GameCanvas.DialogPosition.Bottom
                : GameCanvas.DialogPosition.Top;

            // Slide the map cursor to under the speaking creature before it talks. A dead
            // creature's Position is stale — it is no longer on the map — so leave the
            // cursor where it is in that case.
            if (this.creature != null && this.creature.Position != null && !isDead)
            {
                gameMain.gameMap.SlideCursorTo(this.creature.Position, dialogPosition);
            }

            gameMain.gameCanvas.ShowTalkDialog(creatureAnimationId, this.rawText, needConfirm, dialogPosition,
                (result) =>
                {
                    selectedResult = result;
                },
                isMessage ? -1 : gameMain.ChapterId,
                this.resolvedText);

            this.HasFinished = false;
        }

        // Update is called once per frame
        public override void Update(GameMain gameMain)
        {
            if (this.HasFinished)
            {
                return;
            }

            if (selectedResult.HasValue)
            {
                this.HasFinished = true;
                if (onSelected != null)
                {
                    onSelected(selectedResult.Value);
                }
            }
        }
    }
}