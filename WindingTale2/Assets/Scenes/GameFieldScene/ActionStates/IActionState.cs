using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Map;

namespace WindingTale.Scenes.GameFieldScene.ActionStates
{
    /// <summary>
    /// Keyboard direction, following the on-screen layout of the default camera:
    /// Right increases tile X, Up decreases tile Y (see MapCoordinate).
    /// </summary>
    public enum InputDirection
    {
        Left,
        Up,
        Right,
        Down,
    }

    public abstract class IActionState
    {
        protected GameMain gameMain;

        protected PlayerInterface playerInterface;

        protected FDMap fdMap
        {
            get
            {
                return this.gameMain.gameMap.Map;
            }
        }


        public IActionState(GameMain game)
        {
            this.gameMain = game;
            this.playerInterface = PlayerInterface.getDefault();
        }

        public abstract void onEnter();

        public abstract void onExit();

        public abstract IActionState onSelectedPosition(FDPosition position);

        /// <summary>
        ///  Not used in current version
        /// </summary>
        /// <returns></returns>
        public abstract IActionState onUserCancelled();

        /// <summary>
        /// Whether the map cursor is hidden while this state is active. Menu states
        /// hide it (the selected menu item is the highlight instead).
        /// </summary>
        public virtual bool HidesCursor => false;

        #region Keyboard input

        /// <summary>
        /// Whether holding a direction key auto-repeats. Cursor states (idle / select
        /// target) repeat so the cursor glides while held; menu states do not.
        /// </summary>
        public virtual bool SupportsDirectionRepeat => true;

        /// <summary>
        /// A directional key (arrow) was pressed. The default behaviour (used by the
        /// idle state and every select-target state) moves the field cursor one tile,
        /// clamped to the map, with the camera following near the screen edges.
        /// Menu states override this to move the active menu item instead.
        /// </summary>
        public virtual void OnDirectionKey(InputDirection direction)
        {
            playerInterface.MoveCursorByKey(direction);
        }

        /// <summary>
        /// Confirm key (Enter / Space). Default behaviour presses the "mouse" at the
        /// current cursor tile, sharing the exact same path as a real click.
        /// </summary>
        public virtual void OnConfirmKey()
        {
            FDPosition cursor = gameMain.gameMap.GetCursorPosition();
            playerInterface.onSelectedPosition(cursor);
        }

        /// <summary>
        /// Cancel key (Backspace / ESC). Default behaviour rolls back to the previous
        /// state. The idle state overrides this to cycle the cursor across friends.
        /// </summary>
        public virtual void OnCancelKey()
        {
            playerInterface.onUserCancelled();
        }

        #endregion
    }
}