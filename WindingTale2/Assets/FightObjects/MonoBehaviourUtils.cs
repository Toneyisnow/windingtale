using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.FightObjects
{
    public class MonoBehaviourUtils : MonoBehaviour
    {
        public static void ExecuteWithDelay(MonoBehaviour mono, float delay, System.Action action)
        {
            mono.StartCoroutine(ExecuteWithDelayCoroutine(delay, action));
        }


        private static IEnumerator ExecuteWithDelayCoroutine(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }
    }
}