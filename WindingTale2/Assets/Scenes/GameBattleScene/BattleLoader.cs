using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Objects;
using WindingTale.FightObjects;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.GameBattleScene
{

    public class BattleLoader : MonoBehaviour
    {
        public static float ANIMATION_INTERVAL = 0.5f;
        public static float ANIMATION_END = 1.8f;

        public GameObject localBody;
        public GameObject foreignBody;

        public GameObject localHpBar;
        public GameObject localMpBar;
        public GameObject foreignHpBar;
        public GameObject foreignMpBar;

        
        // Start is called before the first frame update
        void Start()
        {

            // Load Attack Result if it exists
            AttackResult attackResult = GlobalVariables.Get<AttackResult>("AttackResult");
            if (attackResult != null )
            {
                this.AddComponent<AttackRunner>().Initialize(this, attackResult);
            }
            // Load the Magic Result if it exists
            MagicResult magicResult = GlobalVariables.Get<MagicResult>("MagicResult");
            if (magicResult != null )
            {
                this.AddComponent<MagicRunner>().Initialize(this, magicResult);
            }
        }

    }

}