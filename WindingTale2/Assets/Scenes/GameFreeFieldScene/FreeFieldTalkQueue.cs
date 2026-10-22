using System;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.Scenes.GameFreeFieldScene
{
    /// <summary>
    /// The free field's cut-down ActivityQueue: a line of talk waits here until the one
    /// before it has been dismissed, then plays in the scene's TalkDialog. Between the
    /// lines an event may also queue an action -- a creature walking somewhere -- and the
    /// talk waits for that too.
    /// </summary>
    public class FreeFieldTalkQueue
    {
        /// <summary>One thing to wait for: a line of talk, or an action.</summary>
        private class Item
        {
            // A line: its text key, whose portrait it is, and which edge it shows at.
            public Conversation Conversation;
            public int AnimationId;
            public GameCanvas.DialogPosition Position;

            // An action: started once, then polled until it says it is done.
            public Action Start;
            public Func<bool> IsDone;

            public bool IsLine
            {
                get { return Conversation != null; }
            }
        }

        private readonly Queue<Item> items = new Queue<Item>();

        private readonly GameObject dialog;

        private readonly int mapId;

        private Item current = null;

        private bool currentDone = false;

        /// <summary>Frame on which the last line was dismissed; see <see cref="WasClosedThisFrame"/>.</summary>
        private int lastClosedFrame = -1;

        public FreeFieldTalkQueue(GameObject dialog, int mapId)
        {
            this.dialog = dialog;
            this.mapId = mapId;
        }

        /// <summary>True while a line or an action is under way or waiting to be.</summary>
        public bool IsBusy
        {
            get { return current != null || items.Count > 0; }
        }

        /// <summary>
        /// True on the frame the last line went away (or the one after, since the dialog
        /// may close itself after the scene's Update has run): the key that dismissed it
        /// must not also move the hero or start the talk again.
        /// </summary>
        public bool WasClosedThisFrame()
        {
            return Time.frameCount - lastClosedFrame <= 1;
        }

        public void Push(int conversationId, int sequenceId, int animationId, GameCanvas.DialogPosition position)
        {
            items.Enqueue(new Item
            {
                Conversation = Conversation.Create(mapId, conversationId, sequenceId),
                AnimationId = animationId,
                Position = position,
            });
        }

        /// <summary>Queues an action: <paramref name="start"/> runs when its turn comes, and the
        /// queue moves on once <paramref name="isDone"/> says so.</summary>
        public void PushAction(Action start, Func<bool> isDone)
        {
            items.Enqueue(new Item { Start = start, IsDone = isDone });
        }

        public void Update()
        {
            if (current != null)
            {
                if (current.IsLine)
                {
                    // TalkDialog hides itself after calling back; wait for both.
                    if (!currentDone || dialog.activeSelf)
                    {
                        return;
                    }

                    lastClosedFrame = Time.frameCount;
                }
                else if (current.IsDone != null && !current.IsDone())
                {
                    return;
                }

                current = null;
            }

            // A line is started a frame after the key press that queued it, so the dialog's
            // own any-key check does not see that press and skip the typing straight away.
            if (items.Count == 0 || WasClosedThisFrame())
            {
                return;
            }

            current = items.Dequeue();
            currentDone = false;

            if (!current.IsLine)
            {
                current.Start?.Invoke();
                return;
            }

            dialog.SetActive(true);
            TalkDialog talkDialog = dialog.GetComponent<TalkDialog>();
            talkDialog.InitConversation(mapId, current.AnimationId,
                LocalizationManager.GetConversationString(current.Conversation),
                current.Position, (result) => { currentDone = true; });
        }
    }
}
