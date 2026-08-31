using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using WindingTale.Core.Map;

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

        public Action Execution { get; protected set; }

        /// <summary>
        /// Events this one waits on. It stays dormant -- matching or not -- until every one
        /// of them has gone inactive, i.e. has fired or been deactivated by a chapter.
        ///
        /// This is the original's "setEvent:dependentOn:". Chapter 4 is what needs it: the
        /// beasts that wander in are spawned standing on the very tiles their escape events
        /// watch, so those events would fire the instant the beasts arrive. Hanging them off
        /// the beasts' death events keeps them dormant until one of the pack falls and the
        /// rest turn to run.
        ///
        /// Deliberately expressed through IsActive rather than a flag of its own, so a
        /// battle reloaded from a save resolves the gate the same way: GameMapRecord stores
        /// exactly which events are no longer active.
        /// </summary>
        private readonly List<FDEvent> dependentEvents = new List<FDEvent>();

        public FDEvent(int eventId, Action action)
        {
            this.EventId = eventId;
            this.Execution = action;
            this.IsActive = true;
        }

        /// <summary>
        /// Holds this event back until <paramref name="dependency"/> is done with.
        /// </summary>
        public void AddDependentEvent(FDEvent dependency)
        {
            if (dependency == null || dependency == this)
            {
                return;
            }

            this.dependentEvents.Add(dependency);
        }

        /// <summary>
        /// Whether everything this event was told to wait for has happened. True for the
        /// events that were given nothing to wait for, which is nearly all of them.
        /// </summary>
        public bool IsDependencySatisfied
        {
            get
            {
                return this.dependentEvents.TrueForAll(dependency => !dependency.IsActive);
            }
        }

        public void Execute()
        {
            UnityEngine.Debug.Log("Executed event: " + this.EventId);


            if (this.Execution != null) {
                this.Execution();
            }

            this.IsActive = false;
        }

        public void SetActive(bool isActive)
        {
            this.IsActive = isActive;
        }

    }
}