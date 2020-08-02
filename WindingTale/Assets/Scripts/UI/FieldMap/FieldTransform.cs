using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Common;

namespace WindingTale.UI.FieldMap
{
    public class FieldTransform
    {
        public static Vector3 GetPixelPosition(FDPosition position)
        {
            return new Vector3(position.X * 2.4f, 0, - position.Y * 2.4f);
        }

        public static Vector3 GetPixelPosition(int x, int y)
        {
            return new Vector3(x * 2.4f, 0, - y * 2.4f);
        }

        public static GameObject CreateShapeObject(GameObject shapePrefab, int x, int y)
        {
            GameObject go = GameObject.Instantiate(shapePrefab);
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            go.transform.localPosition = GetPixelPosition(x, y);
            go.transform.localRotation = new Quaternion(0f, 1.57f, 1.57f, 0f);

            return go;
        }
    }
}
