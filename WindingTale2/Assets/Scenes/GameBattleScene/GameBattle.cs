using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.UI.Utils;

namespace WindingTale.Scenes.GameBattleScene
{

    public class GameBattle : MonoBehaviour
    {
        public GameObject subjectBody;
        public GameObject targetBody;

        private AttackResult attackResult;

        // Start is called before the first frame update
        void Start()
        {
            // Load the Battle Subject and Target

            attackResult = GlobalVariables.Get<AttackResult>("AttackResult");

            if (attackResult != null)
            {
                var subjectAniId = attackResult.Subject.Definition.AnimationId;
                // var subjectAniId = 701;

                // Load the animation
                var animator = subjectBody.GetComponent<Animator>();
                animator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                    string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(subjectAniId)));
                animator.SetInteger("actionState", 1);


                var targetAniId = attackResult.Target.Definition.AnimationId;

                // Load the animation
                animator = targetBody.GetComponent<Animator>();
                animator.runtimeAnimatorController = Resources.Load<AnimatorController>(
                    string.Format("Fights/{0}/animator_{0}", StringUtils.Digit3(targetAniId)));



            }
        }

        // Update is called once per frame
        void Update()
        {
            if (attackResult == null)
            {
                attackResult = GlobalVariables.Get<AttackResult>("AttackResult");
            }




        }
    }

}