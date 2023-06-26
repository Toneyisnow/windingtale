using System;
using System.Diagnostics;
using UnityEngine;
using WindingTale.Core.Map;
using WindingTale.UI.Scenes.Game;

namespace WindingTale.Core.Events
{
    public enum FDEventType
    {
        Turn = 0,
        Condition = 1,
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class FDEvent
    {
        public int EventId { get; private set; }

        public bool IsActive { get; protected set; }

        public FDEventType EventType { get; protected set; }

        public Action<GameMain> Execution { get; protected set; }

        public FDEvent(int eventId, Action<GameMain> action)
        {
            this.EventId = eventId;
            this.Execution = action;
            this.IsActive = true;
        }

        public void Execute(GameMain game)
        {
            UnityEngine.Debug.Log("Executed event: " + this.EventId);


            if (this.Execution != null) {
                this.Execution(game);
            }

            this.IsActive = false;
        }

    }
}