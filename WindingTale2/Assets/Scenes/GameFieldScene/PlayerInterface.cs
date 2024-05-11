using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Scenes.GameFieldScene.ActionStates;

namespace WindingTale.Scenes.GameFieldScene
{

    /// <summary>
    /// Handles player input, e.g. click on tile, clicked on creature, etc.
    /// </summary>
    public class PlayerInterface : MonoBehaviour
    {
        public GameMain gameMain;

        private IActionState actionState;

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
    }
}