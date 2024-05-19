using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using WindingTale.Core.Common;
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
            PlayerInterface playerInterface = GameObject.Find("GameRoot").GetComponent<PlayerInterface>();
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

        // Update is called once per frame
        void Update()
        {

        }


        public void onSelectedPosition(FDPosition position)
        {
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

        private void onUpdateState(IActionState nextState)
        {
            Debug.Log("PlayerInterface.onUpdateState");

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
                });

            }
        }

    }
}