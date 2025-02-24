
using System.Collections;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.MapObjects.GameMap;

namespace WindingTale.MapObjects.Menus
{
    public class MenuSlidingEffect : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public static float duration = 0.15f; // 滑动持续时间（秒）

        private bool destroyObject  = false;

        void Start()
        {
           
        }

        public void Init(FDPosition position1, FDPosition position2, bool destroyObject = false)
        {
            pointA = MapCoordinate.ConvertPosToVec3(position1);
            pointB = MapCoordinate.ConvertPosToVec3(position2);
            this.destroyObject = destroyObject;

            StartCoroutine(SlideObject());
        }

        private IEnumerator SlideObject()
        {
            float elapsedTime = 0;
            while (elapsedTime < duration)
            {
                transform.SetLocalPositionAndRotation(Vector3.Lerp(pointA, pointB, elapsedTime / duration), Quaternion.Euler(-90, 0, 0));

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.SetLocalPositionAndRotation(pointB, Quaternion.Euler(-90, 0, 0));

            if (destroyObject)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(this);
            }
        }
    }
}