using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Core.Common;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.Scenes.GameFieldScene.ActionStates;

namespace WindingTale.Scenes.GameFieldScene
{

    /// <summary>
    /// Handles player input, e.g. click on tile, clicked on creature, etc.
    /// </summary>
    public class PlayerInterface : MonoBehaviour
    {
        private GameMain gameMain;

        private IActionState actionState;

        public static PlayerInterface getDefault()
        {
            PlayerInterface playerInterface = FindFirstObjectByType<PlayerInterface>(); //// GameObject.Find("GameRoot").GetComponent<PlayerInterface>();
            if (playerInterface == null)
            {
                throw new MissingComponentException("Cannot find component PlayerInterface");
            }

            return playerInterface;
        }

        // Start is called before the first frame update
        void Start()
        {
            this.gameMain = this.gameObject.GetComponent<GameMain>();
            this.actionState = new IdleState(gameMain);
        }

        // ---- Keyboard input ----

        // Auto-repeat while an arrow key is held: wait this long after the first move,
        // then move one tile every RepeatInterval seconds until released.
        private const float RepeatInitialDelay = 0.35f;
        private const float RepeatInterval = 0.08f;

        private InputDirection? heldDirection = null;
        private float repeatTimer = 0f;

        // Last friend id the cursor cycled to via Backspace / ESC in the idle state.
        private int lastCycledFriendId = 0;

        // Update is called once per frame
        void Update()
        {
            if (gameMain == null || actionState == null)
            {
                return;
            }

            if (!CanAcceptKeyboardInput())
            {
                heldDirection = null;
                return;
            }

            HandleDirectionKeys();
            HandleConfirmAndCancelKeys();
        }

        private bool CanAcceptKeyboardInput()
        {
            if (gameMain.gameCanvas != null && gameMain.gameCanvas.IsDialogOpened())
            {
                return false;
            }

            if (gameMain.activityQueue == null || !gameMain.activityQueue.IsIdle)
            {
                return false;
            }

            return true;
        }

        private void HandleDirectionKeys()
        {
            InputDirection? held = GetHeldDirection();

            if (held == null)
            {
                heldDirection = null;
                return;
            }

            if (heldDirection != held)
            {
                // A (new) direction was just pressed: move immediately, then wait the
                // initial delay before auto-repeating.
                heldDirection = held;
                repeatTimer = RepeatInitialDelay;
                actionState.OnDirectionKey(held.Value);
                return;
            }

            if (!actionState.SupportsDirectionRepeat)
            {
                return;
            }

            repeatTimer -= Time.deltaTime;
            if (repeatTimer <= 0f)
            {
                repeatTimer = RepeatInterval;
                actionState.OnDirectionKey(held.Value);
            }
        }

        private static InputDirection? GetHeldDirection()
        {
            if (Input.GetKey(KeyCode.LeftArrow)) return InputDirection.Left;
            if (Input.GetKey(KeyCode.RightArrow)) return InputDirection.Right;
            if (Input.GetKey(KeyCode.UpArrow)) return InputDirection.Up;
            if (Input.GetKey(KeyCode.DownArrow)) return InputDirection.Down;
            return null;
        }

        private void HandleConfirmAndCancelKeys()
        {
            if (Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space))
            {
                actionState.OnConfirmKey();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace)
                || Input.GetKeyDown(KeyCode.Escape))
            {
                actionState.OnCancelKey();
            }
        }

        /// <summary>
        /// Moves the field cursor one tile in the given direction, clamped to the map,
        /// and keeps the camera following the cursor near the screen edges.
        /// </summary>
        public void MoveCursorByKey(InputDirection direction)
        {
            FDMap map = gameMain.gameMap.Map;
            if (map == null || map.Field == null)
            {
                return;
            }

            FDPosition cursor = gameMain.gameMap.GetCursorPosition();
            int x = cursor.X;
            int y = cursor.Y;

            switch (direction)
            {
                case InputDirection.Left: x -= 1; break;
                case InputDirection.Right: x += 1; break;
                case InputDirection.Up: y -= 1; break;
                case InputDirection.Down: y += 1; break;
            }

            x = Mathf.Clamp(x, 1, map.Field.Width);
            y = Mathf.Clamp(y, 1, map.Field.Height);

            FDPosition next = FDPosition.At(x, y);
            if (!next.AreSame(cursor))
            {
                gameMain.gameMap.SetCursorTo(next);
            }

            gameMain.gameMap.BeginCursorEdgeFollow();
        }

        /// <summary>
        /// Slides the cursor to the next friend by ascending id (001, 002, ...),
        /// skipping ids that no longer exist and wrapping back to the first.
        /// </summary>
        public void CycleCursorToNextFriend()
        {
            FDMap map = gameMain.gameMap.Map;
            if (map == null)
            {
                return;
            }

            List<FDCreature> friends = map.Friends
                .Where(f => f.Position != null)
                .OrderBy(f => f.Id)
                .ToList();
            if (friends.Count == 0)
            {
                return;
            }

            FDCreature next = friends.FirstOrDefault(f => f.Id > lastCycledFriendId);
            if (next == null)
            {
                next = friends[0];
            }

            lastCycledFriendId = next.Id;
            gameMain.gameMap.SlideCursorTo(next.Position, GameCanvas.DialogPosition.Bottom);
        }


        public void onSelectedPosition(FDPosition position)
        {
            if (gameMain.gameCanvas.IsDialogOpened())
            {
                return;
            }

            if (!gameMain.activityQueue.IsIdle)
            {
                return;
            }

            FDPosition cursorPos = gameMain.gameMap.GetCursorPosition();
            if (!cursorPos.AreSame(position))
            {
                gameMain.gameMap.SetCursorTo(position);
                return;
            }

            IActionState nextState = actionState.onSelectedPosition(position);
            onUpdateState(nextState);
        }

        public void onUserCancelled()
        {
            IActionState nextState = actionState.onUserCancelled();
            onUpdateState(nextState);
        }

        public void onUpdateState(IActionState nextState)
        {
            Debug.LogFormat("PlayerInterface.onUpdateState. [{0}] => [{1}]", actionState.GetType(), nextState.GetType());

            if (nextState != actionState)
            {
                // Do onExit immediately
                gameMain.InsertActivity(gameMain =>
                {
                    actionState.onExit();
                });

                // Push the next state and call onEnter
                gameMain.PushActivity(gameMain =>
                {
                    nextState.onEnter();
                    actionState = nextState;

                    // The cursor is hidden while a menu is open, shown otherwise.
                    gameMain.gameMap.SetCursorVisible(!nextState.HidesCursor);
                });

            }
        }

    }
}